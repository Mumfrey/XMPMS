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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using XMPMS.Core;
using XMPMS.Util;
using XMPMS.Interfaces;
using XMPMS.Web;

#pragma warning disable 67
namespace XMPMS.UserInterface.GUI
{
    /// <summary>
    /// Master Server GUI Form. Not really recommended
    /// </summary>
    public partial class frmStatus : Form, IStatusDisplay, ICommandInterface
    {
        /// <summary>
        /// Delegate for function to handle cross-thread display updates
        /// </summary>
        /// <param name="masterServer"></param>
        /// <param name="logText"></param>
        /// <param name="upTime"></param>
        private delegate void UpdateDisplayDelegate(MasterServer masterServer, string logText, TimeSpan upTime);

        /// <summary>
        /// Flag used to keep track of whether we are shutting down
        /// </summary>
        private bool shutdown = false;

        /// <summary>
        /// IMasterServerModule Interface
        /// </summary>
        public bool AutoLoad
        {
            get { return false; }
        }

        /// <summary>
        /// String representation of the current server list, 
        /// </summary>
        private string strServerList = "";

        /// <summary>
        /// Command history to support arrow-key scrolling through history
        /// </summary>
        private List<string> commandHistory = new List<string>();

        /// <summary>
        /// Current position in the command history
        /// </summary>
        private int commandHistoryIndex = -1;

        /// <summary>
        /// Raised when the object wants to be redrawn
        /// </summary>
        public event EventHandler OnChange;

        /// <summary>
        /// Echo commands into log
        /// </summary>
        public bool EchoCommands
        {
            get { return true; }
        }

        /// <summary>
        /// LogWriter buffer size to keep
        /// </summary>
        public int LogBufferSize
        {
            get { return 255; }
        }

