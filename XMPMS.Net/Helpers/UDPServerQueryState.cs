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
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Net;

namespace XMPMS.Net.Helpers
{
    /// <summary>
    /// State information for a specific query being executed by a UDPServerQueryClient object
    /// </summary>
    internal sealed class UDPServerQueryState
    {
        /// <summary>
        /// Reference to the thread in which the query is executing
        /// </summary>
        internal Thread QueryThread;

        /// <summary>
        /// Type of query being executed
        /// </summary>
        internal UDPServerQueryClient.UDPServerQueryType QueryType;

        /// <summary>
        /// UDP client for this connection
        /// </summary>
        internal UdpClient QueryClient;

        /// <summary>
        /// Timer which will manage the query timeout
        /// </summary>
        internal Timer QueryTimer;

        /// <summary>
        /// Flag which indicates the query has timed out
        /// </summary>
        internal UDPServerQueryClient.UDPServerQueryResponse QueryResponse = UDPServerQueryClient.UDPServerQueryResponse.None;

        /// <summary>
        /// Flag indicating the query thread was started
        /// </summary>
        private volatile bool started = false;

        /// <summary>
        /// Flag indicating the timeout timer has been stopped
        /// </summary>
        private volatile bool stopped = false;

        /// <summary>
        /// Flag indicating the connection was closed
        /// </summary>
        private volatile bool closed = false;

        /// <summary>
        /// Create a new query state
        /// </summary>
        /// <param name="queryThreadProc">Query thread procedure to call</param>
        /// <param name="queryTimeoutCallback">Query timeout callback procedure</param>
        /// <param name="queryType">Type of query to execute</param>
        internal UDPServerQueryState(ParameterizedThreadStart queryThreadProc, TimerCallback queryTimeoutCallback, UDPServerQueryClient.UDPServerQueryType queryType)
        {
            QueryType   = queryType;
            QueryThread = new Thread(queryThreadProc);
            QueryClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            QueryTimer  = new Timer(queryTimeoutCallback, this, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Start the query thread
        /// </summary>
        /// <param name="timeout"></param>
        internal void Start(int timeout)
        {
            if (!started)
            {
                // Set started flag to prevent multiple starts
                started = true;

                // Start the thread
                QueryThread.Start(this);

                // Start the timeout timer if a timeout was specified
                if (timeout > 0)
                {
                    QueryTimer.Change(timeout * 1000, Timeout.Infinite);
                }
                else
                {
                    stopped = true;
                    QueryTimer.Dispose();
                    QueryTimer = null;
                }
            }
        }

        /// <summary>
        /// Query was terminated, stop the timeout timer and close the connection
        /// </summary>
        internal void End()
        {
            Stop();
            Close();
        }

        /// <summary>
        /// Stop the timeout timer (if it was started)
        /// </summary>
        internal void Stop()
        {
            if (!stopped)
            {
                stopped = true;

                if (QueryTimer != null)
                {
                    try
                    {
                        QueryTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        QueryTimer.Dispose();
                        QueryTimer = null;
                    }
                    catch (ObjectDisposedException) { }
                }
            }
        }

        /// <summary>
        /// Close the UDP connection
        /// </summary>
        internal void Close()
        {
            if (!closed)
            {
                closed = true;

                if (QueryClient != null)
                {
                    try
                    {
                        QueryClient.Close();
                        QueryClient = null;
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Shut down the query, closes query state objects
        /// </summary>
        internal void Shutdown()
        {
            try
            {
                // Stop the timeout timer
                Stop();

                // Close the UDP client
                Close();

                // Kill the query thread
                if (QueryThread != null)
                {
                    QueryThread.Abort();
                    QueryThread.Join();
                    QueryThread = null;
                }
            }
            catch { }
        }
    }
}
