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
    /// Abstract base class for Packets
    /// </summary>
    public abstract class Packet
    {
        /// <summary>
        /// Returns the contents of this packet in printable format for debugging purposes
        /// </summary>
        public abstract string Print();

        /// <summary>
        /// Returns the contents of this packet in printable hexadecimal format for debugging purposes
        /// </summary>
        public abstract string PrintBytes();
    }
}
