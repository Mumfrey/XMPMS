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
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Reflection;
using XMPMS.Core;
using XMPMS.Util;
using XMPMS.Interfaces;
using XMPMS.Net.Packets;
using XMPMS.Validation;

namespace XMPMS.Net.Connections
{
    /// <summary>
    /// Handles the first stage of an inbound connection from a player or server, before the type
    /// of connection is identified. Once the connecting player has been identified, it will be
    /// handled by a ClientConnection or ServerConnection instance in the same thread.
    /// </summary>
    public class Connection : TCPConnection
    {
        /// <summary>
        /// Connection object which handles the more specific type of inbound connection
        /// </summary>
        protected Connection innerConnection = null;

        /// <summary>
        /// Reference to the parent connection if this is a derived connection handler class
        /// </summary>
        protected Connection outerConnection = null;

        /// <summary>
        /// Connection log writer
        /// </summary>
        protected IConnectionLogWriter logWriter;

        /// <summary>
        /// Header with socket ip/port to prepend to all log lines
        /// </summary>
        public string SocketID
        {
            get;
            protected set;
        }

        /// <summary>
        /// CD Key validation module
        /// </summary>
        protected ICDKeyValidator cdKeyValidator;

        /// <summary>
        /// CD Key validation context
        /// </summary>
        private ValidationContext validationContext;

        /// <summary>
        /// Game Stats module
        /// </summary>
        private IGameStatsLog gameStats;

        /// <summary>
        /// Server list to pass to player and server connection handlers
        /// </summary>
        protected ServerList serverList;

        /// <summary>
        /// GeoIP resolver to use for resolving server addresses to locations
        /// </summary>
        protected GeoIP geoIP;

        /// <summary>
        /// MD5 database to use for updating Server MD5 data
        /// </summary>
        protected MD5Manager md5Manager;

        /// <summary>
        /// Remote host's detected operating system
        /// </summary>
        protected OperatingSystem operatingSystem = OperatingSystem.UnknownOS;
        
        /// <summary>
        /// Remote host's locale
        /// </summary>
        protected string locale = "int";

        /// <summary>
        /// Remote host type, expect SERVER or CLIENT
        /// </summary>
        protected string type = String.Empty;

        /// <summary>
        /// Get the remote host type, expect SERVER or CLIENT
        /// </summary>
        public string Type
        {
            get { return type; }
        }

        /// <summary>
        /// Version number received from the remote host
        /// </summary>
        protected int version = 0;

        /// <summary>
        /// Get the version number reported by the remote host
        /// </summary>
        public int Version
        {
            get { return version; }
        }

        /// <summary>
        /// CD key hash received from the remote host
        /// </summary>
        protected string cdKey = String.Empty;

        /// <summary>
        /// Salted CD key hash received from the remote host
        /// </summary>
        protected string saltedCDKey = String.Empty;

        /// <summary>
        /// Get the CD key hash received from the remote host
        /// </summary>
        public string CDKeyHash
        {
            get { return cdKey; }
        }

        /// <summary>
        /// Check whether the MSList should be sent to the remote client
        /// </summary>
        protected virtual bool MSListEnabled
        {
            get
            {
                // Check whether MSLIST is enabled and configured in the settings
                if (!MasterServer.Settings.MSListEnabled || MasterServer.Settings.MSListServers == null || MasterServer.Settings.MSListPorts == null) return false;
                
                // Check whether the MSLIST function is enabled on this interface
                if (MasterServer.Settings.MSListInterfaces == null || MasterServer.Settings.MSListInterfaces.Count == 0 || MasterServer.Settings.MSListInterfaces.Contains((ushort)LocalPort)) return true;
                
                // Return false if no match (normal connection)
                return false;
            }
        }

        /// <summary>
        /// Create a new connection instance to handle an inbound connection
        /// </summary>
        /// <param name="socket">TCP socket for communicating with the remote server</param>
        /// <param name="connectionLogWriter">Log writer module</param>
        /// <param name="serverList">Server List object</param>
        /// <param name="geoIP">GeoIP resolver</param>
        /// <param name="md5Manager">MD5 database manager</param>
        /// <param name="banManager">IP ban manager</param>
        /// <param name="cdKeyValidator">CD key validator</param>
        /// <param name="gameStats">Game stats module</param>
        public Connection(Socket socket, IConnectionLogWriter logWriter, ServerList serverList, GeoIP geoIP, MD5Manager md5Manager, IPBanManager banManager, ICDKeyValidator cdKeyValidator, IGameStatsLog gameStats)
            : this(socket, logWriter, serverList, geoIP)
        {
            // Raise the NewConnection event for connected packet analysers
            OnNewConnection();

            // Check whether the remote host is banned
            if (banManager.IsBanned((socket.RemoteEndPoint as IPEndPoint).Address))
            {
                ConnectionLog("BANNED");
                socket.Close();
                return;
            }

            this.md5Manager     = md5Manager;
            this.cdKeyValidator = cdKeyValidator;
            this.gameStats      = gameStats;

            // Handle this connection in a new thread
            ConnectionThreadManager.CreateStart(new ThreadStart(Handle));
        }

