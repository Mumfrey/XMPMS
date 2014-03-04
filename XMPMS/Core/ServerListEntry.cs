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

namespace XMPMS.Core
{
    /// <summary>
    /// Struct for constructing server lists in the server list packet, adjust the fields in this
    /// struct to change the format of server listings returned to clients 
    /// </summary>
    public struct ServerListEntry
    {
        /// <summary>
        /// Server IP address
        /// </summary>
        public IPAddress Address;

        /// <summary>
        /// Server port
        /// </summary>
        public ushort Port;

        /// <summary>
        /// Server query port
        /// </summary>
        public ushort QueryPort;

        /// <summary>
        /// Server name
        /// </summary>
        public string Name;

        /// <summary>
        /// Current map
        /// </summary>
        public string Map;

        /// <summary>
        /// Current gametype
        /// </summary>
        public string GameType;

        /// <summary>
        /// Current player count
        /// </summary>
        public byte CurrentPlayers;

        /// <summary>
        /// Server max players
        /// </summary>
        public byte MaxPlayers;

        /// <summary>
        /// Server flags
        /// </summary>
        public byte Flags;

        /// <summary>
        /// Create a new server list entry struct
        /// </summary>
        /// <param name="address">Server IP address</param>
        /// <param name="port">Server port</param>
        /// <param name="queryPort">Server query port</param>
        /// <param name="name">Server name</param>
        /// <param name="map">Current map</param>
        /// <param name="gameType">Current game type</param>
        /// <param name="currentPlayers">Current player count</param>
        /// <param name="maxPlayers">Server max players</param>
        /// <param name="flags">Server flags</param>
        public ServerListEntry(IPAddress address, ushort port, ushort queryPort, string name, string map, string gameType, byte currentPlayers, byte maxPlayers, byte flags)
        {
            Address        = address;
            Port           = port;
            QueryPort      = queryPort;
            Name           = name;
            Map            = map;
            GameType       = gameType;
            CurrentPlayers = currentPlayers;
            MaxPlayers     = maxPlayers;
            Flags          = flags;
        }
    }
}
