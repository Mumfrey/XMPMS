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
using XMPMS.Validation;

namespace XMPMS.Interfaces
{
    /// <summary>
    /// Interface for CD key validator modules
    /// </summary>
    public interface ICDKeyValidator : IMasterServerModule
    {
        /// <summary>
        /// Start a new validation process
        /// </summary>
        /// <param name="contextID">(Optional) validation context identifier</param>
        /// <returns>New validation context object to use for subsequent requests</returns>
        ValidationContext BeginValidation(string contextID);

        /// <summary>
        /// Release any resources allocated to the specified context
        /// </summary>
        /// <param name="context">Validation context to use for validation state</param>
        void EndValidation(ValidationContext context);

        /// <summary>
        /// Get the salt value to send to the player, this is a random number in the range 10000 -> 99999.
        /// Any classes performing validation should call this function to get the salt instead of retrieving the
        /// salt from the ValidationContext, as the validator may wish to return a different salt or perform
        /// additional processing using the salt value.
        /// </summary>
        /// <param name="context">Validation context to use for validation state</param>
        /// <returns>A random number to send as salt</returns>
        int GetSalt(ValidationContext context);

        /// <summary>
        /// Validates the CD key against the database or validation algorithm
        /// </summary>
        /// <param name="keyHash">Hashed(MD5) cd key</param>
        /// <param name="context">Validation context to use for validation state</param>
        /// <returns>True if the CD key was successfully validated</returns>
        bool ValidateKey(ValidationContext context);

        /// <summary>
        /// Validates the salted CD key
        /// </summary>
        /// <param name="context">Validation context to use for validation state</param>
        /// <returns>True if the salted key was successfully validated</returns>
        bool ValidateSaltedKey(ValidationContext context);
    }
}
