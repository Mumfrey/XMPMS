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
using System.Xml;
using XMPMS.Core;
using XMPMS.Util;
using XMPMS.Interfaces;

namespace XMPMS.Web.RequestHandlers
{
    /// <summary>
    /// Request handler which handles the default (index) page. 
    /// </summary>
    public class XmlRequestHandler : IRequestHandler
    {
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
            if (Request.HttpMethod.ToUpper() == "GET" && Request.Url.LocalPath == "/serverlist.xml")
            {
                // For stats
                masterServer.RegisterWebQuery();

                // Set up xml document and writer
                XmlDocument xmlDocument = new XmlDocument();
                XmlWriter xml = xmlDocument.CreateNavigator().AppendChild();

                // Write the XML declaration
                xml.WriteStartDocument(true);

                string xmlNamespaceServer           = "http://" + Request.Url.Host + "/xmlns/server";
                string xmlNamespaceServerProperties = "http://" + Request.Url.Host + "/xmlns/serverproperties";
                string xmlNamespacePlayer           = "http://" + Request.Url.Host + "/xmlns/player";
                string xmlNamespacePlayerProperties = "http://" + Request.Url.Host + "/xmlns/playerproperties";

                // Get current server list
                List<Server> servers = masterServer.ServerList.QueryGameType(MasterServer.Settings.WebServerGameTypeFilter, false);

                // Incremented for every active server
                int activeServerCount = servers.Count;

                xml.WriteStartElement("servers");
                {
                    xml.WriteAttributeString("xmlns", "s",  null, xmlNamespaceServer);
                    xml.WriteAttributeString("xmlns", "sp", null, xmlNamespaceServerProperties);
                    xml.WriteAttributeString("xmlns", "p",  null, xmlNamespacePlayer);
                    xml.WriteAttributeString("xmlns", "pp", null, xmlNamespacePlayerProperties); 

                    // Stat information in the document element
                    xml.WriteAttributeString("total", servers.Count.ToString());
                    xml.WriteAttributeString("active", "0");
                    xml.WriteAttributeString("pending", "0");

                    foreach (Server server in servers)
                    {
                        if (server.Active)
                        {
                            // This server is not busy
                            activeServerCount--;

                            xml.WriteStartElement("server", xmlNamespaceServer);
                            {
                                xml.WriteAttributeString("password", xmlNamespaceServer, server.Password.ToString());
                                xml.WriteAttributeString("listen",   xmlNamespaceServer, server.Listen.ToString());

                                xml.WriteElementString("ip",        xmlNamespaceServer, server.Address.ToString());
                                xml.WriteElementString("port",      xmlNamespaceServer, server.Port.ToString());
                                xml.WriteElementString("queryport", xmlNamespaceServer, server.QueryPort.ToString());
                                xml.WriteElementString("country",   xmlNamespaceServer, server.Country);

                                xml.WriteStartElement("name", xmlNamespaceServer);
                                {
                                    xml.WriteElementString("basic", xmlNamespaceServer, ColourCodeParser.StripColourCodes(server.Name));
                                    xml.WriteElementString("html",  xmlNamespaceServer, ColourCodeParser.ColouriseConditional(server.Name));
                                }
                                xml.WriteEndElement(); // name

                                xml.WriteElementString("gametype", xmlNamespaceServer, server.GameType);
                                xml.WriteElementString("map",      xmlNamespaceServer, server.Map);

                                xml.WriteStartElement("players", xmlNamespaceServer);
                                {
                                    xml.WriteAttributeString("current", xmlNamespaceServer, server.CurrentPlayers.ToString());
                                    xml.WriteAttributeString("max",     xmlNamespaceServer, server.MaxPlayers.ToString());

                                    foreach (Player player in server.Players)
                                    {
                                        xml.WriteStartElement("player", xmlNamespacePlayer);

                                        xml.WriteStartElement("name", xmlNamespacePlayer);
                                        {
                                            xml.WriteElementString("basic", xmlNamespacePlayer, ColourCodeParser.StripColourCodes(player.Name));
                                            xml.WriteElementString("html",  xmlNamespacePlayer, ColourCodeParser.ColouriseConditional(player.Name));
                                        }
                                        xml.WriteEndElement(); // name

                                        xml.WriteElementString("ping",  xmlNamespacePlayer, player.Ping.ToString());
                                        xml.WriteElementString("score", xmlNamespacePlayer, player.Score.ToString());

                                        foreach (KeyValuePair<string, string> info in player.Info)
                                        {
                                            xml.WriteElementString(info.Key.ToLower(), xmlNamespacePlayerProperties, ColourCodeParser.StripColourCodes(info.Value));
                                        }

                                        xml.WriteEndElement();
                                    }
                                }
                                xml.WriteEndElement(); // players

                                xml.WriteStartElement("properties", xmlNamespaceServerProperties);
                                {
                                    foreach (KeyValuePair<string, string> property in server.Properties)
                                    {
                                        xml.WriteElementString(property.Key.ToLower(), xmlNamespaceServerProperties, ColourCodeParser.StripColourCodes(property.Value));
                                    }
                                }
                                xml.WriteEndElement(); // properties
                            }
                            xml.WriteEndElement(); // server
                        }
                    }
                }
                xml.WriteEndElement(); // servers

                xml.WriteEndDocument();
                xml.Close();

                xmlDocument.DocumentElement.Attributes["active"].Value = activeServerCount.ToString();
                xmlDocument.DocumentElement.Attributes["pending"].Value = (servers.Count - activeServerCount).ToString();

                Response.Headers["Content-type"] = "text/xml";

                xmlDocument.Save(Response.OutputStream);
                Response.OutputStream.Close();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Helper function which formats a HTML IMG tag with the specified attributes
        /// </summary>
        /// <param name="url">Image URL</param>
        /// <param name="alt">Alt text</param>
        /// <param name="title">Title text (hover)</param>
        /// <param name="width">Width attribute</param>
        /// <param name="height">Height attribute</param>
        /// <returns>HTML IMG tag as text</returns>
        public static string Image(string url, string alt, string title, int width, int height)
        {
            return String.Format("<img src=\"{0}\" alt=\"{1}\" title=\"{2}\" width=\"{3}\" height=\"{4}\" />", url, alt, title, width, height);
        }

        /// <summary>
        /// Priority of this handler relative to other handlers
        /// </summary>
        public virtual int Priority
        {
            get { return 95; }
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
