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
using XMPMS.Net.Packets;

namespace XMPMS.Net.Connections
{
    /// <summary>
    /// Base class for obejcts which communicate via UDP
    /// </summary>
    public abstract class UDPConnection
    {
        /// <summary>
        /// Remote endpoint to send packets to
        /// </summary>
        protected IPEndPoint remoteEndpoint;

        /// <summary>
        /// Send a packet to a remote host
        /// </summary>
        /// <param name="udp">UDP client to use</param>
        /// <param name="packet">Packet to send</param>
        protected virtual void Send(UdpClient udp, OutboundPacket packet)
        {
            if (packet != null && packet.Type == OutboundPacketType.UDP && packet.Length > 0 && udp != null && remoteEndpoint != null)
            {
                udp.Send(packet, packet.FullLength, remoteEndpoint);
            }
            else
                throw new ArgumentException("Cannot send a null, empty, or TCP packet on a UDP socket");
        }

        /// <summary>
        /// Blocking function to receive a packet on the currently bound interface
        /// </summary>
        /// <param name="udp">UDP client to use</param>
        /// <returns>Received data wrapped in a UDPPacket object</returns>
        protected virtual UDPPacket Receive(UdpClient udp)
        {
            IPEndPoint remoteEP = null;
            byte[] packetData = udp.Receive(ref remoteEP);
            return new UDPPacket(packetData, remoteEP);
        }
    }
}
