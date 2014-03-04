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
using System.Threading;
using System.Net;
using System.Net.Sockets;
using XMPMS.Interfaces;
using XMPMS.Core;
using XMPMS.Util;

namespace XMPMS.UserInterface.Telnet
{
    /// <summary>
    /// Really crap implementation of a telnet interface
    /// </summary>
    public class TelnetCommandLine : ICommandInterface, ICommandListener
    {
        /// <summary>
        /// Thread for listening for new connections
        /// </summary>
        private Thread listenThread;

        /// <summary>
        /// Socket for accepting inbound connections
        /// </summary>
        private Socket listenSocket;

        /// <summary>
        /// Flag to indicate whether listening is enabled
        /// </summary>
        private volatile bool aborted = false;

        /// <summary>
        /// Connections which have been accepted but have not yet authenticated
        /// </summary>
        private List<TelnetConnection> pendingConnections = new List<TelnetConnection>();

        /// <summary>
        /// Current active connections (1 at a time)
        /// </summary>
        private TelnetConnection connection;

        /// <summary>
        /// Endpoint for this listener
        /// </summary>
        private IPEndPoint endpoint;

        /// <summary>
        /// Get the endpoint for this listener
        /// </summary>
        public IPEndPoint Endpoint
        {
            get { return endpoint; }
        }

        /// <summary>
        /// True if the command processor should echo commands to the log
        /// </summary>
        public bool EchoCommands
        {
            get { return false; }
        }

        /// <summary>
        /// IMasterServerModule interface support
        /// </summary>
        public bool AutoLoad
        {
            get { return false; }
        }

        /// <summary>
        /// Raised when the display has changed
        /// </summary>
        public event EventHandler OnChange;

        /// <summary>
        /// Initialise the telnet interface
        /// </summary>
        /// <param name="masterServer"></param>
        public void Initialise(MasterServer masterServer)
        {
            if (MasterServer.Settings.TelnetPort > 0)
            {
                MasterServer.Log("[TELNET] Binding telnet interface to port {0}", MasterServer.Settings.TelnetPort);

                try
                {
                    endpoint = new IPEndPoint(IPAddress.Any, MasterServer.Settings.TelnetPort);

                    // Bind the listen socket ready to begin listening
                    listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                    listenSocket.Bind(this.Endpoint);
                }
                catch (Exception ex)
                {
                    MasterServer.Log("[TELNET] Error binding port: {0}", ex.Message);
                    return;
                }

                // Create and start the listen thread
                listenThread = new Thread(ListenThreadProc);
                listenThread.Start();
            }
            else
            {
                MasterServer.Log("[TELNET] Telnet server disabled.");
            }
        }

