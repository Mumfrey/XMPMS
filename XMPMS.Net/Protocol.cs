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

namespace XMPMS.Net
{
#if XMP
    /// <summary>
    /// Contains constants which define the master server protocol. It is largely possible to
    /// reconfigure the server to work with different games by tweaking these values, although
    /// some extra work may be required if the protocol differs too much.
    /// </summary>
    public static class Protocol
    {
        /// <summary>
        /// SERVER identifier, receieved during login identifies that the remote host is
        /// a server
        /// </summary>
        public      const   string  HOST_SERVER                     = "SERVER";

        /// <summary>
        /// CLIENT identifier, received during login identifies the remote host as a client
        /// </summary>
        public      const   string  HOST_CLIENT                     = "CLIENT";

        /// <summary>
        /// Login response returned when the remote host successfully authenticated
        /// </summary>
        public      const   string  LOGIN_RESPONSE_APPROVED         = "APPROVED";

        /// <summary>
        /// Login response returned when the remote host did not successfully authenticate
        /// </summary>
        public      const   string  LOGIN_RESPONSE_DENIED           = "DENIED";

        /// <summary>
        /// Login response returned when the remote host is out of date and must upgrade
        /// </summary>
        public      const   string  LOGIN_RESPONSE_UPGRADE          = "UPGRADE";

        /// <summary>
        /// Login response returned to client to update the local stored master server list
        /// </summary>
        public      const   string  LOGIN_RESPONSE_MSLIST           = "MSLIST";

        /// <summary>
        /// The lowest acceptable client version, versions lower than this will return
        /// LOGIN_RESPONSE_UPGRADE
        /// </summary>
        public      const   int     MIN_SUPPORTED_CLIENT_VERSION    = 2226;

        /// <summary>
        /// Version appended to MOTD requests if optional upgrade version is higher than
        /// client version
        /// </summary>
        public      const   int     OPTIONALUPGRADE_VERSION         = 2226;

        /// <summary>
        /// Value which identifies UDP packets. Inbound UDP packets which don't respect this
        /// value will be discarded.
        /// </summary>
        public      const   int     UDP_PROTOCOL_VERSION            = 0x7E;

        /// <summary>
        /// Maximum amount of time (in seconds) to wait for a UDP server query response from
        /// a remote server before closing the connection.
        /// </summary>
        public      const   int     SERVERQUERY_TIMEOUT             = 10;

        /// <summary>
        /// Command byte sent as a header byte on heartbeat requests
        /// </summary>
        public      const    byte   HEARTBEAT_CMD                   = 0x00;

        /// <summary>
        /// Command byte sent as a header byte to signal the remote server should exit
        /// MSUS_WaitingForUDPResponse and enter MSUS_ChannelOpen
        /// </summary>
        public      const   byte    HEARTBEAT_RESPONSE_CMD          = 0x01;

        /// <summary>
        /// Integer value sent when the heartbeat cycle is complete and sent as the first
        /// parameter in the reply.
        /// </summary>
        public      const   int     HEARTBEAT_COMPLETE_UNKNOWN      = 120;

        /// <summary>
        /// Hard limit on the number of MD5 updates which can be included in an MTS_MD5Update
        /// packet.
        /// </summary>
        public      const   int     MAX_MD5_UPDATES_PER_PACKET      = 32;
    }
#elif UT2003
    /// <summary>
    /// Contains constants which define the master server protocol. It is largely possible to
    /// reconfigure the server to work with different games by tweaking these values, although
    /// some extra work may be required if the protocol differs too much.
    /// </summary>
    public static class Protocol
    {
        /// <summary>
        /// SERVER identifier, receieved during login identifies that the remote host is
        /// a server
        /// </summary>
        public      const   string  HOST_SERVER                     = "SERVER";

        /// <summary>
        /// CLIENT identifier, received during login identifies the remote host as a client
        /// </summary>
        public      const   string  HOST_CLIENT                     = "CLIENT";

        /// <summary>
        /// Login response returned when the remote host successfully authenticated
        /// </summary>
        public      const   string  LOGIN_RESPONSE_APPROVED         = "APPROVED";

        /// <summary>
        /// Login response returned when the remote host did not successfully authenticate
        /// </summary>
        public      const   string  LOGIN_RESPONSE_DENIED           = "DENIED";

        /// <summary>
        /// Login response returned when the remote host is out of date and must upgrade
        /// </summary>
        public      const   string  LOGIN_RESPONSE_UPGRADE          = "UPGRADE";

        /// <summary>
        /// Login response returned to client to update the local stored master server list
        /// </summary>
        public      const   string  LOGIN_RESPONSE_MSLIST           = "MSLIST";

        /// <summary>
        /// The lowest acceptable client version, versions lower than this will return
        /// LOGIN_RESPONSE_UPGRADE
        /// </summary>
        public      const   int     MIN_SUPPORTED_CLIENT_VERSION    = 2225;

        /// <summary>
        /// Version appended to MOTD requests if optional upgrade version is higher than
        /// client version
        /// </summary>
        public      const   int     OPTIONALUPGRADE_VERSION         = 2225;

        /// <summary>
        /// Value which identifies UDP packets. Inbound UDP packets which don't respect this
        /// value will be discarded.
        /// </summary>
        public      const   int     UDP_PROTOCOL_VERSION            = 0x79;

        /// <summary>
        /// Maximum amount of time (in seconds) to wait for a UDP server query response from
        /// a remote server before closing the connection.
        /// </summary>
        public      const   int     SERVERQUERY_TIMEOUT             = 10;

        /// <summary>
        /// Command byte sent as a header byte on heartbeat requests
        /// </summary>
        public      const    byte   HEARTBEAT_CMD                   = 0x00;

        /// <summary>
        /// Command byte sent as a header byte to signal the remote server should exit
        /// MSUS_WaitingForUDPResponse and enter MSUS_ChannelOpen
        /// </summary>
        public      const   byte    HEARTBEAT_RESPONSE_CMD          = 0x01;

        /// <summary>
        /// Integer value sent when the heartbeat cycle is complete and sent as the first
        /// parameter in the reply.
        /// </summary>
        public      const   int     HEARTBEAT_COMPLETE_UNKNOWN      = 120;

        /// <summary>
        /// Hard limit on the number of MD5 updates which can be included in an MTS_MD5Update
        /// packet.
        /// </summary>
        public      const   int     MAX_MD5_UPDATES_PER_PACKET      = 32;
    }
#endif
}
