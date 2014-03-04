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
using System.IO;
using System.Text;
using System.Net;
using XMPMS.Core;
using XMPMS.Util;
using XMPMS.Interfaces;

namespace XMPMS.Web.RequestHandlers
{
    /// <summary>
    /// Request handler which handles a server page
    /// </summary>
    public class ServerRequestHandler : SSIHandler, IRequestHandler
    {
        /// <summary>
        /// HREF for iframe post location
        /// </summary>
        private const string FRAMESRC = "/server.do";

        /// <summary>
        /// Page template file
        /// </summary>
        protected FileInfo pageFile;

        /// <summary>
        /// Player row template file
        /// </summary>
        protected FileInfo playerRowFile;

        /// <summary>
        /// Information row template file
        /// </summary>
        protected FileInfo infoRowFile;

        /// <summary>
        /// Reference to the master server object
        /// </summary>
        protected MasterServer masterServer;

        /// <summary>
        /// Template file for javascript do
        /// </summary>
        protected FileInfo doTemplateFile;

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
        public void Initialise(MasterServer masterServer)
        {
            this.masterServer = masterServer;

            pageFile       = new FileInfo(Path.Combine(WebServer.WebRoot, "server.shtml"));
            playerRowFile  = new FileInfo(Path.Combine(WebServer.WebRoot, "server.playerrow.shtml"));
            infoRowFile    = new FileInfo(Path.Combine(WebServer.WebRoot, "server.inforow.shtml"));
            doTemplateFile = new FileInfo(Path.Combine(WebServer.WebRoot, "server.do.shtml"));
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
            if (Request.HttpMethod.ToUpper() == "GET" && (Request.Url.LocalPath == "/server" || Request.Url.LocalPath == FRAMESRC) && pageFile.Exists && playerRowFile.Exists && infoRowFile.Exists)
            {
                // These GET vars contain the server address and port which the player wants to view
                string requestedIp = Request.QueryString["ip"];
                string requestedPort = Request.QueryString["port"];

                // Container variables for the parsed address and port
                IPAddress address;
                ushort port;

                string pageData = "Bad Query";

                if (requestedIp != null && IPAddress.TryParse(requestedIp, out address) && requestedPort != null && ushort.TryParse(requestedPort, out port))
                {
                    // For stats
                    masterServer.RegisterWebQuery();

                    // Get the server which the player has requested
                    Server server = masterServer.ServerList.GetServer(address, port);

                    variables["skin"] = WebServer.Skin;

                    if (Request.Url.LocalPath == "/server")
                    {
                        pageData = ReadFile(pageFile);

                        variables["title"]    = String.Format("{0}{1}", MasterServer.Settings.WebServerServerHeader, server != null ? " - " + ColourCodeParser.StripColourCodes(server.Name) : "");
                        variables["framesrc"] = String.Format("{0}?ip={1}&port={2}", FRAMESRC, address, port);
                    }
                    else if (Request.Url.LocalPath == FRAMESRC)
                    {
                        pageData = ReadFile(doTemplateFile);

                        if (server != null)
                        {
                            string playerList = "";
                            string infoList = "";

                            // Player list row template
                            string playerListRow = ReadFile(playerRowFile);

                            int playerIndex = 0;

                            foreach (Player player in server.Players)
                            {
                                variables["playerindex"] = (playerIndex++).ToString();
                                variables["playername"]  = ColourCodeParser.Colourise(player.Name);
                                variables["playerping"]  = player.Ping.ToString();
                                variables["playerscore"] = player.Score.ToString();

                                foreach (KeyValuePair<string, string> info in player.Info)
                                {
                                    variables[String.Format("playerinfo[{0}]", info.Key.ToLower())] = ColourCodeParser.Colourise(info.Value);
                                }

                                playerList += ReplaceVariables(playerListRow);
                            }

                            // Info list row template
                            string infoListRow = ReadFile(infoRowFile);

                            foreach (KeyValuePair<string, string> property in server.Properties)
                            {
                                variables["key"] = property.Key;
                                variables["value"] = property.Value;

                                infoList += ReplaceVariables(infoListRow);
                            }

                            variables["headtitle"]      = EncodeBrackets(ColourCodeParser.Colourise(server.Name));
                            variables["playerlistbody"] = EncodeBrackets(playerList);
                            variables["serverinfobody"] = EncodeBrackets(infoList);
                            variables["mapimage"]       = String.Format("/img/maps/{0}.jpg", server.Map.ToLower());
                            variables["mapname"]        = server.Map;
                        }
                        else
                        {
                            variables["headtitle"]      = "Server offline";
                            variables["playerlistbody"] = "";
                            variables["serverinfobody"] = "";
                            variables["mapimage"]       = "/img/maps/unknown.jpg";
                            variables["mapname"]        = "Unknown Map";
                        }
                    }
                    else
                    {
                        pageData = "Bad Query";
                    }

                    pageData = ReplaceVariables(pageData);
                }

                // Write the page to the output stream
                Response.OutputStream.Write(Encoding.ASCII.GetBytes(pageData), 0, pageData.Length);
                Response.OutputStream.Close();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Priority of this handler relative to other handlers
        /// </summary>
        public virtual int Priority
        {
            get { return 90; }
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
