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
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using XMPMS.Core;
using XMPMS.Interfaces;

namespace XMPMS.Util
{
    /// <summary>
    /// Manages the Packages MD5 database
    /// </summary>
    public class MD5Manager : ICommandListener, IDisposable
    {
        /// <summary>
        /// CSV line regex for matching valid CSV rows
        /// </summary>
        private Regex MD5LineRegex = new Regex("^\x22?(?<guid>[0-9a-f]{32})\x22?,\x22?(?<md5>[0-9a-f]{32})\x22?,\x22?(?<revision>[0-9]+)\x22?", RegexOptions.IgnoreCase);

        /// <summary>
        /// Thread lock to prevent concurrent access to the md5 database
        /// </summary>
        private object md5DataLock = new object();

        /// <summary>
        /// MD5 data 
        /// </summary>
        private List<MD5Entry> md5Data = new List<MD5Entry>();

        /// <summary>
        /// Highest revision number in the database
        /// </summary>
        public int maxRevision = 0;

        /// <summary>
        /// Constructor, registers command listener callback
        /// </summary>
        public MD5Manager()
        {
            ModuleManager.RegisterCommandListener(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            lock (md5DataLock)
            {
                md5Data.Clear();
            }

            ModuleManager.UnregisterCommandListener(this);
        }

        /// <summary>
        /// Load GeoIP data from a csv file
        /// </summary>
        /// <param name="fileName"></param>
        public void Load(string fileName)
        {
            // Get path relative to application path
            string assemblyPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            FileInfo md5DataFile = new FileInfo(Path.Combine(assemblyPath, fileName));

            if (md5DataFile.Exists)
            {
                lock (md5DataLock)
                {
                    // Reset any banList we have already
                    md5Data.Clear();
                    maxRevision = 0;

                    MasterServer.Log("Loading MD5 data from {0}...", fileName);

                    string[] lines = File.ReadAllLines(md5DataFile.FullName);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        Match lineMatch = MD5LineRegex.Match(lines[i]);

                        if (lineMatch.Success)
                        {
                            // TryParse is not required because only valid integers can match the regex
                            int revision = int.Parse(lineMatch.Groups["revision"].Value);

                            // Add the entry to the database
                            md5Data.Add(new MD5Entry(lineMatch.Groups["guid"].Value, lineMatch.Groups["md5"].Value, revision));

                            // Store highest revision number
                            maxRevision = Math.Max(revision, maxRevision);
                        }
                    }

                    MasterServer.Log("MD5 database loaded. Got {0} record(s).", md5Data.Count);
                }
            }
            else
            {
                MasterServer.Log("MD5 data file was not found, not loading MD5 database");
            }
        }

        /// <summary>
        /// Get package MD5 entries which are newer than the specified revision
        /// </summary>
        /// <param name="currentRevision">Current revision on the target server</param>
        /// <returns>List of MD5 entries to update</returns>
        public List<MD5Entry> Get(int currentRevision)
        {
            List<MD5Entry> updates = new List<MD5Entry>();

            if (currentRevision < maxRevision)
            {
                lock (md5DataLock)
                {
                    foreach (MD5Entry entry in md5Data)
                    {
                        if (entry.Revision > currentRevision)
                            updates.Add(entry);
                    }
                }
            }

            return updates;
        }

        /// <summary>
        /// Callback from the module manager when a console command is issued
        /// </summary>
        /// <param name="command"></param>
        public void Command(string[] command)
        {
            if (command.Length > 0 && command[0].Trim() != "")
            {
                switch (command[0].ToLower())
                {
                    case "md5":
                        if (command.Length > 1)
                        {
                            switch (command[1].ToLower())
                            {
                                case "load":
                                    Load(MasterServer.Settings.MD5DataFile);
                                    break;

                                case "sync":
                                    MasterServer.Instance.ServerList.SyncMD5(Get(0));
                                    break;
                            }
                        }
                        else
                        {
                            MasterServer.LogMessage("md5 load      Reload MD5 database");
                            MasterServer.LogMessage("md5 sync      Force MD5 refresh on all connected servers");
                        }
                        break;

                    case "help":
                    case "?":
                        MasterServer.LogMessage("md5           MD5 database commands");
                        break;
                }
            }
        }
    }
}