        /// <summary>
        /// Maximum displayLength of a log line
        /// </summary>
        public int LogBufferWrap
        {
            get { return 1024; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public frmStatus()
        {
            InitializeComponent();

            Text = (Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0] as AssemblyDescriptionAttribute).Description;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="masterServer"></param>
        public void Initialise(MasterServer masterServer)
        {
        }

        /// <summary>
        /// Blocking function. Runs a message loop on this object and shuts down the server when the form is closed
        /// </summary>
        public void Run()
        {
            Application.Run(this);
            Shutdown();
        }

        /// <summary>
        /// Update the status display
        /// </summary>
        /// <param name="masterServer">Reference to the master server instance</param>
        /// <param name="log">LogWriter tail</param>
        /// <param name="upTime">Server uptime</param>
        public void UpdateDisplay(MasterServer masterServer, string[] log, TimeSpan upTime)
        {
            if (shutdown || Disposing || IsDisposed) return;

            string logText = String.Join("\r\n", log);

            try
            {
                if (InvokeRequired)
                {
                    Invoke(new UpdateDisplayDelegate(UpdateForm), masterServer, logText, upTime);
                }
                else
                {
                    UpdateForm(masterServer, logText, upTime);
                }
            }
            catch { }
        }

        /// <summary>
        /// In-thread function called by UpdateDisplay, which updates the form elements with the current server status
        /// </summary>
        /// <param name="masterServer"></param>
        /// <param name="logText"></param>
        /// <param name="upTime"></param>
        private void UpdateForm(MasterServer masterServer, string logText, TimeSpan upTime)
        {
            // Update the log if the log has changed
            if (logText != txtConsole.Text)
            {
                txtConsole.Text = logText;
                txtConsole.Select(logText.LastIndexOf('\r') + 2, 1);
                txtConsole.ScrollToCaret();
            }

            // Get servers from the server list
            List<Server> servers = masterServer.ServerList.Query();

            // Calculate the number of local servers
            int localServerCount = 0;
            foreach (Server server in servers)
                if (server.Local) localServerCount++;

            // Set status label text
            lblActiveConnections.Text = localServerCount.ToString();
            lblTotalServers.Text      = servers.Count.ToString();
            lblUpTime.Text            = String.Format("{0} days {1} hours {2} minutes", upTime.Days, upTime.Hours, upTime.Minutes);
            lblQueries.Text           = masterServer.TotalQueries.ToString();
            lblWebQueries.Text        = masterServer.TotalWebQueries.ToString();
            lblTCPPorts.Text          = MasterServer.ListenPorts;
            lblWebServerPort.Text     = WebServer.ListenPorts;

            // Because there's so much to keep track of, and we only want to update the listview when something changes, the comparison
            // method I'm using is to serialise all the relevant data to a string and then use string comparison to determine whether an
            // update is required or not. Whilst this isn't pretty, it does work quite well.

            // Build the new serialised list
            string newServerList = "";
            foreach (Server server in servers)
                newServerList += server.Name + server.Selected.ToString() + server.Address.ToString() + server.Port.ToString() + server.LastUpdate.ToString("hhmmss") + server.Local.ToString();

            // Compare the new serialised list with the stored one and update if different
            if (newServerList != strServerList)
            {
                // Store the new serialised list for next time
                strServerList = newServerList;

                lstServers.Items.Clear();

                // Refresh the server list
                foreach (Server server in servers)
                {
                    ListViewItem serverItem = lstServers.Items.Add(ColourCodeParser.StripColourCodes(server.Name), server.Selected ? 1 : 0);
                    serverItem.Tag = server;
                    serverItem.SubItems.Add(server.Address.ToString());
                    serverItem.SubItems.Add(server.Port.ToString());
                    serverItem.SubItems.Add(server.LastUpdate.ToString("hh:mm:ss"));
                    serverItem.SubItems.Add(server.Local ? "Yes" : "No");
                }
            }
        }

        /// <summary>
        /// Shut down the server
        /// </summary>
        public void Shutdown()
        {
            shutdown = true;
        }

        /// <summary>
        /// Handle an event notification from the control interface or master server
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="info"></param>
        public void Notify(string notification, params string[] info)
        {
            switch (notification)
            {
                case "EXIT":
                    Close();
                    break;
            }
        }

        public void Display()
        {
        }

        public void Log(string text)
        {
        }

        /// <summary>
        /// Handle keystrokes in the command textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtCommand_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    txtCommand.Clear();
                    break;

                case Keys.Enter:
                    Execute();
                    break;

                case Keys.Up:
                    if (commandHistory.Count > 0 && commandHistoryIndex < commandHistory.Count - 1)
                    {
                        commandHistoryIndex++;

                        if (commandHistoryIndex > -1 && commandHistoryIndex < commandHistory.Count)
                        {
                            txtCommand.Text = commandHistory[commandHistoryIndex];
                            txtCommand.Select(txtCommand.Text.Length, 0);
                        }
                    }
                    break;

                case Keys.Down:
                    if (commandHistory.Count > 0 && commandHistoryIndex > -1)
                    {
                        commandHistoryIndex--;

                        if (commandHistoryIndex > -1 && commandHistoryIndex < commandHistory.Count)
                        {
                            txtCommand.Text = commandHistory[commandHistoryIndex];
                            txtCommand.Select(txtCommand.Text.Length, 0);
                        }
                        else
                        {
                            txtCommand.Text = "";
                            txtCommand.Select(0, 0);
                        }

                        Display();
                    }
                    break;
            }
        }

        /// <summary>
        /// Execute the command in the command textbox
        /// </summary>
        private void Execute()
        {
            if (txtCommand.Text != "")
            {
                ModuleManager.DispatchCommand(txtCommand.Text.Split(' '));

                commandHistoryIndex = -1;
                commandHistory.Insert(0, txtCommand.Text);
            }

            txtCommand.Text = "";
        }

        private void HandleShown(object sender, EventArgs e)
        {
            this.TopMost = true;
            this.BringToFront();
            this.TopMost = false;
        }
    }
}
