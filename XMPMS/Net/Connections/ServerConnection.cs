// ======================================================================
//  Unreal2 XMP Master Server
//  Copyright (C) 2010-2011  Adam Mummery-Smith
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

//  Copyright Notice:
//  Unreal and the Unreal logo are registered trademarks of Epic
//  Games, Inc. ALL RIGHTS RESERVED.
// ======================================================================

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using XMPMS.Core;
using XMPMS.Util;
using XMPMS.Util.UScript;
using XMPMS.Net.Packets;
using XMPMS.Net.Packets.Specialised;
using XMPMS.Net.Helpers;
using XMPMS.Interfaces;
using XMPMS.Validation;

namespace XMPMS.Net.Connections
{
    /// <summary>
    /// Handles a connection to a server, server connections are persistent and are only
    /// recreated if the server loses connectivity or when the server changes map
    /// </summary>
    public class ServerConnection : Connection
    {
        /// <summary>
        /// Each ServerConnection relates to a server object which is registered with the server list
        /// </summary>
        protected Server server = null;

        /// <summary>
        /// Thread lock to prevent concurrent threads trying to dispatch the matchid
        /// </summary>
        protected object matchIDlock = new object();

        /// <summary>
        /// True if the matchid has been sent to the server
        /// </summary>
        protected bool sentMatchID = false;

        /// <summary>
        /// Game stats log object
        /// </summary>
        protected IGameStatsLog gameStats = null;

        /// <summary>
        /// Connection State to a remote server
        /// </summary>
        public enum ConnectionState
        {
            /// <summary>This connection is waiting for the initial greeting from the server</summary>
            WaitingHello,
            
            /// <summary>This connection is waiting for the heartbeat from the remote server</summary>
            WaitingHeartbeat,

            /// <summary>Initial handshaking complete, this connection is now established</summary>
            Established
        }

        /// <summary>
        /// When the connection is first opened we have to send the heartbeat requests, until this is
        /// done the server is in a special mode and does not accept the normal commands. Once the heartbeat
        /// requests have been sent we set this flag to true to indicate that it is now safe to use the
        /// connection as normal and treat inbound packets as normal messages.
        /// </summary>
        private ConnectionState state = ConnectionState.WaitingHello;

        /// <summary>
        /// Get or set the current connection State
        /// </summary>
        public ConnectionState State
        {
            get
            {
                return state;
            }

            protected set
            {
                if (value == state)
                {
                    return;
                }
                else if ((int)value > (int)state)
                {
                    state = value;
                    ConnectionLog("ENTERING STATE {0}", state.ToString().ToUpper());
                }
                else
                {
                    // State can only move "forwards", so setting to a previous state throws an exception since
                    // it likely means that stuff is happening out of order or our internal state is invalid

                    throw new InvalidOperationException("Invalid connection state change entering state " + value);
                }
            }
        }

        /// <summary>
        /// The time index at which we started waiting for a heartbeat.
        /// </summary>
        protected DateTime waitingHeartbeatTime;

        /// <summary>
        /// Time to wait for a heartbeat response
        /// </summary>
        protected int waitForHeartbeatSeconds = 60;

        /// <summary>
        /// List of heartbeat signals we are waiting for
        /// </summary>
        protected List<HeartbeatType> waitingHearbeats = new List<HeartbeatType>();

        /// <summary>
        /// Response time to heartbeat request
        /// </summary>
        protected double ping = 0;

        /// <summary>
        /// Socket timeout
        /// </summary>
        protected int socketTimeoutSeconds = 300;

        /// <summary>
        /// Validation contexts for player challenges
        /// </summary>
        protected Dictionary<string, ValidationContext> validationContexts = new Dictionary<string, ValidationContext>();

        /// <summary>
        /// Number of unparsed STM_GameState messages we have recieved. LogWriter a warning after every 10 failures.
        /// </summary>
        protected int gameStateParseFailureCount = 0;

        /// <summary>
        /// Raised when a CheckOptionReply is received
        /// </summary>
        public event CheckOptionReplyEventHandler CheckOptionReply;

