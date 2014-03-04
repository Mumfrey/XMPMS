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
using System.Net;
using System.Threading;
using XMPMS.Net.Connections;
using XMPMS.Net.Packets;

namespace XMPMS.Net.Helpers
{
    /// <summary>
    /// Manages UDP queries against a remote server
    /// </summary>
    public class UDPServerQueryClient : UDPConnection
    {
        /// <summary>
        /// Query threads
        /// </summary>
        private List<UDPServerQueryState> activeQueries = new List<UDPServerQueryState>();

        /// <summary>
        /// Lock for the query thread collection
        /// </summary>
        private object activeQueryLock = new object();

        /// <summary>
        /// Supported query types
        /// </summary>
        public enum UDPServerQueryType : byte
        {
            Basic             = 0x00,
            GameInfo          = 0x01,
            PlayerInfo        = 0x02,
            GameAndPlayerInfo = 0x03
        }

        /// <summary>
        /// Query response types
        /// </summary>
        public enum UDPServerQueryResponse : byte
        {
            None,
            Success,
            Error,
            Timeout
        }

        /// <summary>
        /// Raised when a query completes or times out
        /// </summary>
        public event UDPServerQueryResponseHandler QueryFinished;

        /// <summary>
        /// Start an asynchronous query operation
        /// </summary>
        /// <param name="timeout">Timeout (in seconds) to wait for a response from the server or zero for no timeout</param>
        public void BeginQuery(int timeout, UDPServerQueryType queryType, IPAddress address, int queryPort)
        {
            remoteEndpoint = new IPEndPoint(address, queryPort);

            // Start the query thread
            UDPServerQueryState queryState = new UDPServerQueryState(new ParameterizedThreadStart(QueryThreadProc), new TimerCallback(QueryTimeoutCallback), queryType);

            lock (activeQueryLock)
            {
                activeQueries.Add(queryState);
            }

            queryState.Start(timeout);
        }

        /// <summary>
        /// Thread function where the query is actually executed. The call to Receive() is blocking and the
        /// separate timer thread takes care of the timeout behaviour (if specified)
        /// </summary>
        private void QueryThreadProc(object oQueryState)
        {
            UDPServerQueryState queryState = (UDPServerQueryState)oQueryState;

            try
            {
                // Send the query to the server
                OutboundPacket query = new OutboundPacket(true);
                query.Append((byte)queryState.QueryType);
                Send(queryState.QueryClient, query);

                // Wait for a response from the server
                UDPPacket queryResponse = Receive(queryState.QueryClient);

                // Stop the timeout timer
                queryState.End();

                if (queryState.QueryResponse == UDPServerQueryResponse.None)
                {
                    queryState.QueryResponse = UDPServerQueryResponse.Success; 
                    OnQueryFinished(queryState, queryResponse);
                }
            }
            catch (ThreadAbortException) { }
            catch (Exception)
            {
                if (queryState.QueryResponse != UDPServerQueryResponse.Timeout)
                {
                    queryState.QueryResponse = UDPServerQueryResponse.Error;

                    // Stop the timeout timer
                    queryState.End();
                    OnQueryFinished(queryState, null);
                }
            }

            lock (activeQueryLock)
            {
                activeQueries.Remove(queryState);
            }
        }

        /// <summary>
        /// Callback function for when the timeout timer expires
        /// </summary>
        /// <param name="state"></param>
        private void QueryTimeoutCallback(object state)
        {
            UDPServerQueryState queryState = (UDPServerQueryState)state;

            // Flag the timeout condition
            queryState.QueryResponse = UDPServerQueryResponse.Timeout;

            queryState.End();
            OnQueryFinished(queryState, null);

            lock (activeQueryLock)
            {
                activeQueries.Remove(queryState);
            }
        }

        /// <summary>
        /// Raise the QueryFinished event with the specified parameters
        /// </summary>
        /// <param name="response">Response to the query (success, failure, etc)</param>
        /// <param name="queryType">Type of query that finished</param>
        /// <param name="payload">Received data packet</param>
        private void OnQueryFinished(UDPServerQueryState queryState, UDPPacket payload)
        {
            UDPServerQueryResponseHandler queryFinished = this.QueryFinished;

            if (queryFinished != null)
            {
                queryFinished(this, queryState.QueryResponse, queryState.QueryType, payload);
            }
        }

        /// <summary>
        /// Shut down this object, close any active connections
        /// </summary>
        public void Shutdown()
        {
            lock (activeQueryLock)
            {
                foreach (UDPServerQueryState queryState in activeQueries)
                {
                    queryState.Shutdown();
                }
            }
        }
    }
}