        /// <summary>
        /// Thread function which listens for incoming connections and assigns them to Connection objects
        /// </summary>
        private void ListenThreadProc()
        {
            // Open the listening socket
            listenSocket.Listen(1);

            try
            {
                while (!aborted)
                {
                    Socket inboundSocket = listenSocket.Accept();

                    if (MasterServer.Instance != null && MasterServer.Instance.BanManager != null && MasterServer.Instance.BanManager.IsBanned((inboundSocket.RemoteEndPoint as IPEndPoint).Address))
                    {
                        inboundSocket.Send(Encoding.ASCII.GetBytes("Your IP is banned"));
                        inboundSocket.Close();
                    }
                    else
                    {
                        // Each new inbound connection is handled in a new thread by a new Connection object
                        TelnetConnection newConnection = new TelnetConnection(inboundSocket, this);
                        pendingConnections.Add(newConnection);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Callback from a new connection, the connection failed authentication
        /// </summary>
        /// <param name="newConnection"></param>
        public void RejectConnection(TelnetConnection newConnection)
        {
            pendingConnections.Remove(newConnection);
            newConnection.Shutdown();
        }

        /// <summary>
        /// Callback from a new connection, the connection authenticated successfully so make the new connection the active connection
        /// </summary>
        /// <param name="newConnection"></param>
        public void AcceptConnection(TelnetConnection newConnection)
        {
            // If already an active connection, kick them off
            if (connection != null)
            {
                connection.Send("Connection aborted, another client has connected");
                connection.Shutdown();
                connection = null;
            }

            // Each new inbound connection is handled in a new thread by a new Connection object
            connection = newConnection;
            connection.CommandReceived += new TelnetCommandReceivedEventHandler(HandleCommandReceived);

            // Header
            connection.Send(String.Format("\x1B[2J\r\n{0} Telnet interface\r\n\r\n", MasterServer.Title));

            // Log tail
            if (MasterServer.Instance != null)
            {
                MasterServer.Instance.TailLog();
            }
        }

        /// <summary>
        /// Callback from the active connection, the connection was closed
        /// </summary>
        /// <param name="telnetConnection"></param>
        public void ConnectionClosed(TelnetConnection telnetConnection)
        {
            if (connection == telnetConnection)
            {
                connection = null;
            }
        }

        /// <summary>
        /// Handle a command received from the telnet interface
        /// </summary>
        /// <param name="receivedCommand"></param>
        /// <param name="sender"></param>
        void HandleCommandReceived(string receivedCommand, TelnetConnection sender)
        {
            ModuleManager.DispatchCommand(receivedCommand.Split(' '));
        }

        /// <summary>
        /// Shut down this listener and all child threads
        /// </summary>
        public void Shutdown()
        {
            aborted = true;

            // Try to close the listen socket
            if (listenSocket != null)
            {
                try
                {
                    listenSocket.Close();
                }
                catch { }
            }

            // Shut down pending connections
            foreach (TelnetConnection pendingConnection in pendingConnections)
            {
                pendingConnection.Shutdown();
            }

            // Shut down active connection
            if (connection != null)
            {
                connection.Shutdown();    
            }

            // Try to forcibly abort the listen thread
            if (listenThread != null)
            {
                listenThread.Abort();
                listenThread.Join();
                listenThread = null;
            }
        }

        /// <summary>
        /// Accepts notifications from the MasterServer and status display
        /// </summary>
        /// <param name="notification">Notification message</param>
        /// <param name="info">Notification data</param>
        public void Notify(string notification, params string[] info)
        {
            switch (notification)
            {
                case "LOG":
                    Log(info[0]);
                    break;
            }
        }

        /// <summary>
        /// Print a log line to the interface
        /// </summary>
        /// <param name="text"></param>
        private void Log(string text)
        {
            if (connection != null)
            {
                connection.Send("\r" + text + "\r\n");
                connection.Display();
            }
        }

        /// <summary>
        /// Print current server status to the interface
        /// </summary>
        private void ShowStatus()
        {
            MasterServer masterServer = MasterServer.Instance;

            if (connection != null && masterServer != null)
            {
                List<Server> servers = masterServer.ServerList.Query();

                // Calculate number of local servers
                int localServerCount = 0;
                foreach (Server server in servers)
                    if (server.Local) localServerCount++;

                connection.Send("\r\nMaster Server Status\r\n");
                connection.Send(new String('-', 80) + "\r\n");
                connection.Send(String.Format("Active Connections:    {0,8}\r\n", localServerCount));
                connection.Send(String.Format("Total Servers:         {0,8}\r\n", servers.Count));
                connection.Send(String.Format("Total Inbound Queries: {0,8}\r\n", masterServer.TotalQueries));
                connection.Send(String.Format("Total Web Queries:     {0,8}\r\n", masterServer.TotalWebQueries));

                connection.Send("\r\nServer List\r\n");
                connection.Send(new String('-', 80) + "\r\n");

                // Server list
                if (servers.Count > 0)
                {
                    for (int serverIndex = 0; serverIndex < servers.Count; serverIndex++)
                    {
                        Server server = servers[serverIndex];

                        connection.Send(String.Format("{0,-3}{1,-30}{2,-18}{3,-8}{4,-11}{5,-10}\r\n",
                            server.Selected ? ">>" : "",
                            ColourCodeParser.StripColourCodes(server.Name),
                            server.Address.ToString(),
                            server.Port > 0 ? server.Port.ToString() : "?",
                            server.LastUpdate.ToString("HH:mm:ss"),
                            server.Local ? "Local" : "RPC"
                        ));
                    }
                }
                else
                {
                    connection.Send("   No connected servers\r\n");
                }

                connection.Send(new String('-', 80) + "\r\n\r\n");
            }
        }

        /// <summary>
        /// Display the interface, not used
        /// </summary>
        public void Display()
        {
        }

        /// <summary>
        /// Raises the OnChange event
        /// </summary>
        public void OnChanged()
        {
            EventHandler onChange = this.OnChange;

            if (onChange != null)
            {
                onChange(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handle a console command
        /// </summary>
        /// <param name="command"></param>
        public void Command(string[] command)
        {
            if (command.Length > 0)
            {
                switch (command[0].ToLower())
                {
                    case "telnet":
                        if (command.Length > 1)
                        {
                            switch (command[1].ToLower())
                            {
                                case "pass":
                                    if (command.Length > 2)
                                    {
                                        MasterServer.Settings.TelnetPassword = String.Join(" ", command, 2, command.Length - 2);
                                        MasterServer.Settings.Save();

                                        if (MasterServer.Settings.TelnetPassword.Length < 8)
                                        {
                                            MasterServer.LogMessage("[TELNET] WARNING! Telnet password is less than 8 characters!");
                                        }

                                        MasterServer.LogMessage("[TELNET] Telnet password updated");
                                    }
                                    else
                                    {
                                        MasterServer.LogMessage("telnet pass <password>");
                                    }
                                    break;

                                case "port":
                                    if (command.Length > 2)
                                    {
                                        ushort portNumber = 0;

                                        if (ushort.TryParse(command[2], out portNumber))
                                        {
                                            MasterServer.Settings.TelnetPort = portNumber;
                                            MasterServer.Settings.Save();

                                            MasterServer.LogMessage("[TELNET] Port number updated, restart the master server to apply changes");
                                        }
                                        else
                                        {
                                            MasterServer.LogMessage("[TELNET] Invalid port number specified");
                                        }
                                    }
                                    else
                                    {
                                        MasterServer.LogMessage("telnet port <number>");
                                    }

                                    MasterServer.LogMessage("[TELNET] Port={0}", MasterServer.Settings.TelnetPort);
                                    break;
                            }
                        }
                        else
                        {
                            MasterServer.LogMessage("telnet pass   Set the telnet password");
                            MasterServer.LogMessage("telnet port   Set the telnet port (requires restart)");
                        }
                        break;

                    case "status":
                        ShowStatus();
                        break;

                    case "tail":
                        if (MasterServer.Instance != null)
                        {
                            MasterServer.Instance.TailLog();
                        }
                        break;

                    case "quit":
                    case "exit":
                    case "logout":
                        if (connection != null)
                        {
                            connection.Shutdown();
                        }
                        break;

                    case "help":
                    case "?":
                        MasterServer.LogMessage("status        Displays server status");
                        MasterServer.LogMessage("tail          Displays the log tail");
                        MasterServer.LogMessage("telnet        Telnet server commands");
                        MasterServer.LogMessage("logout        Close the telnet connection");
                        break;
                }
            }
        }
    }
}
