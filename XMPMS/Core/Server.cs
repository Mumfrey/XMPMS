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
using System.Net;
using System.Text;
using XMPMS.Util;
using XMPMS.Util.UScript;
using XMPMS.Net;
using XMPMS.Net.Connections;
using XMPMS.Net.Packets;
using XMPMS.Net.Packets.Specialised;
using XMPMS.Net.Helpers;

namespace XMPMS.Core
{
    /// <summary>
    /// Server encapsulates a single uplinked server. It keeps track of the server properties and allows
    /// queries to be executed and server lists to be compiled by the ServerList. All socket communication
    /// with directly-uplinked servers is managed by the ServerConnection
    /// </summary>
    public class Server : IDisposable
    {
        /// <summary>
        /// Flag indicating the object was disposed
        /// </summary>
        protected bool disposed = false;

        /// <summary>
        /// True if this server is directly uplinked to this master server, or false if it
        /// was pushed by another remote master server
        /// </summary>
        public bool Local
        {
            get;
            private set;
        }

        /// <summary>
        /// Server version, received during handshake
        /// </summary>
        public int Version
        {
            get;
            private set;
        }

        /// <summary>
        /// If this is a non-local server, the last time it was updated by the remote master, we
        /// use this for scavenging orphaned server entries (eg. when the remote master loses
        /// communication)
        /// </summary>
        public DateTime LastUpdate
        {
            get;
            private set;
        }

        /// <summary>
        /// Set once the first Update has occurred
        /// </summary>
        public bool Active
        {
            get;
            private set;
        }

        /// <summary>
        /// True if the server is behind a NAT firewall
        /// </summary>
        public bool ServerBehindNAT
        {
            get;
            private set;
        }

        /// <summary>
        /// True if the server is uplinking to Gamespy
        /// </summary>
        public bool UplinkToGamespy
        {
            get;
            private set;
        }

        /// <summary>
        /// True if the server has stat logging enabled
        /// </summary>
        public bool EnableStatLogging
        {
            get;
            private set;
        }

        /// <summary>
        /// Server's operating system
        /// </summary>
        public string OperatingSystem
        {
            get;
            private set;
        }

        /// <summary>
        /// Server's locale (eg. int)
        /// </summary>
        public string Locale
        {
            get;
            private set;
        }

        /// <summary>
        /// Address and port combo for display purposes
        /// </summary>
        public string DisplayAddress
        {
            get
            {
                return (Port > 0) ? String.Format("{0}:{1}", Address, Port) : Address.ToString();
            }
        }

        /// <summary>
        /// Identifier to use for stats logging
        /// </summary>
        #pragma warning disable 429
        public string StatsID
        {
            get
            {
                return (Constants.CDKEY_IDENTIFIES_SERVER) ? CDKey : String.Format("{0}+{1}", Address.ToString().Replace('.', '-'), Port);
            }
        }
        #pragma warning restore 429

        /// <summary>
        /// Remote address of the server
        /// </summary>
        public IPAddress Address
        {
            get;
            private set;
        }

        /// <summary>
        /// CD key hash of the server
        /// </summary>
        public String CDKey
        {
            get;
            private set;
        }

        /// <summary>
        /// Country code for the server reverse-engineered from GeoIP data
        /// </summary>
        public string Country
        {
            get;
            private set;
        }

        /// <summary>
        /// Server name
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Listen port
        /// </summary>
        public int Port
        {
            get;
            private set;
        }

        /// <summary>
        /// Query port received from heartbeat
        /// </summary>
        private int queryPort = 0;

        /// <summary>
        /// Query port
        /// </summary>
        public int QueryPort
        {
            get
            {
                return (queryPort > 0) ? queryPort : (Port + 1);
            }
        }

        /// <summary>
        /// Gamespy Query Port received from heartbeat
        /// </summary>
        public int GamespyQueryPort
        {
            get;
            private set;
        }

        /// <summary>
        /// Current map
        /// </summary>
        public string Map
        {
            get;
            private set;
        }

        /// <summary>
        /// Current gametype
        /// </summary>
        public string GameType
        {
            get;
            private set;
        }

        /// <summary>
        /// Server maximum players
        /// </summary>
        public int MaxPlayers
        {
            get;
            private set;
        }

