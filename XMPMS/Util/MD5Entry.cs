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

namespace XMPMS.Util
{
    /// <summary>
    /// Stores package MD5 data
    /// </summary>
    public struct MD5Entry
    {
        /// <summary>
        /// GUID of the package
        /// </summary>
        public string PackageGUID
        {
            get;
            private set;
        }

        /// <summary>
        /// MD5 of the package
        /// </summary>
        public string PackageMD5
        {
            get;
            private set;
        }

        /// <summary>
        /// MD5 revision for the package
        /// </summary>
        public int Revision
        {
            get;
            private set;
        }

        /// <summary>
        /// Create a new package MD5 entry with the specified data
        /// </summary>
        /// <param name="guid">GUID of the package</param>
        /// <param name="md5">MD5 of the package</param>
        /// <param name="revision">MD5 revision</param>
        public MD5Entry(string guid, string md5, int revision)
            : this()
        {
            PackageGUID = guid;
            PackageMD5 = md5;
            Revision = revision;
        }
    }
}
