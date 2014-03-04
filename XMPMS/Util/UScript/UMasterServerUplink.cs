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

// ======================================================================
// This file contains enums imported from MasterServerUplink.uc in the
// UnrealScript source for U2XMP. These enums are used for
// server->masterserver and masterserver->server communications, 
// generally as header bytes prepended to a packet.
// ======================================================================

namespace XMPMS.Util.UScript
{
    /// <summary>
    /// Header bytes expected on inbound packets from remote servers
    /// </summary>
    internal enum ServerToMaster : byte
    {
        ClientResponse,
        GameState,
        Stats,
        ClientDisconnectFailed,
        MD5Version,
        CheckOptionReply
    };

    /// <summary>
    /// Header bytes to use for outbound packets to connected servers
    /// </summary>
    internal enum MasterToServer : byte
    {
        ClientChallenge,
        ClientAuthFailed,
        Shutdown,
        MatchID,
        MD5Update,
        UpdateOption,
        CheckOption
    };


    // NOTE: Moved this enum to XMPMS.Net

    ///// <summary>
    ///// Heartbeat types
    ///// </summary>
    //public enum HeartbeatType : byte
    //{
    //    QueryInterface,
    //    GamePort,
    //    GamespyQueryPort
    //};
}
