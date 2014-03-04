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
using System.Net;
using System.Reflection;
using XMPMS.Core;
using XMPMS.Interfaces;

#pragma warning disable 618
namespace XMPMS.Util
{
    /// <summary>
    /// GeoIP Database class, loads GeoIP data from CSV and allows queries
    /// </summary>
    public class IPBanManager : ICommandListener, IDisposable
    {
        /// <summary>
        /// Container struct for IP ban information
        /// </summary>
        private struct IPBanEntry
        {
            /// <summary>
            /// Empty IP ban entry
            /// </summary>
            private static IPBanEntry empty = new IPBanEntry(true);

            /// <summary>
            /// Gets the "empty" IP ban entry
            /// </summary>
            public static IPBanEntry Empty
            {
                get { return empty; } 
            }

            /// <summary>
            /// Banned IP address
            /// </summary>
            public IPAddress Address
            {
                get;
                private set;
            }

            /// <summary>
            /// Banned IP address mask
            /// </summary>
            public IPAddress NetMask
            {
                get;
                private set;
            }

            /// <summary>
            /// True if this entry is the empty entry
            /// </summary>
            public bool IsEmpty
            {
                get;
                private set;
            }

            /// <summary>
            /// Calculated masked address
            /// </summary>
            private long maskedAddress;

            /// <summary>
            /// Private constructor to support IPBanEntry.Empty instantiation
            /// </summary>
            /// <param name="empty">True if this is the empty entry</param>
            private IPBanEntry(bool empty)
                : this()
            {
                IsEmpty = empty;                
            }

            /// <summary>
            /// Create a new IP ban entry
            /// </summary>
            /// <param name="address">IP address string from file</param>
            /// <param name="mask">IP mask string from file</param>
            public IPBanEntry(string address, string mask)
                : this()
            {
                Address = IPAddress.Parse(address);
                NetMask = (mask == "*" || mask == "") ? IPAddress.Broadcast : IPAddress.Parse(mask);
                maskedAddress = MaskAddress(Address, NetMask);
            }

            /// <summary>
            /// Create a new IP ban entry
            /// </summary>
            /// <param name="address">IP address</param>
            /// <param name="mask">IP parsedMask</param>
            public IPBanEntry(IPAddress address, IPAddress mask)
                : this()
            {
                Address = address;
                NetMask = mask;
                maskedAddress = MaskAddress(Address, NetMask);
            }

            /// <summary>
            /// Calculate the masked value of an IP address
            /// </summary>
            /// <param name="address">IP address</param>
            /// <param name="mask">Address mask</param>
            /// <returns>Masked value</returns>
            public long MaskAddress(IPAddress address, IPAddress mask)
            {
                return address.Address & mask.Address;
            }

            /// <summary>
            /// Compares the specified address with the stored (masked) address
            /// </summary>
            /// <param name="address">Address to compare</param>
            /// <returns>True if the supplied address matches this filter</returns>
            public bool Match(IPAddress address)
            {
                return MaskAddress(address, NetMask) == maskedAddress;
            }

            /// <summary>
            /// Compare this object to another object
            /// </summary>
            /// <param name="obj">Object to compare to</param>
            /// <returns>True if the other object is equal to this object</returns>
            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is IPBanEntry)) return false;
                IPBanEntry e = (IPBanEntry)obj;
                return (e.Address == Address && e.NetMask == NetMask && e.IsEmpty == IsEmpty);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        /// <summary>
        /// Thread lock to prevent concurrent access to the ban list
        /// </summary>
        private object banListLock = new object();

        /// <summary>
        /// Ban list
        /// </summary>
        private List<IPBanEntry> banList = new List<IPBanEntry>();

        /// <summary>
        /// Ban list file
        /// </summary>
        private FileInfo banFile;

        /// <summary>
        /// Regex to match IP ban entries in the file
        /// </summary>
        private Regex ipBanRegex = new Regex(@"^(?<ip>[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})\s+(?<mask>(\*|[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}|))");

