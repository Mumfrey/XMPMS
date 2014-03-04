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
using System.Collections.Specialized;
using System.Text;
using System.Xml;
using System.IO;
using System.Threading;
using System.Net;
using System.Reflection;
using XMPMS.Core;
using XMPMS.Interfaces;
using XMPMS.Util;

namespace XMPMS.Web
{
    /// <summary>
    /// Web server which provides the public web interface
    /// </summary>
    public partial class WebServer : IDisposable
    {
        /// <summary>
        /// Web server listen ports, for display purposes
        /// </summary>
        public static string ListenPorts
        {
            get;
            private set;
        }

        /// <summary>
        /// HTTP listener which handles inbound connections
        /// </summary>
        protected HttpListener listener;

        /// <summary>
        /// Listening thread
        /// </summary>
        protected Thread listenThread;

        /// <summary>
        /// Handler threads
        /// </summary>
        protected List<Thread> handlerThreads = new List<Thread>();

        /// <summary>
        /// Request handler objects, populated using reflection
        /// </summary>
        protected List<IRequestHandler> requestHandlers = new List<IRequestHandler>();

        /// <summary>
        /// Set this true when shutting down so we know to handle those exceptions
        /// </summary>
        protected volatile bool listening;

        /// <summary>
        /// Web log writer
        /// </summary>
        protected IWebLogWriter log;

        /// <summary>
        /// Ban manager
        /// </summary>
        protected IPBanManager banManager;

        /// <summary>
        /// Root folder for web documents
        /// </summary>
        public static string WebRoot
        {
            get;
            private set;
        }

        /// <summary>
        /// Get the skin folder name
        /// </summary>
        public static string Skin
        {
            get
            {
                DirectoryInfo skinFolder = new DirectoryInfo(Path.Combine(WebRoot, MasterServer.Settings.WebServerSkinFolder));
                return (skinFolder.Exists) ? MasterServer.Settings.WebServerSkinFolder : "default";
            }
        }

        /// <summary>
        /// Constructor. Loads request handler classes from the current assembly.
        /// </summary>
        public WebServer(IPBanManager banManager)
        {
            this.banManager = banManager;

            WebServer.ListenPorts = "-";

            // Determine web root
            string assemblyPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            WebServer.WebRoot = Path.Combine(assemblyPath, MasterServer.Settings.WebRootFolder);

            // Register as a handler for console commands
            ModuleManager.RegisterCommandListener(this);
        }

        /// <summary>
        /// Release any resources used
        /// </summary>
        public void Dispose()
        {
            banManager = null;

            ModuleManager.UnregisterCommandListener(this);
        }

        /// <summary>
        /// Start listening on the specified interface
        /// </summary>
        public void BeginListening()
        {
            if (!listening && (listenThread == null || listenThread.ThreadState != ThreadState.Running))
            {
                // 0 means disable the web server 
                if (MasterServer.Settings.WebServerListenPort == 0)
                {
                    MasterServer.Log("[WEB] Web server is disabled.");
                    return;
                }

                // Get all loaded request handler modules
                requestHandlers = ModuleManager.GetModules<IRequestHandler>();

                // Sort the request handlers into priority order, highest first
                requestHandlers.Sort();

                // Flag to indicate the server is active
                listening = true;
                listener = new HttpListener();

                log = ModuleManager.GetModule<IWebLogWriter>();

                try
                {
                    WebServer.ListenPorts = MasterServer.Settings.WebServerListenPort.ToString();

                    // Attempt to bind specified addresses and prefixes
                    foreach (string listenAddress in MasterServer.Settings.WebServerListenAddresses)
                    {
                        listener.Prefixes.Add(String.Format("http://{0}:{1}/", listenAddress, MasterServer.Settings.WebServerListenPort));
                    }

                    MasterServer.Log("[WEB] Starting web server on port {0}", MasterServer.Settings.WebServerListenPort);
                    listener.Start();
                    MasterServer.Log("[WEB] Web server started ok.");

                    // Start the listen thread
                    listenThread = new Thread(new ThreadStart(ListenerThread));
                    listenThread.Start();
                }
                catch (HttpListenerException ex)
                {
                    WebServer.ListenPorts = "-";

                    if (ex.ErrorCode == 32)
                        MasterServer.Log("[WEB] Error, port already in use!");
                    else
                        MasterServer.Log("[WEB] Error binding listen port: {0}", ex.Message);

                    listening = false;
                    listener = null;

                    // Release request handler modules
                    ModuleManager.ReleaseModules<IRequestHandler>();
                    ModuleManager.ReleaseModule<IWebLogWriter>();
                }
                catch (Exception ex)
                {
                    WebServer.ListenPorts = "-";
                    MasterServer.Log("[WEB] Error binding listen port: {0}", ex.Message);

                    listening = false;
                    listener = null;

                    // Release request handler modules
                    ModuleManager.ReleaseModules<IRequestHandler>();
                    ModuleManager.ReleaseModule<IWebLogWriter>();
                }
            }
            else
            {
                MasterServer.Log("[WEB] Error starting web server. Web server already running");
                //throw new InvalidOperationException("Web server is already listening");
            }
        }

