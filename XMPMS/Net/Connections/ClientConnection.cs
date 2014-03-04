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
using XMPMS.Core;
using XMPMS.Util;
using XMPMS.Util.UScript;
using XMPMS.Net.Packets;
using XMPMS.Interfaces;

namespace XMPMS.Net.Connections
{
    /// <summary>
    /// Handles a connection from a player (game)
    /// </summary>
    public class ClientConnection : Connection
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parentConnection">Parent connection which owns this player connection</param>
        /// <param name="socket">TCP communication socket</param>
        /// <param name="connectionLogWriter">Log writer module</param>
        /// <param name="serverList">Server list object</param>
        /// <param name="geoIP">GeoIP resolver</param>
        /// <param name="locale">Locale reported by remote host</param>
        public ClientConnection(Connection parentConnection, Socket socket, IConnectionLogWriter logWriter, ServerList serverList, GeoIP geoIP, OperatingSystem operatingSystem, string locale)
            : base(socket, logWriter, serverList, geoIP)
        {
            this.outerConnection = parentConnection;
            this.operatingSystem = operatingSystem;
            this.locale          = locale;

            socket.ReceiveTimeout = 300000;

            //MasterServer.Log("Accepting player connection from {0}", socket.RemoteEndPoint.ToString());
        }

        /// <summary>
        /// Handles the player connection, the connection is closed once the request is completed
        /// </summary>
        protected override void Handle()
        {
            try
            {
                InboundPacket clientRequest = Receive();
                ClientToMaster clientRequestType = (ClientToMaster)clientRequest.PopByte();

                ConnectionLog("REQUEST TYPE={0}", clientRequestType);

                switch (clientRequestType)
                {
                    case ClientToMaster.Query:          HandleQuery(clientRequest);             break;
                    case ClientToMaster.GetMOTD:        HandleMOTDRequest(clientRequest);       break;
                    case ClientToMaster.QueryUpgrade:   HandleQueryUpgrade(clientRequest);      break;
                    default:                            HandleUnknownRequest(clientRequest);    break;
                }
            }
            catch (ThreadAbortException)
            {
                aborted = true;
            }
            catch (Exception) // ex)
            {
                //if (!aborted)
                //    MasterServer.Log("Client connection error: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Client has queried the server list
        /// </summary>
        /// <param name="clientRequest"></param>
        private void HandleQuery(InboundPacket clientRequest)
        {
            // Query arrives as an array of QueryData structs
            QueryData[] queries = clientRequest.PopStructArray<QueryData>();

            // Write query to log and notify master server for stats
            LogQuery(queries);

            // Get a filtered list of servers based on the queries which were receieved
            List<Server> servers = serverList.Query(queries);

            // Server count is the first reply following the query and tells the player how many servers to expect
            OutboundPacket serverCountPacket = new OutboundPacket();
            serverCountPacket.Append(servers.Count);
            serverCountPacket.Append((byte)0x01);
            Send(serverCountPacket);

            // Send server list if any were found
            if (servers.Count > 0)
            {
                foreach (Server server in servers)
                {
                    OutboundPacket serverListPacket = new OutboundPacket();
                    serverListPacket.AppendStruct<ServerListEntry>(server.ListEntry);
                    Send(serverListPacket);
                }
            }
        }

        /// <summary>
        /// Log a query's data to the connection log
        /// </summary>
        /// <param name="queries"></param>
        private void LogQuery(QueryData[] queries)
        {
            // LogWriter this query with the master server for stats purposes
            MasterServer.RegisterQuery();

            // Build list of query items in their string representation
            string[] queryItems = new string[queries.Length];
            for (int i = 0; i < queries.Length; i++)
                queryItems[i] = queries[i].ToString();

            ConnectionLog("EXECUTING QUERY {0}", String.Join(" ", queryItems));
        }

        /// <summary>
        /// Client has requested the MOTD
        /// </summary>
        /// <param name="clientRequest"></param>
        private void HandleMOTDRequest(InboundPacket clientRequest)
        {
            ConnectionLog("SENDING MOTD");

            // Response packet contains the MOTD string
            OutboundPacket MOTD = new OutboundPacket(MasterServer.GetMOTD(locale, true));

            // Send the MR_OptionalUpgrade value if this connection is valid but player is an outdated version
            if (outerConnection.Version < Protocol.OPTIONALUPGRADE_VERSION)
            {
                MOTD.Append(Protocol.OPTIONALUPGRADE_VERSION);
            }

            // Send the MOTD packet
            Send(MOTD);
        }

        /// <summary>
        /// Client has issued an upgrade query. I have no idea of the format of this packet.
        /// </summary>
        /// <param name="clientRequest"></param>
        private void HandleQueryUpgrade(InboundPacket clientRequest)
        {
            //MasterServer.Log("Client at {0} sent CTM_QueryUpgrade", socket.RemoteEndPoint.ToString());
        }

        /// <summary>
        /// Client has made an unrecognised request
        /// </summary>
        /// <param name="clientRequest"></param>
        private void HandleUnknownRequest(InboundPacket clientRequest)
        {
            clientRequest.Rewind();

            ConnectionLog("UNKNOWN REQUEST CODE={0}", clientRequest.PopByte());
            //MasterServer.Log("Client at {0} sent unrecognised query ", socket.RemoteEndPoint.ToString());
        }

        /// <summary>
        /// Connection aborted by master server
        /// </summary>
        public override void Abort()
        {
            aborted = true;    
        }
    }
}