        /// <summary>
        /// Server current players
        /// </summary>
        public int CurrentPlayers
        {
            get;
            private set;
        }

        /// <summary>
        /// List of properties pushed by the servers
        /// </summary>
        public Dictionary<string, string> Properties
        {
            get;
            private set;
        }

        /// <summary>
        /// Players on this server
        /// </summary>
        public List<Player> Players
        {
            get;
            private set;
        }

        /// <summary>
        /// Flags is appended to the server info packet and reports values such as dedicated/listen and passworded/public
        /// </summary>
        public byte Flags
        {
            get
            {
                byte flags = 0x10;
                if (Listen)   flags |= 0x08;
                if (Password) flags |= 0x40;
                return flags;
            }
        }

        /// <summary>
        /// True if this is a listen server (non-dedicated)
        /// </summary>
        public bool Listen
        {
            get { return (GetProperty("servermode") != "dedicated"); }
        }

        /// <summary>
        /// True if this server is passworded
        /// </summary>
        public bool Password
        {
            get { return (GetProperty("password") == "true"); }
        }

        /// <summary>
        /// List entry is the struct inserted into the server list packet in response to a client query
        /// </summary>
        private ServerListEntry listEntry = new ServerListEntry();

        /// <summary>
        /// Returns this server's details in a struct suitable for inclusion in the server list
        /// </summary>
        public ServerListEntry ListEntry
        {
            get
            {
                return listEntry;
            }
        }

        /// <summary>
        /// If this a local server, the TCP connection to the remote server
        /// </summary>
        public ServerConnection Connection
        {
            get;
            private set;
        }

        /// <summary>
        /// Code for matching heartbeat to a server
        /// </summary>
        public int HeartbeatCode
        {
            get;
            private set;
        }

        /// <summary>
        /// Backing field for MatchID
        /// </summary>
        protected int matchId = -1;

        /// <summary>
        /// The match ID we have assigned to the server, this is NOT globally unique, only unique per server-to-masterserver session
        /// </summary>
        public int MatchID
        {
            get { return matchId; }
            set { matchId = value; }
        }

        /// <summary>
        /// True if we have been assigned a match ID
        /// </summary>
        public bool HasMatchID
        {
            get { return matchId != -1; }
        }

        /// <summary>
        /// True if the server is selected for management tasks
        /// </summary>
        public bool Selected
        {
            get; set;
        }

        /// <summary>
        /// Query client object for performing remote queries against the server
        /// </summary>
        private UDPServerQueryClient queryClient;

        /// <summary>
        /// Flag which indicates whether we have requested a query pingback yet
        /// </summary>
        private bool queryDone = false;

        /// <summary>
        /// Raised when a connection error occurs
        /// </summary>
        public event EventHandler ConnectionError;

        /// <summary>
        /// Creates a new server instance
        /// </summary>
        /// <param name="endpoint"></param>
        public Server(ServerConnection connection, int version, IPAddress address, string cdKey, GeoIP geoip, int heartbeatCode, bool enableStatLogging, XMPMS.Net.OperatingSystem operatingSystem, string locale, int matchId)
        {
            this.Connection        = connection;
            this.Local             = connection != null;
            this.Version           = version;
            this.Address           = address;
            this.CDKey             = cdKey;
            this.HeartbeatCode     = heartbeatCode;
            this.EnableStatLogging = enableStatLogging;
            this.OperatingSystem   = EnumInfo.Description(operatingSystem);
            this.Locale            = locale;
            this.matchId           = matchId;

            this.Name = "< Waiting gamestate... >";
            this.Port = 0;

            this.LastUpdate = DateTime.Now;
            this.Country = geoip.Match(address).Country;

            this.queryClient = new UDPServerQueryClient();
            this.queryClient.QueryFinished += new UDPServerQueryResponseHandler(HandleQueryFinished);

            Properties = new Dictionary<string, string>();
            Properties.Add("servermode", "dedicated");

            Players = new List<Player>();
        }

