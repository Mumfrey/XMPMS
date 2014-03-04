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
    /// Interface for loadable Master Server Modules
    /// </summary>
    public interface IMasterServerModule
    {
        /// <summary>
        /// This module should return true if the module manager should create an instance of this module automatically
        /// </summary>
        bool AutoLoad { get; }

        /// <summary>
        /// Initialise this module. This function is called when the first instance of the module is requested
        /// using GetModule. Since all modules are loaded irrespective of whether they are used or not, modules
        /// should perform the bulk of their initialisation here.
        /// </summary>
        /// <param name="masterServer">Reference to the master server instance</param>
        void Initialise(MasterServer masterServer);

        /// <summary>
        /// Shut down the module. This function is called when the last instance of a module is released. The
        /// module may subsequently be re-initialised if it is requested again.
        /// </summary>
        void Shutdown();
    }
}
