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
using XMPMS.Net.Connections;
using System.Net.Sockets;
using System.Security.Cryptography;
using XMPMS.Net.Packets;

namespace XMPMS.Net.Client
{
    /// <summary>
    /// Simple client which can uplink to the Epic Master Server, used by the passthrough CD key validator
    /// </summary>
    public class MasterServerClient : TCPConnection
    {
        /// <summary>
        /// Remote host name of the master server
        /// </summary>
        private string host;

        /// <summary>
        /// Master server port
        /// </summary>
        private int port;

        /// <summary>
        /// Flag to indicate the initial connection did not succeed - validation will fail
        /// </summary>
        private bool connectionError = false;

        /// <summary>
        /// Create a new master server client to connect to the specified server address
        /// </summary>
        /// <param name="host">Server address</param>
        /// <param name="port">Server port</param>
        public MasterServerClient(string host, int port)
        {
            this.host = host;
            this.port = port;

            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        }

        /// <summary>
        /// Establish the connection to the remote master server and get the challenge salt
        /// </summary>
        /// <returns>Master server challenge salt (or 0 on failure)</returns>
        public int Connect()
        {
            int salt = 0;

            try
            {
                socket.Connect(host, port);
                InboundPacket challenge = Receive();
                salt = int.Parse(challenge.PopString());
            }
            catch
            {
                connectionError = true;
            }

            return salt;
        }

        /// <summary>
        /// Try to login with the specified details and get the response
        /// </summary>
        /// <param name="cdKey">CD key hash for login</param>
        /// <param name="saltedKey">Salted key hash for login</param>
        /// <param name="type">Remote client type (eg. CLIENT or SERVER)</param>
        /// <param name="version">Remote client version</param>
        /// <param name="locale">Remote client locale</param>
        /// <returns>Login response from the server</returns>
        public string Login(string cdKey, string saltedKey, string type, int version, string locale)
        {
            string response = Protocol.LOGIN_RESPONSE_DENIED;

            if (!connectionError)
            {
                try
                {
                    OutboundPacket login = new OutboundPacket();
                    login.Append(cdKey).Append(saltedKey).Append(type).Append(version);
                    if (type == Protocol.HOST_SERVER) login.Append(-1); // stats enabled flag for server
                    login.Append((byte)0x04).Append(locale);
                    Send(login);

                    InboundPacket loginResponse = Receive();
                    response = loginResponse.PopString();
                }
                catch
                {
                    connectionError = true;
                }
            }

            Close();

            return response;
        }

        /// <summary>
        /// Close the socket
        /// </summary>
        public void Close()
        {
            try
            {
                socket.Close();
            }
            catch { }
        }
    }
}
