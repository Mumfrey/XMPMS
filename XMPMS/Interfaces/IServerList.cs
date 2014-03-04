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
using System.ServiceModel;
using XMPMS.Core;

namespace XMPMS
{
    /// <summary>
    /// Interface for server list which we will use for the WCF contract which links the master servers.
    /// </summary>
    [ServiceContract]
    public interface IServerList
    {
        /// <summary>
        /// Simple pingTime request. Can return anything but it will likely be something like "OK", we're really
        /// just interested at the player end whether this throws a WCF exception or not.
        /// </summary>
        /// <returns>Any string</returns>
        [OperationContract]
        string Ping();

        /// <summary>
        /// Update a server on the remote master server.
        /// </summary>
        /// <param name="active">Server is currently active.</param>
        /// <param name="address">Server's IP address</param>
        /// <param name="cdkey">Server's CD Key hash</param>
        /// <param name="name">Server name</param>
        /// <param name="country">Server country (if known)</param>
        /// <param name="locale">Server's locale (eg. int)</param>
        /// <param name="port">Server port</param>
        /// <param name="queryport">Server query port</param>
        /// <param name="map">Current map</param>
        /// <param name="gametype">Current game type</param>
        /// <param name="maxplayers">Current player cap</param>
        /// <param name="currentplayers">Current player count</param>
        /// <param name="properties">Additional key/value properties</param>
        /// <param name="players">Array of players on the server</param>
        [OperationContract]
        void Update(
            bool active,
            IPAddress address,
            string cdkey,
            string name,
            string country,
            string locale,
            int port,
            int queryport,
            string map,
            string gametype,
            int maxplayers,
            int currentplayers,
            Dictionary<string, string> properties,
            List<Player> players
        );

        /// <summary>
        /// Remove a server on the remote master server.
        /// </summary>
        /// <param name="address">Address of the server to remove</param>
        /// <param name="port">Port of the server to remove</param>
        [OperationContract]
        void Remove(
            IPAddress address,
            int port
        );
    }
}
