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
using System.ServiceModel;
using System.IdentityModel.Selectors;
using System.Diagnostics;
using XMPMS.Core;

namespace XMPMS.Net.WCF
{
    /// <summary>
    /// Custom credential validator for RPC link between master servers
    /// </summary>
    class RPCCredentialValidator : UserNamePasswordValidator
    {
        /// <summary>
        /// Validates the username and password of the WCF connection against those defined in the application config.
        /// </summary>
        /// <remarks>
        /// Since this function has to throw an exception to work properly, I have marked it as non-user code so that
        /// the exception can be ignored by the debugger even when exceptions are set to "throw" in the debugger settings.
        /// </remarks>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        [DebuggerNonUserCode]
        public override void Validate(string userName, string password)
        {
            if (userName != MasterServer.Settings.SyncServiceUsername || password != MasterServer.Settings.SyncServicePassword)
            {
                throw new FaultException("Invalid credentials");
            }
        }
    }
}