        /// <summary>
        /// Update this server instance with information from the supplied packet
        /// </summary>
        /// <param name="data"></param>
        public void Update(ServerInfoPacket data)
        {
            this.Active         = true;
            this.Name           = data.Name;
            this.Port           = data.Port;
            this.Map            = data.Map;
            this.GameType       = data.GameType;
            this.MaxPlayers     = data.MaxPlayers;
            this.CurrentPlayers = data.NumPlayers;
            this.Properties     = data.Info;
            this.Players        = data.Players;

            this.LastUpdate     = DateTime.Now;
            this.listEntry      = new ServerListEntry(Address, (ushort)Port, (ushort)QueryPort, Name, Map, GameType, (byte)CurrentPlayers, (byte)MaxPlayers, Flags);
        }

        /// <summary>
        /// Update this server instance with information from the supplied heartbeat packet
        /// </summary>
        /// <param name="data"></param>
        public void Update(byte packetType, UDPPacket data)
        {
            switch ((UDPServerQueryClient.UDPServerQueryType)packetType)
            {
                case UDPServerQueryClient.UDPServerQueryType.Basic:
                    /* this.serverID = */ data.PopInt();
                    /* this.serverIP = */ data.PopString();
                    this.Port           = data.PopInt();
                    /* this.QueryPort = */data.PopInt();
                    this.Name           = data.PopString();
                    this.Map            = data.PopString();
                    this.GameType       = data.PopString();
                    this.CurrentPlayers = data.PopInt();
                    this.MaxPlayers     = data.PopInt();
                    /* this.Ping = */     data.PopInt();

                    this.Active         = true;
                    this.LastUpdate     = DateTime.Now;
                    this.listEntry      = new ServerListEntry(Address, (ushort)Port, (ushort)QueryPort, Name, Map, GameType, (byte)CurrentPlayers, (byte)MaxPlayers, Flags);

                    break;

                case UDPServerQueryClient.UDPServerQueryType.GameInfo:
                    Properties = data.PopKeyValues();

                    break;

                case UDPServerQueryClient.UDPServerQueryType.PlayerInfo:
                    Players.Clear();

                    while (!data.EOF)
                        Players.Add(new Player(this, data, ""));

                    break;
            }
        }

        /// <summary>
        /// Update this server with information from a remote master
        /// </summary>
        /// <param name="active">True if the server is active (gamestate has been received)</param>
        /// <param name="address">IP address of the server</param>
        /// <param name="cdkey">CD key hash of the server</param>
        /// <param name="name">Name of the server</param>
        /// <param name="country">Server's country</param>
        /// <param name="locale">Server's locale (eg. int)</param>
        /// <param name="port">Server listen port</param>
        /// <param name="queryport">Server's query port</param>
        /// <param name="map">Current map</param>
        /// <param name="gametype">Current game type</param>
        /// <param name="maxplayers">Max connections</param>
        /// <param name="currentplayers">Current player count</param>
        /// <param name="properties">Array of server properties</param>
        /// <param name="players">List of players on the server</param>
        public void Update(bool active, IPAddress address, string cdkey, string name, string country, string locale, int port, int queryport, string map, string gametype, int maxplayers, int currentplayers, Dictionary<string, string> properties, List<Player> players)
        {
            this.Active         = active;
            this.Address        = address;
            this.CDKey          = cdkey;
            this.Name           = name;
            this.Country        = country;
            this.Locale         = locale;
            this.Port           = port;
            this.queryPort      = queryport;
            this.Map            = map;
            this.GameType       = gametype;
            this.MaxPlayers     = maxplayers;
            this.CurrentPlayers = currentplayers;
            this.Properties     = properties;
            this.Players        = players;

            this.LastUpdate     = DateTime.Now;
            this.listEntry      = new ServerListEntry(Address, (ushort)Port, (ushort)QueryPort, Name, Map, GameType, (byte)CurrentPlayers, (byte)MaxPlayers, Flags);
        }

        /// <summary>
        /// Called when the connection receives a hello packet during initial handshake
        /// </summary>
        /// <param name="hello"></param>
        public void HandleHello(ServerInfoPacket hello)
        {
            ServerBehindNAT = hello.ServerBehindNAT;
            UplinkToGamespy = hello.UplinkToGamespy;

            this.LastUpdate = DateTime.Now;
        }