        /// <summary>
        /// Constructor, registers the command handler
        /// </summary>
        public IPBanManager()
        {
            ModuleManager.RegisterCommandListener(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            lock (banListLock)
            {
                banList.Clear();
            }

            ModuleManager.UnregisterCommandListener(this);
        }

        /// <summary>
        /// Load IP ban information from file
        /// </summary>
        /// <param name="fileName"></param>
        public void Load(string fileName)
        {
            // Get path relative to application path
            string assemblyPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            banFile = new FileInfo(Path.Combine(assemblyPath, fileName));

            if (banFile.Exists)
            {
                lock (banListLock)
                {
                    MasterServer.Log("Loading IP ban data from {0}...", fileName);

                    banList.Clear();

                    string[] lines = File.ReadAllLines(banFile.FullName);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        Match banInfo = ipBanRegex.Match(lines[i]);
                        if (banInfo.Success)
                            banList.Add(new IPBanEntry(banInfo.Groups["ip"].Value, banInfo.Groups["mask"].Value));
                    }

                    MasterServer.Log("IP ban list loaded. Got {0} record(s).", banList.Count);
                }
            }
        }

        /// <summary>
        /// Save IP ban information to disk
        /// </summary>
        public void Save()
        {
            string serialisedBanList = "";

            lock (banListLock)
            {
                for (int i = 0; i < banList.Count; i++)
                {
                    serialisedBanList += String.Format("{0}\t{1}\r\n", banList[i].Address.ToString(), banList[i].NetMask.ToString());
                }
            }

            if (Directory.Exists(banFile.Directory.FullName))
            {
                File.WriteAllText(banFile.FullName, serialisedBanList);
            }
        }

