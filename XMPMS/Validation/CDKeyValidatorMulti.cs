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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using XMPMS.Core;
using XMPMS.Interfaces;

namespace XMPMS.Validation
{
    /// <summary>
    /// Simple implementation of CD key validator which validates clients against a list of keys stored in a file
    /// </summary>
    public class CDKeyValidatorMulti : CDKeyValidatorBase, ICommandListener
    {
        /// <summary>
        /// Regex used to match valid CD keys in the file
        /// </summary>
        private Regex cdKeyRegex = new Regex(@"[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}", RegexOptions.IgnoreCase);

        private object cdKeyLock = new object();

        /// <summary>
        /// Table of valid CD key hashes
        /// </summary>
        private Hashtable CDKeys;

        /// <summary>
        /// Initialise the CD key database
        /// </summary>
        /// <param name="masterServer">Reference to the master server</param>
        public override void Initialise(MasterServer masterServer)
        {
            base.Initialise(masterServer);
            LoadKeyList();
        }

        protected virtual void LoadKeyList()
        {
            lock (cdKeyLock)
            {
                // Only initialise if not done previously
                if (CDKeys == null)
                {
                    MasterServer.Log("Initialising CD Key database...");

                    CDKeys = new Hashtable();

                    string assemblyPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
                    FileInfo cdKeyFile = new FileInfo(Path.Combine(assemblyPath, MasterServer.Settings.CDKeyFile));

                    if (cdKeyFile.Exists)
                    {
                        string[] lines = File.ReadAllLines(cdKeyFile.FullName);

                        for (int i = 0; i < lines.Length; i++)
                        {
                            string key = lines[i].Trim().ToUpper();

                            if (cdKeyRegex.IsMatch(key))
                            {
                                // Store the CD key with the hash as the table key and the key as the value
                                CDKeys.Add(EncodeMD5(key), key);
                            }
                        }

                        MasterServer.Log("CD Key database loaded. Got {0} valid key(s).", CDKeys.Count);
                    }
                    else
                    {
                        MasterServer.Log("CD Key database not loaded. File not found.");
                    }
                }
            }
        }

        /// <summary>
        /// Check whether the key is valid
        /// </summary>
        /// <param name="keyHash">Hashed key from the player</param>
        /// <returns></returns>
        public override bool ValidateKey(ValidationContext context)
        {
            return CDKeys.ContainsKey(context.KeyHash);
        }

        /// <summary>
        /// Check whether the salted key is valid
        /// </summary>
        /// <param name="keyHash">Hashed key</param>
        /// <param name="saltedKeyHash">Hashed salted key</param>
        /// <param name="salt">Salt sent to the player</param>
        /// <returns></returns>
        public override bool ValidateSaltedKey(ValidationContext context)
        {
            if (CDKeys.ContainsKey(context.KeyHash))
            {
                string saltedCdKey = EncodeMD5(CDKeys[context.KeyHash].ToString() + context.Salt.ToString());
                return context.SaltedKeyHash == saltedCdKey;
            }

            return false;
        }

        /// <summary>
        /// Handle console commands
        /// </summary>
        /// <param name="command"></param>
        public void Command(string[] command)
        {
            if (command.Length > 0)
            {
                switch (command[0].ToLower())
                {
                    case "help":
                    case "?":
                        MasterServer.LogMessage("cdkeys        CD Key database functions");
                        break;

                    case "cdkeys":
                        if (command.Length > 1)
                        {
                            switch (command[1].ToLower())
                            {
                                case "load":
                                    CDKeys = null;
                                    LoadKeyList();
                                    break;
                            }
                        }
                        else
                        {
                            MasterServer.LogMessage("cdkeys load   Reload CD key information from the file");
                        }
                        break;
                }
            }
        }
    }
}