        /// <summary>
        /// Called when the heartbeat listener receives a heartbeat packet from this server
        /// </summary>
        /// <remarks>
        /// Code is matched against this server before this function will be called so there is no
        /// requirement to check the code matches the server's heartbeat code, although it may be
        /// a good idea to do so in case a bad code gets passed for some reason.
        /// </remarks>
        /// <param name="heartbeatType">Type of heartbeat packet which was received</param>
        /// <param name="code">Code which was received</param>
        /// <param name="port">Outgoing port from which the heartbeat packet was received</param>
        public void HandleReceivedHeartBeat(HeartbeatType heartbeatType, int code, int port)
        {
            switch (heartbeatType)
            {
                case HeartbeatType.QueryInterface:
                    if (!ServerBehindNAT)
                        queryPort = (ushort)port;
                    break;

                case HeartbeatType.GamePort:
                    Port = port;
                    break;

                case HeartbeatType.GamespyQueryPort:
                    GamespyQueryPort = port;
                    break;
            }

            if (Connection != null)
            {
                Connection.HandleHeartBeat(heartbeatType, code, port);
            }
        }

        /// <summary>
        /// Called when all the heartbeats have been received 
        /// </summary>
        public void HeartbeatResponseComplete()
        {
            if (queryClient != null && !queryDone && queryPort > 0)
            {
                queryDone = true;
                queryClient.BeginQuery(Protocol.SERVERQUERY_TIMEOUT, UDPServerQueryClient.UDPServerQueryType.Basic, Address, QueryPort);
            }
        }

