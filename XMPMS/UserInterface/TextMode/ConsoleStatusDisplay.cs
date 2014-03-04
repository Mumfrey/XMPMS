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
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;
using XMPMS.Core;
using XMPMS.Util;
using XMPMS.Interfaces;
using XMPMS.Web;

namespace XMPMS.UserInterface.TextMode
{
    /// <summary>
    /// Status display implementation for console
    /// </summary>
    public class ConsoleStatusDisplay : IStatusDisplay, ICommandListener
    {
        /// <summary>
        /// Flag which indicates the object is shutting down
        /// </summary>
        private volatile bool shutdown = false;

        /// <summary>
        /// Current scroll position in the server list
        /// </summary>
        private int listPos = 0;

        /// <summary>
        /// Number of visible servers per "page"
        /// </summary>
        private int listCount = 5;

        /// <summary>
        /// Width of the console
        /// </summary>
        private int consoleWidth = 80;

        /// <summary>
        /// Height to set console window
        /// </summary>
        private int consoleHeight = 25;

        /// <summary>
        /// Console background colour
        /// </summary>
        private static ConsoleColor BackgroundColour = ConsoleColor.Black;

        /// <summary>
        /// Console foreground colour
        /// </summary>
        private static ConsoleColor ForegroundColour = ConsoleColor.White;

        /// <summary>
        /// Console highlight colour
        /// </summary>
        private static ConsoleColor HighlightColour = ConsoleColor.Gray;

        /// <summary>
        /// Console log colour
        /// </summary>
        private static ConsoleColor LogColour = ConsoleColor.Gray;

        /// <summary>
        /// Listen port list
        /// </summary>
        private string ListenPorts
        {
            get { return MasterServer.ListenPorts.Length > Console.WindowWidth - 64 ? MasterServer.ListenPorts.Substring(0, Console.WindowWidth - 67) + "..." : MasterServer.ListenPorts; }
        }

        /// <summary>
        /// IMasterServerModule Interface
        /// </summary>
        public bool AutoLoad
        {
            get { return false; }
        }

        /// <summary>
        /// Size of log buffer to keep
        /// </summary>
        public int LogBufferSize
        {
            get { return consoleHeight; }
        }

