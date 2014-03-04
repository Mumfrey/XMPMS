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

namespace XMPMS.Util
{
    /// <summary>
    /// Parses XMP colour codes in strings and returns HTML-formatted text with appropriate colours
    /// </summary>
    public static class ColourCodeParser
    {
        /// <summary>
        /// Array containing mapping of colour codes to the HTML colours they represent
        /// </summary>
        private static Dictionary<int, string> Colours = new Dictionary<int, string>();

        /// <summary>
        /// Static ctor - init colours
        /// </summary>
        static ColourCodeParser()
        {
            InitColours();
        }

        /// <summary>
        /// Populates the mapping table with the XMP colour code mappings
        /// </summary>
        static void InitColours()
        {
            Colours.Clear();
            Colours.Add(2, "#F0F8FF");
            Colours.Add(3, "#FAEBD7");
            Colours.Add(4, "#00FFFF");
            Colours.Add(5, "#7FFFD4");
            Colours.Add(6, "#F0FFFF");
            Colours.Add(7, "#F5F5DC");
            Colours.Add(8, "#FFE4C4");
            /* Colours.Add(9, "#000000"); */ Colours.Add(9, "#808080"); // Grey instead of black so it shows up
            Colours.Add(10, "#FFEBCD");
            Colours.Add(11, "#0000FF");
            Colours.Add(12, "#8A2BE2");
            Colours.Add(13, "#A52A2A");
            Colours.Add(14, "#DEB887");
            Colours.Add(15, "#5F9EA0");
            Colours.Add(16, "#7FFF00");
            Colours.Add(17, "#D2691E");
            Colours.Add(18, "#FF7F50");
            Colours.Add(19, "#6495ED");
            Colours.Add(20, "#FFF8DC");
            Colours.Add(21, "#DC143C");
            Colours.Add(22, "#00FFFF");
            Colours.Add(23, "#00008B");
            Colours.Add(24, "#008B8B");
            Colours.Add(25, "#B8860B");
            Colours.Add(26, "#A9A9A9");
            Colours.Add(27, "#006400");
            Colours.Add(28, "#BDB76B");
            Colours.Add(29, "#8B008B");
            Colours.Add(30, "#556B2F");
            Colours.Add(31, "#FF8C00");
            Colours.Add(32, "#9932CC");
            Colours.Add(33, "#8B0000");
            Colours.Add(34, "#E9967A");
            Colours.Add(35, "#8FBC8F");
            Colours.Add(36, "#483D8B");
            Colours.Add(37, "#2F4F4F");
            Colours.Add(38, "#00CED1");
            Colours.Add(39, "#9400D3");
            Colours.Add(40, "#FF1493");
            Colours.Add(41, "#00BFFF");
            Colours.Add(42, "#696969");
            Colours.Add(43, "#1E90FF");
            Colours.Add(44, "#B22222");
            Colours.Add(45, "#FFFAF0");
            Colours.Add(46, "#228B22");
            Colours.Add(47, "#FF00FF");
            Colours.Add(48, "#DCDCDC");
            Colours.Add(49, "#F8F8FF");
            Colours.Add(50, "#FFD700");
            Colours.Add(51, "#DAA520");
            Colours.Add(52, "#808080");
            Colours.Add(53, "#008000");
            Colours.Add(54, "#ADFF2F");
            Colours.Add(55, "#F0FFF0");
            Colours.Add(56, "#FF69B4");
            Colours.Add(57, "#CD5C5C");
            Colours.Add(58, "#4B0082");
            Colours.Add(59, "#FFFFF0");
            Colours.Add(60, "#F0E68C");
            Colours.Add(61, "#E6E6FA");
            Colours.Add(62, "#FFF0F5");
            Colours.Add(63, "#7CFC00");
            Colours.Add(64, "#FFFACD");
            Colours.Add(65, "#ADD8E6");
            Colours.Add(66, "#F08080");
            Colours.Add(67, "#E0FFFF");
            Colours.Add(68, "#FAFAD2");
            Colours.Add(69, "#90EE90");
            Colours.Add(70, "#D3D3D3");
            Colours.Add(71, "#FFB6C1");
            Colours.Add(72, "#FFA07A");
            Colours.Add(73, "#20B2AA");
            Colours.Add(74, "#87CEFA");
            Colours.Add(75, "#778899");
            Colours.Add(76, "#B0C4DE");
            Colours.Add(77, "#FFFFE0");
            Colours.Add(78, "#00FF00");
            Colours.Add(79, "#32CD32");
            Colours.Add(80, "#FAF0E6");
            Colours.Add(81, "#FF00FF");
            Colours.Add(82, "#800000");
            Colours.Add(83, "#66CDAA");
            Colours.Add(84, "#0000CD");
            Colours.Add(85, "#BA55D3");
            Colours.Add(86, "#9370D8");
            Colours.Add(87, "#3CB371");
            Colours.Add(88, "#7B68EE");
            Colours.Add(89, "#00FA9A");
            Colours.Add(90, "#48D1CC");
            Colours.Add(91, "#C71585");
            Colours.Add(92, "#191970");
            Colours.Add(93, "#F5FFFA");
            Colours.Add(94, "#FFE4E1");
            Colours.Add(95, "#FFE4B5");
            Colours.Add(96, "#FFDEAD");
            Colours.Add(97, "#000080");
            Colours.Add(98, "#FDF5E6");
            Colours.Add(99, "#808000");
            Colours.Add(100, "#6B8E23");
            Colours.Add(101, "#FFA500");
            Colours.Add(102, "#FF4500");
            Colours.Add(103, "#DA70D6");
            Colours.Add(104, "#EEE8AA");
            Colours.Add(105, "#98FB98");
            Colours.Add(106, "#AFEEEE");
            Colours.Add(107, "#D87093");
            Colours.Add(108, "#FFEFD5");
            Colours.Add(109, "#FFDAB9");
            Colours.Add(110, "#CD853F");
            Colours.Add(111, "#FFC0CB");
            Colours.Add(112, "#DDA0DD");
            Colours.Add(113, "#B0E0E6");
            Colours.Add(114, "#800080");
            Colours.Add(115, "#FF0000");
            Colours.Add(116, "#BC8F8F");
            Colours.Add(117, "#4169E1");
            Colours.Add(118, "#8B4513");
            Colours.Add(119, "#FA8072");
            Colours.Add(120, "#F4A460");
            Colours.Add(121, "#2E8B57");
            Colours.Add(122, "#FFF5EE");
            Colours.Add(123, "#A0522D");
            Colours.Add(124, "#C0C0C0");
            Colours.Add(125, "#87CEEB");
            Colours.Add(126, "#6A5ACD");
            Colours.Add(127, "#708090");
            Colours.Add(128, "#FFFAFA");
            Colours.Add(129, "#00FF7F");
            Colours.Add(130, "#4682B4");
            Colours.Add(131, "#D2B48C");
            Colours.Add(132, "#008080");
            Colours.Add(133, "#D8BFD8");
            Colours.Add(134, "#FF6347");
            Colours.Add(135, "#40E0D0");
            Colours.Add(136, "#EE82EE");
            Colours.Add(137, "#F5DEB3");
            Colours.Add(138, "#FFFFFF");
            Colours.Add(139, "#F5F5F5");
            Colours.Add(140, "#FFFF00");
            Colours.Add(141, "#9ACD32");
        }

