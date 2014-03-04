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
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using XMPMS.Core;
using XMPMS.Interfaces;

namespace XMPMS.Net.WCF
{
    /// <summary>
    /// Represents a server list on a remote master server, this class is actually the RPC player
    /// which connects to the remote service host
    /// </summary>
    public partial class RemoteMasterServer : ClientBase<IServerList>, IServerList
    {
        /// <summary>
        /// Constructor, create a new RemoteMasterServer object
        /// </summary>
        /// <param name="binding">RPC binding to use</param>
        /// <param name="remoteMasterUri">Address to bind RPC connection to</param>
        public RemoteMasterServer(Binding binding, string remoteMasterUri)
            : base(binding, new EndpointAddress(remoteMasterUri))
        {
            // Get credentials from the supplied url
            string[] userInfo = new Uri(remoteMasterUri).UserInfo.Split(':');

            if (userInfo.Length > 1)
            {
                ClientCredentials.UserName.UserName = userInfo[0];
                ClientCredentials.UserName.Password = userInfo[1];
            }
            else
            {
                ClientCredentials.UserName.UserName = "default";
                ClientCredentials.UserName.Password = "default";
            }
        }

        /// <summary>
        /// Simple request to the remote server to check whether the connection is alive or not
        /// </summary>
        /// <returns></returns>
        public string Ping()
        {
            return base.Channel.Ping();
        }

        /// <summary>
        /// Add/update a local server to the remote server
        /// </summary>
        /// <param name="active">True if the server is active (gamestate has been received)</param>
        /// <param name="address">IP address of the server</param>
        /// <param name="cdkey">CD key hash of the server</param>
        /// <param name="name">Name of the server</param>
        /// <param name="country">Server's country</param>
        /// <param name="locale">Server's locale</param>
        /// <param name="port">Server listen port</param>
        /// <param name="queryport">Server query port</param>
        /// <param name="map">Current map</param>
        /// <param name="gametype">Current game type</param>
        /// <param name="maxplayers">Max connections</param>
        /// <param name="currentplayers">Current player count</param>
        /// <param name="properties">Array of server properties</param>
        /// <param name="players">List of players on the server</param>
        public void Update(bool active, IPAddress address, string cdkey, string name, string country, string locale, int port, int queryport, string map, string gametype, int maxplayers, int currentplayers, Dictionary<string, string> properties, List<Player> players)
        {
            base.Channel.Update(active, address, cdkey, name, country, locale, port, queryport, map, gametype, maxplayers, currentplayers, properties, players);
        }

        /// <summary>
        /// Remove a specified server by IP and port
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public void Remove(IPAddress address, int port)
        {
            base.Channel.Remove(address, port);
        }
    }
}
