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

namespace XMPMS.Net.Packets
{
    /// <summary>
    /// The empty inbound packet is returned when we get invalid or empty data from the socket.
    /// All functions are overridden to return zero, null or empty results
    /// </summary>
    public class EmptyInboundPacket : InboundPacket
    {
        /// <summary>
        /// True if this is an empty inbound packet (no data were receieved)
        /// </summary>
        public override bool Empty
        {
            get { return true; }
        }

        /// <summary>
        /// This inbound packet is empty, but that is still valid so this always returns true
        /// </summary>
        public override bool Valid
        {
            get { return true; }
        }

        public EmptyInboundPacket()
        {
        }

        /// <summary>
        /// Always returns 0x00
        /// </summary>
        /// <returns></returns>
        public override byte PopByte()
        {
            return 0x00;
        }

        /// <summary>
        /// Always returns an empty array
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public override byte[] PopBytes(int count)
        {
            return new byte[0];
        }

        /// <summary>
        /// Always returns 0
        /// </summary>
        /// <returns></returns>
        public override ushort PopUShort()
        {
            return 0;
        }

        /// <summary>
        /// Always returns 0
        /// </summary>
        /// <returns></returns>
        public override int PopInt()
        {
            return 0;
        }

        /// <summary>
        /// Always returns an empty string
        /// </summary>
        /// <returns></returns>
        public override string PopString()
        {
            return String.Empty;
        }

        /// <summary>
        /// Always returns an empty array
        /// </summary>
        /// <returns></returns>
        public override Dictionary<string, string> PopKeyValueArray()
        {
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Always returns an empty array
        /// </summary>
        /// <returns></returns>
        public override string[] PopArray()
        {
            return new string[0];
        }
    }
}
