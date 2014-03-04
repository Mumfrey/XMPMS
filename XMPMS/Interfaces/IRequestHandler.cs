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
using System.Text;
using XMPMS.Core;

namespace XMPMS.Interfaces
{
    /// <summary>
    /// Interface for web request handlers. All implementors of this interface will be 
    /// instantiated by the WebServer upon initialisation and expected to handle inbound
    /// requests by descending order of priority.
    /// </summary>
    /// <remarks>
    /// Since the CompareTo member of IComparable is used to sort entries by priority, the
    /// standard implementation of CompareTo should follow the following pattern:
    /// 
    /// <code>
    /// public int CompareTo(IRequestHandler other)
    /// {
    ///     if (other.Priority > Priority) return 1;
    ///     if (other.Priority < Priority) return -1;
    ///     return 0;
    /// }
    /// </code>
    /// 
    /// </remarks>
    public interface IRequestHandler : IMasterServerModule, IComparable<IRequestHandler>
    {
        /// <summary>
        /// Priority for this handler, higher handlers will be called first.
        /// Standard priority levels are:
        ///     Index   ->  100
        ///     Server  ->  90
        ///     Support ->  50
        ///     404     ->  0
        /// Setting a level lower than 0 makes no sense since the 404 handler
        /// returns true in all cases.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Asks this handler to handle the specified request. If the handler handles the
        /// request it should close the output stream and return true. If the handler does
        /// not wish to handle the request then it should return false and the next handler
        /// will be called.
        /// </summary>
        /// <param name="Request">HTTP listener Request object</param>
        /// <param name="Response">HTTP listener Response object</param>
        /// <returns>True if the request was handled here</returns>
        bool HandleRequest(HttpListenerRequest Request, HttpListenerResponse Response);
    }
}
