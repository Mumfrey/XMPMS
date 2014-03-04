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
using XMPMS.Interfaces;
using XMPMS.Net.WCF;

namespace XMPMS.Core
{
    /// <summary>
    /// Console Command-line parsing support for ServerList
    /// </summary>
    partial class ServerList : ICommandListener
    {
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
                    case "client": ClientCommand(command, GetSelectedServer()); break;
                    case "select": SelectCommand(command);                      break;
                    case "sel":    SelectCommand(command);                      break;
                    case "s":      SelectCommand(command);                      break;
                    case "info":   InfoCommand(command);                        break;
                    case "poke":   PokeCommand(command);                        break;
                    case "get":    GetCommand(command, GetSelectedServer());    break;
                    case "set":    SetCommand(command, GetSelectedServer());    break;
#if WCF
                    case "link": LinkCommand(command); break;
#endif
                    case "help":
                    case "?":
                        MasterServer.LogMessage("link          Administer remote server links");
                        MasterServer.LogMessage("select        Select a server to command");
                        MasterServer.LogMessage("client        Server client commands");
                        MasterServer.LogMessage("info          Show information about the selected server");
                        MasterServer.LogMessage("get           Get a remote INI file setting value");
                        MasterServer.LogMessage("set           Set a remote file setting value");
                        break;
                }
            }
        }

        /// <summary>
        /// Handle a server selection command
        /// </summary>
        /// <param name="command"></param>
        protected void SelectCommand(string[] command)
        {
            if (command.Length > 1)
            {
                SelectServer(command[1]);
            }
            else
            {
                if (servers.Count == 1)
                {
                    servers[0].Selected = true;
                    MasterServer.LogMessage("Selected server [{0}]", servers[0]);
                }
            }
        }

        /// <summary>
        /// Handle a "poke" command
        /// </summary>
        /// <param name="command"></param>
        protected void PokeCommand(string[] command)
        {
            Server server = GetSelectedServer();

            if (server != null)
            {
                server.Connection.Poke(command);
            }
        }

        /// <summary>
        /// Handle an "info" command
        /// </summary>
        /// <param name="command"></param>
        protected void InfoCommand(string[] command)
        {
            Server server = GetSelectedServer();

            if (server != null)
            {
                server.Print();
            }
        }

        /// <summary>
        /// Handle a "link" command
        /// </summary>
        /// <param name="command"></param>
        protected void LinkCommand(string[] command)
        {
            if (command.Length < 2)
            {
                MasterServer.LogMessage("link list     Displays current remote links");
                MasterServer.LogMessage("link user     Set username password for this server");
                MasterServer.LogMessage("link add      Add a new master server link");
                MasterServer.LogMessage("link remove   Remove a master server link");
                MasterServer.LogMessage("link test     Test a master server link");
                return;
            }

            switch (command[1].ToLower())
            {
#if !WCF
#warning "WCF functionality disabled - ServerList RPC will not function"
#else
                case "list":
                    MasterServer.LogMessage("Configured links:");

                    int serverIndex = 0;

                    foreach (RemoteMasterServer remoteMaster in remoteMasters)
                    {
                        MasterServer.LogMessage("{0,2} -> {1}", serverIndex++, remoteMaster.InnerChannel.RemoteAddress.Uri.ToString());
                    }
                    break;
#endif
                case "user":
                    if (command.Length > 3)
                    {
                        MasterServer.Settings.SyncServiceUsername = command[2];
                        MasterServer.Settings.SyncServicePassword = command[3];
                        MasterServer.Settings.Save();

                        MasterServer.Log("[RPC] Local user/pass updated");
                    }
                    else
                    {
                        MasterServer.LogMessage("link user <user> <pass>");
                    }
                    break;

                case "add":
                    if (command.Length > 7)
                    {
                        ushort port = 0;

                        if (ushort.TryParse(command[3], out port) && port > 0)
                        {
                            if (AddLink(command[2], port, command[4], command[5], command[6], command[7]))
                                MasterServer.Log("[RPC] Add link succeeded");
                            else
                                MasterServer.LogMessage("[RPC] Add link failed");
                        }
                        else
                        {
                            MasterServer.LogMessage("Error: port must be a valid number");
                            MasterServer.LogMessage("link add <host> <port> <endpoint> <svc> <user> <pass>");
                        }
                    }
                    else
                    {
                        MasterServer.LogMessage("link add <host> <port> <endpoint> <svc> <user> <pass>");
                    }
                    break;

                case "remove":
                    if (command.Length > 2)
                    {
                        int index = 0;

                        if (int.TryParse(command[2], out index) && index >= 0 && index < remoteMasters.Count)
                        {
                            if (RemoveLink(index))
                                MasterServer.Log("[RPC] Remove link succeeded");
                            else
                                MasterServer.LogMessage("[RPC] Remove link failed");
                        }
                        else
                        {
                            MasterServer.LogMessage("Error: index must be a valid index number");
                        }
                    }
                    else
                    {
                        MasterServer.LogMessage("link remove <index>");
                        MasterServer.LogMessage("Hint: use \"link list\" to determine link index");
                    }
                    break;

                case "test":
                    if (command.Length > 2)
                    {
                        int index = 0;

                        if (int.TryParse(command[2], out index) && index >= 0 && index < remoteMasters.Count)
                        {
                            TestLink(index);
                        }
                        else
                        {
                            MasterServer.LogMessage("Error: index must be a valid index number");
                        }
                    }
                    else
                    {
                        MasterServer.LogMessage("link test <index>");
                        MasterServer.LogMessage("Hint: use \"link list\" to determine link index");
                    }
                    break;
            }
        }

        /// <summary>
        /// Handle a "client" command
        /// </summary>
        /// <param name="command"></param>
        protected void ClientCommand(string[] command, Server server)
        {
            if (command.Length < 2)
            {
                MasterServer.LogMessage("client list       Displays clients on the selected server");
                MasterServer.LogMessage("client challenge  Challenges the specified client");
                MasterServer.LogMessage("client kick       Disconnects a client from the server");
                return;
            }

            switch (command[1].ToLower())
            {
                case "list":
                    if (server != null)
                    {
                        server.ListClients();
                        return;
                    }
                    break;

                case "challenge":
                    if (command.Length > 2)
                    {
                        if (server != null)
                        {
                            server.ChallengeClient(command[2]);
                            return;
                        }
                    }
                    else
                    {
                        MasterServer.LogMessage("client challenge <address|*>");
                        MasterServer.LogMessage("Hint: use \"client list\" to determine client address");
                    }
                    break;

                case "kick":
                    if (command.Length > 2)
                    {
                        if (server != null)
                        {
                            server.DisconnectClient(command[2]);
                            return;
                        }
                    }
                    else
                    {
                        MasterServer.LogMessage("client kick <address>");
                        MasterServer.LogMessage("Hint: use \"client list\" to determine client address");
                    }
                    break;
            }
        }

        /// <summary>
        /// Handle a "get" command
        /// </summary>
        /// <param name="command"></param>
        protected void GetCommand(string[] command, Server server)
        {
            if (command.Length > 2)
            {
                if (server != null)
                {
                    server.CheckOption(command[1], command[2]);
                    return;
                }
            }
            else
            {
                MasterServer.LogMessage("get <package.section> <variable>");
            }
        }

        /// <summary>
        /// Handle a "get" command
        /// </summary>
        /// <param name="command"></param>
        protected void SetCommand(string[] command, Server server)
        {
            if (command.Length > 3)
            {
                if (server != null)
                {
                    string value = command[3];

                    for (int i = 4; i < command.Length; i++)
                        value += " " + command[i];

                    server.SetOption(command[1], command[2], value);
                    return;
                }
            }
            else
            {
                MasterServer.LogMessage("set <package.section> <variable> <value>");
            }
        }
    }
}
