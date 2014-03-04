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
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using XMPMS.Core;
using XMPMS.Util;
using XMPMS.Interfaces;

namespace XMPMS.Web.RequestHandlers
{
    /// <summary>
    /// Request handler which handles the default (index) page. 
    /// </summary>
    public class IndexRequestHandler : SSIHandler, IRequestHandler
    {
        /// <summary>
        /// HREF for iframe post location
        /// </summary>
        private const string FRAMESRC = "/serverlist.do";

        /// <summary>
        /// Index page template file
        /// </summary>
        protected FileInfo indexFile;

        /// <summary>
        /// Server row template file
        /// </summary>
        protected FileInfo serverRowFile;

        /// <summary>
        /// Server spacer row template file
        /// </summary>
        protected FileInfo serverSpacerRowFile;

        /// <summary>
        /// Template file for javascript do
        /// </summary>
        protected FileInfo doTemplateFile;

        /// <summary>
        /// Reference to the master server object
        /// </summary>
        protected MasterServer masterServer;

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
            this.masterServer = masterServer;

            indexFile           = new FileInfo(Path.Combine(WebServer.WebRoot, "index.shtml"));
            serverRowFile       = new FileInfo(Path.Combine(WebServer.WebRoot, "index.tablerow.shtml"));
            serverSpacerRowFile = new FileInfo(Path.Combine(WebServer.WebRoot, "index.tablespacer.shtml"));
            doTemplateFile      = new FileInfo(Path.Combine(WebServer.WebRoot, "index.do.shtml"));
        }

        /// <summary>
        /// Called when the web server is shutting down
        /// </summary>
        public void Shutdown()
        {
            this.masterServer = null;
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
                // Get current server list
                List<Server> servers = masterServer.ServerList.QueryGameType(MasterServer.Settings.WebServerGameTypeFilter, false);

                if (Request.Url.LocalPath == "/" && indexFile.Exists)
                {
                    // For stats
                    masterServer.RegisterWebQuery();

                    // Set include variables
                    variables["title"]           = MasterServer.Settings.WebServerServerHeader;
                    variables["skin"]            = WebServer.Skin;
                    variables["servercount"]     = "0";
                    variables["busyservercount"] = "0";
                    variables["listspacer"]      = ReadFile(serverSpacerRowFile);;
                    variables["framesrc"]        = FRAMESRC;

                    // Substitute in server list and status line into index page template
                    string indexPage = ReadFileAndParse(indexFile);

                    Response.OutputStream.Write(Encoding.ASCII.GetBytes(indexPage), 0, indexPage.Length);
                    Response.OutputStream.Close();

                    return true;
                }
                else if (Request.Url.LocalPath == FRAMESRC && serverRowFile.Exists && serverSpacerRowFile.Exists && doTemplateFile.Exists)
                {
                    // Set up page resources
                    string serverListRow = ReadFile(serverRowFile);
                    string serverSpacer  = ReadFile(serverSpacerRowFile);
                    string doTemplate    = ReadFile(doTemplateFile);

                    string serverList = "";

                    // Decremented for every active server, therefore this will count the "pending" servers in the list
                    int busyServerCount = servers.Count;

                    // Add server rows to server list
                    foreach (Server server in servers)
                    {
                        if (server.Active)
                        {
                            busyServerCount--;

                            variables["hrefserver"]     = String.Format("server?ip={0}&port={1}", server.Address.ToString(), server.Port);
                            variables["hrefmap"]        = MapFileUrl(server.Map);
                            variables["pwd"]            = (server.Password) ? "Yes" : "";
                            variables["imgpwd"]         = (server.Password) ? Image("lock.gif", "L", "Server is passworded", 13, 13) : "";
                            variables["listen"]         = (server.Listen) ? "Yes" : "";
                            variables["imglisten"]      = (server.Listen) ? Image("listen.gif", "P", "Server is a listen server (non-dedicated)", 13, 13) : "";
                            variables["name"]           = ColourCodeParser.ColouriseConditional(server.Name);
                            variables["map"]            = server.Map;
                            variables["gametype"]       = server.GameType;
                            variables["currentplayers"] = server.CurrentPlayers.ToString();
                            variables["maxplayers"]     = server.MaxPlayers.ToString();
                            variables["flag"]           = Image("img/flags/" + server.Country.ToLower() + ".gif", server.Country, server.Country, 18, 12);
                            variables["country"]        = server.Country;
                            variables["ip"]             = server.Address.ToString();
                            variables["port"]           = server.Port.ToString();
                            variables["queryport"]      = server.QueryPort.ToString();

                            // Add populated row to the server list
                            serverList += ReplaceVariables(serverListRow);
                        }
                    }

                    variables["serverlistbody"]  = EncodeBrackets(serverSpacer + serverList + serverSpacer);
                    variables["servercount"]     = servers.Count.ToString();
                    variables["busyservercount"] = busyServerCount.ToString();

                    string doHtml = ReplaceVariables(doTemplate);

                    Response.OutputStream.Write(Encoding.ASCII.GetBytes(doHtml), 0, doHtml.Length);
                    Response.OutputStream.Close();

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get the URL for a map file or return "#" if the file does not exist
        /// </summary>
        /// <param name="mapName">Map name</param>
        /// <returns></returns>
        private string MapFileUrl(string mapName)
        {
            string mapFileUrl = String.Format("{0}{1}.zip", MasterServer.Settings.WebServerMapUrl, mapName);
            return (new FileInfo(Path.Combine(WebServer.WebRoot, mapFileUrl)).Exists) ? mapFileUrl : "#";
        }

        /// <summary>
        /// Priority of this handler relative to other handlers
        /// </summary>
        public virtual int Priority
        {
            get { return 100; }
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
