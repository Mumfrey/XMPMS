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
using System.Net;
using System.Text;

namespace XMPMS.Util
{
    /// <summary>
    /// Struct which stores information about a single GeoIP database entry
    /// </summary>
    public struct GeoIPEntry
    {
        /// <summary>
        /// Invalid GeoIP entry
        /// </summary>
        private static GeoIPEntry empty = new GeoIPEntry(true);

        /// <summary>
        /// Represents an invalid GeoIP entry
        /// </summary>
        public static GeoIPEntry Empty
        {
            get { return empty; }
        }

        /// <summary>
        /// Start of the IP range this entry applies too
        /// </summary>
        public IPAddress StartAddress
        {
            get;
            private set;
        }

        /// <summary>
        /// End of the IP range this entry applies to
        /// </summary>
        public IPAddress EndAddress
        {
            get;
            private set;
        }

        /// <summary>
        /// Start IP as long integer that this entry applies to
        /// </summary>
        public long Start
        {
            get;
            private set;
        }

        /// <summary>
        /// End IP as long integer that this entry applies to
        /// </summary>
        public long End
        {
            get;
            private set;
        }

        /// <summary>
        /// Country code
        /// </summary>
        public string Country
        {
            get;
            private set;
        }

        /// <summary>
        /// Extra, possibly more specific, location information
        /// </summary>
        public string Location
        {
            get;
            private set;
        }

        /// <summary>
        /// True if this is an empty entry
        /// </summary>
        public bool IsEmpty
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructor for GeoIP.Empty
        /// </summary>
        /// <param name="empty"></param>
        private GeoIPEntry(bool empty)
            : this()
        {
            IsEmpty = empty;

            if (empty) Country = "00";
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="startAddress">Start of range (dotted quad)</param>
        /// <param name="endAddress">End of range (dotted quad)</param>
        /// <param name="start">Start of range (long)</param>
        /// <param name="end">End of range (long)</param>
        /// <param name="country">Country code</param>
        /// <param name="location">More specific location information</param>
        internal GeoIPEntry(string startAddress, string endAddress, string start, string end, string country, string location)
            : this()
        {
            IPAddress parsedStartAddress, parsedEndAddress;
            long parsedStart, parsedEnd;

            if (IPAddress.TryParse(startAddress, out parsedStartAddress)) StartAddress = parsedStartAddress;
            if (IPAddress.TryParse(endAddress, out parsedEndAddress)) EndAddress = parsedEndAddress;

            //if (long.TryParse(start, out parsedStart)) Start = Reverse(parsedStart);
            //if (long.TryParse(end, out parsedEnd)) End = Reverse(parsedEnd);

            if (long.TryParse(start, out parsedStart)) Start = parsedStart;
            if (long.TryParse(end, out parsedEnd)) End = parsedEnd;

            Country = country;
            Location = location;
        }

        /// <summary>
        /// Reverses the bytes in a 32-bit unsigned integer, used to convert IPAddress.Long to the long format used by GeoIP
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static long Reverse(long value)
        {
            return (0xFF000000 & (value << 24)) | (0xFF0000 & (value << 8)) | (0xFF00 & (value >> 8)) | (0xFF & (value >> 24));
        }
    }
}