        /// <summary>
        /// Handler function for when the udp server query returns a response
        /// </summary>
        /// <param name="sender">Server query object</param>
        /// <param name="response">Response type</param>
        /// <param name="payload">Response packet (if successful)</param>
        void HandleQueryFinished(UDPServerQueryClient sender, UDPServerQueryClient.UDPServerQueryResponse response, UDPServerQueryClient.UDPServerQueryType queryType, UDPPacket payload)
        {
            switch (response)
            {
                case UDPServerQueryClient.UDPServerQueryResponse.Success:
                    QueryResponseSuccess(queryType, payload);
                    break;

                case UDPServerQueryClient.UDPServerQueryResponse.Error:
                    QueryResponseError(queryType);
                    break;

                case UDPServerQueryClient.UDPServerQueryResponse.Timeout:
                    QueryResponseTimeout(queryType);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Got a response to a UDP server query
        /// </summary>
        /// <param name="data"></param>
        protected virtual void QueryResponseSuccess(UDPServerQueryClient.UDPServerQueryType queryType, UDPPacket data)
        {
            // The packet type should be the first byte
            byte packetType = data.PopByte();

            // Try to update this server with data from the query response
            Update(packetType, data);

            // Notify the connection that the query was successful
            if (Connection != null)
            {
                Connection.HandleQueryResponse((UDPServerQueryClient.UDPServerQueryType)packetType);
            }

            switch ((UDPServerQueryClient.UDPServerQueryType)packetType)
            {
                case UDPServerQueryClient.UDPServerQueryType.Basic:
                    queryClient.BeginQuery(Protocol.SERVERQUERY_TIMEOUT, UDPServerQueryClient.UDPServerQueryType.GameInfo, Address, QueryPort);
                    break;

                case UDPServerQueryClient.UDPServerQueryType.GameInfo:
                    queryClient.BeginQuery(Protocol.SERVERQUERY_TIMEOUT, UDPServerQueryClient.UDPServerQueryType.PlayerInfo, Address, QueryPort);
                    break;

                case UDPServerQueryClient.UDPServerQueryType.PlayerInfo:
                    break;
            }
        }

        /// <summary>
        /// UDP query failed
        /// </summary>
        protected virtual void QueryResponseError(UDPServerQueryClient.UDPServerQueryType queryType)
        {
            if (queryType == UDPServerQueryClient.UDPServerQueryType.Basic)
            {
                MasterServer.Log("[{0}] Failed contacting queryport on {1}. Type={2}", DisplayAddress, QueryPort, queryType);

                Shutdown();
                OnConnectionError();
            }
        }

        /// <summary>
        /// The query response timed out, close the connection since non-queryable servers are probably not accessible
        /// </summary>
        /// <param name="queryType">Query Type which timed out</param>
        protected virtual void QueryResponseTimeout(UDPServerQueryClient.UDPServerQueryType queryType)
        {
            if (queryType == UDPServerQueryClient.UDPServerQueryType.Basic)
            {
                MasterServer.Log("[{0}] Timeout contacting queryport on {1}. Type={2}", DisplayAddress, QueryPort, queryType);

                Shutdown();
                OnConnectionError();
            }
        }

        /// <summary>
        /// Get a property or pseudo-property value from the server. Primarily to support filtered queries.
        /// </summary>
        /// <param name="propertyName">Name of the property to retrieve</param>
        /// <returns>Property value or empty string if the property was not found</returns>
        public string GetProperty(string propertyName)
        {
            return GetProperty(propertyName, "");
        }

        /// <summary>
        /// Get a property or pseudo-property value from the server. Primarily to support filtered queries.
        /// </summary>
        /// <param name="propertyName">Name of the property to retrieve</param>
        /// <param name="defaultValue">Default value to return if the property does not exist</param>
        /// <returns>Property value or specified default value if the property was not found</returns>
        public string GetProperty(string propertyName, string defaultValue)
        {
            switch (propertyName.ToLower())
            {
                case "country":     return Country;
                case "name":        return Name;
                case "port":        return Port.ToString();
                case "map":         return Map;
                case "gametype":    return GameType;
                default:            return (this.Properties.ContainsKey(propertyName)) ? Properties[propertyName] : defaultValue;
            }
        }

        /// <summary>
        /// Used by the ServerList when filtering servers, queries whether this server matches the
        /// specified criterion
        /// </summary>
        /// <param name="propertyName">Name of property to match</param>
        /// <param name="propertyValue">Property value to match</param>
        /// <param name="matchType">Match method to use, eg. Equals, NotEquals</param>
        /// <returns>True if the match succeeded</returns>
        internal bool Matches(string propertyName, string propertyValue, QueryType matchType)
        {
            string myPropertyValue = GetProperty(propertyName);
            int iPropertyValue, iMyPropertyValue;
            int.TryParse(propertyValue, out iPropertyValue);
            int.TryParse(myPropertyValue, out iMyPropertyValue);

            switch (matchType)
            {
                case QueryType.Equals:              return (myPropertyValue  == propertyValue );
                case QueryType.NotEquals:           return (myPropertyValue  != propertyValue );
                case QueryType.LessThan:            return (iMyPropertyValue <  iPropertyValue);
                case QueryType.LessThanEquals:      return (iMyPropertyValue <= iPropertyValue);
                case QueryType.GreaterThan:         return (iMyPropertyValue >  iPropertyValue);
                case QueryType.GreaterThanEquals:   return (iMyPropertyValue >= iPropertyValue);
                case QueryType.Disabled:            return true;
                default:                            return true;
            }
        }

        /// <summary>
        /// Shut down the server connection
        /// </summary>
        public void Shutdown()
        {
            if (Connection != null)
            {
                Connection.Shutdown();
            }

            if (queryClient != null)
            {
                queryClient.Shutdown();
            }
        }

        /// <summary>
        /// Send package MD5 information to the server
        /// </summary>
        /// <param name="updates"></param>
        public void SyncMD5(List<MD5Entry> updates)
        {
            if (Connection != null)
            {
                Connection.SendMD5Updates(updates);
            }
        }

        /// <summary>
        /// Release resources being used by this object
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                Properties.Clear();
                Properties = null;

                Players.Clear();
                Players = null;

                Connection = null;

                if (queryClient != null)
                {
                    queryClient.Shutdown();
                    queryClient.QueryFinished -= new UDPServerQueryResponseHandler(HandleQueryFinished);
                    queryClient = null;
                }
            }
            else
            {
                throw new ObjectDisposedException("Server " + DisplayAddress);
            }
        }

        /// <summary>
        /// List clients connected to this server to the console
        /// </summary>
        public void ListClients()
        {
            if (Players != null && Players.Count > 0)
            {
                foreach (Player player in Players)
                {
                    MasterServer.LogMessage("{0,-22}{1,-20}{2,5}", player.Address, ColourCodeParser.StripColourCodes(player.Name), player.Ping);
                }
            }
            else
            {
                MasterServer.LogMessage("No clients connected");
            }
        }

