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
using System.IO;
using System.Text;
using System.Net;
using System.Reflection;

namespace XMPMS.Net.Packets
{
    /// <summary>
    /// Collects data ready to be sent as a packet to a remote host
    /// </summary>
    public class OutboundPacket : Packet
    {
        /// <summary>
        /// Data to send
        /// </summary>
        private List<byte> packetData = new List<byte>();

        /// <summary>
        /// Length of this packet
        /// </summary>
        public int Length
        {
            get { return packetData.Count - 4; }
        }

        /// <summary>
        /// Length of this packet including header
        /// </summary>
        public int FullLength
        {
            get { return packetData.Count; }
        }

        /// <summary>
        /// Type of packet (TCP or UDP)
        /// </summary>
        public OutboundPacketType Type
        {
            get;
            private set;
        }

        /// <summary>
        /// Create a new outbound packet
        /// </summary>
        /// <param name="udp">True if this is a UDP packet</param>
        public OutboundPacket(bool udp)
        {
            if (udp)
            {
                Append(Protocol.UDP_PROTOCOL_VERSION);
                Type = OutboundPacketType.UDP;
            }
            else
            {
                packetData.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                Type = OutboundPacketType.TCP;
            }
        }

        /// <summary>
        /// Create a new outbound packet
        /// </summary>
        public OutboundPacket()
            : this(false)
        { }

        /// <summary>
        /// Create a new outbound packet containing the specified string
        /// </summary>
        /// <param name="response"></param>
        public OutboundPacket(string response)
            : this(false)
        {
            Append(response);
        }

        /// <summary>
        /// Create a new outbound packet containing the specified bytes
        /// </summary>
        /// <param name="response"></param>
        public OutboundPacket(params byte[] response)
            : this(false)
        {
            Append(response);
        }

        /// <summary>
        /// Create a new outbound packet containing the specified integer
        /// </summary>
        /// <param name="response"></param>
        public OutboundPacket(int response)
            : this(false)
        {
            Append(response);
        }

        /// <summary>
        /// Append the specified string to the packet 
        /// </summary>
        /// <param name="s">String to append, </param>
        /// <param name="maxLength"></param>
        public OutboundPacket Append(string s, int maxLength)
        {
            if (s.Length > maxLength) s = s.Substring(0, maxLength);
            Append(s);

            return this;
        }

        /// <summary>
        /// Append the specified string to the packet
        /// </summary>
        /// <param name="s"></param>
        public OutboundPacket Append(string s)
        {
            // Only add non-empty strings to the packet
            if (s.Length > 0)
            {
                AppendCompactIndex(s.Length + 1);
                packetData.AddRange(Encoding.ASCII.GetBytes(s));
            }

            packetData.Add(0x00);

            return this;
        }

        /// <summary>
        /// Append the specified string array to the packet
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public OutboundPacket Append(string[] s)
        {
            Append((byte)s.Length);

            foreach (string item in s)
                Append(s);

            return this;
        }

        /// <summary>
        /// Append the specified unsigned short int to the packet
        /// </summary>
        /// <param name="u"></param>
        public OutboundPacket Append(ushort u)
        {
            packetData.Add((byte)(u & 255));
            packetData.Add((byte)(u >> 8));

            return this;
        }

        /// <summary>
        /// Append a single byte to the packet
        /// </summary>
        /// <param name="b"></param>
        public OutboundPacket Append(byte b)
        {
            packetData.Add(b);

            return this;
        }

        /// <summary>
        /// Append multiple bytes to the packet
        /// </summary>
        /// <param name="b"></param>
        public OutboundPacket Append(params byte[] b)
        {
            packetData.AddRange(b);

            return this;
        }

        /// <summary>
        /// Append the specified 32-bit (4 byte) signed integer to the packet
        /// </summary>
        /// <param name="i"></param>
        public OutboundPacket Append(int i)
        {
            packetData.AddRange(BitConverter.GetBytes(i));

            return this;
        }

        /// <summary>
        /// Append the supplied integer array to the packet
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public OutboundPacket Append(int[] i)
        {
            Append((byte)i.Length);

            foreach (int item in i)
                Append(i);

            return this;
        }

