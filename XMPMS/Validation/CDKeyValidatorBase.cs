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
using System.Security.Cryptography;
using System.Text;
using XMPMS.Interfaces;
using XMPMS.Core;

namespace XMPMS.Validation
{
    /// <summary>
    /// Abstract base class for CD key validators
    /// </summary>
    public abstract class CDKeyValidatorBase : ICDKeyValidator
    {
        /// <summary>
        /// Crypto service provider for MD5 operations
        /// </summary>
        protected static MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

        /// <summary>
        /// Random number generator
        /// </summary>
        protected static Random RNG = new Random();

        /// <summary>
        /// IMasterServerModule Interface
        /// </summary>
        public bool AutoLoad
        {
            get { return false; }
        }

        /// <summary>
        /// IMasterServerModule Interface
        /// </summary>
        /// <param name="masterServer"></param>
        public virtual void Initialise(MasterServer masterServer)
        {
        }

        /// <summary>
        /// IMasterServerModule Interface
        /// </summary>
        public virtual void Shutdown()
        {
        }

        /// <summary>
        /// Begin a new validation operation
        /// </summary>
        /// <param name="contextID">Optional context identifer</param>
        /// <returns>Validation context to use for later calls</returns>
        public virtual ValidationContext BeginValidation(string contextID)
        {
            return new ValidationContext(contextID, this, null, RNG.Next(90000) + 10000);
        }

        /// <summary>
        /// Release any resources allocated during this validation
        /// </summary>
        /// <param name="context"></param>
        public virtual void EndValidation(ValidationContext context)
        {
            if (context != null)
            {
                context.EndValidation();
            }
        }

        /// <summary>
        /// Get the salt value to send to the player, this is a random number in the range 10000 -> 99999
        /// </summary>
        /// <returns>A random number to send as salt</returns>
        public virtual int GetSalt(ValidationContext context)
        {
            return context.Salt;
        }

        /// <summary>
        /// Validates the CD key against the database or validation algorithm
        /// </summary>
        /// <param name="keyHash">Hashed(MD5) cd key</param>
        /// <returns>True if the CD key was successfully validated</returns>
        public abstract bool ValidateKey(ValidationContext context);

        /// <summary>
        /// Validates the salted CD key against the specified salt 
        /// </summary>
        /// <param name="keyHash">Hashed CD key</param>
        /// <param name="saltedKeyHash">Hashed salted CD key</param>
        /// <param name="salt">Salt</param>
        /// <returns>True if the salted key was successfully validated</returns>
        public abstract bool ValidateSaltedKey(ValidationContext context);

        /// <summary>
        /// Generates the MD5 hash of the specified string
        /// </summary>
        /// <param name="text">Text to generate hash for</param>
        /// <returns>MD5 hash for the specified string</returns>
        protected static string EncodeMD5(string text)
        {
            byte[] md5data = md5.ComputeHash(Encoding.ASCII.GetBytes(text));
            return String.Format("{0:x2}{1:x2}{2:x2}{3:x2}{4:x2}{5:x2}{6:x2}{7:x2}{8:x2}{9:x2}{10:x2}{11:x2}{12:x2}{13:x2}{14:x2}{15:x2}", md5data[0], md5data[1], md5data[2], md5data[3], md5data[4], md5data[5], md5data[6], md5data[7], md5data[8], md5data[9], md5data[10], md5data[11], md5data[12], md5data[13], md5data[14], md5data[15]);
        }
    }
}
