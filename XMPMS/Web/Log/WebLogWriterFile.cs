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
using System.Net;

namespace XMPMS.Web.Log
{
    /// <summary>
    /// Basic log writer which writes web server logs to flat files
    /// </summary>
    public class WebLogWriterFile : LogWriterFile, IWebLogWriter
    {
        /// <summary>
        /// Name pattern for log files
        /// </summary>
        protected override string logFileName
        {
            get { return "web{0:yyyyMMdd}.log"; }
        }

        /// <summary>
        /// Format string for log file lines
        /// </summary>
        protected override string logFormat
        {
            get { return "{0:yyyy-MM-dd HH:mm:ss} {1}\r\n"; }
        }

        /// <summary>
        /// Format string for log file lines
        /// </summary>
        protected override string logDivider
        {
            get { return "Web request logging started\r\n\r\ndate time http-status method path-query remote-ip local-ip user-host-name handler-class referrer user-agent\r\n"; }
        }

        /// <summary>
        /// Configure the log file path
        /// </summary>
        protected override void SetupLogFolder(string assemblyPath)
        {
            logFilePath = Path.Combine(assemblyPath, MasterServer.Settings.WebServerLogFolder);
        }

        /// <summary>
        /// Write a line to the web log
        /// </summary>
        /// <param name="source">Web server</param>
        /// <param name="handlerName">Name of the handler which handled the request</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="method">Request method</param>
        /// <param name="pathAndQuery">Request path including query</param>
        /// <param name="remoteAddress">Remote host address</param>
        /// <param name="localAddress">Local host address</param>
        /// <param name="userHostName">Host name supplied by the user agent</param>
        /// <param name="userAgent">User agent string</param>
        /// <param name="referrer">URL referrer</param>
        public void Write(WebServer source, string handlerName, int statusCode, string method, string pathAndQuery, IPAddress remoteAddress, IPAddress localAddress, string userHostName, string userAgent, string referrer)
        {
            Write(String.Format("{1} {2} {3} {4} {5} {6} <{0}> {8} {7}", handlerName, statusCode, method.Replace(' ', '+'), pathAndQuery.Replace(' ', '+'), remoteAddress, localAddress, userHostName.Replace(' ', '+'), userAgent.Replace(' ', '+'), referrer.Replace(' ', '+')), source);
        }
    }
}
