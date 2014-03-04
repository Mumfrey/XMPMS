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
using System.Net;
using XMPMS.Core;
using XMPMS.Interfaces;

namespace XMPMS.Web.RequestHandlers
{
    /// <summary>
    /// Request handler which serves support files such as images and CSS
    /// </summary>
    public class SupportFileRequestHandler : IRequestHandler
    {
        /// <summary>
        /// Mapping of supported extensions and the appropriate MIME types to return
        /// </summary>
        private Dictionary<string, string> mimeTypes = new Dictionary<string, string>();

        /// <summary>
        /// Constructor
        /// </summary>
        public SupportFileRequestHandler()
        {
            // Add MIME type mappings to the config 
            mimeTypes.Add(".css",  "text/css"                    );
            mimeTypes.Add(".js",   "text/javascript"             );
            mimeTypes.Add(".jpg",  "image/jpeg"                  );
            mimeTypes.Add(".jpeg", "image/jpeg"                  );
            mimeTypes.Add(".gif",  "image/gif"                   );
            mimeTypes.Add(".png",  "image/png"                   );
            mimeTypes.Add(".ico",  "image/x-icon"                );
            mimeTypes.Add(".zip",  "application/zip"             );
            mimeTypes.Add(".rar",  "application/x-rar-compressed");
        }

        /// <summary>
        /// IMasterServerModule Interface
        /// </summary>
        public bool AutoLoad
        {
            get { return false; }
        }

        /// <summary>
        /// Called when the web server is starting up
        /// </summary>
        /// <param name="masterServer"></param>
        public virtual void Initialise(MasterServer masterServer)
        {
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
        public bool HandleRequest(HttpListenerRequest Request, HttpListenerResponse Response)
        {
            if (Request.HttpMethod.ToUpper() == "GET")
            {
                // Get the requested path from the HTTP request
                string requestPath = Request.Url.LocalPath;

                // Strip leading slashes from the path to ensure we don't step out of the webroot directory
                while ((requestPath.StartsWith("/") || requestPath.StartsWith("\\")) && requestPath.Length > 1) requestPath = requestPath.Substring(1);

                // Filter bad queries
                if (requestPath.Length < 2) return false;

                // Locate the file
                FileInfo requestedFile = GetRealFile(requestPath);

                // Look up the requested file extension 
                if (requestedFile != null && requestedFile.Exists && mimeTypes.ContainsKey(requestedFile.Extension.ToLower()))
                {
                    Response.Headers["Content-type"] = mimeTypes[requestedFile.Extension.ToLower()];
                    Response.Headers["Expires"] = DateTime.Now.AddHours(1).ToString("r");

                    byte[] data = File.ReadAllBytes(requestedFile.FullName);

                    Response.OutputStream.Write(data, 0, data.Length);
                    Response.OutputStream.Close();

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get a real FileInfo for a file on disk based on the webroot and the incoming request path,
        /// allows subclasses to override the path finding logic. No check is made as to whether the file
        /// exists or not or is a valid file type, this function simply maps request paths to real paths.
        /// </summary>
        /// <param name="requestPath">Sanitised path from the request (eg. with leading slashes removed)</param>
        /// <returns>FileInfo for the specified path</returns>
        protected virtual FileInfo GetRealFile(string requestPath)
        {
            return new FileInfo(Path.Combine(WebServer.WebRoot, requestPath));
        }

        /// <summary>
        /// Priority of this handler relative to other handlers
        /// </summary>
        public virtual int Priority
        {
            get { return 50; }
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
