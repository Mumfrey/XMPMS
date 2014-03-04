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
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using XMPMS.Core;
using XMPMS.Interfaces;

namespace XMPMS.UserInterface.TextMode
{
    /// <summary>
    /// Command line parser for console sessions
    /// </summary>
    public class ConsoleCommandLine : ICommandInterface
    {
        /// <summary>
        /// Flag set when the console is being shut down
        /// </summary>
        private bool shutdown = false;

        /// <summary>
        /// Thread which monitors the console and processes commands
        /// </summary>
        private Thread inputThread;

        /// <summary>
        /// Current input buffer, contains keystrokes issued so far
        /// </summary>
        private string inputBufferHead = "";

        /// <summary>
        /// Input buffer which is AFTER the caret
        /// </summary>
        private string inputBufferTail = "";

        /// <summary>
        /// Current full input buffer, concatenated head and tail
        /// </summary>
        private string inputBuffer
        {
            get
            {
                return inputBufferHead + inputBufferTail;
            }

            set
            {
                inputBufferHead = value;
                inputBufferTail = "";
            }
        }

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
        /// Echo console commands to log
        /// </summary>
        public bool EchoCommands
        {
            get { return true; }
        }

        /// <summary>
        /// IMasterServerModule Interface
        /// </summary>
        public bool AutoLoad
        {
            get { return false; }
        }

        /// <summary>
        /// Initialise the command processor
        /// </summary>
        /// <param name="masterServer"></param>
        /// <param name="statusDisplay"></param>
        public void Initialise(MasterServer masterServer)
        {
            shutdown = false;

            inputThread = new Thread(new ThreadStart(InputThreadProc));
            inputThread.Start();
        }

        /// <summary>
        /// Handle input in the thread calling this function
        /// </summary>
        public void Run()
        {
            // Kill the old thread if it exists
            if (inputThread != null && inputThread.IsAlive && inputThread != Thread.CurrentThread)
            {
                shutdown = true;
                inputThread.Abort();
                inputThread.Join();
            }

            inputThread = null;
            shutdown = false;

            InputThreadProc();
        }

        /// <summary>
        /// Shut down the command processor and release resources
        /// </summary>
        public void Shutdown()
        {
            shutdown = true;

            // The thread may end on its own
            Thread.Sleep(50);

            if (inputThread != null && inputThread.IsAlive && inputThread != Thread.CurrentThread)
            {
                inputThread.Abort();
                inputThread.Join();
                inputThread = null;
            }
        }

        /// <summary>
        /// Allow this command interface to display its current state
        /// </summary>
        public void Display()
        {
            int startIndex = 0;
            int wrapWidth = Console.BufferWidth - 3;
            int displayLength = inputBuffer.Length;
            int cursorOffset = inputBufferHead.Length;

            if (inputBuffer.Length < wrapWidth || inputBufferHead.Length < wrapWidth)
            {
                displayLength = Math.Min(inputBuffer.Length, wrapWidth);
            }
            else
            {
                displayLength = wrapWidth;
                cursorOffset = Math.Max(0, wrapWidth - inputBufferTail.Length);
                startIndex = (inputBufferTail.Length < wrapWidth) ? inputBuffer.Length - wrapWidth : inputBufferHead.Length;
            }

            Console.CursorVisible = false;
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.Write("> {0,-" + wrapWidth + "}", inputBuffer.Substring(startIndex, displayLength));
            Console.SetCursorPosition(2 + cursorOffset, Console.WindowHeight - 1);
            Console.CursorVisible = true;
        }

        /// <summary>
        /// Input thread
        /// </summary>
        private void InputThreadProc()
        {
            while (!shutdown)
            {
                Console.TreatControlCAsInput = true;

                // Wait for a keypress
                while (!shutdown && !Console.KeyAvailable)
                {
                    // Needed because we don't have a real message pump on this thread
                    Application.DoEvents();
                    Thread.Sleep(10);
                }

                ProcessInput();
            }
        }

