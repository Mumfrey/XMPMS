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
using System.Reflection;
using System.ComponentModel;

namespace XMPMS.Net
{
    /// <summary>
    /// Type of operating system reported by remote host when connecting
    /// </summary>
    public enum OperatingSystem : byte
    {
        [Description("Windows 95")]
        Windows95 = 0x00,

        [Description("Windows 98")]
        Windows98 = 0x01,

        [Description("Windows 2000")]
        Windows2000 = 0x03,

        [Description("Windows XP")]
        WindowsXP = 0x04,

        [Description("Windows NT")]
        WindowsNT = 0x05,

        [Description("Unknown Operating System")]
        UnknownOS = 0xFF
    }

    /// <summary>
    /// Contains static methods for accessing information about enums
    /// </summary>
    public static class EnumInfo
    {
        /// <summary>
        /// Get the description of an enum value
        /// </summary>
        /// <param name="value">Enum value to get description for</param>
        /// <returns>Value from description attribute if present, or enum value string if not</returns>
        public static string Description(Enum value)
        {
            DescriptionAttribute[] attributes = (DescriptionAttribute[])value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);
            return (attributes.Length > 0) ? attributes[0].Description : value.ToString();
        }
    }
}
