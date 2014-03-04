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
using System.ServiceModel;
using System.ServiceModel.Security;
using System.ServiceModel.Description;
using System.Net;
using System.Threading;
using System.Diagnostics;
using XMPMS.Interfaces;
using XMPMS.Util;
using XMPMS.Util.UScript;
using XMPMS.Net;
using XMPMS.Net.WCF;
using XMPMS.Net.Packets.Specialised;

namespace XMPMS.Core
{
    /// <summary>
    /// Stores and manages the list of local and remote servers and handles the link to other
    /// remote master servers
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public partial class ServerList : IServerList
    {
        /// <summary>
        /// Random number generator used to generate heartbeat codes
        /// </summary>
        private static Random RNG = new Random();

        /// <summary>
        /// Reference to the master server instance
        /// </summary>
        private MasterServer masterServer = null;

        /// <summary>
        /// Reference to the GeoIP resolver
        /// </summary>
        private GeoIP geoIP = null;

        /// <summary>
        /// Connection log writer
        /// </summary>
        private IConnectionLogWriter connectionLogWriter;

        /// <summary>
        /// Thread lock object for the server list
        /// </summary>
        private object serverListLock = new object();

        /// <summary>
        /// List of servers
        /// </summary>
        private List<Server> servers = new List<Server>();

        /// <summary>
        /// Timer used to clean up nonlocal servers which don't get removed (eg. when the remote server times out)
        /// </summary>
        private Timer scavengeTimer = null;

        /// <summary>
        /// Time duraation after which remote servers will be scavenged if they haven't been updated
        /// </summary>
        private TimeSpan scavengeAfter = new TimeSpan(0, 3, 0);

        /// <summary>
        /// WCF RPC Service Host
        /// </summary>
        private ServiceHost serviceHost;

        /// <summary>
        /// Remote master servers to which we are linked
        /// </summary>
        private List<RemoteMasterServer> remoteMasters = new List<RemoteMasterServer>();

        /// <summary>
        /// Lock to prevent simultaneous access to the NextMatchID member
        /// </summary>
        private object matchIdLock = new object();

        /// <summary>
        /// Next Match ID to assign
        /// </summary>
        private int NextMatchID
        {
            get
            {
                int nextMatchID = MasterServer.Settings.MatchID;
                MasterServer.Settings.MatchID++;
                MasterServer.Settings.Save();
                return nextMatchID;
            }
        }

        /// <summary>
        /// Creates a new ServerList
        /// </summary>
        /// <param name="masterServer"></param>
        public ServerList(MasterServer masterServer)
        {
            this.masterServer = masterServer;
            this.geoIP = masterServer.GeoIP;

            this.connectionLogWriter = ModuleManager.GetModule<IConnectionLogWriter>();

            StartService(); // RPC Server
            UpdateLinks();  // RPC Client

            scavengeTimer = new Timer(new TimerCallback(this.Scavenge), null, 10000, Timeout.Infinite);

            ModuleManager.RegisterCommandListener(this);
        }

        /// <summary>
        /// Callback for the scavenge timer, performs scavenging of stale remote server records
        /// </summary>
        /// <param name="State"></param>
        protected void Scavenge(object state)
        {
            List<Server> pendingRemoval = new List<Server>();

            lock (serverListLock)
            {
                foreach (Server server in servers)
                {
                    if (!server.Local && (DateTime.Now - server.LastUpdate).TotalSeconds > scavengeAfter.TotalSeconds)
                    {
                        // Can't modify the collection here
                        pendingRemoval.Add(server);
                    }
                }
            }

            foreach (Server removeServer in pendingRemoval)
            {
                MasterServer.Log("[{0}] Removing stale server record.", removeServer);
                Remove(removeServer);
            }

            scavengeTimer.Change(10000, Timeout.Infinite);
        }

        /// <summary>
        /// Try to start the RPC server
        /// </summary>
        public void StartService()
        {
#if !WCF
#warning "WCF functionality disabled - ServerList RPC will not function"
#else
            if (MasterServer.Settings.WebServerListenPort == 0) return;

            Uri uri = new Uri(String.Format("http://localhost:{0}/{1}/", MasterServer.Settings.WebServerListenPort, MasterServer.Settings.SyncServiceEndpoint));

            if (serviceHost == null)
            {
                try
                {
                    MasterServer.Log("[RPC] Starting service host");

                    // ServiceBinding for the RPC service
                    BasicHttpBinding ServiceBinding = new BasicHttpBinding(BasicHttpSecurityMode.TransportCredentialOnly);
                    ServiceBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

                    // Host for the RPC service
                    serviceHost = new ServiceHost(this, uri);

                    // Add local endpoint
                    serviceHost.AddServiceEndpoint(typeof(IServerList), ServiceBinding, MasterServer.Settings.SyncServiceName);

                    // Set up custom authentication
                    serviceHost.Credentials.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
                    serviceHost.Credentials.UserNameAuthentication.CustomUserNamePasswordValidator = new RPCCredentialValidator();

                    // Enable WSDL fetch functionality over HTTP
                    ServiceMetadataBehavior remoteServiceHostMetaDataBehaviour = new ServiceMetadataBehavior();
                    remoteServiceHostMetaDataBehaviour.HttpGetEnabled = true;
                    serviceHost.Description.Behaviors.Add(remoteServiceHostMetaDataBehaviour);

                    // Disable the HTML help message (just return the WSDL to browser clients)
                    ServiceDebugBehavior remoteServiceHostDebugBehaviour = serviceHost.Description.Behaviors.Find<ServiceDebugBehavior>();
                    remoteServiceHostDebugBehaviour.HttpHelpPageEnabled = false;

                    // Open the service host
                    serviceHost.Open();

                    MasterServer.Log("[RPC] Initialised ok. Listening at {0}/{1}/{2}/", 80, MasterServer.Settings.SyncServiceEndpoint, MasterServer.Settings.SyncServiceName);
                }
                catch (AddressAlreadyInUseException)
                {
                    MasterServer.Log("[RPC] FAULT initialising listener: Port {0} already in use.", MasterServer.Settings.WebServerListenPort);
                }
                catch (CommunicationObjectFaultedException ex)
                {
                    MasterServer.Log("[RPC] FAULT initialising listener: {0}", ex.Message);
                }
                catch (TimeoutException ex)
                {
                    MasterServer.Log("[RPC] TIMEOUT initialising listener: {0}", ex.Message);
                }
                catch (InvalidOperationException)
                {
                    MasterServer.Log("[RPC] Error initialising listener, invalid operation.");
                }
                catch (ArgumentNullException)
                {
                    MasterServer.Log("[RPC] Error initialising listener, configuration error.");
                }
                catch (ArgumentOutOfRangeException)
                {
                    MasterServer.Log("[RPC] Error initialising listener, configuration error.");
                }
            }
#endif
        }

        /// <summary>
        /// Server is shutting down, release resources
        /// </summary>
        public void Shutdown()
        {
            lock (serverListLock)
            {
                foreach (Server server in servers)
                    server.Shutdown();
            }

#if !WCF
#warning "WCF functionality disabled - ServerList RPC will not function"
#else
            if (serviceHost != null)
            {
                try
                {
                    if (serviceHost.State == CommunicationState.Opened)
                    {
                        MasterServer.Log("[RPC] Closing service host");
                        serviceHost.Close();
                    }

                    serviceHost.Abort();
                }
                catch { }
            }
#endif
            ModuleManager.ReleaseModule<IConnectionLogWriter>();
        }

        /// <summary>
        /// Get a random, unique heartbeat code to send to a new server
        /// </summary>
        /// <returns></returns>
        public int GetHeartbeatCode()
        {
            int code = -1;

            while (code == -1)
            {
                code = RNG.Next(999);

                lock (serverListLock)
                {
                    foreach (Server server in servers)
                    {
                        if (code == server.HeartbeatCode)
                        {
                            code = -1;
                            break;
                        }
                    }
                }
            }

            return code;
        }

        /// <summary>
        /// Get the next available match ID to assign to a new server
        /// </summary>
        /// <param name="server"></param>
        public int GetMatchID(Server server)
        {
            lock (matchIdLock)
            {
                if (server != null && server.HasMatchID) return server.MatchID; else return NextMatchID;
            }
        }

        /// <summary>
        /// Called when the heartbeat listener receives a valid heartbeat packet. Attempts to match the
        /// heartbeat's code to a server in the list and notifies the server object if a match is found.
        /// </summary>
        /// <param name="heartbeatType">Type of heartbeat packet which was received</param>
        /// <param name="heartbeatCode">Code embedded in the heartbeat packet</param>
        /// <param name="port">UDP port from which the heartbeat packet was received</param>
        public void ReceivedHeartbeat(HeartbeatType heartbeatType, int heartbeatCode, int port)
        {
            lock (serverListLock)
            {
                // Look for a matching server and send the heartbeat if we find one
                foreach (Server server in servers)
                {
                    if (server.HeartbeatCode == heartbeatCode)
                    {
                        server.HandleReceivedHeartBeat(heartbeatType, heartbeatCode, port);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Add a new server to the server list
        /// </summary>
        /// <param name="server"></param>
        public void Add(Server server)
        {
            bool serverAdded = false;

            lock (serverListLock)
            {
                if (!servers.Contains(server))
                {
                    serverAdded = true;
                    servers.Add(server);
                }
            }

            if (serverAdded)
            {
                server.ConnectionError += new EventHandler(server_ConnectionError);
            }
        }

        /// <summary>
        /// Called when a server experiences a connection error 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void server_ConnectionError(object sender, EventArgs e)
        {
            //Remove(sender as Server);
        }

        /// <summary>
        /// Update information for a local server using the supplied gamestate packet
        /// </summary>
        /// <param name="server">Server to update</param>
        /// <param name="serverInfo">Gamestate packet</param>
        public void UpdateServer(Server server, ServerInfoPacket serverInfo)
        {
            // Update the server
            server.Update(serverInfo);

            // Remove duplicate servers
            List<Server> pendingRemoval = new List<Server>();

            lock (serverListLock)
            {
                foreach (Server otherServer in servers)
                {
                    if (otherServer != server && otherServer.IsDuplicateOf(server))
                    {
                        // Can't modify the collection here
                        pendingRemoval.Add(otherServer);
                    }
                }
            }

            foreach (Server removeServer in pendingRemoval)
            {
                MasterServer.Log("[{0}] Removing duplicate server record.", removeServer);
                Remove(removeServer);
            }

#if !WCF
#warning "WCF functionality disabled - ServerList RPC will not function"
#else
            // Propogate server information to remote master servers
            foreach (RemoteMasterServer remoteMaster in remoteMasters)
            {
                try
                {
                    remoteMaster.Update(
                        server.Active,
                        server.Address,
                        server.CDKey,
                        server.Name,
                        server.Country,
                        server.Locale,
                        server.Port,
                        server.QueryPort,
                        server.Map,
                        server.GameType,
                        server.MaxPlayers,
                        server.CurrentPlayers,
                        server.Properties,
                        server.Players
                    );
                }
                catch (MessageSecurityException)
                {
                    MasterServer.Log("[RPC] Credentials rejected by remote server at {0}", remoteMaster.InnerChannel.RemoteAddress.Uri.Host);
                }
                catch (CommunicationException) { }
            }
#endif
        }

        /// <summary>
        /// Remove a server from the server list
        /// </summary>
        /// <param name="server">Server to remove</param>
        public void Remove(Server server)
        {
            // Don't want to do the remote removal inside the critical section
            bool serverRemoved = false;

            lock (serverListLock)
            {
                if (servers.Contains(server))
                {
                    servers.Remove(server);
                    serverRemoved = true;
                }
            }

            if (serverRemoved)
            {
                // If the server is local, notify remote master servers to remove the server
                if (server.Local)
                {
                    server.ConnectionError -= new EventHandler(server_ConnectionError);

#if !WCF
#warning "WCF functionality disabled - ServerList RPC will not function"
#else
                    foreach (RemoteMasterServer remoteMaster in remoteMasters)
                    {
                        try
                        {
                            Debug.WriteLine(String.Format("[RPC] Attempting to remove server remotely on {0}", remoteMaster.InnerChannel.RemoteAddress.Uri));
                            remoteMaster.Remove(server.Address, server.Port);
                        }
                        catch (MessageSecurityException)
                        {
                            MasterServer.Log("[RPC] Credentials rejected by remote server at {0}", remoteMaster.InnerChannel.RemoteAddress.Uri.Host);
                        }
                        catch (CommunicationException) { }
                    }
#endif
                }

                server.Dispose();
            }
        }

        /// <summary>
        /// Add/update a remote server 
        /// </summary>
        /// <param name="active">True if the server is active (gamestate has been received)</param>
        /// <param name="address">IP address of the server</param>
        /// <param name="cdkey">CD key hash of the server</param>
        /// <param name="name">Name of the server</param>
        /// <param name="country">Server's country</param>
        /// <param name="locale">Server's locale (eg. int)</param>
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
            Debug.WriteLine(String.Format("[RPC] Received UPDATE packet for {0}:{1}", address.ToString(), port));

            lock (serverListLock)
            {
                foreach (Server server in servers)
                {
                    // Match remote servers on IP and listen port
                    if (server.Address.Equals(address) && server.Port == port)
                    {
                        if (!server.Local)
                        {
                            Debug.WriteLine(String.Format("[RPC] Applying update packet for {0}:{1}", address.ToString(), port));
                            server.Update(active, address, cdkey, name, country, locale, port, queryport, map, gametype, maxplayers, currentplayers, properties, players);
                        }

                        // Found server so return
                        return;
                    }
                }
            }

            MasterServer.Log("[{0}:{1}] Creating non-local server", address.ToString(), port);

            //
            // Didn't find a matching server so create a new nonlocal server
            //
            Server newServer = new Server(null, Protocol.MIN_SUPPORTED_CLIENT_VERSION, address, "", geoIP, 0, false, XMPMS.Net.OperatingSystem.UnknownOS, locale, 0);
            newServer.Update(active, address, cdkey, name, country, locale, port, queryport, map, gametype, maxplayers, currentplayers, properties, players);
            Add(newServer);
        }

        /// <summary>
        /// Remove a server by IP and port
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public void Remove(IPAddress address, int port)
        {
            Debug.WriteLine(String.Format("[RPC] Received REMOVE packet for {0}:{1}", address.ToString(), port));

            // Find the matching server
            Server serverToRemove = GetServer(address, port, true);

            if (serverToRemove != null)
            {
                MasterServer.Log("[{0}:{1}] Removing non-local server", address.ToString(), port);

                Remove(serverToRemove);
            }
        }

        /// <summary>
        /// Get a server from the list by IP and port
        /// </summary>
        /// <param name="address">IP Address of the server</param>
        /// <param name="port">Listen port of the server</param>
        /// <returns>A reference to the specified server or null if not found</returns>
        public Server GetServer(IPAddress address, int port)
        {
            return GetServer(address, port, false);
        }

        /// <summary>
        /// Get a server from the list by IP and port
        /// </summary>
        /// <param name="address">IP Address of the server</param>
        /// <param name="port">Listen port of the server</param>
        /// <param name="remoteOnly">True to only find nonlocal servers</param>
        /// <returns>A reference to the specified server or null if not found</returns>
        public Server GetServer(IPAddress address, int port, bool remoteOnly)
        {
            lock (serverListLock)
            {
                foreach (Server server in servers)
                {
                    if (server.Address.Equals(address) && server.Port == port && (!remoteOnly || !server.Local))
                        return server;
                }
            }

            return null;
        }

        /// <summary>
        /// Return all servers
        /// </summary>
        /// <returns></returns>
        public List<Server> Query()
        {
            lock (serverListLock)
            {
                return new List<Server>(servers);
            }
        }

        /// <summary>
        /// Return all servers which match the specified query criteria
        /// </summary>
        /// <param name="queries">Array of query criteria</param>
        /// <returns></returns>
        public List<Server> Query(QueryData[] queries)
        {
            List<Server> filteredServers;

            lock (serverListLock)
            {
                filteredServers = new List<Server>(servers);
            }

            foreach (QueryData query in queries)
            {
                if (MasterServer.Settings.GlobalIgnoreGameTypeFilter && query.Key.ToLower() == "gametype")
                {
                    connectionLogWriter.Write("<ServerList> Global option \"ignore gametype filter\" is enabled. All gametypes will be returned by the query", this);
                }
                else
                {
                    foreach (Server server in servers)
                    {
                        if (!server.Active || !server.Matches(query.Key, query.Value, query.QueryType))
                        {
                            filteredServers.Remove(server);
                        }
                    }
                }
            }

            connectionLogWriter.Write(String.Format("<ServerList> Query returned {0} server(s).", filteredServers.Count), this);

            return filteredServers;
        }

        /// <summary>
        /// Return all servers which match the specified query
        /// </summary>
        /// <param name="queryType">Type of query to execute</param>
        /// <param name="queryValue">Value of query</param>
        /// <returns></returns>
        public List<Server> QueryGameType(string queryValue)
        {
            return QueryGameType(queryValue, true);
        }

        /// <summary>
        /// Return all servers which match the specified query
        /// </summary>
        /// <param name="queryType">Type of query to execute</param>
        /// <param name="queryValue">Value of query</param>
        /// <param name="onlyActive">Only return servers for which gamestate has been received</param>
        /// <returns></returns>
        public List<Server> QueryGameType(string queryValue, bool onlyActive)
        {
            List<Server> matchingServers = new List<Server>();

            lock (serverListLock)
            {
                foreach (Server server in servers)
                {
                    if (!onlyActive || server.Active)
                    {
                        if (queryValue == "*" || server.GameType == queryValue) matchingServers.Add(server);
                    }
                }
            }

            return matchingServers;
        }

        /// <summary>
        /// Synchronise MD5 information with all servers
        /// </summary>
        /// <param name="updates">List of MD5 updates to send</param>
        public void SyncMD5(List<MD5Entry> updates)
        {
            lock (serverListLock)
            {
                foreach (Server server in servers)
                {
                    if (server.Local) server.SyncMD5(updates);
                }
            }
        }

        /// <summary>
        /// Ping request from another server
        /// </summary>
        /// <returns></returns>
        public string Ping()
        {
            return "OK";
        }

        /// <summary>
        /// Re-initialise remote master server links
        /// </summary>
        protected void UpdateLinks()
        {
            remoteMasters.Clear();

            foreach (string remoteMasterUri in MasterServer.Settings.SyncServiceUris)
            {
                AddRemoteServer(remoteMasterUri);
            }
        }

        /// <summary>
        /// Add a new remote master server entry
        /// </summary>
        /// <param name="remoteMasterUri">RPC URI of the remote server</param>
        private void AddRemoteServer(string remoteMasterUri)
        {
            try
            {
#if !WCF
#warning "WCF functionality disabled - ServerList RPC will not function"
#else
                BasicHttpBinding remoteBinding = new BasicHttpBinding(BasicHttpSecurityMode.TransportCredentialOnly);
                remoteBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
                remoteMasters.Add(new RemoteMasterServer(remoteBinding, remoteMasterUri));
#endif
            }
            catch (Exception ex)
            {
                MasterServer.Log("[RPC] Error initialising remote link to {0}: {1}", remoteMasterUri, ex.Message);
            }
        }

        /// <summary>
        /// Get a reference to the remote master server matching the supplied URI
        /// </summary>
        /// <param name="remoteMasterUri">RPC URI of the remote server</param>
        /// <returns>Reference to the remote master server or null if not found</returns>
        private RemoteMasterServer GetRemoteServer(string remoteMasterUri)
        {
#if !WCF
#warning "WCF functionality disabled - ServerList RPC will not function"
#else
            foreach (RemoteMasterServer remoteMaster in remoteMasters)
            {
                if (remoteMaster.InnerChannel.RemoteAddress.Uri.Equals(remoteMasterUri))
                {
                    return remoteMaster;
                }
            }
#endif

            return null;
        }

        /// <summary>
        /// Remove a remote master server entry
        /// </summary>
        /// <param name="remoteMasterUri"></param>
        private void RemoveRemoteServer(string remoteMasterUri)
        {
            RemoteMasterServer removeMe = GetRemoteServer(remoteMasterUri);

            if (removeMe != null)
                remoteMasters.Remove(removeMe);
        }

        /// <summary>
        /// Attempts to add a new remote master server link using the specified information
        /// </summary>
        /// <param name="host">Remote master server host</param>
        /// <param name="port">Remote master server HTTP listen port</param>
        /// <param name="endpoint">Remote master server endpoint (usually "rpc")</param>
        /// <param name="serviceName">Remote master server service name (usually "link")</param>
        /// <param name="username">Username to use to connect to the remote master server</param>
        /// <param name="password">Password to use to connect to the remote master server</param>
        /// <returns>True if the link was succesfully added</returns>
        public bool AddLink(string host, ushort port, string endpoint, string serviceName, string username, string password)
        {
            if (host.Contains("/") || host.Contains(":") || username.Contains("/") || username.Contains("@") || username.Contains(":") || password.Contains("/") || password.Contains("@") || password.Contains(":")) return false;

            string linkUri = String.Format("http://{0}:{1}@{2}:{3}/{4}/{5}", username, password, host, port, endpoint, serviceName);

            if (Uri.IsWellFormedUriString(linkUri, UriKind.Absolute))
            {
                foreach (string existingLinkUri in MasterServer.Settings.SyncServiceUris)
                    if (existingLinkUri.Equals(linkUri)) return false;

                MasterServer.Settings.SyncServiceUris.Add(linkUri);
                MasterServer.Settings.Save();
                AddRemoteServer(linkUri);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to remove a master server link at the specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool RemoveLink(int index)
        {
            if (index > -1 && index < MasterServer.Settings.SyncServiceUris.Count)
            {
                string removeUri = MasterServer.Settings.SyncServiceUris[index];

                MasterServer.Settings.SyncServiceUris.RemoveAt(index);
                MasterServer.Settings.Save();

                RemoveRemoteServer(removeUri);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Tests a remote master server link by requesting PING
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool TestLink(int index)
        {
#if !WCF
#warning "WCF functionality disabled - ServerList RPC will not function"
#else
            if (index > -1 && index < MasterServer.Settings.SyncServiceUris.Count)
            {
                string remoteMasterUri = MasterServer.Settings.SyncServiceUris[index];
                RemoteMasterServer testMe = GetRemoteServer(remoteMasterUri);

                if (testMe != null)
                {
                    MasterServer.Log("[RPC] Testing connection to {0}", testMe.InnerChannel.RemoteAddress.Uri.Host);
                    try
                    {
                        string pingResponse = testMe.Ping();
                        MasterServer.Log("[RPC] Test link succeeded.");
                        return true;
                    }
                    catch (MessageSecurityException)
                    {
                        MasterServer.Log("[RPC] Test link failed: credentials rejected by remote server");
                    }
                    catch (CommunicationException ex)
                    {
                        MasterServer.Log("[RPC] Test link failed:");
                        MasterServer.Log("[RPC] {0}", ex.Message);
                    }
                }
            }
#endif
            return false;
        }

        /// <summary>
        /// Selects a server for management, the control interface calls this function to nominate a server for
        /// further commands. Commands executed after this point apply to the selected server. We tag the server
        /// as selected rather than keeping a reference to ensure that servers are automatically deselected when
        /// they go out of scope, and also to allow possible multi-selection of servers in the future.
        /// </summary>
        /// <param name="serverId">Server ID comprising the server IP and listen port</param>
        public void SelectServer(string serverId)
        {
            Server selectedServer = null;

            if (!serverId.Contains(":"))
            {
                serverId = serverId + ":7777";
            }

            lock (serverListLock)
            {
                foreach (Server server in servers)
                {
                    if (server.DisplayAddress == serverId)
                    {
                        selectedServer = server;
                        break;
                    }
                }

                if (selectedServer != null)
                {
                    foreach (Server server in servers)
                        server.Selected = (server == selectedServer);

                    MasterServer.LogMessage("Selected server [{0}]", serverId);
                }
                else
                {
                    MasterServer.LogMessage("Server [{0}] was not found", serverId);
                }
            }
        }

        /// <summary>
        /// Get the currently selected server
        /// </summary>
        /// <returns></returns>
        private Server GetSelectedServer()
        {
            lock (serverListLock)
            {
                foreach (Server s in servers)
                    if (s.Selected) return s;
            }

            MasterServer.LogMessage("No server selected");
            return null;
        }
    }
}
