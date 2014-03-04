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
using XMPMS.Core;

namespace XMPMS.Interfaces
{
    /// <summary>
    /// Interface for master server command processors, such as console or telnet
    /// </summary>
    public interface ICommandInterface : IMasterServerModule
    {
        /// <summary>
        /// Raised when the object wants to be redrawn
        /// </summary>
        event EventHandler OnChange;

        /// <summary>
        /// True if the master server should echo commands to the console
        /// </summary>
        bool EchoCommands { get; }

        /// <summary>
        /// The State of the command processor has changed, update the display.
        /// </summary>
        void Display();

        /// <summary>
        /// Send an abstract notification to this command interface
        /// </summary>
        /// <param name="command"></param>
        void Notify(string notification, params string[] info);
    }
}
