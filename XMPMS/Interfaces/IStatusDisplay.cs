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
using XMPMS.Core;

namespace XMPMS.Interfaces
{
    /// <summary>
    /// Interface for master server display modules
    /// </summary>
    public interface IStatusDisplay : IMasterServerModule
    {
        /// <summary>
        /// Get the size of the log buffer to keep
        /// </summary>
        int LogBufferSize { get; }

        /// <summary>
        /// Get the number of characters to wrap log lines
        /// </summary>
        int LogBufferWrap { get; }

        /// <summary>
        /// Update this status display with the information provided
        /// </summary>
        /// <param name="masterServer">Reference to the master server</param>
        /// <param name="log">Master server log tail</param>
        /// <param name="upTime">Current master server UpTime</param>
        void UpdateDisplay(MasterServer masterServer, string[] log, TimeSpan upTime);

        /// <summary>
        /// Send a notification to this status display
        /// </summary>
        /// <param name="command"></param>
        void Notify(string notification, params string[] info);
    }
}
