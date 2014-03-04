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
using XMPMS.Core;
using XMPMS.Util.UScript;
using XMPMS.Net.Packets;
using XMPMS.Interfaces;

namespace XMPMS.Net.Packets.Specialised
{
    /// <summary>
    /// Contains parsed server data from the server info packet
    /// </summary>
    public sealed class ServerInfoPacket : InboundPacket
    {
        public enum ServerInfoPacketType
        {
            Unknown,
            Hello,
            GameState,
            GameStats,
            ClientResponse,
            CheckOptionResponse,
            MD5Version,
            ClientDisconnectFailed
        }

        /// <summary>Indicates the type of packet that this packet has been parsed as (if successfully parsed)</summary>
        public    ServerInfoPacketType       PacketType          { get; private set; }

        /// <summary>Header byte from the packet which hints at the type (hopefully)</summary>
        internal  ServerToMaster             PacketCode          { get; private set; }


        /// <summary>Set by HELLO, flag which indicates whether the server is configured as behind a NAT firewall</summary>
        public    bool                       ServerBehindNAT     { get; private set; }

        /// <summary>Set by HELLO, flag which indicates whether the server is uplinking to Gamespy</summary>
        public    bool                       UplinkToGamespy     { get; private set; }


        /// <summary>Set by GAMESTATE, not sure what this field is, it seems to always be zero</summary>
        public    int                        ServerID            { get; private set; }

        /// <summary>Set by GAMESTATE, seems to always be empty</summary>
        public    string                     ServerIP            { get; private set; }

        /// <summary>Set by GAMESTATE, the server's listen port</summary>
        public    int                        Port                { get; private set; }

        /// <summary>Set by GAMESTATE, the server's query port (never set, always zero)</summary>
        public    int                        QueryPort           { get; private set; }

        /// <summary>Set by GAMESTATE, the server name</summary>
        public    string                     Name                { get; private set; }

        /// <summary>Set by GAMESTATE, the server's current map</summary>
        public    string                     Map                 { get; private set; }

        /// <summary>Set by GAMESTATE, the current game type</summary>
        public    string                     GameType            { get; private set; }

        /// <summary>Set by GAMESTATE, the current number of connected players</summary>
        public    int                        NumPlayers          { get; private set; }

        /// <summary>Set by GAMESTATE, the maximum number of player slots on the server</summary>
        public    int                        MaxPlayers          { get; private set; }

        /// <summary>Set by GAMESTATE, the pingTime (of something, never set: always zero)</summary>
        public    int                        Ping                { get; private set; }

        /// <summary>Set by GAMESTATE, array of connected player IP/port information</summary>
        private   string[]                   ClientIPs           { get; set; }

        /// <summary>Set by GAMESTATE, additional key/value pairs of information about the server</summary>
        public    Dictionary<string, string> Info                { get; private set; }

        /// <summary>Set by GAMESTATE, array of player information</summary>
        public    List<Player>               Players             { get; private set; }
                                                             

        /// <summary>Set by CLIENT RESPONSE and CLIENT DISCONNECT FAILED, the IP/port of the player</summary>
        public    string                     ClientIP            { get; private set; }

        /// <summary>Set by CLIENT RESPONSE, the player's CD key MD5 hash</summary>
        public    string                     ClientCDKey         { get; private set; }

        /// <summary>Set by CLIENT RESPONSE, the player's salted CD key MD5 hash</summary>
        public    string                     ClientCDKeySalted   { get; private set; }


        /// <summary>Set by CHECK OPTION REPLY, the package and group name of the option</summary>
        public    string                     OptionPackageName   { get; private set; }

        /// <summary>Set by CHECK OPTION REPLY, the variable name of the option</summary>
        public    string                     OptionVariableName  { get; private set; }

        /// <summary>Set by CHECK OPTION REPLY, the option's value (from the ini, not necessarily current "active" value</summary>
        public    string                     OptionVariableValue { get; private set; }


        /// <summary>Set by MD5VERSION, the current MD5 database revision at the server</summary>
        public    int                        MD5Version          { get; private set; }