        /// <summary>
        /// Process pending input in the buffer
        /// </summary>
        public virtual void ProcessInput()
        {
            if (!Console.KeyAvailable || shutdown) return;

            ConsoleKeyInfo key = Console.ReadKey(true);

            int iKey = (int)key.KeyChar;

            if (key.Key == ConsoleKey.C && key.Modifiers == ConsoleModifiers.Control)
            {
                Clipboard.SetText(inputBuffer);
            }
            else if (key.Key == ConsoleKey.V && key.Modifiers == ConsoleModifiers.Control)
            {
                try
                {
                    if (Clipboard.ContainsText(TextDataFormat.Text))
                    {
                        string pasteData = Clipboard.GetText(TextDataFormat.Text);
                        inputBufferHead += pasteData;
                        OnChanged();
                    }
                }
                catch (ExternalException)
                {
                }
            }
            else if (iKey > 31 && iKey < 127)           // Printable ASCII characters
            {
                commandHistoryIndex = -1;

                inputBufferHead += key.KeyChar;
                OnChanged();
            }
            else if (key.Key == ConsoleKey.Enter)       // Enter key
            {
                string[] command = inputBuffer.Split(' ');

                commandHistoryIndex = -1;
                commandHistory.Insert(0, inputBuffer);

                inputBuffer = "";
                OnChanged();

                ModuleManager.DispatchCommand(command);
            }
            else if (key.Key == ConsoleKey.Backspace)   // Backspace
            {
                if (inputBufferHead.Length > 0)
                {
                    commandHistoryIndex = -1;

                    inputBufferHead = inputBufferHead.Substring(0, inputBufferHead.Length - 1);
                    OnChanged();
                }
            }
            else if (key.Key == ConsoleKey.Delete)      // Del
            {
                if (inputBufferTail.Length > 0)
                {
                    commandHistoryIndex = -1;

                    inputBufferTail = inputBufferTail.Substring(1);
                    OnChanged();
                }
            }
            else if (key.Key == ConsoleKey.UpArrow)     // Scroll back through command history
            {
                if (commandHistory.Count > 0 && commandHistoryIndex < commandHistory.Count - 1)
                {
                    commandHistoryIndex++;

                    if (commandHistoryIndex > -1 && commandHistoryIndex < commandHistory.Count)
                    {
                        inputBuffer = commandHistory[commandHistoryIndex];
                        OnChanged();
                    }
                }
            }
            else if (key.Key == ConsoleKey.DownArrow)   // Scroll forward through command history
            {
                if (commandHistory.Count > 0 && commandHistoryIndex > -1)
                {
                    commandHistoryIndex--;

                    if (commandHistoryIndex > -1 && commandHistoryIndex < commandHistory.Count)
                        inputBuffer = commandHistory[commandHistoryIndex];
                    else
                        inputBuffer = "";

                    OnChanged();
                }
            }
            else if (key.Key == ConsoleKey.LeftArrow)   // Move left in the input buffer
            {
                if (inputBufferHead.Length > 0)
                {
                    char shift = inputBufferHead[inputBufferHead.Length - 1];
                    inputBufferHead = inputBufferHead.Substring(0, inputBufferHead.Length - 1);
                    inputBufferTail = shift + inputBufferTail;
                    OnChanged();
                }
            }
            else if (key.Key == ConsoleKey.RightArrow)  // Move right in the input buffer
            {
                if (inputBufferTail.Length > 0)
                {
                    char shift = inputBufferTail[0];
                    inputBufferTail = inputBufferTail.Substring(1);
                    inputBufferHead = inputBufferHead + shift;
                    OnChanged();
                }
            }
            else if (key.Key == ConsoleKey.Home)        // Move to the start of the input buffer
            {
                if (inputBufferHead.Length > 0)
                {
                    inputBufferTail = inputBuffer;
                    inputBufferHead = "";
                    OnChanged();
                }
            }
            else if (key.Key == ConsoleKey.End)         // Move to the end of the input buffer
            {
                if (inputBufferTail.Length > 0)
                {
                    inputBufferHead = inputBuffer;
                    inputBufferTail = "";
                    OnChanged();
                }
            }
            else if (key.Key == ConsoleKey.Escape)      // Clear the input buffer
            {
                commandHistoryIndex = -1;
                inputBuffer = "";
                OnChanged();
            }
            else if (key.Key == ConsoleKey.PageUp)      // Scroll the server list upwards
            {
                if (MasterServer.Instance != null)
                    MasterServer.Instance.Notify("PAGEUP");
            }
            else if (key.Key == ConsoleKey.PageDown)    // Scroll the server list downwards
            {
                if (MasterServer.Instance != null)
                    MasterServer.Instance.Notify("PAGEDOWN");
            }
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
        /// Handle notifications
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="info"></param>
        public void Notify(string notification, params string[] info)
        {
        }
    }
}