        /// <summary>
        /// Protected constructor, common functionality for this class and subclasses
        /// </summary>
        /// <param name="socket">TCP socket for communicating with the remote server</param>
        /// <param name="connectionLogWriter">Log writer module</param>
        /// <param name="serverList">Server List object</param>
        /// <param name="geoIP">GeoIP resolver</param>
        protected Connection(Socket socket, IConnectionLogWriter logWriter, ServerList serverList, GeoIP geoIP)
        {
            this.socket     = socket;
            this.logWriter  = logWriter;
            this.serverList = serverList;
            this.geoIP      = geoIP;

            // Socket ID for logging
            SocketID = String.Format("{0}:{1}", (socket.RemoteEndPoint as IPEndPoint).Address, (socket.RemoteEndPoint as IPEndPoint).Port);
        }

        /// <summary>
        /// Handle the connection
        /// </summary>
        protected virtual void Handle()
        {
            try
            {
                // Initialise validation context for this session
                validationContext = cdKeyValidator.BeginValidation("login");

                // Log the new connection to the connection log
                ConnectionLog("ACCEPT LOCALPORT={0} SALT={1}", LocalPort, cdKeyValidator.GetSalt(validationContext));

                // Send the challenge salt
                Send(cdKeyValidator.GetSalt(validationContext).ToString());

                // Read back the authentication from the player
                InboundPacket login = Receive();

                cdKey       = login.PopString();  // Get the first MD5 which should be the CD key hash
                saltedCDKey = login.PopString();  // Get the second MD5 which should be the CD key plus salt hash
                type        = login.PopString();  // Type of client eg. CLIENT or SERVER    
                version     = login.PopInt();     // Client's engine version

                // Write the login info to the connection log
                ConnectionLog("CONNECT MD5={0} SALTED={1} TYPE={2} VERSION={3}", cdKey, saltedCDKey, type, version);

                // Set values into the validation context
                validationContext.SetClientInfo(cdKey, saltedCDKey, type, version);

                // Check the CD key
                if (Validate(validationContext))
                {
                    if (version < Protocol.MIN_SUPPORTED_CLIENT_VERSION)
                    {
                        ConnectionLog(Protocol.LOGIN_RESPONSE_UPGRADE);
                        MasterServer.Log("{0} at {1} rejected, outdated version: got {2}", type, (socket.RemoteEndPoint as IPEndPoint).Address.ToString(), version);

                        // This is my best guess for how an UPGRADE packet should be structured, if it's wrong it seems to crash the client
                        OutboundPacket UpgradePacket = new OutboundPacket(Protocol.LOGIN_RESPONSE_UPGRADE);
                        UpgradePacket.Append(Protocol.MIN_SUPPORTED_CLIENT_VERSION);
                        UpgradePacket.Append(0x00);

                        // Send the UPGRADE response
                        Send(UpgradePacket);
                    }
                    else
                    {
                        // Send MSLIST packet if enabled, if the MSLIST is sent successfully then close the
                        // connection (SendMSList() returns true if the MSLIST was sent)
                        if (!SendMSList())
                        {
                            switch (type)
                            {
                                case Protocol.HOST_CLIENT:  HandleClientConnection(login);  break;
                                case Protocol.HOST_SERVER:  HandleServerConnection(login);  break;
                                default:                    HandleUnknownConnection(login); break;
                            }
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                aborted = true;
            }
            catch (Exception ex)
            {
                ConnectionLog("EXCEPTION: {0}", ex.Message);
            }

            try
            {
                socket.Close();
            }
            catch { }

            ConnectionLog("CLOSED");

            ConnectionThreadManager.Remove(Thread.CurrentThread);
            ConnectionManager.DeRegister(this);
        }

        /// <summary>
        /// Perform validation with the supplied validation context
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected virtual bool Validate(ValidationContext validationContext)
        {
            bool validationResult = true;

            if (!cdKeyValidator.ValidateKey(validationContext))
            {
                ConnectionLog("{0}: STAGE 1 INVALID CD KEY", Protocol.LOGIN_RESPONSE_DENIED);
                MasterServer.Log("Client at {0} failed first stage authentication. Invalid CD key", (socket.RemoteEndPoint as IPEndPoint).Address.ToString());
                Send(Protocol.LOGIN_RESPONSE_DENIED);

                validationResult = false;
            }
            else if (!cdKeyValidator.ValidateSaltedKey(validationContext))
            {
                ConnectionLog("{0}: STAGE 2 INVALID CD KEY", Protocol.LOGIN_RESPONSE_DENIED);
                MasterServer.Log("Client at {0} failed second stage authentication. Salted key was rejected.", (socket.RemoteEndPoint as IPEndPoint).Address.ToString());
                Send(Protocol.LOGIN_RESPONSE_DENIED);

                validationResult = false;
            }

            // Release any resources allocated to performing this validation operation
            cdKeyValidator.EndValidation(validationContext);

            return validationResult;
        }

        /// <summary>
        /// Handles a connection from a CLIENT
        /// </summary>
        /// <param name="login">Login response packet</param>
        protected virtual void HandleClientConnection(InboundPacket login)
        {
            ConnectionLog(Protocol.LOGIN_RESPONSE_APPROVED);
            Send(Protocol.LOGIN_RESPONSE_APPROVED);

            byte osByte = login.PopByte();              // Host's detected operating system
            locale      = login.PopString();            // Host's locale, eg. int, est

            // Map the OS value to the relevant enum value if it's valid
            operatingSystem = Enum.IsDefined(typeof(OperatingSystem), osByte) ? (OperatingSystem)osByte : OperatingSystem.UnknownOS;

            ConnectionLog("{0} OS={1} LOCALE={2}", type, operatingSystem, locale);

            innerConnection = new ClientConnection(this, socket, logWriter, serverList, geoIP, operatingSystem, locale);
            innerConnection.Handle();                   // Handle the connection in this thread
        }

        /// <summary>
        /// Handles a connection from a SERVER
        /// </summary>
        /// <param name="login">Login response packet</param>
        protected virtual void HandleServerConnection(InboundPacket login)
        {
            ConnectionLog(Protocol.LOGIN_RESPONSE_APPROVED);
            Send(Protocol.LOGIN_RESPONSE_APPROVED);

            bool bStatLogging = (login.PopInt() == 0);  // Seems to be -1 if disabled, 0 if enabled
            byte osByte       = login.PopByte();        // Host's detected operating system
            locale            = login.PopString();      // Host's locale, eg. int
                     
            // Map the OS value to the relevant enum value if it's valid
            operatingSystem = Enum.IsDefined(typeof(OperatingSystem), osByte) ? (OperatingSystem)osByte : OperatingSystem.UnknownOS;

            ConnectionLog("{0} BSTATLOGGING={1} OS={2} LOCALE={3}", type, bStatLogging, operatingSystem, locale);

            innerConnection = new ServerConnection(this, socket, logWriter, serverList, geoIP, operatingSystem, locale, bStatLogging, md5Manager, cdKeyValidator, gameStats);
            innerConnection.Handle();                   // Handle the connection in this thread
        }

        /// <summary>
        /// Handles a connection from an unrecognised host type
        /// </summary>
        /// <param name="login">Login response packet</param>
        protected virtual void HandleUnknownConnection(InboundPacket login)
        {
            ConnectionLog("UNRECOGNISED LOGIN TYPE={0}", type);
            MasterServer.Log("Unrecognised LOGIN from {0}. Expecting {1} or {2}, got: '{3}'", (socket.RemoteEndPoint as IPEndPoint).Address.ToString(), Protocol.HOST_SERVER, Protocol.HOST_CLIENT, type);
        }

        /// <summary>
        /// Send the updated MS list to the remote client
        /// </summary>
        /// <returns>True if the packet was sent ok</returns>
        protected virtual bool SendMSList()
        {
            // Check whether we should send the MSLIST message on this port
            if (MSListEnabled)
            {
                // Calculate the number of entries we need to send
                int msListEntryCount = Math.Min(Math.Min(MasterServer.Settings.MSListServers.Length, MasterServer.Settings.MSListPorts.Length), MasterServer.Settings.MSListMaxServers);

                // Only send the list if there are entries to send
                if (msListEntryCount > 0)
                {
                    ConnectionLog("{0} COUNT={1}", Protocol.LOGIN_RESPONSE_MSLIST, msListEntryCount);

                    // Create MSLIST packet
                    OutboundPacket msListPacket = new OutboundPacket(Protocol.LOGIN_RESPONSE_MSLIST);

                    // Append server addresses
                    msListPacket.Append((byte)msListEntryCount);
                    for (int entry = 0; entry < msListEntryCount; entry++)
                        msListPacket.Append(MasterServer.Settings.MSListServers[entry]);

                    // Append server ports
                    msListPacket.Append((byte)msListEntryCount);
                    for (int entry = 0; entry < msListEntryCount; entry++)
                        msListPacket.Append((int)MasterServer.Settings.MSListPorts[entry]);

                    // Send packet to the remote host
                    Send(msListPacket);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try to gracefully abort/close the connection by closing the socket
        /// </summary>
        public override void Abort()
        {
            if (!aborted)
            {
                aborted = true;
                ConnectionLog("SOFTWARE ABORT");
            }

            if (innerConnection != null)
            {
                innerConnection.Abort();
            }

            base.Abort();
        }

        /// <summary>
        /// Write a line to the connection log
        /// </summary>
        /// <param name="format">Format string</param>
        /// <param name="args">Parameters</param>
        protected virtual void ConnectionLog(string format, params object[] args)
        {
            if (logWriter != null)
            {
                logWriter.Write(String.Format("<{0}> {1}", SocketID, String.Format(format, args)), this);
            }
        }
    }
}
