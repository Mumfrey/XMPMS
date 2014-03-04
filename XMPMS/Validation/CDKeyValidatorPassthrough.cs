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
using XMPMS.Net.Client;
using XMPMS.Core;
using XMPMS.Net;

namespace XMPMS.Validation
{
    /// <summary>
    /// Simple example validator which uplinks to the epic master server to perform validation.
    /// </summary>
    public class CDKeyValidatorPassthrough : CDKeyValidatorBase
    {
        /// <summary>
        /// Begin the validation process, fetch the salt from the epic server
        /// </summary>
        /// <param name="contextID">Context identifier</param>
        /// <returns>New validation context to use</returns>
        public override ValidationContext BeginValidation(string contextID)
        {
            MasterServerClient msc = new MasterServerClient(MasterServer.Settings.PassthroughValidatorHost, MasterServer.Settings.PassthroughValidatorPort);
            int salt = msc.Connect();

            return new ValidationContext(contextID, this, msc, salt);
        }

        /// <summary>
        /// Release any resources required for this validation operation
        /// </summary>
        /// <param name="context"></param>
        public override void EndValidation(ValidationContext context)
        {
            ((MasterServerClient)context.ValidationHelper).Close();    
        }

        /// <summary>
        /// Validate the key hash, this always returns true since we have to do the validation all at once
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool ValidateKey(ValidationContext context)
        {
            return true;
        }

        /// <summary>
        /// Validate the key and salted key, this uplinks to the master server using the connection created by beginvalidation
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool ValidateSaltedKey(ValidationContext context)
        {
            string response = ((MasterServerClient)context.ValidationHelper).Login(context.KeyHash, context.SaltedKeyHash, context.ClientType, context.ClientVersion, "int");
            return (response == Protocol.LOGIN_RESPONSE_APPROVED);
        }
    }
}
