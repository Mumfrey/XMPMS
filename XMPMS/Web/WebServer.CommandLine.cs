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
using XMPMS.Interfaces;
using XMPMS.Core;

namespace XMPMS.Web
{
    /// <summary>
    /// Console Command-line parsing support for MasterServer
    /// </summary>
    partial class WebServer : ICommandListener
    {
        public void Command(string[] command)
        {
            if (command.Length > 0 && command[0].Trim() != "")
            {
                switch (command[0].ToLower())
                {
                    case "web":
                        if (command.Length > 1)
                        {
                            switch (command[1].ToLower())
                            {
                                case "stop":
                                    EndListening();
                                    break;

                                case "start":
                                    BeginListening();
                                    break;

                                case "restart":
                                    Restart();
                                    break;

                                case "port":
                                    if (command.Length > 2)
                                    {
                                        ushort portNumber = 0;

                                        if (ushort.TryParse(command[2], out portNumber))
                                        {
                                            MasterServer.Settings.WebServerListenPort = portNumber;
                                            MasterServer.Settings.Save();
                                        }
                                        else
                                        {
                                            MasterServer.LogMessage("[WEB] Invalid port number specified");
                                        }
                                    }
                                    else
                                    {
                                        MasterServer.LogMessage("web port <number>");
                                    }

                                    MasterServer.LogMessage("[WEB] Listenport={0}", MasterServer.Settings.WebServerListenPort);
                                    break;

                                case "skin":
                                    if (command.Length > 2 && command[2] != "")
                                    {
                                        MasterServer.Settings.WebServerSkinFolder = command[2];
                                        MasterServer.Settings.Save();
                                    }
                                    else
                                    {
                                        MasterServer.LogMessage("web skin <name>");
                                    }

                                    MasterServer.LogMessage("[WEB] Skin={0}", MasterServer.Settings.WebServerSkinFolder);

                                    break;

                                case "list":
                                    ListHostnames();
                                    break;

                                case "add":
                                    if (command.Length > 2)
                                    {
                                        if (!MasterServer.Settings.WebServerListenAddresses.Contains(command[2]))
                                        {
                                            MasterServer.Settings.WebServerListenAddresses.Add(command[2]);
                                            MasterServer.Settings.Save();
                                            ListHostnames();
                                        }
                                    }
                                    else
                                    {
                                        MasterServer.LogMessage("web add <host>");
                                    }
                                    break;

                                case "remove":
                                    if (command.Length > 2)
                                    {
                                        MasterServer.Settings.WebServerListenAddresses.Remove(command[2]);
                                        MasterServer.Settings.Save();
                                        ListHostnames();
                                    }
                                    else
                                    {
                                        MasterServer.LogMessage("web remove <host>");
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            MasterServer.LogMessage("web stop      Stop web server");
                            MasterServer.LogMessage("web start     Start web server");
                            MasterServer.LogMessage("web restart   Retart web server");
                            MasterServer.LogMessage("web port      Set web server port");
                            MasterServer.LogMessage("web skin      Set web server skin");
                            MasterServer.LogMessage("web list      List host names");
                            MasterServer.LogMessage("web add       Add host name");
                            MasterServer.LogMessage("web remove    Remove host name");
                        }
                        break;

                    case "help":
                    case "?":
                        MasterServer.LogMessage("web           Web server commands");
                        break;
                }
            }
        }

        /// <summary>
        /// Restart the web server, allows changes to be applied on the fly
        /// </summary>
        private void Restart()
        {
            MasterServer.Log("Web server is restarting...");
            EndListening();
            BeginListening();
        }

        /// <summary>
        /// List the current web server host names to the console
        /// </summary>
        private void ListHostnames()
        {
            MasterServer.LogMessage("Configured host names:");

            foreach (string hostName in MasterServer.Settings.WebServerListenAddresses)
                MasterServer.LogMessage("  {0}", hostName);
        }
    }
}