        /// <summary>
        /// Append the specified 32-bit (4 byte) signed integer to the packet as an unreal-style compact index
        /// </summary>
        /// <param name="i"></param>
        public OutboundPacket AppendCompactIndex(int intValue)
        {
            int absValue = Math.Abs(intValue);
            byte[] bytes = new byte[] { (byte)(absValue & 0x3F), (byte)((absValue >> 6)  & 0x7F), (byte)((absValue >> 13) & 0x7F), (byte)((absValue >> 20) & 0x7F), (byte)((absValue >> 27) & 0x1F) };

            if (bytes[4] > 0) bytes[3] |= 0x80;
            if (bytes[3] > 0) bytes[2] |= 0x80;
            if (bytes[2] > 0) bytes[1] |= 0x80;
            if (bytes[1] > 0) bytes[0] |= 0x40;
            if (intValue < 0) bytes[0] |= 0x80;

            Append(bytes[0]);
            if (bytes[1] > 0) Append(bytes[1]);
            if (bytes[2] > 0) Append(bytes[2]);
            if (bytes[3] > 0) Append(bytes[3]);
            if (bytes[4] > 0) Append(bytes[4]);

            return this;
        }

        /// <summary>
        /// Append the specified IP address to the packet as a 4 byte sequence
        /// </summary>
        /// <param name="ip"></param>
        public OutboundPacket Append(IPAddress ip)
        {
            byte[] addr = ip.GetAddressBytes();
            Append(addr[0], addr[1], addr[2], addr[3]);

            return this;
        }

        /// <summary>
        /// Append an array of structs to the packet
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public OutboundPacket AppendStruct<T>(T[] data)
            where T : struct
        {
            AppendCompactIndex(data.Length);

            foreach (T item in data)
                AppendStruct<T>(item);

            return this;
        }

        /// <summary>
        /// Appends a struct to the packet, note that only appendable types will be appended
        /// </summary>
        /// <typeparam name="T">Struct type to append</typeparam>
        /// <param name="data">Struct to append</param>
        /// <returns>Fluent interface</returns>
        public OutboundPacket AppendStruct<T>(T data)
            where T : struct
        {
            // Enumerate the fields of the struct and append relevant data to the packet
            foreach (FieldInfo field in typeof(T).GetFields())
                AppendDynamic(field.FieldType, field.GetValue(data));

            return this;
        }

        /// <summary>
        /// Append a variable type to the packet
        /// </summary>
        /// <param name="fieldType">Type of data to append</param>
        /// <param name="fieldValue"></param>
        private void AppendDynamic(Type fieldType, object fieldValue)
        {
            // For enums, do a dynamic append based on the underlying type
            if (fieldType.BaseType == typeof(Enum))
            {
                AppendDynamic(Enum.GetUnderlyingType(fieldType), fieldValue);
                return;
            }

            if (fieldType == typeof(int))       { Append((int)fieldValue);       return; }
            if (fieldType == typeof(byte))      { Append((byte)fieldValue);      return; }
            if (fieldType == typeof(ushort))    { Append((ushort)fieldValue);    return; }
            if (fieldType == typeof(string))    { Append(fieldValue.ToString()); return; }
            if (fieldType == typeof(IPAddress)) { Append((IPAddress)fieldValue); return; }

            // throw new InvalidDataException("AppendDynamic failed because an non-serializable field was found in the struct");
        }

        /// <summary>
        /// Prepare the packet for transmission and return the packet data as a byte array
        /// </summary>
        /// <returns>Prepared packet data ready for transmission</returns>
        public virtual byte[] Prepare()
        {
            byte[] length = BitConverter.GetBytes(packetData.Count - 4);

            packetData[0] = length[0];
            packetData[1] = length[1];
            packetData[2] = length[2];
            packetData[3] = length[3];

            return packetData.ToArray();
        }

        /// <summary>
        /// This operator allows the packet to be passed directly to the Socket.Send() method
        /// </summary>
        /// <param name="rp"></param>
        /// <returns></returns>
        public static implicit operator byte[](OutboundPacket rp)
        {
            return rp.Prepare();
        }

        /// <summary>
        /// Returns the contents of this packet in printable format for debugging purposes
        /// </summary>
        public override string Print()
        {
            if (packetData.Count < 5) return "<NULL>";

            string result = "";

            for (int i = 4; i < packetData.Count; i++)
            {
                if (packetData[i] > 31 && packetData[i] < 127)
                    result += (char)packetData[i];
                else
                {
                    result += String.Format("<0x{0:x2}>", packetData[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the contents of this packet in printable hexadecimal format for debugging purposes
        /// </summary>
        public override string PrintBytes()
        {
            List<string> result = new List<string>();

            for (int i = 4; i < packetData.Count; i++)
                result.Add(String.Format("{0:x2}", packetData[i]));

            return String.Format("[{0}]", String.Join(" ", result.ToArray()));
        }
    }
}
