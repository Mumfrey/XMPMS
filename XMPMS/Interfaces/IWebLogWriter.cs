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
using XMPMS.Core;
using System.Net;
using XMPMS.Web;

namespace XMPMS.Interfaces
{
    /// <summary>
    /// Interface for log writer objects
    /// </summary>
    public interface IWebLogWriter : ILogWriter
    {
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
        void Write(WebServer source, string handlerName, int statusCode, string method, string pathAndQuery, IPAddress remoteAddress, IPAddress localAddress, string userHostName, string userAgent, string referrer);
    }
}