        /// <summary>
        /// End listening
        /// </summary>
        public void EndListening()
        {
            if (listening && listenThread != null)
            {
                MasterServer.Log("[WEB] Shutting down web server. {0} active connections.", handlerThreads.Count);

                // Flag
                listening = false;

                try
                {
                    // Clear prefixes
                    listener.Prefixes.Clear();
                }
                catch { }

                try
                {
                    // Abort the HTTP listener
                    listener.Abort();
                }
                catch { }

                // Shut down active listener threads
                while (handlerThreads.Count > 0)
                {
                    handlerThreads[0].Abort();
                    handlerThreads[0].Join();
                    handlerThreads.RemoveAt(0);
                }

                listener = null;

                // Abort the listen thread
                listenThread.Abort();
                listenThread.Join();
                listenThread = null;

                // Release request handler modules
                ModuleManager.ReleaseModules<IRequestHandler>();
                ModuleManager.ReleaseModule<IWebLogWriter>();

                WebServer.ListenPorts = "-";

                MasterServer.Log("[WEB] Web server shut down.");
            }
            else
            {
                MasterServer.Log("[WEB] Error stopping web server. Web server already stopped");
                //throw new InvalidOperationException("Web server is not listening");
            }
        }

        /// <summary>
        /// Web Server listen thread function
        /// </summary>
        protected void ListenerThread()
        {
            // listener started successfully
            if (listener.IsListening)
            {
                try
                {
                    while (listening)
                    {
                        // Blocking function, returns a new listener context with each inbound query
                        HttpListenerContext context = listener.GetContext();
                        Thread handlerThread = new Thread(new ParameterizedThreadStart(HandleRequest));
                        handlerThreads.Add(handlerThread);
                        handlerThread.Start(context);
                    }
                }
                catch (HttpListenerException)
                {
                    if (listening) throw;
                }
                catch (ThreadAbortException)
                {
                    // Don't care, just shut down silently
                }
                catch (Exception ex)
                {
                    MasterServer.Log("[WEB] Server error: {0}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Outer function for handling an inbound request in a new thread
        /// </summary>
        /// <param name="oContext"></param>
        protected void HandleRequest(object oContext)
        {
            HttpListenerContext context = (HttpListenerContext)oContext;

            if (context != null)
            {
                try
                {
                    HandleRequest(context.Request, context.Response);
                }
                catch (HttpListenerException) // ex)
                {
                    // MasterServer.Log("[WEB] Web server exception: {0}", ex.ErrorCode);
                }
                catch (Exception ex)
                {
                    MasterServer.Log("[WEB] Server error: {0}", ex.Message);
                }
            }

            handlerThreads.Remove(Thread.CurrentThread);
        }

        /// <summary>
        /// Handles a HTTP request
        /// </summary>
        /// <param name="Request">HTTP Request</param>
        /// <param name="Response">HTTP Response object to use</param>
        protected void HandleRequest(HttpListenerRequest Request, HttpListenerResponse Response)
        {
            Response.Headers["Server"] = MasterServer.Settings.WebServerServerHeader;

            // Check the IP ban list to see whether the remote host is banned
            if (banManager.IsBanned(Request.RemoteEndPoint.Address))
            {
                Response.ContentType = "text/html";
                Response.StatusCode = 401;

                StreamWriter Writer = new StreamWriter(Response.OutputStream);
                Writer.WriteLine("<html><head><title>401: Forbidden</title></head><body>Your IP is banned</body></html>");
                Writer.Close();
            }
            else
            {
                // Loop through active request handlers and see whether any of them can handle the request
                foreach (IRequestHandler requestHandler in requestHandlers)
                {
                    if (requestHandler.HandleRequest(Request, Response))
                    {
                        if (log != null) log.Write(this, requestHandler.GetType().Name, Response.StatusCode, Request.HttpMethod, Request.Url.PathAndQuery, Request.RemoteEndPoint.Address, Request.LocalEndPoint.Address, Request.UserHostName, Request.UserAgent, (Request.UrlReferrer != null) ? Request.UrlReferrer.OriginalString : "-");

                        // Request was handled by the handler
                        return;
                    }
                }

                // Provide some basic failover behaviour in case no handler handled the request (even the 404 handler!)
                Response.ContentType = "text/html";
                Response.StatusCode = 404;

                StreamWriter Writer = new StreamWriter(Response.OutputStream);
                Writer.WriteLine("<html><head><title>404: Not Found</title></head><body><h2>404: Not Found</h2></body></html>");
                Writer.Close();
            }

            if (log != null) log.Write(this, "None", Response.StatusCode, Request.HttpMethod, Request.Url.PathAndQuery, Request.RemoteEndPoint.Address, Request.LocalEndPoint.Address, Request.UserHostName, Request.UserAgent, (Request.UrlReferrer != null) ? Request.UrlReferrer.OriginalString : "-");
        }
    }
}