        /// <summary>
        /// Challenge a connected client
        /// </summary>
        /// <param name="clientAddress"></param>
        public void ChallengeClient(string clientAddress)
        {
            if (Connection == null) return;

            if (clientAddress == "*")
            {
                foreach (Player player in Players)
                {
                    Connection.ChallengeClient(player.Address);
                }
            }
            else
            {
                if (GetClientPlayer(clientAddress) != null)
                {
                    Connection.ChallengeClient(clientAddress);
                }
                else
                {
                    MasterServer.LogMessage("Client {0} not found", clientAddress);
                }
            }
        }

        /// <summary>
        /// Disconnect a player
        /// </summary>
        /// <param name="clientAddress"></param>
        public void DisconnectClient(string clientAddress)
        {
            if (Connection == null) return;

            Player player = GetClientPlayer(clientAddress);

            if (player != null)
            {
                Connection.DisconnectClient(player.Address);
                Players.Remove(player);
            }
            else
            {
                MasterServer.LogMessage("Client {0} not found", clientAddress);
            }
        }

        /// <summary>
        /// Get a reference to a connected player client
        /// </summary>
        /// <param name="clientAddress"></param>
        /// <returns></returns>
        public Player GetClientPlayer(string clientAddress)
        {
            if (Connection != null && Players != null)
            {
                foreach (Player player in Players)
                {
                    if (player.Address == clientAddress || ColourCodeParser.StripColourCodes(player.Name).ToLower() == clientAddress.ToLower()) return player;
                }
            }

            return null;
        }

        /// <summary>
        /// Set INI file option on the remote server
        /// </summary>
        /// <param name="packageName">Name of package and group</param>
        /// <param name="variableName">Variable name</param>
        /// <param name="value">Value to set</param>
        public void SetOption(string packageName, string variableName, string value)
        {
            if (Connection != null) Connection.SetOption(packageName, variableName, value);
        }

        /// <summary>
        /// Check INI option on the remote server
        /// </summary>
        /// <param name="packageName">Name of package and group</param>
        /// <param name="variableName">Variable name</param>
        public void CheckOption(string packageName, string variableName)
        {
            if (Connection != null) Connection.CheckOption(packageName, variableName);
        }

        /// <summary>
        /// Compare this server to another server to determine whether it is a duplicate
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        #pragma warning disable 162
        public bool IsDuplicateOf(Server other)
        {
            if (other == null) return false;

            if (Constants.CDKEY_IDENTIFIES_SERVER)
                return other.CDKey == CDKey;
            else
                return other.DisplayAddress == DisplayAddress;
        }
        #pragma warning restore 162

        /// <summary>
        /// String representation of the server, for logging purposes
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return DisplayAddress;
        }

        /// <summary>
        /// List server info to the console
        /// </summary>
        public void Print()
        {
            MasterServer.LogMessage("Server info:");
            MasterServer.LogMessage("------------------------------------------------------------");
            MasterServer.LogMessage("                 Name: {0}", Name);
            MasterServer.LogMessage("              Address: {0}", Address);
            MasterServer.LogMessage("  GPort/QPort/GSQPort: {0}/{1}/{2}", Port, QueryPort, GamespyQueryPort > 0 ? GamespyQueryPort.ToString() : "-");
            MasterServer.LogMessage("               Active: {0}", Active);
            MasterServer.LogMessage("                 Type: {0}", Local ? "Local" : "RPC");
            MasterServer.LogMessage("              Version: {0}", Version);
            MasterServer.LogMessage("                CDKey: {0}", CDKey);
            MasterServer.LogMessage("      ServerBehindNAT: {0}", ServerBehindNAT);
            MasterServer.LogMessage("      UplinkToGamespy: {0}", UplinkToGamespy);
            MasterServer.LogMessage("    EnableStatLogging: {0}", EnableStatLogging);
            MasterServer.LogMessage("      OperatingSystem: {0}", OperatingSystem);
            MasterServer.LogMessage("       Map (GameType): {0} ({1})", Map, GameType);
            MasterServer.LogMessage("        Players (Max): {0} ({1})", CurrentPlayers, MaxPlayers);

            foreach (KeyValuePair<string, string> property in Properties)
            {
                MasterServer.LogMessage("  {0,19}: {1}", property.Key, property.Value);
            }
        }

        /// <summary>
        /// Raise the connection error event
        /// </summary>
        protected void OnConnectionError()
        {
            EventHandler connectionError = this.ConnectionError;

            if (connectionError != null)
                connectionError(this, EventArgs.Empty);
        }
    }
}
