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
using System.Net;
using System.Threading;
using System.Reflection;
using WinForms = System.Windows.Forms;
using XMPMS.Interfaces;
using XMPMS.Service;
using XMPMS.Properties;
using XMPMS.Util;
using XMPMS.Net.Listeners;
using XMPMS.Net.Connections;
using XMPMS.Web;
using XMPMS.UserInterface.GUI;
using XMPMS.UserInterface.TextMode;

namespace XMPMS.Core
{
    /// <summary>
    /// Master Server Main Class
    /// </summary>
    public partial class MasterServer
    {
        /// <summary>
        /// Singleton pattern
        /// </summary>
        private static MasterServer instance;

        /// <summary>
        /// Service object
        /// </summary>
        private static MasterServerService service;

        /// <summary>
        /// Master Server Settings class
        /// </summary>
        public static Settings Settings
        {
            get { return Settings.Default; }
        }

        /// <summary>
        /// Master Server MOTD Settings class
        /// </summary>
        private static MOTD MOTD
        {
            get { return MOTD.Default; }
        }

        /// <summary>
        /// Application title
        /// </summary>
        public static string Title
        {
            get;
            private set;
        }

        /// <summary>
        /// Application version
        /// </summary>
        public static string Version
        {
            get;
            private set;
        }

        /// <summary>
        /// Network library version
        /// </summary>
        public static string NetVersion
        {
            get;
            private set;
        }

        /// <summary>
        /// Application copyright info
        /// </summary>
        public static string Copyright
        {
            get;
            private set;
        }

        /// <summary>
        /// List of active listen ports as string
        /// </summary>
        public static string ListenPorts
        {
            get;
            private set;
        }

        /// <summary>
        /// Keeps track of connected and remote servers and allows the collection to be queried
        /// </summary>
        private ServerList serverList;

        /// <summary>
        /// Flag to indicate whether listeners are enabled
        /// </summary>
        private volatile bool listening = false;

        /// <summary>
        /// Query listeners listen for player and server queries on a TCP port
        /// </summary>
        private Dictionary<int, QueryListener> queryListeners = new Dictionary<int, QueryListener>();

        /// <summary>
        /// Heartbeat listener manages UDP heartbeats
        /// </summary>
        private Dictionary<int, HeartbeatListener> heartbeatListeners = new Dictionary<int, HeartbeatListener>();

        /// <summary>
        /// Supports the web-based interface (if enabled)
        /// </summary>
        private WebServer webServer;

        /// <summary>
        /// GeoIP resolver for identifying server locations
        /// </summary>
        private GeoIP geoIP;

        /// <summary>
        /// MD5 database
        /// </summary>
        private MD5Manager md5Manager;

        /// <summary>
        /// IP ban manager
        /// </summary>
        private IPBanManager banManager;

        /// <summary>
        /// CD Key validator module used to validate client and server CD keys
        /// </summary>
        private ICDKeyValidator cdKeyValidator;

        /// <summary>            
        /// Game Stats LogWriter object
        /// </summary>
        private IGameStatsLog gameStats;

        /// <summary>
        /// Time the master server was started (for measuring uptime)
        /// </summary>
        private DateTime startTime = DateTime.Now;

        /// <summary>
        /// LogWriter messages
        /// </summary>
        private static StringCollection log = new StringCollection();

        /// <summary>
        /// Maximum number of log messages to keep in memory
        /// </summary>
        private static int logBufferSize = 16;

        /// <summary>
        /// Maximum displayLength of log lines
        /// </summary>
        private static int logBufferWrap = 80;

        /// <summary>
        /// Thread lock to prevent concurrent threads accessing the log
        /// </summary>
        private static object logLock = new object();

        /// <summary>
        /// LogWriter writer
        /// </summary>
        private static ILogWriter logWriter;

        /// <summary>
        /// Thread lock to prevent concurrent threads accessing the console
        /// </summary>
        private static object displayLock = new object();

        /// <summary>
        /// Status display interface
        /// </summary>
        private IStatusDisplay statusDisplay;

        /// <summary>
        /// Command interface
        /// </summary>
        private ICommandInterface commandInterface;

        /// <summary>
        /// Status display update timer
        /// </summary>
        private Timer displayTimer;

        /// <summary>
        /// Display update interval (in milliseconds)
        /// </summary>
        private const int displayUpdateInterval = 500;

