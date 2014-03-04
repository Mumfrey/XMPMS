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
using System.Diagnostics;

namespace XMPMS.Validation
{
    /// <summary>
    /// Simple implementation of CD key validator which validates all clients. Just for testing purposes really.
    /// </summary>
    public class CDKeyValidatorNull : CDKeyValidatorBase
    {
        /// <summary>
        /// Always returns true, dumps the key to the debug output for testing purposes
        /// </summary>
        /// <param name="keyHash"></param>
        /// <returns>Always returns true</returns>
        public override bool ValidateKey(ValidationContext context)
        {
            Debug.WriteLine("VALIDATE KEY " + context.KeyHash);
            return true;
        }

        /// <summary>
        /// Always returns true, dumps the keys to the debug output for testing purposes
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="saltedKeyHash"></param>
        /// <param name="salt"></param>
        /// <returns>Always returns true</returns>
        public override bool ValidateSaltedKey(ValidationContext context)
        {
            Debug.WriteLine("VALIDATE SALTED KEY " + context.KeyHash + " " + context.SaltedKeyHash + " " + context.Salt.ToString());
            return true;
        }
    }
}