        /// <summary>
        /// Constructor, called by the Connection class when it has established that this connection is to a
        /// server and has performed the initial authentication handshake. At this point the remote server
        /// should still be in the waiting for heartbeat State.
        /// </summary>
        /// <param name="serverList"></param>
        /// <param name="socket"></param>
        /// <param name="banFile"></param>
        public ServerConnection(Connection parentConnection, Socket socket, IConnectionLogWriter logWriter, ServerList serverList, GeoIP geoip, OperatingSystem operatingSystem, string locale, bool bStatLogging, MD5Manager md5Manager, ICDKeyValidator cdKeyValidator, IGameStatsLog gameStats)
            : base(socket, logWriter, serverList, geoip)
        {
            this.outerConnection = parentConnection;
            this.operatingSystem = operatingSystem;
            this.locale          = locale;
            this.md5Manager      = md5Manager;
            this.cdKeyValidator  = cdKeyValidator;
            this.gameStats       = gameStats;

            // Socket ID for logging
            SocketID = String.Format("{0}:{1}", (socket.RemoteEndPoint as IPEndPoint).Address, (socket.RemoteEndPoint as IPEndPoint).Port);

            // Set socket timeout
            socket.ReceiveTimeout = socketTimeoutSeconds * 1000;

            MasterServer.Log("[{0}] Accepting new server connection. Version={1}", (socket.RemoteEndPoint as IPEndPoint).Address.ToString(), parentConnection.Version);

            int matchID = serverList.GetMatchID(null);

            // Create the server object and register it with the server list
            server = new Server(this, parentConnection.Version, (socket.RemoteEndPoint as IPEndPoint).Address, parentConnection.CDKeyHash, geoip, serverList.GetHeartbeatCode(), bStatLogging, operatingSystem, locale, matchID);
            serverList.Add(server);
        }