        /// <summary>
        /// Thread for performing shutdowns
        /// </summary>
        private static Thread shutdownThread;

        /// <summary>
        /// Access the singleton instance
        /// </summary>
        public static MasterServer Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// Get a reference to the current server list
        /// </summary>
        public ServerList ServerList
        {
            get { return serverList; }
        }

        /// <summary>
        /// Get a reference to the GeoIP resolver
        /// </summary>
        public GeoIP GeoIP
        {
            get { return geoIP; }
        }

        /// <summary>
        /// Get a reference to the MD5 database
        /// </summary>
        public MD5Manager MD5Manager
        {
            get { return md5Manager; }
        }

        /// <summary>
        /// Get a reference to the IP ban manager
        /// </summary>
        public IPBanManager BanManager
        {
            get { return banManager; }
        }

        /// <summary>
        /// Regex for matching settings values on the command line
        /// </summary>
        private static Regex commandLineSettingRegex = new Regex(@" (?<var>[a-z0-9]+)=(\x22(?<value>[^\x22]*)\x22|(?<value>[^\x20]+))", RegexOptions.IgnoreCase);

        /// <summary>
        /// Get the total query count since the last reset
        /// </summary>
        public int TotalQueries
        {
            get { return Stats.Default.TotalQueries; }
        }

        /// <summary>
        /// Get the total web query count since the last reset
        /// </summary>
        public int TotalWebQueries
        {
            get { return Stats.Default.TotalWebQueries; }
        }

        /// <summary>
        /// Static constructor, initialises application variables
        /// </summary>
        static MasterServer()
        {
            Title = (Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0] as AssemblyDescriptionAttribute).Description;
            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            NetVersion = Assembly.GetAssembly(typeof(XMPMS.Net.Protocol)).GetName().Version.ToString();
            Copyright = (Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0] as AssemblyCopyrightAttribute).Copyright;