        /// <summary>Set by STATS, the stat line as it was received from the server</summary>
        public    string                     StatLine            { get; private set; }

        
        /// <summary>
        /// Create a new server info packet as a wrapper for an inbound packet from a server
        /// </summary>
        /// <param name="data"></param>
        public ServerInfoPacket(byte[] data)
            : base(data)
        {
            Players = new List<Player>();
            PacketCode = (ServerToMaster)PopByte();
            PacketType = ServerInfoPacketType.Unknown;
        }

        /// <summary>
        /// Process this packet assuming it is a handshake packet, returns true if the data were parsed correctly
        /// </summary>
        /// <returns>True if no errors were encountered whilst reading the values from the packet</returns>
        public bool ProcessHello()
        {
            try
            {
                Rewind();
                ServerBehindNAT = PopInt() == 1;
                UplinkToGamespy = PopInt() == 1;
            }
            catch { return false; }

            PacketType = ServerInfoPacketType.Hello;

            return true;
        }

        /// <summary>
        /// Process this packet assuming it is a gamestate packet, returns true if parsed with no errors
        /// </summary>
        /// <returns>True if no errors were encountered whilst reading the values from the packet</returns>
        public bool ProcessGameState(Server server, IConnectionLogWriter log)
        {
            try
            {
                ClientIPs       = PopArray();           // Array of player IP/port, for some reason at the start of the packet not the end with the other player data
                ServerID        = PopInt();             // ??? Seems to always be zero
                ServerIP        = PopString();          // ??? Seems to always be empty
                Port            = PopInt();             // Server's listen port
                QueryPort       = PopInt();             // **NOTE** Always zero!
                Name            = PopString();          // Server name
                Map             = PopString();          // Current map
                GameType        = PopString();          // Current game type
                NumPlayers      = PopInt();             // Current players
                MaxPlayers      = PopInt();             // Maximum players
                Ping            = PopInt();             // **NOTE** Always zero!
                Info            = PopKeyValueArray();   // Additional server information as key/value pairs
            }
            catch { return false; }

            PacketType = ServerInfoPacketType.GameState;

            try
            {
                // This could probably be done using PopStructArray but this works as well
                int playercount = PopByte();

                // Read player info from the packet
                for (int player = 0; player < playercount; player++)
                {
                    // Client IP array may be shorter than the player list if this is a listen server
                    string clientIP = (player < ClientIPs.Length) ? ClientIPs[player] : "local";
                    Players.Add(new Player(server, this, clientIP));
                }
            }
            catch
            {
                if (log != null)
                {
                    log.Write(String.Format("<{0}> ERROR PARSING GAMESTATE PLAYER DATA", server.Connection.SocketID), this);
                    log.Write(String.Format("<{0}> PACKET={1}", server.Connection.SocketID, Print()), this);
                }
            }

            return true;
        }

        /// <summary>
        /// Process this packet assuming it is a gamestats line
        /// </summary>
        public bool ProcessGameStats()
        {
            try
            {
                StatLine = PopString();
            }
            catch { return false; }

            PacketType = ServerInfoPacketType.GameStats;

            return true;
        }

        /// <summary>
        /// Process this packet assuming it is a player challenge response
        /// </summary>
        public bool ProcessClientResponse()
        {
            try
            {
                ClientIP          = PopString();
                ClientCDKey       = PopString();
                ClientCDKeySalted = PopString();
            }
            catch { return false; }

            PacketType = ServerInfoPacketType.ClientResponse;

            return true;
        }

        /// <summary>
        /// Process this packet assuming it is a player challenge response
        /// </summary>
        public bool ProcessCheckOptionResponse()
        {
            try
            {
                OptionPackageName   = PopString();
                OptionVariableName  = PopString();
                OptionVariableValue = PopString();
            }
            catch { return false; }

            PacketType = ServerInfoPacketType.CheckOptionResponse;

            return true;
        }

        /// <summary>
        /// Process this packet assuming it is a player challenge response
        /// </summary>
        public bool ProcessMD5Version()
        {
            try
            {
                MD5Version = PopInt();
            }
            catch { return false; }

            PacketType = ServerInfoPacketType.MD5Version;

            return true;
        }

        /// <summary>
        /// Process this packet assuming it is a player disconnect failed message
        /// </summary>
        public bool ProcessClientDisconnectFailed()
        {
            try
            {
                ClientIP = PopString();
            }
            catch { return false; }

            PacketType = ServerInfoPacketType.ClientDisconnectFailed;

            return true;
        }
    }
}