        /// <summary>
        /// Maximum displayLength of a log line
        /// </summary>
        public int LogBufferWrap
        {
            get { return consoleWidth; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ConsoleStatusDisplay()
        {
        }

        /// <summary>
        /// IMasterServerModule Interface
        /// </summary>
        /// <param name="masterServer"></param>
        public void Initialise(MasterServer masterServer)
        {
            consoleWidth = MasterServer.Settings.ConsoleWidth;
            consoleHeight = MasterServer.Settings.ConsoleHeight;

            SetConsoleColours(ForegroundColour, BackgroundColour);
            Console.Clear();
            SetupConsole();

            shutdown = false;

            SetConsoleColours(ForegroundColour, BackgroundColour);

            Console.WriteLine();
            Console.WriteLine("{0} version {1}", MasterServer.Title, MasterServer.Version);
            Console.WriteLine("{0} version {1}", "Using network lib", MasterServer.NetVersion);
            Console.WriteLine("{0}\n", MasterServer.Copyright);
            Console.WriteLine("Unreal and the Unreal logo are registered trademarks of Epic\nGames, Inc. ALL RIGHTS RESERVED.");
            Console.WriteLine("Master Server starting up...\n");

            AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(CurrentDomain_AssemblyLoad);
        }

        /// <summary>
        /// Shut down the status display
        /// </summary>
        public void Shutdown()
        {
            shutdown = true;

            Console.WriteLine("\n\nMaster Server shutting down...");
            AppDomain.CurrentDomain.AssemblyLoad -= new AssemblyLoadEventHandler(CurrentDomain_AssemblyLoad);

            Console.ResetColor();
        }

        /// <summary>
        /// Set console parameters to suit the status display requirements
        /// </summary>
        void SetupConsole()
        {
            try
            {
                Console.Title = MasterServer.Title;
                Console.SetWindowSize(consoleWidth, consoleHeight);
                Console.SetBufferSize(consoleWidth, consoleHeight);

                listCount = Math.Max((consoleHeight - 7) / 3, 5);
            }
            catch { }
        }

        /// <summary>
        /// Set console foreground and background colours
        /// </summary>
        /// <param name="foreground">New foreground colour</param>
        /// <param name="background">New background colour</param>
        void SetConsoleColours(ConsoleColor foreground, ConsoleColor background)
        {
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
        }

        /// <summary>
        /// Set console foreground colour and set background to black
        /// </summary>
        /// <param name="foreground">New foreground colour</param>
        void Normal(ConsoleColor foreground)
        {
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = BackgroundColour;
        }

        /// <summary>
        /// Set console background colour and set foreground to black
        /// </summary>
        /// <param name="background">New background colour</param>
        void Reverse(ConsoleColor background)
        {
            Console.ForegroundColor = BackgroundColour;
            Console.BackgroundColor = background;
        }

        /// <summary>
        /// Callback function for when an assembly is loaded, so we can show it in the log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            string loadedDll = Path.GetFileName(args.LoadedAssembly.Location);
            if (loadedDll == "") loadedDll = args.LoadedAssembly.GetName().Name;

            MasterServer.Log("Loaded {0}", loadedDll);
            Console.WriteLine("Loaded {0}", loadedDll);
        }

        /// <summary>
        /// Display the current status
        /// </summary>
        /// <param name="masterServer"></param>
        /// <param name="log"></param>
        /// <param name="upTime"></param>
        public void UpdateDisplay(MasterServer masterServer, string[] log, TimeSpan upTime)
        {
            if (shutdown || masterServer == null || masterServer.ServerList == null) return;

            // Query for all servers
            List<Server> servers = masterServer.ServerList.Query();

            // Calculate number of local servers
            int localServerCount = 0;
            foreach (Server server in servers)
                if (server.Local) localServerCount++;

            // Set up format strings to size of console
            string widthFormat1 = String.Format("{0}0,-{1}{2}", "{", Console.WindowWidth, "}");
            string widthFormat2 = String.Empty;
            
            if (Console.WindowWidth < 88) widthFormat2 = String.Format("{0}5,-{1}{2}", "{0,-3}{1,-30}{2,-18}{3,-8}{4,-11}{", Console.WindowWidth - 70, "}");
            else widthFormat2 = String.Format("{0}7,-{1}{2}", "{0,-3}{1,-30}{2,-18}{3,-8}{4,-11}{5,-8}{6,2}/{", Console.WindowWidth - 81, "}");

            // Hide cursor whilst updating the screen
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, 0);

            // Banner
            Reverse(HighlightColour);
            Console.Write(widthFormat1, String.Format("{0,-32} TCP Ports: {1}   Web Server: {2}", MasterServer.Title, ListenPorts, WebServer.ListenPorts));

            // Server information
            Normal(ForegroundColour);
            Console.Write(widthFormat1, String.Format("   Server uptime: {0} days {1} hours {2} minutes", upTime.Days, upTime.Hours, upTime.Minutes));
            Console.Write(widthFormat1, String.Format("   Active Connections:    {0,8}          Total Servers:     {1,8}", localServerCount, servers.Count));
            Console.Write(widthFormat1, String.Format("   {0,-23}{1,8}{2,10}{3,-19}{4,8}", "Total Inbound Queries:", masterServer.TotalQueries, "", "Total Web Queries:", masterServer.TotalWebQueries));

            // Server list header
            Reverse(HighlightColour);
            Console.Write(widthFormat1, "*  Name                          IP                Port    Updated    Type      ");

            // Background colour for server list
            Normal(ForegroundColour);

            // Clamp list position within server list size
            while (listPos > servers.Count && listPos > 0)
                listPos -= listCount;

            // Server list
            if (servers.Count > 0)
            {
                for (int serverIndex = listPos; serverIndex < listPos + listCount; serverIndex++)
                {
                    if (serverIndex < servers.Count)
                    {
                        Server server = servers[serverIndex];
                        string serverName = ColourCodeParser.StripColourCodes(server.Name);
                        if (serverName.Length > 28) serverName = serverName.Substring(0, 28);

                        Console.Write(
                            widthFormat2,
                            server.Selected ? ">>" : "",
                            ColourCodeParser.StripColourCodes(server.Name),
                            server.Address.ToString(),
                            server.Port > 0 ? server.Port.ToString() : "?",
                            server.LastUpdate.ToString("HH:mm:ss"),
                            server.Local ? "Local" : "RPC",
                            server.CurrentPlayers,
                            server.MaxPlayers,
                            server.GameType
                        );
                    }
                    else
                    {
                        // Pad the server list with blank rows
                        Console.Write(widthFormat1, "");
                    }
                }
            }
            else
            {
                Console.Write(widthFormat1, "   No connected servers");

                for (int serverIndex = 1; serverIndex < listCount; serverIndex++)
                    Console.Write(widthFormat1, "");
            }

            Reverse(HighlightColour);
            Console.Write(widthFormat1, "Master Server Console");
            Normal(LogColour);

            // Display log lines using remaining space
            int remainingLines = Console.BufferHeight - Console.CursorTop - 1;
            for (int i = log.Length - remainingLines; i < log.Length; i++)
                Console.Write(widthFormat1, i >= 0 ? log[i].Substring(0, Math.Min(log[i].Length, Console.BufferWidth)) : "");

            Normal(ForegroundColour);
            Console.CursorVisible = true;
        }

        /// <summary>
        /// NOTIFY messages are passed from the CommandInterface to the StatusDisplay
        /// </summary>
        /// <param name="notification">Notification instruction</param>
        /// <param name="info">Additional arguments</param>
        public void Notify(string notification, params string[] info)
        {
            switch (notification)
            {
                case "EXIT":
                    //Shutdown();
                    break;

                case "PAGEUP":
                    listPos = Math.Max(0, listPos - listCount);
                    break;

                case "PAGEDOWN":
                    listPos += listCount;
                    break;
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
                    case "mode":
                        if (command.Length > 1)
                        {
                            Match modeMatch = Regex.Match(command[1], @"^(?<w>[0-9]+),(?<h>[0-9]+)$");

                            if (modeMatch.Success)
                            {
                                consoleWidth = Math.Min(Math.Max(int.Parse(modeMatch.Groups["w"].Value), 80), 160);
                                consoleHeight = Math.Min(Math.Max(int.Parse(modeMatch.Groups["h"].Value), 25), 200);
                                SetupConsole();

                                MasterServer.Settings.ConsoleWidth = consoleWidth;
                                MasterServer.Settings.ConsoleHeight = consoleHeight;
                                MasterServer.Settings.Save();
                            }
                        }
                        else
                        {
                            MasterServer.LogMessage("mode <width>,<height>");
                        }
                        break;

                    case "quit":
                    case "exit":
                        MasterServer.BeginStop();
                        break;

                    case "restart":
                        MasterServer.BeginRestart();
                        break;

                    case "?":
                    case "help":
                        MasterServer.LogMessage("restart       Restart the master server");
                        MasterServer.LogMessage("mode          Console mode commands");
                        break;
                }
            }
        }
    }
}
