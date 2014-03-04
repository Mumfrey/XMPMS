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
using System.IO;
using System.Net;
using System.Reflection;
using XMPMS.Core;

#pragma warning disable 618
namespace XMPMS.Util
{
    /// <summary>
    /// GeoIP Database class, loads GeoIP data from CSV and allows queries
    /// </summary>
    public class GeoIP : IDisposable
    {
        /// <summary>
        /// GeoIP data 
        /// </summary>
        private List<GeoIPEntry> data = new List<GeoIPEntry>();

        /// <summary>
        /// Load GeoIP data from a csv file
        /// </summary>
        /// <param name="fileName"></param>
        public void Load(string fileName)
        {
            // Get path relative to application path
            string assemblyPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            FileInfo geoip = new FileInfo(Path.Combine(assemblyPath, fileName));

            if (geoip.Exists)
            {
                MasterServer.Log("Loading GeoIP data from {0}...", fileName);

                string[] lines = File.ReadAllLines(geoip.FullName);

                for (int i = 0; i < lines.Length; i++)
                {
                    // Simple (naive) way of splitting a CSV line whilst preserving quotation, probably quite easy to break
                    string[] parts = lines[i].Substring(1, lines[i].Length - 2).Replace("\",\"", "\x0a").Split('\x0a');

                    // Sanity check
                    if (parts.Length == 6)
                        data.Add(new GeoIPEntry(parts[0], parts[1], parts[2], parts[3], parts[4], parts[5]));
                }

                MasterServer.Log("GeoIP database loaded. Got {0} record(s).", data.Count);
            }
            else
            {
                MasterServer.Log("GeoIP data file was not found, not loading GeoIP database");
            }
        }

        /// <summary>
        /// Perform a GeoIP match on the 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public GeoIPEntry Match(IPAddress address)
        {
            long addr = GeoIPEntry.Reverse(address.Address);

            for (int i = 0; i < data.Count; i++)
            {
                if (addr >= data[i].Start && addr <= data[i].End)
                {
                    return data[i];
                }                
            }

            return GeoIPEntry.Empty;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            data.Clear();
        }
    }
}
