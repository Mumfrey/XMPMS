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
using System.IO;
using System.Reflection;
using XMPMS.Core;
using XMPMS.Interfaces;

namespace XMPMS.LogWriter
{
    /// <summary>
    /// Basic log writer which writes logs to flat files
    /// </summary>
    public class LogWriterFile : ILogWriter
    {
        /// <summary>
        /// Path to write log files
        /// </summary>
        protected string logFilePath;

        /// <summary>
        /// Log file lock
        /// </summary>
        protected object logFileLock = new object();

        /// <summary>
        /// Name pattern for log files
        /// </summary>
        protected virtual string logFileName
        {
            get { return "log{0:yyyyMMdd}.log"; }
        }

        /// <summary>
        /// Format string for log file lines
        /// </summary>
        protected virtual string logFormat
        {
            get { return "[{0:dd/MM/yyyy HH:mm:ss}] {1}\r\n"; }
        }

        /// <summary>
        /// Format string for log file lines
        /// </summary>
        protected virtual string logDivider
        {
            get { return new String('-', 80); }
        }

        /// <summary>
        /// Message written when the log writer is being released
        /// </summary>
        protected virtual string logShutdownMessage
        {
            get { return "Log writer shutting down"; }
        }

        /// <summary>
        /// IMasterServerModule Interface
        /// </summary>
        public bool AutoLoad
        {
            get { return false; }
        }

        /// <summary>
        /// IMasterServerModule Interface
        /// </summary>
        public virtual void Initialise(MasterServer masterServer)
        {
            SetupLogFolder(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName);

            if (!Directory.Exists(logFilePath))
                Directory.CreateDirectory(logFilePath);

            Write(logDivider, this);
        }

        /// <summary>
        /// Configure the log file path
        /// </summary>
        protected virtual void SetupLogFolder(string assemblyPath)
        {
            logFilePath = Path.Combine(assemblyPath, MasterServer.Settings.LogFolder);
        }

        /// <summary>
        /// Write a message to the log
        /// </summary>
        /// <param name="logMessage">Message to write</param>
        /// <param name="source">Message source (ignored)</param>
        public virtual void Write(string logMessage, object source)
        {
            try
            {
                string logFile = Path.Combine(logFilePath, String.Format(logFileName, DateTime.Now));

                lock (logFileLock)
                    File.AppendAllText(logFile, String.Format(logFormat, DateTime.Now, logMessage));
            }
            catch { }
        }

        /// <summary>
        /// Commit unwritten changes to disk
        /// </summary>
        public virtual void Commit()
        {
            // not used
        }

        /// <summary>
        /// ConnectionLog writer module was released
        /// </summary>
        public virtual void Shutdown()
        {
            if (logShutdownMessage != null)
            {
                Write(logShutdownMessage, this);
            }

            Commit();
        }
    }
}