            Console.WriteLine();
            Console.WriteLine("{0} version {1}", MasterServer.Title, MasterServer.Version);
            Console.WriteLine("{0} version {1}", "Using network lib", MasterServer.NetVersion);
            Console.WriteLine("{0}\n", MasterServer.Copyright);
            Console.WriteLine("Unreal and the Unreal logo are registered trademarks of Epic\nGames, Inc. ALL RIGHTS RESERVED.");
        }

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            // Migrate settings from previous version if available
            if (MasterServer.Settings.SettingsUpgradeRequired)
            {
                Console.WriteLine("Migrating application settings...");

                MasterServer.Settings.Upgrade();
                MasterServer.Settings.SettingsUpgradeRequired = false;
                MasterServer.Settings.Save();
            }

            // Bind default listen ports if not configured
            if (MasterServer.Settings.ListenPorts == null || MasterServer.Settings.ListenPorts.Count == 0)
            {
                Console.WriteLine("Configuring default listen port...");

                MasterServer.Settings.ListenPorts = new List<ushort>();
                MasterServer.Settings.ListenPorts.Add(Constants.DEFAULT_LISTEN_PORT);
                MasterServer.Settings.Save();
            }

            MasterServer.ListenPorts = "-";

            switch ((args.Length > 0) ? args[0] : null)
            {
                case "console":
                    MasterServer.ConsoleMain(args);
                    break;

                case "gui":
                    MasterServer.GUIMain(args);
                    break;

                case "install":
                    MasterServerService.InstallService();
                    break;

                case "uninstall":
                    MasterServerService.UninstallService();
                    break;

                default:
                    if (ConsoleUtilities.InConsoleSession())
                    {
                        MasterServer.ConsoleMain(args);
                    }
                    else
                    {
                        service = new MasterServerService();
                        MasterServerService.ServiceMain(service, args);
                    }

                    break;
            }
        }

        /// <summary>
        /// Handle application shutting down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            MasterServer.Log("ProcessExit()");
            ModuleManager.ReleaseAllModules();
        }

        /// <summary>
        /// Unhandled exceptions, try to gracefully stop the master server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MasterServer.Log("CRITICAL: {0}", (e.ExceptionObject as Exception).Message);
            MasterServer.Log("CRITICAL: {0}", (e.ExceptionObject as Exception).StackTrace);
            MasterServer.Stop();
        }

        /// <summary>
        /// Parse variable values specified on the command line
        /// </summary>
        /// <param name="commandLine">Parameters passed to the executable</param>
        public static void ParseCommandLineVars(string commandLine)
        {
            int pos = 0;
            Match argValue = commandLineSettingRegex.Match(commandLine, pos);

            while (argValue.Success)
            {
                if (MasterServer.Settings.SetProperty(argValue.Groups["var"].Value, argValue.Groups["value"].Value))
                {
                    Console.WriteLine("Property '{0}' set to '{1}'", argValue.Groups["var"].Value, argValue.Groups["value"].Value);
                }

                pos = argValue.Index + argValue.Length;
                argValue = commandLineSettingRegex.Match(commandLine, pos);
            }
        }

        /// <summary>
        /// Console Main function
        /// </summary>
        public static void ConsoleMain(string[] args)
        {
            IStatusDisplay statusDisplay = ModuleManager.GetModule<IStatusDisplay>(typeof(ConsoleStatusDisplay));
            ICommandInterface commandInterface = ModuleManager.GetModule<ICommandInterface>(typeof(ConsoleCommandLine));

            // Process the command-line options (allows settings overrides to be specified on the command line)
            ParseCommandLineVars(Environment.CommandLine);

            if (!Start(statusDisplay, commandInterface))
            {
                // Start was unsuccessful, release interface modules
                ModuleManager.ReleaseModule<IStatusDisplay>();
                ModuleManager.ReleaseModule<ICommandInterface>();
            }
            else
            {
                (commandInterface as ConsoleCommandLine).Run();
            }
        }

        /// <summary>
        /// GUI Main function
        /// </summary>
        public static void GUIMain(string[] args)
        {
            //ConsoleUtilities.HideConsoleWindow();
            frmStatus statusWindow = new frmStatus();

            // Process the command-line options (allows settings overrides to be specified on the command line)
            ParseCommandLineVars(Environment.CommandLine);

            if (Start(statusWindow, statusWindow))
            {
                statusWindow.Run();
                Stop();
            }

            //ConsoleUtilities.ShowConsoleWindow();
        }

        /// <summary>
        /// Start the server instance
        /// </summary>
        /// <param name="statusDisplay">Status display object to use (can be null)</param>
        /// <param name="commandInterface">Command interface to use (can be null)</param>
        /// <param name="cdKeyValidator">CD key validator to use</param>
        /// <param name="connectionLogWriter">LogWriter writer to use</param>
        public static bool Start(IStatusDisplay statusDisplay, ICommandInterface commandInterface)
        {
            if (instance == null)
            {
                IGameStatsLog gameStats;
                ICDKeyValidator cdKeyValidator;

                if (LoadConfiguredModules(out gameStats, out cdKeyValidator))
                {
                    log.Clear();
                    instance = new MasterServer(statusDisplay, commandInterface, cdKeyValidator, gameStats, logWriter);
                    instance.BeginListening();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Begin stopping the master server asynchronously if another shutdown is not in progress
        /// </summary>
        public static void BeginStop()
        {
            if (shutdownThread == null)
            {
                // Have to shut down in a different thread or it will all go horribly wrong when we close down THIS thread.
                shutdownThread = new Thread(new ThreadStart(Stop));
                shutdownThread.Start();
            }
        }

        /// <summary>
        /// Stop the server instance
        /// </summary>
        public static void Stop()
        {
            if (instance != null)
            {
                instance.Shutdown();
                instance = null;

                ReleaseModules();

                if (service != null)
                {
                    service.Stop();
                }
                else
                {
                    WinForms.Application.Exit();
                }
            }

            // Don't need this thread reference any more :)
            if (shutdownThread != null)
                shutdownThread = null;
        }

        /// <summary>
        /// Begin a restart of the master server asynchronously if another restart or shutdown is not in progress
        /// </summary>
        public static void BeginRestart()
        {
            if (shutdownThread == null)
            {
                // Have to restart in a different thread or it will all go horribly wrong when we close down THIS thread.
                shutdownThread = new Thread(new ThreadStart(Restart));
                shutdownThread.Start();
            }
        }

        /// <summary>
        /// Restart the master server instance
        /// </summary>
        public static void Restart()
        {
            if (instance != null)
            {
                if (service != null)
                {
                    MasterServer.LogMessage("Master server cannot be restarted in service mode, restart the service instead");
                }
                else if (instance.statusDisplay is ConsoleStatusDisplay && instance.commandInterface is ConsoleCommandLine)
                {
                    // Stop the server
                    instance.Shutdown();
                    instance = null;

                    // Release log writer, game stats, and validation modules
                    ReleaseModules();

                    // Clean up garbage
                    Console.WriteLine("Purging garbage...");
                    GC.Collect();

                    // Wait a couple of seconds to let any processes which haven't terminated yet finish closing
                    Console.WriteLine("Restarting master server...");
                    Thread.Sleep(2000);
                    Console.Clear();

                    // Get new status display and input modules
                    IStatusDisplay statusDisplay = ModuleManager.GetModule<IStatusDisplay>(typeof(ConsoleStatusDisplay));
                    ICommandInterface commandInterface = ModuleManager.GetModule<ICommandInterface>(typeof(ConsoleCommandLine));

                    // Start a new master server
                    Start(statusDisplay, commandInterface);
                }
            }

            if (shutdownThread != null)
            {
                shutdownThread = null;
            }
        }

        /// <summary>
        /// Get configured modules from the module manager
        /// </summary>
        /// <param name="gameStats">GameStats module will be returned in this variable</param>
        /// <param name="cdKeyValidator">CD Key Validator module will be returned in this variable</param>
        /// <returns>True if all modules were loaded correctly</returns>
        private static bool LoadConfiguredModules(out IGameStatsLog gameStats, out ICDKeyValidator cdKeyValidator)
        {
            Console.WriteLine("Initialising log writer module...");
            logWriter = ModuleManager.GetModule<ILogWriter>();

            ConsoleColor oldColour = Console.ForegroundColor;

            // Warn if the CD key validator module was not loaded
            if (logWriter == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Configuration error: the specified log writer module was not loaded");
                Console.ForegroundColor = oldColour;

                MasterServer.Log("Configuration error: the specified log writer module was not loaded");

                if (!ConsoleUtilities.InConsoleSession())
                {
                    WinForms.MessageBox.Show("Configuration error: the specified log writer module was not loaded", "Configuration error", WinForms.MessageBoxButtons.OK);
                }
            }

            Console.WriteLine("Initialising GameStats module...");
            gameStats = ModuleManager.GetModule<IGameStatsLog>();

            // Warn if the GameStats module was not loaded
            if (gameStats == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Configuration error: the specified gamestats module was not loaded");
                Console.ForegroundColor = oldColour;

                MasterServer.Log("Configuration error: the specified gamestats module was not loaded");

                if (!ConsoleUtilities.InConsoleSession())
                {
                    WinForms.MessageBox.Show("Configuration error: the specified gamestats module was not loaded", "Configuration error", WinForms.MessageBoxButtons.OK);
                }
            }

            Console.WriteLine("Initialising CD key validator module...");
            cdKeyValidator = ModuleManager.GetModule<ICDKeyValidator>();

            // Can't continue without a CD key validator module
            if (cdKeyValidator == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Configuration error: the specified CD key validator module was not loaded");
                Console.WriteLine("Critical. Master server shutting down");
                Console.ForegroundColor = oldColour;

                if (!ConsoleUtilities.InConsoleSession())
                {
                    WinForms.MessageBox.Show("Configuration error: the specified CD key validator module was not loaded", "Critical error", WinForms.MessageBoxButtons.OK);
                }

                ReleaseModules();
                return false;
            }

            Console.WriteLine();

            return true;
        }

        /// <summary>
        /// Release configured modules
        /// </summary>
        private static void ReleaseModules()
        {
            Console.WriteLine("Releasing GameStats module...");
            ModuleManager.ReleaseModule<IGameStatsLog>();

            Console.WriteLine("Releasing CD key validator module...");
            ModuleManager.ReleaseModule<ICDKeyValidator>();

            Console.WriteLine("Releasing log writer module...");
            ModuleManager.ReleaseModule<ILogWriter>();

            logWriter = null;
        }

        /// <summary>
        /// Master server constructor. Initialises child objects, helpers and listeners
        /// </summary>
        /// <param name="statusDisplay"></param>
        /// <param name="commandInterface"></param>
        private MasterServer(IStatusDisplay statusDisplay, ICommandInterface commandInterface, ICDKeyValidator cdKeyValidator, IGameStatsLog gameStats, ILogWriter logWriter)
        {
            if (MasterServer.instance != null)
            {
                throw new InvalidOperationException("Attempted to create a Master Server instance whilst another instance was still active");
            }

            // Assign static references
            MasterServer.instance  = this;
            MasterServer.logWriter = logWriter;

            // Assign instance references
            this.statusDisplay     = statusDisplay;
            this.commandInterface  = commandInterface;
            this.cdKeyValidator    = cdKeyValidator;
            this.gameStats         = gameStats;

            // Initialise the command interface if we have one
            if (commandInterface != null)
            {
                commandInterface.OnChange += new EventHandler(DisplayCommandInterface);
            }

            // GeoIP resolver is used to resolve IP addresses to locations
            geoIP = new GeoIP();

            // MD5 database is used to download new MD5 package data to connecting servers
            md5Manager = new MD5Manager();

            // IP ban manager used to ban clients from accessing the server
            banManager = new IPBanManager();

            // Create the Server List object
            serverList = new ServerList(this);

            // Create the web server
            webServer = new WebServer(banManager);

            // Initialise the status display module if we have one
            if (statusDisplay != null)
            {
                logBufferSize = statusDisplay.LogBufferSize;
                logBufferWrap = statusDisplay.LogBufferWrap;

                displayTimer = new Timer(new TimerCallback(this.Display));
                Display(null);
            }

            // Load the GeoIP database, MD5 database and ban list from the files (this happens last because they may take a while)
            geoIP.Load(MasterServer.Settings.GeoIPDataFile);
            md5Manager.Load(MasterServer.Settings.MD5DataFile);
            banManager.Load(MasterServer.Settings.BanListFile);
        }

        /// <summary>
        /// Shut down the master server instance
        /// </summary>
        private void Shutdown()
        {
            // End listen thread and close the listen sockets
            EndListening();

            // Web server
            if (webServer != null)
            {
                webServer.Dispose();
                webServer = null;
            }

            // Shut down the server list
            if (serverList != null)
            {
                serverList.Shutdown();
                serverList = null;
            }

            // Ban manager
            if (banManager != null)
            {
                banManager.Dispose();
                banManager = null;
            }

            // MD5 database
            if (md5Manager != null)
            {
                md5Manager.Dispose();
                md5Manager = null;
            }

            // GeoIP resolver
            if (geoIP != null)
            {
                geoIP.Dispose();
                geoIP = null;
            }

            // Display update timer
            if (displayTimer != null)
            {
                displayTimer.Change(Timeout.Infinite, Timeout.Infinite);
                displayTimer.Dispose();
                displayTimer = null;
            }

            // Shut down the status display if we have one
            if (statusDisplay != null)
            {
                statusDisplay.Notify("EXIT");
                ModuleManager.ReleaseModule<IStatusDisplay>();
                statusDisplay = null;
            }

            // Shut down the command interface if we have one
            if (commandInterface != null)
            {
                commandInterface.OnChange -= new EventHandler(DisplayCommandInterface);
                ModuleManager.ReleaseModule<ICommandInterface>();
                commandInterface = null;
            }
        }

        /// <summary>
        /// Begin listening on all configured network sockets
        /// </summary>
        private void BeginListening()
        {
            if (!listening)
            {
                listening = true;

                // Begin web server listening
                if (webServer != null) webServer.BeginListening();

                // Bind all configured listen ports
                foreach (ushort listenPort in MasterServer.Settings.ListenPorts)
                {
                    Bind(listenPort);
                }
            }
        }

        /// <summary>
        /// Terminate the listeners
        /// </summary>
        private void EndListening()
        {
            if (listening)
            {
                MasterServer.Log("Shutting down sockets...");
                listening = false;

                // Create list of active listen ports
                List<int> listenPorts = new List<int>(queryListeners.Keys);

                // Unbind listen ports in order
                foreach (int listenPort in listenPorts)
                {
                    UnBind(listenPort);
                }

                // Abort any remaining connections which were not terminated by the listeners (shouldn't be any but it's best to make sure!
                ConnectionManager.AbortAll();

                try
                {
                    if (webServer != null) webServer.EndListening();
                }
                catch { }
            }
        }

        /// <summary>
        /// Bind query and heartbeat listeners to the specified port
        /// </summary>
        /// <param name="listenPort">Port (TCP and UDP) to bind the listeners to</param>
        private void Bind(int listenPort)
        {
            if (queryListeners.ContainsKey(listenPort))
            {
                MasterServer.Log("[NET] Port {0} already bound", listenPort);
                return;
            }

            try
            {
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, listenPort);

                QueryListener queryListener = new QueryListener(endpoint, serverList, geoIP, md5Manager, banManager, cdKeyValidator, gameStats);
                queryListeners.Add(listenPort, queryListener);

                HeartbeatListener heartbeatListener = new HeartbeatListener(endpoint);
                heartbeatListener.ReceivedHeartbeat += new ReceivedHeartbeatHandler(serverList.ReceivedHeartbeat);
                heartbeatListeners.Add(listenPort, heartbeatListener);

                MasterServer.Log("[NET] Port {0} bound successfully", listenPort);
            }
            catch (Exception ex)
            {
                MasterServer.Log("[NET] Error binding port(s) {0}, {1}", listenPort, ex.Message);

                queryListeners.Remove(listenPort);
                heartbeatListeners.Remove(listenPort);
            }

            UpdateListenPortList();
        }

        /// <summary>
        /// Unbind query and heartbeat listeners from the specified port
        /// </summary>
        /// <param name="listenPort">Port to unbind from</param>
        private void UnBind(int listenPort)
        {
            if (queryListeners.ContainsKey(listenPort))
            {
                try
                {
                    queryListeners[listenPort].Shutdown();
                    queryListeners.Remove(listenPort);

                    heartbeatListeners[listenPort].Shutdown();
                    heartbeatListeners[listenPort].ReceivedHeartbeat -= new ReceivedHeartbeatHandler(serverList.ReceivedHeartbeat);
                    heartbeatListeners.Remove(listenPort);

                    MasterServer.Log("[NET] Port {0} unbound successfully", listenPort);
                }
                catch (Exception ex)
                {
                    MasterServer.Log("[NET] Error unbinding port(s) {0}, {1}", listenPort, ex.Message);
                }
            }
            else
            {
                MasterServer.Log("[NET] Port {0} not bound", listenPort);
            }

            UpdateListenPortList();
        }

        /// <summary>
        /// Update list of listen ports
        /// </summary>
        private void UpdateListenPortList()
        {
            List<string> strListenPorts = new List<string>();
            foreach (int listenPort in queryListeners.Keys)
                strListenPorts.Add(listenPort.ToString());
            MasterServer.ListenPorts = String.Join(",", strListenPorts.ToArray());
        }

        /// <summary>
        /// Get the localised MOTD string
        /// </summary>
        /// <param name="locale">Client's locale</param>
        /// <param name="useIntOnError">Return value for int locale if the specified locale is not found</param>
        /// <returns></returns>
        public static String GetMOTD(string locale, bool useIntOnError)
        {
            switch (locale.Trim().ToLower())
            {
                case "int": return MOTD.eng;
                case "det": return MOTD.det;
                case "est": return MOTD.est;
                case "frt": return MOTD.frt;
                case "itt": return MOTD.itt;
                case "kot": return MOTD.kot;
                case "tct": return MOTD.tct;
                case "rut": return MOTD.rut;
                default: return useIntOnError ? MOTD.eng : "";       // Unknown locale, send int or blank
            }
        }

        /// <summary>
        /// Try to set the MOTD for the specified locale, if the locale does not exist returns false
        /// </summary>
        /// <param name="locale">Locale to set MOTD for</param>
        /// <param name="motd">New MOTD</param>
        /// <returns>True if the MOTD was set for the specified locale</returns>
        public static bool SetMOTD(string locale, string motd)
        {
            switch (locale.Trim().ToLower())
            {
                case "int": MOTD.eng = motd; break;
                case "det": MOTD.det = motd; break;
                case "est": MOTD.est = motd; break;
                case "frt": MOTD.frt = motd; break;
                case "itt": MOTD.itt = motd; break;
                case "kot": MOTD.kot = motd; break;
                case "tct": MOTD.tct = motd; break;
                case "rut": MOTD.rut = motd; break;
                default: return false;
            }

            MOTD.Save();
            return true;
        }

        /// <summary>
        /// Write a message to the log with date and time stamps
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Log(string format, params object[] args)
        {
            string logEntry = String.Format(format, args);

            // Write entry to the log file
            if (logWriter != null)
                logWriter.Write(logEntry, instance);

            // Write log line to the debug console
            System.Diagnostics.Debug.WriteLine(logEntry);

            string prepend = DateTime.Now.ToString("dd/MM HH:mm:ss:");

            foreach (string logLine in logEntry.Split('\n'))
                LogLine(logLine, prepend);                
        }

        /// <summary>
        /// Write a message to the log with no date and time stamp
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogMessage(string format, params object[] args)
        {
            string[] logLines = String.Format(format, args).Split('\n');

            foreach (string logLine in logLines)
                LogLine(logLine, " ");
        }

        /// <summary>
        /// Write a message to the log with no date and time stamp
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogCommand(string[] command)
        {
            LogLine(String.Join(" ", command), ">");
        }

        /// <summary>
        /// Writes a formatted log line to the logging interface
        /// </summary>
        /// <param name="logLine"></param>
        /// <param name="prependDateTime"></param>
        private static void LogLine(string logLine, string prepend)
        {
            int logLineSize = logBufferWrap - prepend.Length - 1;
            string logLineFormat = "{0," + prepend.Length + "} {1}";

            lock (logLock)
            {
                while (logLine.Length > 0)
                {
                    // Trim lines to the current log wrap displayLength
                    WriteLogLine(String.Format(logLineFormat, prepend, logLine.Substring(0, Math.Min(logLine.Length, logLineSize))));
                    logLine = logLine.Substring(Math.Min(logLine.Length, logLineSize));
                    prepend = String.Empty;
                }
            }

            // Update the display immediately, in the display update thread
            if (instance != null && instance.displayTimer != null) instance.displayTimer.Change(0, Timeout.Infinite);
        }

        /// <summary>
        /// Writes a real log line to the log buffer
        /// </summary>
        /// <param name="text"></param>
        private static void WriteLogLine(string text)
        {
            if (instance != null && instance.commandInterface != null)
                instance.commandInterface.Notify("LOG", text);

            log.Add(text);

            while (log.Count > logBufferSize) log.RemoveAt(0);
        }

        /// <summary>
        /// NOTIFY messages are passed from the CommandInterface to the StatusDisplay
        /// </summary>
        /// <param name="notification">Notification instruction</param>
        /// <param name="info">Additional arguments</param>
        public void Notify(string notification, params string[] info)
        {
            if (statusDisplay != null)
            {
                statusDisplay.Notify(notification, info);
            }
        }

        /// <summary>
        /// Updates the status display (if there is one)
        /// </summary>
        /// <param name="State"></param>
        public void Display(object state)
        {
            try
            {
                if (statusDisplay != null)
                {
                    // Assign log limit params from the status display
                    logBufferSize = statusDisplay.LogBufferSize;
                    logBufferWrap = statusDisplay.LogBufferWrap;

                    if (displayTimer != null)
                        displayTimer.Change(Timeout.Infinite, Timeout.Infinite);

                    // Holder for the log tail since we will pass a copy to the display interface
                    string[] logLines;

                    lock (logLock)
                    {
                        // Copy log lines into the temporary array
                        logLines = new string[log.Count];
                        log.CopyTo(logLines, 0);
                    }

                    lock (displayLock)
                    {
                        // Update the status display
                        statusDisplay.UpdateDisplay(this, logLines, DateTime.Now - startTime);
                    }

                    if (displayTimer != null)
                    {
                        // Set the next update interval
                        displayTimer.Change(displayUpdateInterval, Timeout.Infinite);
                    }
                }

                if (commandInterface != null)
                {
                    lock (displayLock)
                    {
                        // Update the command interface display (if required)
                        commandInterface.Display();
                    }
                }
            }
            catch //(Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// The command interface wants to be redrawn
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DisplayCommandInterface(object sender, EventArgs e)
        {
            if (commandInterface != null)
            {
                lock (displayLock)
                {
                    commandInterface.Display();
                }
            }
        }

        /// <summary>
        /// Dump the log tail to the command interface
        /// </summary>
        public void TailLog()
        {
            if (commandInterface != null)
            {
                lock (logLock)
                {
                    foreach (string logLine in log)
                    {
                        commandInterface.Notify("LOG", logLine);
                    }
                }
            }
        }

        /// <summary>
        /// Increment the query counter
        /// </summary>
        public static void RegisterQuery()
        {
            Stats.Default.TotalQueries++;
            Stats.Default.Save();
        }

        /// <summary>
        /// Increment the web query counter
        /// </summary>
        public void RegisterWebQuery()
        {
            Stats.Default.TotalWebQueries++;
            Stats.Default.Save();
        }
    }
}
