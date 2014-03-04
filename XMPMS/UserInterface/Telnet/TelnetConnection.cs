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
using System.Text.RegularExpressions;
using System.Net.Sockets;
using XMPMS.Core;

namespace XMPMS.UserInterface.Telnet
{
    /// <summary>
    /// A telnet connection to a remote host
    /// </summary>
    public class TelnetConnection
    {
        /// <summary>
        /// Command history to support arrow-key scrolling through history
        /// </summary>
        private List<string> commandHistory = new List<string>();

        /// <summary>
        /// Current position in the command history
        /// </summary>
        private int commandHistoryIndex = -1;

        /// <summary>
        /// Command queue, in case commands are recieved more quickly than we can process them or multiple
        /// commands are received in a single packet
        /// </summary>
        private List<string> commands = new List<string>();

        /// <summary>
        /// Command line object which owns this connection
        /// </summary>
        private TelnetCommandLine owner;

        /// <summary>
        /// Socket for this connection
        /// </summary>
        protected Socket socket;

        /// <summary>
        /// Data not yet returned
        /// </summary>
        private string pendingData = "";

        /// <summary>
        /// True if this thread was forcibly aborted and we should terminate
        /// </summary>
        protected volatile bool aborted = false;

        /// <summary>
        /// Current authentication retry count
        /// </summary>
        protected volatile int authenticationTries = 0;

        /// <summary>
        /// True if authenticated successfully
        /// </summary>
        protected volatile bool authenticated = false;

        /// <summary>
        /// Main thread for this socket
        /// </summary>
        protected Thread handlerThread;

        /// <summary>
        /// Raised when a new command has been received
        /// </summary>
        public event TelnetCommandReceivedEventHandler CommandReceived;

        /// <summary>
        /// Regex for matching escape sequences
        /// </summary>
        private static Regex escapeSequenceRegex = new Regex(@"\x1B\x5B(?<code>.)");

        /// <summary>
        /// Escape sequence code for UP
        /// </summary>
        private const string escUP = "\x41";

        /// <summary>
        /// Escape sequence code for DOWN
        /// </summary>
        private const string escDOWN = "\x42";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="socket">TCP socket to communicate on</param>
        /// <param name="commandLine">Owner</param>
        public TelnetConnection(Socket socket, TelnetCommandLine commandLine)
        {
            this.socket = socket;
            this.owner = commandLine;

            // Start handler thread
            handlerThread = new Thread(new ThreadStart(Handle));
            handlerThread.Start();
        }

        /// <summary>
        /// Handle the connection
        /// </summary>
        protected virtual void Handle()
        {
            // Set telnet mode to send escape sequences
            Send("\x1B&k0L");

            while (!aborted)
            {
                try
                {
                    // Show the command line
                    Display();

                    // Receive keystrokes from the socket
                    if (Receive())
                    {
                        // Process received keystrokes
                        ProcessPendingCommands();
                    }
                }
                catch (ThreadAbortException)
                {
                    System.Diagnostics.Debug.WriteLine("Telnet connection thread aborted");
                    Abort();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Telnet connection error: " + ex.Message);
                }
            }

            // Notify the owner that the connection was terminated
            owner.ConnectionClosed(this);
        }

        /// <summary>
        /// Show the current prompt, either password prompt or command line
        /// </summary>
        public void Display()
        {
            if (authenticated)
            {
                Send("\r> " + pendingData + " \b");
            }
            else
            {
                Send("\x1B[2J\r\nPassword: " + new String('*', pendingData.Length));
            }
        }

        /// <summary>
        /// Process any received commands in the buffer
        /// </summary>
        protected virtual void ProcessPendingCommands()
        {
            while (commands.Count > 0)
            {
                string nextCommand = commands[0].Trim();
                commands.RemoveAt(0);

                if (authenticated)
                {
                    if (nextCommand != "") OnCommand(nextCommand);
                }
                else
                {
                    TryAuthenticate(nextCommand);
                }
            }
        }

        /// <summary>
        /// Attempt to authenticate with the supplied password
        /// </summary>
        /// <param name="nextCommand"></param>
        private void TryAuthenticate(string nextCommand)
        {
            if (nextCommand == MasterServer.Settings.TelnetPassword)
            {
                authenticated = true;
                owner.AcceptConnection(this);
            }
            else
            {
                authenticationTries++;

                if (authenticationTries > 2)
                {
                    owner.RejectConnection(this);
                }
            }
        }

