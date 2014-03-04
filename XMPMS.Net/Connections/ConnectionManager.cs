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

namespace XMPMS.Net.Connections
{
    /// <summary>
    /// Contains references to all active connections so they can be terminated all at once when shutting 
    /// down the master server. Also prevents stray connections in case a listener thread crashes or a
    /// terminating listener fails to clean up all of its child connections.
    /// </summary>
    public static class ConnectionManager
    {
        /// <summary>
        /// Lock for accessing the connection list
        /// </summary>
        private static object connectionListLock = new object();

        /// <summary>
        /// List of active connections
        /// </summary>
        private static List<TCPConnection> connections = new List<TCPConnection>();

        /// <summary>
        /// Registers a new connection
        /// </summary>
        /// <param name="connection"></param>
        public static void Register(TCPConnection connection)
        {
            lock (connectionListLock)
                connections.Add(connection);
        }

        /// <summary>
        /// Deregisters a connection
        /// </summary>
        /// <param name="connection"></param>
        public static void DeRegister(TCPConnection connection)
        {
            lock (connectionListLock)
                connections.Remove(connection);
        }

        /// <summary>
        /// Abort all active connections with matching listen ports
        /// </summary>
        /// <param name="listenPort">Port to abort connection</param>
        public static void AbortAll(int listenPort)
        {
            lock (connectionListLock)
            {
                foreach (TCPConnection connection in connections)
                {
                    try
                    {
                        if (connection.LocalPort == listenPort)
                            connection.Abort();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Abort all active connections. Call this function only when shutting down.
        /// </summary>
        public static void AbortAll()
        {
            Console.WriteLine("Closing all active connections, please wait...");

            lock (connectionListLock)
            {
                foreach (TCPConnection connection in connections)
                {
                    try
                    {
                        connection.Abort();
                    }
                    catch { }
                }
            }

            // Terminate all connection handler threads
            ConnectionThreadManager.TerminateAll();
        }
    }
}
