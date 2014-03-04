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
using XMPMS.Util.UScript;
using XMPMS.Net.Connections;
using XMPMS.Net.Packets;
using XMPMS.Core;

namespace XMPMS.Net.Listeners
{
    /// <summary>
    /// HeartbeatListener listens for inbound UDP heartbeat packets
    /// </summary>
    public class HeartbeatListener : UDPConnection
    {
        /// <summary>
        /// Raised when a heartbeat is received 
        /// </summary>
        public event ReceivedHeartbeatHandler ReceivedHeartbeat;

        /// <summary>
        /// Thread where we do the listening
        /// </summary>
        private Thread listenThread;

        /// <summary>
        /// Flag to indicate when the object is shutting down
        /// </summary>
        private volatile bool aborted = false;

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
        /// UDP player object which will handle communication
        /// </summary>
        protected UdpClient udpClient;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="endpoint">Endpoint to bind this listener to</param>
        public HeartbeatListener(IPEndPoint endpoint)
        {
            this.endpoint = endpoint;
            udpClient = new UdpClient(Endpoint.Port, AddressFamily.InterNetwork);

            listenThread = new Thread(ListenThreadProc);
            listenThread.Start();
        }

        /// <summary>
        /// Thread function for listening for inbound heartbeats
        /// </summary>
        private void ListenThreadProc()
        {
            while (!aborted)
            {
                try
                {
                    UDPPacket packet = Receive(udpClient);

                    if (packet.Valid)
                    {
                        if (packet.Version == Protocol.UDP_PROTOCOL_VERSION)
                        {
                            HeartbeatType heartbeatType = (HeartbeatType)packet.PopByte();
                            int heartbeatCode = packet.PopInt();

                            // Raise the heartbeat received event
                            OnReceivedHeartbeat(heartbeatType, heartbeatCode, packet.RemoteEndpoint.Port);
                        }
                        else
                        {
                            // MasterServer.Log("[NET] Packet with bad version on heartbeat port: Version={0}", packet.Version);
                            System.Diagnostics.Debug.WriteLine(String.Format("[NET] Packet with bad version on heartbeat port: Version={0}", packet.Version));
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[NET] Unknown UDP data on heartbeat port");
                    }
                }
                catch (ThreadAbortException)
                {
                    aborted = true;
                }
                catch (Exception)
                {
                    //MasterServer.Log("Heartbeat listener caught exception: {0}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Raise the heartbeat received event
        /// </summary>
        /// <param name="heartbeatType">Type of heartbeat that was received</param>
        /// <param name="heartbeatCode">Heartbeat code</param>
        /// <param name="port">Remote port the heartbeat was received from</param>
        private void OnReceivedHeartbeat(HeartbeatType heartbeatType, int heartbeatCode, int port)
        {
            ReceivedHeartbeatHandler receivedHeartbeat = this.ReceivedHeartbeat;

            if (receivedHeartbeat != null)
            {
                receivedHeartbeat(heartbeatType, heartbeatCode, port);
            }
        }

        /// <summary>
        /// Close the open sockets and terminate the listen thread
        /// </summary>
        public void Shutdown()
        {
            aborted = true;

            if (udpClient != null)
            {
                try
                {
                    udpClient.Close();
                }
                catch { }
            }

            if (listenThread != null)
            {
                listenThread.Abort();
                listenThread.Join();
                listenThread = null;

                MasterServer.Log("[NET] Heartbeat listener socket {0} shut down.", endpoint.Port);
            }
        }
    }
}
