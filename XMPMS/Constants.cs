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

namespace XMPMS
{
#if XMP
    /// <summary>
    /// Contains constants which define the master server behaviour.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// For games which require servers to have unique CD keys, the CD key can be used
        /// as a unique identifier (for stats). For games where the CD key does NOT uniquely
        /// identify the server, this value should be set to false and the address/port will
        /// be used to identify the server.
        /// </summary>
        internal    const   bool    CDKEY_IDENTIFIES_SERVER         = false;

        /// <summary>
        /// Default listen port for this game
        /// </summary>
        internal    const   int     DEFAULT_LISTEN_PORT             = 27900;
    }
#elif UT2003
    /// <summary>
    /// Contains constants which define the master server behaviour.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// For games which require servers to have unique CD keys, the CD key can be used
        /// as a unique identifier (for stats). For games where the CD key does NOT uniquely
        /// identify the server, this value should be set to false and the address/port will
        /// be used to identify the server.
        /// </summary>
        internal    const   bool    CDKEY_IDENTIFIES_SERVER         = false;

        /// <summary>
        /// Default listen port for this game
        /// </summary>
        internal    const   int     DEFAULT_LISTEN_PORT             = 28902;
    }
#endif
}
