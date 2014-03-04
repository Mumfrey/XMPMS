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

// ======================================================================
// This file contains enums imported from MasterServerClient.uc in the
// UnrealScript source for U2XMP. These enums are used for
// player->masterserver communications.
// ======================================================================

namespace XMPMS.Util.UScript
{
    /// <summary>
    /// Header bytes expected on inbound packets from remote clients
    /// </summary>
    internal enum ClientToMaster : byte
    {
        Query,
        GetMOTD,
        QueryUpgrade,
    };

    /// <summary>
    /// Byte included as part of a QueryData struct to specify the type of query
    /// </summary>
    public enum QueryType : byte
    {
        Equals,
        NotEquals,
        LessThan,
        LessThanEquals,
        GreaterThan,
        GreaterThanEquals,
        Disabled
    };

    /// <summary>
    /// Struct containing query information from the player, a single query packet contains
    /// an array of these structs which specify the query criteria.
    /// </summary>
    public struct QueryData
    {
        /// <summary>
        /// Mapping of QueryType enum values to string symbols, used by ToString() for logging purposes
        /// </summary>
        private static string[] operatorSymbols = { "==", "!=", "<", "<=", ">", ">=", "--" };

        /// <summary>
        /// Match on this property
        /// </summary>
        public string Key;

        /// <summary>
        /// Match property to this value using the QueryType
        /// </summary>
        public string Value;

        /// <summary>
        /// Type of match to perform, eg. equals, notequals
        /// </summary>
        public QueryType QueryType;

        /// <summary>
        /// PopStructArray compatible struct constructor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="queryType"></param>
        public QueryData(string key, string value, byte queryType)
        {
            Key = key;
            Value = value;
            QueryType = (QueryType)queryType;
        }

        /// <summary>
        /// Get a string representation of this QueryData for logging purposes
        /// </summary>
        /// <returns>String representation of this QueryData</returns>
        public override string ToString()
        {
            return String.Format("{0}{1}\"{2}\"", Key, operatorSymbols[(int)QueryType], Value);
        }
    };
}
