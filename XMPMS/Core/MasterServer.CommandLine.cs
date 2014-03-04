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
using XMPMS.Properties;
using XMPMS.Interfaces;

namespace XMPMS.Core
{
    /// <summary>
    /// Console Command-line parsing support for MasterServer
    /// </summary>
    partial class MasterServer : ICommandListener
    {
        /// <summary>
        /// Callback from the module manager when a console command is issued
        /// </summary>
        /// <param name="command"></param>
        public void Command(string[] command)
        {
            if (command.Length > 0 && command[0].Trim() != "")
            {
                if (commandInterface == null || commandInterface.EchoCommands)
                {
                    LogCommand(command);
                }

                switch (command[0].ToLower())
                {
                    case "stop":
                        BeginStop();
                        break;

                    case "ver":
                        MasterServer.LogMessage("[INFO] Application  : {0}", MasterServer.Title);
                        MasterServer.LogMessage("[INFO] Version      : {0}", MasterServer.Version);
                        MasterServer.LogMessage("[INFO] NetVersion   : {0}", MasterServer.NetVersion);
                        MasterServer.LogMessage("[INFO] Copyright    : {0}", MasterServer.Copyright);
                        MasterServer.LogMessage("[INFO] Legal Notice : Unreal and the Unreal logo are registered trademarks of Epic");
                        MasterServer.LogMessage("                      Games, Inc. ALL RIGHTS RESERVED.");
                        break;

                    case "clear":
                    case "cls":
                        log.Clear();

                        if (commandInterface != null)
                        {
                            commandInterface.Notify("LOG", "\u001B[2J");
                        }
                        break;

                    case "ls":
                        MasterServer.LogMessage("List what?");
                        break;

                    case "dir":
                        MasterServer.LogMessage("This isn't DOS...");
                        break;

                    case "motd":
                        if (command.Length > 1)
                        {
                            string locale = command[1].ToLower();

                            if (command.Length > 2)
                            {
                                if (!MasterServer.SetMOTD(locale, String.Join(" ", command, 2, command.Length - 2)))
                                {
                                    MasterServer.LogMessage("[MOTD] Error, locale \"{0}\" is not defined. MOTD was not updated.", locale);
                                }
                            }

                            MasterServer.LogMessage("[MOTD] {0} = \"{1}\"", locale, MasterServer.GetMOTD(locale, false));
                        }
                        else
                        {
                            MasterServer.LogMessage("motd <locale> <message>");
                        }

                        break;

                    case "stat":
                        if (command.Length > 1)
                        {
                            switch (command[1].ToLower())
                            {
                                case "clear":
                                    MasterServer.Log("Total Queries = {0}", TotalQueries);
                                    MasterServer.Log("Total Web Queries = {0}", TotalWebQueries);

                                    Stats.Default.TotalQueries = 0;
                                    Stats.Default.TotalWebQueries = 0;
                                    Stats.Default.Save();

                                    MasterServer.Log("Stats cleared");
                                    break;
                            }
                        }
                        else
                        {
                            MasterServer.LogMessage("stat clear    Clear statistics");
                        }
                        break;

                    case "log":
                        if (command.Length > 1)
                        {
                            switch (command[1].ToLower())
                            {
                                case "clear":
                                    log.Clear();
                                    break;

                                case "commit":
                                    if (logWriter != null)
                                        logWriter.Commit();
                                    break;
                            }
                        }
                        else
                        {
                            MasterServer.LogMessage("log clear     Clear log buffer");
                            MasterServer.LogMessage("log commit    Commit unsaved log");
                        }
                        break;

                    case "mslist":
                        if (command.Length > 1)
                        {
                            switch (command[1].ToLower())
                            {
                                case "on":
                                    MasterServer.Settings.MSListEnabled = true;
                                    MasterServer.Settings.Save();
                                    MasterServer.LogMessage("MSLIST function turned ON");
                                    break;

                                case "off":
                                    MasterServer.Settings.MSListEnabled = false;
                                    MasterServer.Settings.Save();
                                    MasterServer.LogMessage("MSLIST function turned OFF");
                                    break;

                                case "add":
                                    if (command.Length > 3)
                                    {
                                        ushort portNumber = 0;

                                        if (ushort.TryParse(command[3], out portNumber))
                                        {
                                        }
                                        else
                                        {
                                            MasterServer.LogMessage("Invalid port number specified");
                                        }
                                    }
                                    else
                                    {
                                        MasterServer.LogMessage("mslist add <host> <port>");
                                    }
                                    break;

                                case "port":
                                    if (command.Length > 2)
                                    {
                                        ushort portNumber = 0;

                                        if (ushort.TryParse(command[2], out portNumber))
                                        {
                                            if (MasterServer.Settings.MSListInterfaces == null)
                                                MasterServer.Settings.MSListInterfaces = new List<ushort>();

                                            if (MasterServer.Settings.MSListInterfaces.Contains(portNumber))
                                            {
                                                if (MasterServer.Settings.ListenPorts.Contains(portNumber))
                                                {
                                                    MasterServer.Settings.MSListInterfaces.Remove(portNumber);
                                                    MasterServer.Settings.Save();
                                                }
                                                else
                                                {
                                                    MasterServer.LogMessage("Error adding MSLIST port, the specified port is not bound");
                                                }
                                            }
                                            else
                                            {
                                                MasterServer.Settings.MSListInterfaces.Add(portNumber);
                                                MasterServer.Settings.Save();
                                            }

                                            break;
                                        }
                                        else
                                        {
                                            MasterServer.LogMessage("Invalid port number specified");
                                        }
                                    }
                                    else
                                    {
                                        MasterServer.LogMessage("mslist port <port>");

                                        List<string> boundPorts = new List<string>();

                                        if (MasterServer.Settings.MSListInterfaces != null)
                                        {
                                            foreach (ushort port in MasterServer.Settings.MSListInterfaces)
                                                boundPorts.Add(port.ToString());
                                        }

                                        MasterServer.LogMessage("Current MSLIST port bindings: {0}", String.Join(",", boundPorts.ToArray()));
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            MasterServer.LogMessage("--------------------------------");
                            MasterServer.LogMessage("MSLIST function is currently {0}", MasterServer.Settings.MSListEnabled ? "ON" : "OFF");
                            MasterServer.LogMessage("--------------------------------");
                            MasterServer.LogMessage("mslist on     Turn MSLIST on");
                            MasterServer.LogMessage("mslist off    Turn MSLIST off");
                            MasterServer.LogMessage("mslist port   Set MSLIST ports");
                            MasterServer.LogMessage("mslist add    Add MSLIST entries");
                            MasterServer.LogMessage("mslist remove Remove MSLIST entries");
                        }
                        break;

                    case "port":

                        if (command.Length > 1 && (command[1].ToLower() == "bind" || command[1].ToLower() == "unbind"))
                        {
                            if (command.Length > 2)
                            {
                                ushort portNumber = 0;

                                if (ushort.TryParse(command[2], out portNumber))
                                {
                                    switch (command[1].ToLower())
                                    {
                                        case "bind":
                                            if (!MasterServer.Settings.ListenPorts.Contains(portNumber))
                                            {
                                                MasterServer.Settings.ListenPorts.Add(portNumber);
                                                MasterServer.Settings.Save();
                                            }

                                            Bind(portNumber);
                                            break;

                                        case "unbind":
                                            if (MasterServer.Settings.ListenPorts.Contains(portNumber))
                                            {
                                                MasterServer.Settings.ListenPorts.Remove(portNumber);
                                                MasterServer.Settings.Save();
                                            }

                                            if (MasterServer.Settings.MSListInterfaces != null && MasterServer.Settings.MSListInterfaces.Contains(portNumber))
                                            {
                                                MasterServer.Settings.MSListInterfaces.Remove(portNumber);
                                                MasterServer.Settings.Save();
                                            }

                                            UnBind(portNumber);
                                            break;
                                    }
                                }
                                else
                                {
                                    MasterServer.LogMessage("[NET] Invalid port number specified");
                                }
                            }
                            else
                            {
                                MasterServer.LogMessage("port {0} <port>", command[1]);
                                MasterServer.LogMessage("Current listen ports: {0}", MasterServer.ListenPorts);
                            }
                        }
                        else
                        {
                            MasterServer.LogMessage("port bind     Bind a new listen port");
                            MasterServer.LogMessage("port unbind   Unbind a listen port");
                            MasterServer.LogMessage("Current listen ports: {0}", MasterServer.ListenPorts);
                        }
                        break;

                    case "help":
                    case "?":
                        MasterServer.LogMessage("help          Displays this message");
                        MasterServer.LogMessage("stop          Gracefully stops the master server");
                        MasterServer.LogMessage("motd          Set the Message of the Day (MOTD)");
                        MasterServer.LogMessage("stat          Statistics commands");
                        MasterServer.LogMessage("log           Server log commands");

                        break;
                }
            }
        }
    }
}
