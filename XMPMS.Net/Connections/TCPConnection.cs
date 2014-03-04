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
    /// Base class for objects which communicate via TCP, provides input buffering and packet wrapping functions
    /// </summary>
    public abstract class TCPConnection
    {
        /// <summary>
        /// Raised when a new connection is created, primarily used by the packet analyser libs
        /// </summary>
        public static event ConnectionEventHandler NewConnection;

        /// <summary>
        /// Raised when a new packet is receieved, primarily used by the packet analyser libs
        /// </summary>
        public event ReceivedPacketEventHandler ReceivedPacket;

        /// <summary>
        /// Socket for this connection
        /// </summary>
        protected Socket socket;

        /// <summary>
        /// Data not yet returned
        /// </summary>
        private byte[] pendingData = new byte[0];

        /// <summary>
        /// True if this thread was forcibly aborted and we should terminate
        /// </summary>
        protected volatile bool aborted = false;

        /// <summary>
        /// Get the port of the local endpoint
        /// </summary>
        public int LocalPort
        {
            get
            {
                return (socket != null && socket.LocalEndPoint != null) ? (socket.LocalEndPoint as IPEndPoint).Port : 0;
            }
        }

        /// <summary>
        /// Try to gracefully abort/close the connection by closing the socket
        /// </summary>
        public virtual void Abort()
        {
            aborted = true;

            if (socket != null)
            {
                try
                {
                    socket.Close();
                }
                catch { }
            }
        }

        /// <summary>
        /// Send the specified text string to the socket
        /// </summary>
        /// <param name="text">Text to send</param>
        protected virtual void Send(string text)
        {
            Send(new OutboundPacket(text));
        }

        /// <summary>
        /// Send the specified byte to the socket
        /// </summary>
        /// <param name="b">Byte to send</param>
        protected virtual void Send(byte b)
        {
            Send(new OutboundPacket(b));
        }

        /// <summary>
        /// Send the specified outbound packet to the socket
        /// </summary>
        /// <param name="packet"></param>
        protected virtual void Send(OutboundPacket packet)
        {
            if (packet != null && packet.Type == OutboundPacketType.TCP && packet.Length > 0 && socket != null && socket.Connected)
            {
                socket.Send(packet);
            }
        }

        /// <summary>
        /// Receive data from the socket and wrap them in an InboundPacket
        /// </summary>
        /// <returns>InboundPacket containing the received data</returns>
        protected virtual InboundPacket Receive()
        {
            byte[] packetData = ReceieveData();

            InboundPacket packet = (packetData.Length > 4) ? new InboundPacket(packetData) : new EmptyInboundPacket();
            OnReceivedPacket(packet);
            return packet;
        }

        /// <summary>
        /// Read a packet from the socket and return the raw data as a byte array
        /// </summary>
        /// <returns></returns>
        protected byte[] ReceieveData()
        {
            byte[] buffer;
            int count = 0;

            // If no data pending, block and receive data from the socket
            if (pendingData.Length == 0)
            {
                buffer = new byte[65536];
                count = socket.Receive(buffer, SocketFlags.None);
            }
            else
            {
                buffer = pendingData;
                count = pendingData.Length;
                pendingData = new byte[0];
            }

            if (count == 0)     // Remote connection closed
            {
                socket.Close();
                return new byte[0];
            }
            else if (count < 4) // Packet is too short (can't have a valid header)
            {
                return new byte[0];
            }
            else                // Normal packet data, split and return
            {
                int packetLength = BitConverter.ToInt32(buffer, 0) + 4;

                // If the packet is longer than the header indicates, add the remaining data to the pending buffer
                if (packetLength < count)
                {
                    int pendingLength = count - packetLength;
                    pendingData = new byte[pendingLength];
                    Buffer.BlockCopy(buffer, packetLength, pendingData, 0, pendingLength);
                }

                // Return data for the first packet
                byte[] packetData = new byte[packetLength];
                Buffer.BlockCopy(buffer, 0, packetData, 0, packetLength);
                return packetData;
            }
        }

        /// <summary>
        /// Raise the NewConnection event
        /// </summary>
        protected void OnNewConnection()
        {
            ConnectionEventHandler newConnection = TCPConnection.NewConnection;

            if (newConnection != null)
            {
                newConnection(this);
            }
        }

        /// <summary>
        /// Raise the ReceivedPacket event
        /// </summary>
        /// <param name="packet">Packet that was received</param>
        public void OnReceivedPacket(InboundPacket packet)
        {
            ReceivedPacketEventHandler receivedPacket = this.ReceivedPacket;

            if (receivedPacket != null)
            {
                receivedPacket(this, packet);
            }
        }
    }
}
