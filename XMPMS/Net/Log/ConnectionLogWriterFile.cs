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
using XMPMS.LogWriter;

namespace XMPMS.Web.Log
{
    /// <summary>
    /// Basic log writer which writes web server logs to flat files
    /// </summary>
    public class ConnectionLogWriterFile : LogWriterFile, IConnectionLogWriter
    {
        /// <summary>
        /// Name pattern for log files
        /// </summary>
        protected override string logFileName
        {
            get { return "net{0:yyyyMMdd}.log"; }
        }

        /// <summary>
        /// Format string for log file lines
        /// </summary>
        protected override string logFormat
        {
            get { return "[{0:yyyy-MM-dd HH:mm:ss}] {1}\r\n"; }
        }

        /// <summary>
        /// Message written when the log writer is being released
        /// </summary>
        protected override string logShutdownMessage
        {
            get { return null; }
        }

        /// <summary>
        /// Configure the log file path
        /// </summary>
        protected override void SetupLogFolder(string assemblyPath)
        {
            logFilePath = Path.Combine(assemblyPath, MasterServer.Settings.ConnectionLogFolder);
        }
    }
}
