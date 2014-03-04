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
using System.Threading;

namespace XMPMS.Net.Connections
{
    /// <summary>
    /// Manages threads used by incoming connections and provides a central location for shutting
    /// down all active threads when closing the master server
    /// </summary>
    public static class ConnectionThreadManager
    {
        /// <summary>
        /// Thread lock to prevent cross-thread concurrent access to the thread list
        /// </summary>
        private static object threadListLock = new object();

        /// <summary>
        /// List of active threads
        /// </summary>
        private static List<Thread> threadList = new List<Thread>();

        /// <summary>
        /// Create a thread using the specified ThreadStart
        /// </summary>
        /// <param name="start">ThreadStart delegate for the new thread</param>
        /// <returns>New thread</returns>
        public static Thread Create(ThreadStart start)
        {
            Thread connectionThread = new Thread(start);

            lock (threadListLock)
                threadList.Add(connectionThread);

            return connectionThread;
        }

        /// <summary>
        /// Create a thread using the specified ThreadStart
        /// </summary>
        /// <param name="start">ThreadStart delegate for the new thread</param>
        /// <returns>New thread</returns>
        public static Thread Create(ParameterizedThreadStart start)
        {
            Thread connectionThread = new Thread(start);

            lock (threadListLock)
                threadList.Add(connectionThread);

            return connectionThread;
        }

        /// <summary>
        /// Create a thread using the specified ThreadStart and start it
        /// </summary>
        /// <param name="start">ThreadStart delegate for the new thread</param>
        /// <returns>New thread</returns>
        public static Thread CreateStart(ThreadStart start)
        {
            Thread connectionThread = Create(start);

            connectionThread.Start();

            return connectionThread;
        }

        /// <summary>
        /// Create a thread using the specified ThreadStart and start it
        /// </summary>
        /// <param name="start">ThreadStart delegate for the new thread</param>
        /// <returns>New thread</returns>
        public static Thread CreateStart(ParameterizedThreadStart start, object parameter)
        {
            Thread connectionThread = Create(start);

            connectionThread.Start(parameter);

            return connectionThread;
        }

        /// <summary>
        /// Remove the specified thread from the list of threads, this is called by the
        /// thread proc when a thread terminates normally
        /// </summary>
        /// <param name="thread"></param>
        public static void Remove(Thread thread)
        {
            lock (threadListLock)
                threadList.Remove(thread);
        }

        /// <summary>
        /// Forcibly terminates all active connection threads
        /// </summary>
        public static void TerminateAll()
        {
            Console.WriteLine("Terminating active connection threads, please wait...");

            lock (threadListLock)
            {
                while (threadList.Count > 0)
                {
                    if (threadList[0].IsAlive)
                    {
                        threadList[0].Abort();
                        threadList[0].Join();
                    }
                    threadList.RemoveAt(0);
                }
            }
        }

    }
}
