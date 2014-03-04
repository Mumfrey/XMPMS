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

namespace XMPMS.Validation
{
    /// <summary>
    /// Simple implementation of CD key validator which validates clients against a single fixed key
    /// </summary>
    public abstract class CDKeyValidatorSingle : CDKeyValidatorBase
    {
        /// <summary>
        /// Fixed key to validate clients
        /// </summary>
        private string cdKey = "";

        /// <summary>
        /// Since the key is hashed, we need only compute the MD5 once rather than for every query
        /// </summary>
        private string cdKeyHash = "";

        /// <summary>
        /// Create a new single key validator for the specified key
        /// </summary>
        /// <param name="key">Fixed key to use when validating</param>
        public CDKeyValidatorSingle(string key)
        {
            cdKey = key;
            cdKeyHash = EncodeMD5(cdKey);
        }

        /// <summary>
        /// Validates the CD key against the fixed key
        /// </summary>
        /// <param name="keyHash">Hashed(MD5) cd key</param>
        /// <returns>True if the CD key was successfully validated</returns>
        public override bool ValidateKey(ValidationContext context)
        {
            return context.KeyHash == cdKeyHash;            
        }

        /// <summary>
        /// Validates the salted CD key against the salted fixed key
        /// </summary>
        /// <param name="keyHash">Hashed CD key</param>
        /// <param name="saltedKeyHash">Hashed salted CD key</param>
        /// <param name="salt">Salt</param>
        /// <returns>True if the salted key was successfully validated</returns>
        public override bool ValidateSaltedKey(ValidationContext context)
        {
            string saltedCdKey = EncodeMD5(cdKey + context.Salt.ToString());
            return context.SaltedKeyHash == saltedCdKey;
        }
    }
}
