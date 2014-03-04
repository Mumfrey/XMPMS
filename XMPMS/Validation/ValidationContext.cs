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
using XMPMS.Interfaces;

namespace XMPMS.Validation
{
    /// <summary>
    /// A validation context keeps track of a validation operation, storing the generated salt
    /// and any related resources in a single object which encapsulates the validation transaction
    /// </summary>
    /// <typeparam name="T">Type of helper class</typeparam>
    public class ValidationContext
    {
        /// <summary>
        /// Flag which keeps track of whether the player info has been set
        /// </summary>
        private bool clientInfoSet;

        /// <summary>
        /// Context Identifier (optionally assigned during BeginValidation)
        /// </summary>
        public string ContextID
        {
            get;
            protected set;
        }

        /// <summary>
        /// Reference to the validator
        /// </summary>
        public ICDKeyValidator Validator
        {
            get;
            protected set;
        }

        /// <summary>
        /// Helper object for this context
        /// </summary>
        public object ValidationHelper
        {
            get;
            protected set;
        }

        /// <summary>
        /// Salt generated for this context. Classes performing validation SHOULD NOT retrieve the salt
        /// from this property, but should instead retrieve it from the Validator by calling the GetSalt
        /// function with this context as the argument.
        /// </summary>
        public int Salt
        {
            get;
            protected set;
        }

        /// <summary>
        /// CD key hash, set using the SetClientInfo() function
        /// </summary>
        public string KeyHash
        {
            get;
            protected set;
        }

        /// <summary>
        /// Salted CD key hash, set using the SetClientInfo() function
        /// </summary>
        public string SaltedKeyHash
        {
            get;
            protected set;
        }

        /// <summary>
        /// Type of player (eg. CLIENT or SERVER), set using the SetClientInfo() function
        /// </summary>
        public string ClientType
        {
            get;
            protected set;
        }

        /// <summary>
        /// Client version, set using the SetClientInfo() function
        /// </summary>
        public int ClientVersion
        {
            get;
            protected set;
        }

        /// <summary>
        /// Create a new validation context with the specified properties
        /// </summary>
        /// <param name="contextID">(Optional) identifier used to track this context</param>
        /// <param name="validator">Validator which owns this context</param>
        /// <param name="validationHelper">Helper object which is being used in this context</param>
        /// <param name="salt">Validation salt</param>
        public ValidationContext(string contextID, ICDKeyValidator validator, object validationHelper, int salt)
        {
            ContextID        = contextID;
            Validator        = validator;
            ValidationHelper = validationHelper;
            Salt             = salt;

            KeyHash          = "";
            SaltedKeyHash    = "";
            ClientType       = "NONE";
            ClientVersion    = 0;
        }

        /// <summary>
        /// Sets the player information to validate, before the validation occurs
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="saltedHash"></param>
        /// <param name="type"></param>
        /// <param name="version"></param>
        public void SetClientInfo(string keyHash, string saltedHash, string type, int version)
        {
            if (!clientInfoSet)
            {
                clientInfoSet = true;       // Flag

                KeyHash       = keyHash;
                SaltedKeyHash = saltedHash;
                ClientType    = type;
                ClientVersion = version;
            }
            else
            {
                throw new InvalidOperationException("Client information was already set in the specified context. Create a new context to validate a new client");
            }
        }

        /// <summary>
        /// Clean up or release resources, do not call this function directly, you should call EndValidation on
        /// the validator itself passing this object as the parameter
        /// </summary>
        public virtual void EndValidation()
        {
            Validator = null;
            ValidationHelper = null;
        }
    }
}