        /// <summary>
        /// Handle the server connection
        /// </summary>
        protected override void Handle()
        {
            // Loop until connection is closed or forcibly aborted
            while (socket.Connected && !aborted)
            {
                try
                {
                    // Read data from the socket
                    InboundPacket packet = Receive();

                    if (!packet.Empty && packet.Valid)
                    {
                        ServerInfoPacket serverInfo = (ServerInfoPacket)packet;

                        // Connection is considered "established" once the heartbeat has been established
                        switch (State)
                        {
                            // Waiting for initial greeting from the server
                            case ConnectionState.WaitingHello:
                                HandleHello(serverInfo);
                                break;

                            // Waiting to receive heartbeat from the server
                            case ConnectionState.WaitingHeartbeat:
                                if ((DateTime.Now - waitingHeartbeatTime).TotalSeconds > waitForHeartbeatSeconds)
                                {
                                    ConnectionLog("TIMEOUT WAITING HEARTBEAT IN STATE {0}", State);
                                    MasterServer.Log("[{0}] Timeout waiting for heartbeat response.");
                                    outerConnection.Abort();
                                }
                                else
                                {
                                    ConnectionLog("UNSOLICITED MESSAGE IN STATE {0}", State);
                                }
                                break;

                            // Connection is established, process inbound packets as normal server conversation
                            case ConnectionState.Established:
                                switch (serverInfo.PacketCode)
                                {
                                    case ServerToMaster.ClientResponse:         HandleClientChallengeResponse(serverInfo);  break;
                                    case ServerToMaster.GameState:              HandleGameState(serverInfo);                break;
                                    case ServerToMaster.Stats:                  HandleStats(serverInfo);                    break;
                                    case ServerToMaster.ClientDisconnectFailed: HandleClientDisconnectFailed(serverInfo);   break; 
                                    case ServerToMaster.MD5Version:             HandleMD5Version(serverInfo);               break;
                                    case ServerToMaster.CheckOptionReply:       HandleCheckOptionReply(serverInfo);         break;
                                    default:
                                        packet.Rewind();
                                        ConnectionLog("INVALID MESSAGE STATE={0} CODE={1}", State, packet.PopByte());
                                        break;
                                }

                                break;
                        }
                    }
                    else if (socket.Connected)
                    {
                        ConnectionLog("INVALID PACKET STATE={0} DATA={1}", State, packet.PrintBytes(true));
                        Debug.WriteLine(String.Format("Invalid packet from server at {0} in state {1}", server.Address, State));
                        Debug.WriteLine(packet.Print());

                        if (State == ConnectionState.WaitingHeartbeat && (DateTime.Now - waitingHeartbeatTime).TotalSeconds > waitForHeartbeatSeconds)
                        {
                            MasterServer.Log("[{0}] Timeout waiting for heartbeat response.", server);
                            ConnectionLog("TIMEOUT WAITING HEARTBEAT IN STATE {0}", State);
                            outerConnection.Abort();
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    aborted = true;
                }
                catch (Exception ex)
                {
                    if (!aborted)
                        ConnectionLog("ERROR: {0}", ex.Message);

                    break;
                }
            }

            if (!socket.Connected)
                MasterServer.Log("[{0}] Connection closed", server);

            serverList.Remove(server);
            server = null;
        }

        /// <summary>
        /// Abort the connection, usually the abort is handled in the parent connection
        /// </summary>
        public override void Abort()
        {
            aborted = true;
        }

        #region Message Handlers

        /// <summary>
        /// Handle a server greeting "HELLO"
        /// </summary>
        /// <param name="serverInfo"></param>
        protected virtual void HandleHello(ServerInfoPacket serverInfo)
        {
            if (serverInfo.ProcessHello())
            {
                ConnectionLog("HELLO SERVERBEHINDNAT={0} UPLINKTOGAMESPY={1}", serverInfo.ServerBehindNAT, serverInfo.UplinkToGamespy);
                MasterServer.Log("[{0}] sent greeting. ServerBehindNAT={1}", server.Address, serverInfo.ServerBehindNAT);

                server.HandleHello(serverInfo);

                SendHeartbeatRequest(HeartbeatType.QueryInterface, server.HeartbeatCode);
                SendHeartbeatRequest(HeartbeatType.GamePort, server.HeartbeatCode);

                if (server.UplinkToGamespy)
                {
                    SendHeartbeatRequest(HeartbeatType.GamespyQueryPort, server.HeartbeatCode);
                }

                waitingHeartbeatTime = DateTime.Now;
                State = ConnectionState.WaitingHeartbeat;
            }
            else
            {
                ConnectionLog("HELLO INVALID");
                MasterServer.Log("[{0}] Invalid greeting received, closing connection", server.Address);
                Abort();
            }
        }

        /// <summary>
        /// Handle a gamestate packet from a server
        /// </summary>
        /// <param name="serverInfo"></param>
        protected virtual void HandleGameState(ServerInfoPacket serverInfo)
        {
            // MasterServer.Log("[{0}] STM_GameState", server);
            if (serverInfo.ProcessGameState(server, logWriter))
            {
                ConnectionLog("STM_GAMESTATE OK");
                serverList.UpdateServer(server, serverInfo);
                SendMatchID();
            }
            else
            {
                ConnectionLog("STM_GAMESTATE BAD: {0}", serverInfo.Print());
                gameStateParseFailureCount++;
            }

            if (gameStateParseFailureCount > 5)
            {
                MasterServer.Log("[{0}] WARN: Invalid STM_GameState", server);
                gameStateParseFailureCount = 0;
            }
        }

        /// <summary>
        /// Handle an MD5 version packet
        /// </summary>
        /// <param name="serverInfo"></param>
        protected virtual void HandleMD5Version(ServerInfoPacket serverInfo)
        {
            if (serverInfo.ProcessMD5Version())
            {
                ConnectionLog("STM_MD5VERSION VERSION={0}", serverInfo.MD5Version);
                MasterServer.Log("[{0}] STM_MD5Version with: {1}", server, serverInfo.MD5Version);

                List<MD5Entry> updates = md5Manager.Get(serverInfo.MD5Version);

                if (updates.Count > 0)
                    SendMD5Updates(updates);
            }
            else
            {
                ConnectionLog("STM_MD5VERSION BAD: {0}", serverInfo.Print());
            }
        }

        /// <summary>
        /// Handle stats information from a server
        /// </summary>
        /// <param name="serverInfo"></param>
        protected virtual void HandleStats(ServerInfoPacket serverInfo)
        {
            ConnectionLog("STM_STATS BTSTATLOGGING={0}", server.EnableStatLogging);

            if (server.EnableStatLogging && serverInfo.ProcessGameStats() && gameStats != null)
            {
                gameStats.Log(DateTime.Now, server, serverInfo.StatLine);
            }
        }

        #endregion

        #region Response Handlers

        /// <summary>
        /// Handle a player challenge response
        /// </summary>
        /// <param name="serverInfo"></param>
        protected virtual void HandleClientChallengeResponse(ServerInfoPacket serverInfo)
        {
            MasterServer.Log("[{0}] Received STM_ClientResponse", server);

            if (serverInfo.ProcessClientResponse())
            {
                ConnectionLog("STM_CLIENTRESPONSE MD5={0} MD5={1}", serverInfo.ClientCDKey, serverInfo.ClientCDKeySalted);

                MasterServer.Log("{0} MD5={1}", serverInfo.ClientIP, serverInfo.ClientCDKey);
                MasterServer.Log("{0} MD5={1}", serverInfo.ClientIP, serverInfo.ClientCDKeySalted);

                if (validationContexts.ContainsKey(serverInfo.ClientIP))
                {
                    ValidationContext clientValidationContext = validationContexts[serverInfo.ClientIP];
                    validationContexts.Remove(serverInfo.ClientIP);

                    clientValidationContext.SetClientInfo(serverInfo.ClientCDKey, serverInfo.ClientCDKeySalted, outerConnection.Type, outerConnection.Version);

                    // Check player CD key is valid?
                    if (!cdKeyValidator.ValidateKey(clientValidationContext))
                    {
                        MasterServer.Log("Client {0} CD key invalid. Disconnecting client.", serverInfo.ClientIP);
                        DisconnectClient(serverInfo.ClientIP);
                    }
                    else if (!cdKeyValidator.ValidateSaltedKey(clientValidationContext))
                    {
                        MasterServer.Log("Client {0} failed challenge. Disconnecting client.", serverInfo.ClientIP);
                        DisconnectClient(serverInfo.ClientIP);
                    }
                    else
                    {
                        MasterServer.Log("Client {0} challenge succeeded.", serverInfo.ClientIP);
                    }

                    cdKeyValidator.EndValidation(clientValidationContext);
                }
                else
                {
                    MasterServer.Log("Client {0} challenge unverified. No matching context found!");
                }
            }
            else
            {
                ConnectionLog("STM_CLIENTRESPONSE BAD: {0}", serverInfo.Print());
            }
        }

        /// <summary>
        /// Handle a player disconnect failed response
        /// </summary>
        /// <param name="serverInfo"></param>
        protected virtual void HandleClientDisconnectFailed(ServerInfoPacket serverInfo)
        {
            ConnectionLog("STM_CLIENTDISCONNECTFAILED");

            if (serverInfo.ProcessClientDisconnectFailed())
            {
                MasterServer.Log("[{0}] Disconnect client {1} failed", server, serverInfo.ClientIP);
            }
        }

        /// <summary>
        /// Handle a CheckOption reply packet. Displays the value on screen
        /// </summary>
        /// <param name="serverInfo"></param>
        protected virtual void HandleCheckOptionReply(ServerInfoPacket serverInfo)
        {
            if (serverInfo.ProcessCheckOptionResponse())
            {
                ConnectionLog("STM_CHECKOPTIONREPLY PKG={0} VAR={1} VALUE=\"{2}\"", serverInfo.OptionPackageName, serverInfo.OptionVariableName, serverInfo.OptionVariableValue);

                MasterServer.LogMessage("[{0}] {1} {2}=\"{3}\"", server, serverInfo.OptionPackageName, serverInfo.OptionVariableName, serverInfo.OptionVariableValue);

                CheckOptionReplyEventHandler checkOptionReply = this.CheckOptionReply;

                if (checkOptionReply != null)
                    checkOptionReply(serverInfo.OptionPackageName, serverInfo.OptionVariableName, serverInfo.OptionVariableValue);
            }
            else
            {
                ConnectionLog("STM_CHECKOPTIONREPLY BAD: {0}", serverInfo.Print());
            }
        }

        /// <summary>
        /// Callback from the Server object when a heartbeat is received
        /// </summary>
        /// <param name="heartbeatType">Type of heartbeat which was received</param>
        /// <param name="code">Heartbeat code, we shouldn't need to check this since the
        /// ServerList shouldn't call the function if the code doesn't match</param>
        /// <param name="port">Port from which the heartbeat was received</param>
        public virtual void HandleHeartBeat(HeartbeatType heartbeatType, int code, int port)
        {
            ConnectionLog("HEARTBEAT TYPE={0} CODE={1} PORT={2} WAITING={3}", heartbeatType, code, port, waitingHearbeats.Count);

            if (State == ConnectionState.WaitingHeartbeat)
            {
                waitingHearbeats.Remove(heartbeatType);

                if (ping == 0)
                {
                    ping = (DateTime.Now - waitingHeartbeatTime).TotalMilliseconds;
                }

                if (waitingHearbeats.Count == 0)
                {
                    // To start with I thought the first number in the heartbeat reply was the ping to the
                    // master server, but it seems to always be 120 so I'm  not really sure what it actually
                    // means. Different values don't seem to make any difference.   
                    SendHeartbeatAcknowledgment(Protocol.HEARTBEAT_COMPLETE_UNKNOWN);
                    //SendHeartbeatAcknowledgment((int)ping);

                    State = ConnectionState.Established;

                    // Begin the server query process
                    server.HeartbeatResponseComplete();

                    // Assign the match ID
                    SendMatchID();
                }
            }
        }

        /// <summary>
        /// Callback from the Server object when a query response was received
        /// </summary>
        /// <param name="queryType">Type of query that was received</param>
        public virtual void HandleQueryResponse(UDPServerQueryClient.UDPServerQueryType queryType)
        {
            State = ConnectionState.Established;
        }

        #endregion

        #region Master to Server

        /// <summary>
        /// Master server requests heartbeat blah with code blah
        /// </summary>
        /// <param name="type">Type of heartbeat to request</param>
        /// <param name="code">Heartbeat code to request</param>
        protected virtual void SendHeartbeatRequest(HeartbeatType type, int code)
        {
            if (State == ConnectionState.WaitingHello)
            {
                OutboundPacket HeartbeatPacket = new OutboundPacket();

                HeartbeatPacket.Append(Protocol.HEARTBEAT_CMD);
                HeartbeatPacket.Append((byte)type);
                HeartbeatPacket.Append(code);

                waitingHearbeats.Add(type);

                Send(HeartbeatPacket);
            }
        }

        /// <summary>
        /// Receieved all heartbeats okay, send acknowledgment to server
        /// </summary>
        /// <param name="unknown">Not sure what this value is for, seems to always be 120</param>
        protected virtual void SendHeartbeatAcknowledgment(int unknown)
        {
            OutboundPacket HeartbeatExitPacket = new OutboundPacket(Protocol.HEARTBEAT_RESPONSE_CMD);
            HeartbeatExitPacket.Append(unknown);    // Always 120 ?
            HeartbeatExitPacket.Append(server.QueryPort);
            HeartbeatExitPacket.Append(server.Port);
            HeartbeatExitPacket.Append(server.GamespyQueryPort);
            Send(HeartbeatExitPacket);
        }

        /// <summary>
        /// Updates the server's match ID if it hasn't been sent yet
        /// </summary>
        public virtual void SendMatchID()
        {
            lock (matchIDlock)
            {
                if (!sentMatchID)
                {
                    sentMatchID = true;
                    MasterServer.Log("[{0}] got match ID {1}", server, server.MatchID);
                    SendMatchID(server.MatchID);
                }
            }
        }

        /// <summary>
        /// Set the remote server's match ID
        /// </summary>
        /// <param name="MatchID">Match ID to set on the remote server</param>
        public virtual void SendMatchID(int MatchID)
        {
            ConnectionLog("ASSIGNING MATCH ID {0}", MatchID);

            OutboundPacket MatchIDPacket = new OutboundPacket((byte)MasterToServer.MatchID);
            MatchIDPacket.Append(MatchID);
            Send(MatchIDPacket);
        }

        /// <summary>
        /// Set INI file option on the remote server
        /// </summary>
        /// <param name="packageName">Name of package and group</param>
        /// <param name="variableName">Variable name</param>
        /// <param name="value">Value to set</param>
        public virtual void SetOption(string packageName, string variableName, string value)
        {
            OutboundPacket SetOptionPacket = new OutboundPacket((byte)MasterToServer.UpdateOption);
            SetOptionPacket.Append(packageName);
            SetOptionPacket.Append(variableName);
            SetOptionPacket.Append(value);
            Send(SetOptionPacket);
        }

        /// <summary>
        /// Check INI option on the remote server, remote server will reply with STM_CheckOptionReply
        /// </summary>
        /// <param name="packageName">Name of package and group</param>
        /// <param name="variableName">Variable name</param>
        public virtual void CheckOption(string packageName, string variableName)
        {
            OutboundPacket CheckOptionPacket = new OutboundPacket((byte)MasterToServer.CheckOption);
            CheckOptionPacket.Append(packageName);
            CheckOptionPacket.Append(variableName);
            Send(CheckOptionPacket);
        }

        /// <summary>
        /// Challenge the specified player to return its CD key and the salted CD key
        /// </summary>
        /// <param name="clientAddress">Address of the player to challenge, in IP:port notation</param>
        public virtual void ChallengeClient(string clientAddress)
        {
            if (clientAddress != "" && clientAddress != "local")
            {
                ConnectionLog("SENDING MTS_CLIENTCHALLENGE CLIENT={0}", clientAddress);
                MasterServer.Log("[{0}] Sending MTS_ClientChallenge to {1}", server, clientAddress);

                ValidationContext clientValidationContext = cdKeyValidator.BeginValidation(clientAddress);
                validationContexts[clientAddress] = clientValidationContext;

                OutboundPacket ChallengeClientPacket = new OutboundPacket((byte)MasterToServer.ClientChallenge);
                ChallengeClientPacket.Append(clientAddress);
                ChallengeClientPacket.Append(clientValidationContext.Salt.ToString());
                Send(ChallengeClientPacket);
            }
            else
            {
                MasterServer.Log("Unable to send MTS_ClientChallenge. Cannot challenge local player");
            }
        }

        /// <summary>
        /// Disconnect the specified player (eg. because of failed challenge or invalid CD key)
        /// </summary>
        /// <param name="clientAddress">Address of the player to disconnect, in IP:port notation</param>
        public virtual void DisconnectClient(string clientAddress)
        {
            ConnectionLog("SENDING MTS_CLIENTAUTHFAILED CLIENT={0}", clientAddress);
            MasterServer.Log("[{0}] Sending MTS_ClientAuthFailed to {1}", server, clientAddress);

            OutboundPacket DisconnectClientPacket = new OutboundPacket((byte)MasterToServer.ClientAuthFailed);
            DisconnectClientPacket.Append(clientAddress);
            Send(DisconnectClientPacket);
        }

        /// <summary>
        /// Send updated MD5 data to the server
        /// </summary>
        /// <param name="updates">List of MD5 data to send</param>
        public virtual void SendMD5Updates(List<MD5Entry> updates)
        {
            MasterServer.Log("[{0}] Updating MD5 database to revision {1}", server, md5Manager.maxRevision);

            int index = 0;

            while (index < updates.Count)
            {
                int updatesInPacket = Math.Min(updates.Count - index, Protocol.MAX_MD5_UPDATES_PER_PACKET);

                OutboundPacket MD5UpdatePacket = new OutboundPacket((byte)MasterToServer.MD5Update);

                MD5UpdatePacket.Append((byte)updatesInPacket);

                for (int offset = 0; offset < updatesInPacket; offset++)
                {
                    MD5UpdatePacket.Append(updates[index + offset].PackageGUID);
                    MD5UpdatePacket.Append(updates[index + offset].PackageMD5);
                    MD5UpdatePacket.Append(updates[index + offset].Revision);
                }

                ConnectionLog("SENDING MTS_MD5UPDATE COUNT={0}", updatesInPacket);
                Send(MD5UpdatePacket);

                index += updatesInPacket;
            }
        }

        /// <summary>
        /// Shut down the connection (sends shutdown packet)
        /// </summary>
        public virtual void Shutdown()
        {
            ConnectionLog("SENDING MTS_SHUTDOWN");

            // Tell remote server to close the connection
            Send((byte)MasterToServer.Shutdown);

            // Forcibly close the socket
            outerConnection.Abort();
        }

        /// <summary>
        /// Poke raw data down the connection, used for testing purposes
        /// </summary>
        /// <param name="command"></param>
        public void Poke(string[] command)
        {
            OutboundPacket ob = new OutboundPacket();
            byte b = 0x00;

            for (int i = 1; i < command.Length; i++)
            {
                if (byte.TryParse(command[i], out b))
                    ob.Append(b);
                else
                    ob.Append(command[i]);
            }

            ConnectionLog("POKE {0}", ob.Print());

            Send(ob);
        }
        #endregion

        /// <summary>
        /// Overridden so that we can wrap the received data in a ServerInfoPacket instead of an InboundPacket
        /// </summary>
        /// <returns></returns>
        protected override InboundPacket Receive()
        {
            byte[] packetData = ReceieveData();

            InboundPacket packet = (packetData.Length > 4) ? (InboundPacket)new ServerInfoPacket(packetData) : new EmptyInboundPacket();
            outerConnection.OnReceivedPacket(packet);
            return packet;
        }
    }
}