        /// <summary>
        /// Check whether the specified IP address appears in the ban list
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool IsBanned(IPAddress address)
        {
            lock (banListLock)
            {
                foreach (IPBanEntry banEntry in banList)
                {
                    if (banEntry.Match(address)) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Find a ban entry matching the specified IP address and mask
        /// </summary>
        /// <param name="address"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        private IPBanEntry FindBan(IPAddress address, IPAddress mask)
        {
            lock (banListLock)
            {
                foreach (IPBanEntry banEntry in banList)
                {
                    if (banEntry.Address.Equals(address) && banEntry.NetMask.Equals(mask)) return banEntry;
                }
            }

            return IPBanEntry.Empty;
        }

        /// <summary>
        /// Check whether a matching ban exists
        /// </summary>
        /// <param name="address"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        private bool BanExists(IPAddress address, IPAddress mask)
        {
            return !FindBan(address, mask).IsEmpty;
        }

        /// <summary>
        /// Add a new ban entry (if a matching entry does not already exist)
        /// </summary>
        /// <param name="address">Address for the new ban entry</param>
        /// <param name="mask">Net mask for the new ban entry</param>
        public void AddBan(string address, string mask)
        {
            IPAddress parsedAddress;
            IPAddress parsedMask = CIDRToMask(ref address, mask);

            if (IPAddress.TryParse(address, out parsedAddress))
            {
                if (BanExists(parsedAddress, parsedMask))
                {
                    MasterServer.LogMessage("Add ban failed, ban already exists.");
                    return;
                }
                else
                {
                    lock (banListLock)
                    {
                        banList.Add(new IPBanEntry(parsedAddress, parsedMask));
                        MasterServer.LogMessage("{0}/{1} added to ban list.", parsedAddress, parsedMask);
                        Save();
                    }
                }
            }
            else
            {
                MasterServer.LogMessage("Invalid address specified.");
                return;
            }
        }

        /// <summary>
        /// Remove a ban entry from the list (if a matching entry is found)
        /// </summary>
        /// <param name="address">Address of the entry to remove</param>
        /// <param name="mask">Netmask of the entry to remove</param>
        public void RemoveBan(string address, string mask)
        {
            if (address.ToLower() == "all")
            {
                lock (banListLock)
                {
                    banList.Clear();
                    Save();
                    MasterServer.LogMessage("IP ban list cleared");
                }
            }
            else
            {
                IPAddress parsedAddress;
                IPAddress parsedMask = CIDRToMask(ref address, mask);

                if (IPAddress.TryParse(address, out parsedAddress))
                {
                    IPBanEntry existingEntry = FindBan(parsedAddress, parsedMask);

                    if (existingEntry.IsEmpty)
                    {
                        MasterServer.LogMessage("Add ban failed, the specified ban was not found.");
                        return;
                    }
                    else
                    {
                        lock (banListLock)
                        {
                            banList.Remove(existingEntry);
                            MasterServer.LogMessage("{0}/{1} removed from ban list.", parsedAddress, parsedMask);
                            Save();
                        }
                    }
                }
                else
                {
                    MasterServer.LogMessage("Invalid address specified.");
                }
            }
        }

        /// <summary>
        /// Convert an address in CIDR notation to an address and netmask
        /// </summary>
        /// <param name="address"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        private IPAddress CIDRToMask(ref string address, string mask)
        {
            IPAddress result = IPAddress.Broadcast;

            Match maskMatch = Regex.Match(address, @"(?<ip>.+)\/(?<mask>[0-9]{1,2})$");

            if (mask == "255.255.255.255" && maskMatch.Success)
            {
                byte[] maskBytes = BitConverter.GetBytes(~(uint.MaxValue >> (Math.Min(Math.Max(int.Parse(maskMatch.Groups["mask"].Value), 0), 31))));
                result = IPAddress.Parse(String.Format("{3}.{2}.{1}.{0}", maskBytes[0], maskBytes[1], maskBytes[2], maskBytes[3]));
                address = maskMatch.Groups["ip"].Value;
            }
            else
            {
                IPAddress.TryParse(mask, out result);
            }

            return result;
        }

        /// <summary>
        /// List bans matching the specified address
        /// </summary>
        /// <param name="address">Address to check</param>
        public void ListMatchingBans(string address)
        {
            IPAddress parsedAddress;

            if (IPAddress.TryParse(address, out parsedAddress))
            {
                if (IsBanned(parsedAddress))
                {
                    MasterServer.LogMessage("The address {0} matches the following IP ban rules:", parsedAddress);

                    lock (banListLock)
                    {
                        foreach (IPBanEntry banEntry in banList)
                        {
                            if (banEntry.Match(parsedAddress))
                            {
                                MasterServer.LogMessage("  {0,-16} {1}", banEntry.Address, banEntry.NetMask);
                            }
                        }
                    }
                }
                else
                {
                    MasterServer.LogMessage("The address {0} does not match any existing ban rules", parsedAddress);
                }
            }
            else
            {
                MasterServer.LogMessage("Invalid address specified.");
            }
        }

        /// <summary>
        /// List current ban entries to the console
        /// </summary>
        public void ListBans()
        {
            lock (banListLock)
            {
                MasterServer.LogMessage("Current ban list entries:");

                foreach (IPBanEntry banEntry in banList)
                {
                    MasterServer.LogMessage("  {0,-16} {1}", banEntry.Address, banEntry.NetMask);
                }
            }
        }

        /// <summary>
        /// Process a console command
        /// </summary>
        /// <param name="command"></param>
        public void Command(string[] command)
        {
            if (command.Length > 0 && command[0].Trim() != "")
            {
                switch (command[0].ToLower())
                {
                    case "ban":
                        if (command.Length > 1)
                        {
                            switch (command[1].ToLower())
                            {
                                case "add":
                                    if (command.Length > 2)
                                    {
                                        AddBan(command[2], (command.Length > 3) ? command[3] : "255.255.255.255");
                                    }
                                    else
                                    {
                                        MasterServer.LogMessage("ban add <address>/<net>");
                                        MasterServer.LogMessage("ban add <address> <mask>");
                                    }
                                    break;

                                case "remove":
                                    if (command.Length > 2)
                                    {
                                        RemoveBan(command[2], (command.Length > 3) ? command[3] : "255.255.255.255");
                                    }
                                    else
                                    {
                                        MasterServer.LogMessage("ban remove all");
                                        MasterServer.LogMessage("ban remove <address>");
                                        MasterServer.LogMessage("ban remove <address>/<net>");
                                        MasterServer.LogMessage("ban remove <address> <mask>");
                                    }
                                    break;

                                case "test":
                                    if (command.Length > 2)
                                    {
                                        ListMatchingBans(command[2]);
                                    }
                                    else
                                    {
                                        MasterServer.LogMessage("ban test <address>");
                                    }
                                    break;

                                case "list":
                                    ListBans();
                                    break;
                            }
                        }
                        else
                        {
                            MasterServer.LogMessage("ban add       Add an IP ban");
                            MasterServer.LogMessage("ban remove    Remove an IP ban");
                            MasterServer.LogMessage("ban list      List current IP bans");
                            MasterServer.LogMessage("ban test      Check an address against the IP ban list");
                        }
                        break;

                    case "help":
                    case "?":
                        MasterServer.LogMessage("ban           Global IP ban commands");
                        break;
                }
            }
        }
    }
}
