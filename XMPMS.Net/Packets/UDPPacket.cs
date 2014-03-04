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

namespace XMPMS.Net.Packets
{
    /// <summary>
    /// Encapsulates an inbound UDP packet
    /// </summary>
    public class UDPPacket : InboundPacket
    {
        /// <summary>
        /// Get the version header from this packet
        /// </summary>
        public int Version
        {
            get { return udpPacketVersion; }
        }

        /// <summary>
        /// Check the packet version matches the expected UDP packet version
        /// </summary>
        public override bool Valid
        {
            get { return Length > 4; }
        }

        /// <summary>
        /// True if the pointer is at or beyond the end of the packet
        /// </summary>
        public virtual bool EOF
        {
            get { return Pointer >= data.Length; }
        }

        /// <summary>
        /// Originating endpoint of the packet
        /// </summary>
        public IPEndPoint RemoteEndpoint
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data">Packet data directly from the socket</param>
        /// <param name="endpoint"></param>
        public UDPPacket(byte[] data, IPEndPoint endpoint)
            : base(data, true)
        {
            RemoteEndpoint = endpoint;
        }

        /// <summary>
        /// Pops a key/value array from the packet until the end of the packet is reached
        /// </summary>
        /// <returns></returns>
        public virtual Dictionary<string, string> PopKeyValues()
        {
            Dictionary<string, string> kva = new Dictionary<string, string>();

            while (!EOF)
            {
                string key = PopStringSafe();
                if (key == null || EOF) return kva;

                string value = PopStringSafe();
                if (value == null) return kva;

                kva.Add(key, value);
            }

            return kva;
        }

        /// <summary>
        /// Pops a string from the packet, but returns null if the string cannot be retrieved rather than throwing an exception
        /// </summary>
        /// <returns></returns>
        public virtual string PopStringSafe()
        {
            if (EOF) return null;

            string result = "";
            bool bUnicode = false;
            int length = 0;
            int oldPointer = pointer;

            try
            {
                length = PopCompactIndex(out bUnicode);
            }
            catch
            {
                pointer = oldPointer;
                return null;
            }

            if (length == 0) return "";

            if (bUnicode)
            {
                length *= 2;
                if (pointer + length > data.Length) return null;
                result = Encoding.Unicode.GetString(data, pointer, length - 2);
            }
            else
            {
                if (pointer + length > data.Length) return null;
                result = Encoding.ASCII.GetString(data, pointer, length - 1);
            }

            pointer += length;
            return result;

        }
    }
}
