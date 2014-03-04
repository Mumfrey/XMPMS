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
using System.Net;
using System.IO;
using XMPMS.Core;
using XMPMS.Interfaces;

namespace XMPMS.Web.RequestHandlers
{
    /// <summary>
    /// Request handler which serves a single file with no modifications
    /// </summary>
    public class PageRequestHandler : IRequestHandler
    {
        /// <summary>
        /// Name of the html file which will be served
        /// </summary>
        protected string pageName = String.Empty;

        /// <summary>
        /// Page template file
        /// </summary>
        protected FileInfo pageFile;

        /// <summary>
        /// IMasterServerModule Interface
        /// </summary>
        public bool AutoLoad
        {
            get { return false; }
        }

        /// <summary>
        /// Default constructor, handles 404 error messages
        /// </summary>
        public PageRequestHandler()
            : this("404.htm")
        { }

        /// <summary>
        /// Constructor which specifies the page name to serve
        /// </summary>
        /// <param name="pageName">Name of the page to serve, the file must exist beneath the webroot folder</param>
        public PageRequestHandler(string pageName)
        {
            this.pageName = pageName;
        }

        /// <summary>
        /// Called when the web server is starting up
        /// </summary>
        /// <param name="masterServer"></param>
        public virtual void Initialise(MasterServer masterServer)
        {
            this.pageFile = new FileInfo(Path.Combine(WebServer.WebRoot, pageName));
        }

        /// <summary>
        /// Called when the web server is shutting down
        /// </summary>
        public void Shutdown()
        {
        }

        /// <summary>
        /// Attempt to handle a request, returns true if the request was handled and should not be passed
        /// to handlers with lower priority
        /// </summary>
        /// <param name="Request">HTTP Request</param>
        /// <param name="Response">HTTP Response</param>
        /// <returns>Bool indicating whether the request was handled or not</returns>
        public virtual bool HandleRequest(HttpListenerRequest Request, HttpListenerResponse Response)
        {
            // 404 handler handles all requests
            if (pageFile.Exists)
            {
                Response.StatusCode = 404;

                StreamWriter writer = new StreamWriter(Response.OutputStream);
                writer.Write(File.ReadAllText(pageFile.FullName));
                writer.Close();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Priority of this handler relative to other handlers
        /// </summary>
        public virtual int Priority
        {
            get { return 0; }
        }

        /// <summary>
        /// To support sorting using the Sort() method
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(IRequestHandler other)
        {
            if (other.Priority > Priority) return 1;
            if (other.Priority < Priority) return -1;
            return 0;
        }
    }
}
