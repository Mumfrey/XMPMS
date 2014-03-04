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
using System.Threading;
using System.Net;
using System.Net.Sockets;
using XMPMS.Core;
using XMPMS.Util;
using XMPMS.Interfaces;
using XMPMS.Net.Connections;

namespace XMPMS.Net.Listeners
{
    /// <summary>
    /// QueryListener listens for and allocates connections to incoming queries
    /// </summary>
    public class QueryListener
    {
        /// <summary>
        /// Endpoint for this listener
        /// </summary>
        private IPEndPoint endpoint;

        /// <summary>
        /// Get the endpoint for this listener
        /// </summary>
        public IPEndPoint Endpoint
        {
            get { return endpoint; }
        }

        /// <summary>
        /// TCP socket for accepting player and server connections
        /// </summary>
        private Socket listenSocket;

        /// <summary>
        /// Thread for listening for new connections
        /// </summary>
        private Thread listenThread;

        /// <summary>
        /// Flag to indicate whether listening is enabled
        /// </summary>
        private volatile bool aborted = false;

        /// <summary>
        /// Reference to the server list
        /// </summary>
        private ServerList serverList;

        /// <summary>
        /// Reference to the GeoIP resolver
        /// </summary>
        private GeoIP geoIP;

        /// <summary>
        /// Reference to the MD5 manager
        /// </summary>
        private MD5Manager md5Manager;

        /// <summary>
        /// Reference to the ban list manager
        /// </summary>
        private IPBanManager banManager;

        /// <summary>
        /// Validator to use for CD keys on this connection
        /// </summary>
        private ICDKeyValidator cdKeyValidator;

        /// <summary>
        /// Game stats log
        /// </summary>
        private IGameStatsLog gameStats;

        /// <summary>
        /// Connection log writer
        /// </summary>
        private IConnectionLogWriter logWriter;

        /// <summary>
        /// Constructor, create a new query listener at the specified endpoint
        /// </summary>
        /// <param name="endpoint">Endpoint for this listener</param>
        /// <param name="serverList">Server list object to pass to new connections</param>
        /// <param name="geoIP">GeoIP resolver to pass to new connections</param>
        /// <param name="md5Manager">MD5 manager to pass to new connections</param>
        /// <param name="banManager">IP ban manager to pass to new connections</param>
        /// <param name="cdKeyValidator">CD key validator module to pass to new connections</param>
        /// <param name="gameStats">Game stats module to pass to new connections</param>
        public QueryListener(IPEndPoint endpoint, ServerList serverList, GeoIP geoIP, MD5Manager md5Manager, IPBanManager banManager, ICDKeyValidator cdKeyValidator, IGameStatsLog gameStats)
        {
            // Endpoint to listen
            this.endpoint       = endpoint;

            // These objects are passed to new connections
            this.serverList     = serverList;
            this.geoIP          = geoIP;
            this.md5Manager     = md5Manager;
            this.banManager     = banManager;
            this.cdKeyValidator = cdKeyValidator;
            this.gameStats      = gameStats;

            // Get configured connection log writer module
            this.logWriter = ModuleManager.GetModule<IConnectionLogWriter>();

            // Bind the listen socket ready to begin listening
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            listenSocket.Bind(this.Endpoint);

            // Create and start the listen thread
            listenThread = new Thread(ListenThreadProc);
            listenThread.Start();
        }

        /// <summary>
        /// Thread function which listens for incoming connections and assigns them to Connection objects
        /// </summary>
        private void ListenThreadProc()
        {
            // Open the listening socket
            listenSocket.Listen(1);

            try
            {
                while (!aborted)
                {
                    // Each new inbound connection is handled in a new thread by a new Connection object
                    Connection newConnection = new Connection(listenSocket.Accept(), logWriter, serverList, geoIP, md5Manager, banManager, cdKeyValidator, gameStats);
                    ConnectionManager.Register(newConnection);
                }
            }
            catch { }
        }

        /// <summary>
        /// Shut down this listener and all child threads
        /// </summary>
        public void Shutdown()
        {
            aborted = true;

            this.serverList     = null;
            this.geoIP          = null;
            this.cdKeyValidator = null;
            this.gameStats      = null;
            this.md5Manager     = null;
            this.banManager     = null;

            if (listenThread != null)
            {
                // Try to close the listen socket
                if (listenSocket != null)
                {
                    try
                    {
                        listenSocket.Close();
                    }
                    catch { }
                }

                // Try to forcibly abort the listen thread
                listenThread.Abort();
                listenThread.Join();
                listenThread = null;

                MasterServer.Log("[NET] Query listener socket {0} shut down.", endpoint.Port);
            }

            // Abort active connections
            ConnectionManager.AbortAll(endpoint.Port);

            // Release log writer
            ModuleManager.ReleaseModule<IConnectionLogWriter>();
        }
    }
}