        /// <summary>
        /// Returns true if the supplied text contains colour codes
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool HasColourCodes(string text)
        {
            return (Regex.Match(text, @"\^#.")).Success;
        }

        /// <summary>
        /// Colourise the supplied text, but only if it contains colour codes (avoids wrapping in a <span> unnecessarily)
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string ColouriseConditional(string text)
        {
            if (HasColourCodes(text))
                return Colourise(text);
            else
                return text;
        }

        /// <summary>
        /// Replace XMP colour codes with appropriate HTML
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Colourise(string text)
        {
            Match m = Regex.Match(text, @"\^#(?<value>.)");

            while (m.Success)
            {
                text = String.Format("{0}</span><span style=\"color:{1}\">{2}", text.Substring(0, m.Index), GetColour(m.Groups["value"].Value), text.Substring(m.Index + 3));
                m = Regex.Match(text, @"\^#(?<value>.)");
            }

            return String.Format("<span style=\"color: #FFFFFF\">{0}</span>", text);
        }

        /// <summary>
        /// Return the colour with the specified mapping index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string GetColour(string index)
        {
            int i = (int)(byte)index[0] + 1;
            return GetColour(i);
        }
        
        /// <summary>
        /// Return the colour with the specified mapping index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string GetColour(int index)
        {
            if (Colours.ContainsKey(index)) return Colours[index];
            return "#FFFFFF";
        }

        /// <summary>
        /// Do no parsing of colour codes but remove all colour codes from the specified string
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string StripColourCodes(string text)
        {
            return Regex.Replace(text, @"\^#.", "");
        }
    }
}