        /// <summary>
        /// Try to gracefully abort/close the connection by closing the socket
        /// </summary>
        public virtual void Abort()
        {
            aborted = true;

            if (socket != null)
            {
                try
                {
                    socket.Close();
                }
                catch { }
            }
        }

        /// <summary>
        /// Shut down this connection
        /// </summary>
        public virtual void Shutdown()
        {
            Abort();

            if (handlerThread != null)
            {
                handlerThread.Abort();
                handlerThread.Join();
                handlerThread = null;
            }
        }

        /// <summary>
        /// Raise the command received event
        /// </summary>
        /// <param name="command"></param>
        protected virtual void OnCommand(string command)
        {
            commandHistoryIndex = -1;
            commandHistory.Remove(command);
            commandHistory.Insert(0, command);

            TelnetCommandReceivedEventHandler commandReceived = this.CommandReceived;

            if (commandReceived != null)
            {
                commandReceived(command, this);
            }
        }

        /// <summary>
        /// Send the specified string to the socket
        /// </summary>
        /// <param name="text"></param>
        public virtual void Send(string text)
        {
            if (socket.Connected)
            {
                socket.Send(Encoding.ASCII.GetBytes(text));
            }
        }

        /// <summary>
        /// Receive data from the socket, blocking function
        /// </summary>
        /// <returns>True if pending commands should be processed</returns>
        protected virtual bool Receive()
        {
            byte[] buffer = new byte[65536];
            int count = socket.Receive(buffer, SocketFlags.None);

            if (count == 0)     // Connection closed
            {
                aborted = true;
                socket.Close();
                return false;
            }
            else
            {
                pendingData += Encoding.ASCII.GetString(buffer, 0, count);

                if (pendingData.IndexOf('\x4') >= 0)    // Ctrl-D aborts connection
                {
                    aborted = true;
                    Send("logout");
                    socket.Close();
                    return false;
                }
                else
                {
                    // Check for escape sequences in the received buffer
                    MatchCollection escapeSequences = escapeSequenceRegex.Matches(pendingData);

                    // Process any escape sequences that were received
                    if (escapeSequences.Count > 0)
                    {
                        foreach (Match escapeSequence in escapeSequences)
                        {
                            // Up arrow key, scroll backwards through the command buffer
                            if (escapeSequence.Groups["code"].Value == escUP)
                            {
                                if (commandHistory.Count > 0 && commandHistoryIndex < commandHistory.Count - 1)
                                {
                                    commandHistoryIndex++;

                                    if (commandHistoryIndex > -1 && commandHistoryIndex < commandHistory.Count)
                                    {
                                        pendingData = commandHistory[commandHistoryIndex];
                                        return false;
                                    }
                                }
                            }

                            // Down arrow key, scroll forwards through the command buffer
                            else if (escapeSequence.Groups["code"].Value == escDOWN)
                            {
                                if (commandHistory.Count > 0 && commandHistoryIndex > -1)
                                {
                                    commandHistoryIndex--;

                                    if (commandHistoryIndex > -1 && commandHistoryIndex < commandHistory.Count)
                                    {
                                        pendingData = commandHistory[commandHistoryIndex];
                                        return false;
                                    }
                                    else
                                    {
                                        pendingData = "";
                                        return false;
                                    }
                                }
                            }
                        }
                    }

                    commandHistoryIndex = -1;

                    pendingData = Regex.Replace(pendingData, @"\x1B\x5B.", "");                     // Strip escape sequences
                    pendingData = Regex.Replace(pendingData, @"^.*\x1B", "");                       // Strip ESC
                    pendingData = Regex.Replace(pendingData, @".\x08", "");                         // Strip BACKSPACE
                    pendingData = Regex.Replace(pendingData, @"[\x00-\x09\x0b\x0c\x0e-\x1F]", "");  // Strip nonprinting chars

                    // Get complete lines from the buffer and add them to the pending commands array
                    int crIndex = pendingData.IndexOf('\n');
                    while (crIndex >= 0)
                    {
                        commands.Add(pendingData.Substring(0, crIndex));
                        pendingData = pendingData.Substring(crIndex + 1);
                        crIndex = pendingData.IndexOf('\n');
                    }
                }
            }

            return true;
        }
    }
}
