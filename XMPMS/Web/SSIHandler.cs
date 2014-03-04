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
using XMPMS.Core;

namespace XMPMS.Web
{
    /// <summary>
    /// Contains functions for parsing pseudo- server-side-include style page variables and includes
    /// </summary>
    public abstract class SSIHandler
    {
        /// <summary>
        /// Regex used to replace variables in pages
        /// </summary>
        private static Regex varRegex = new Regex(@"\<!--#echo\s+var=\x22(?<var>[a-z0-9\-_\[\]\.]+)\x22\s+-->", RegexOptions.IgnoreCase);

        /// <summary>
        /// Regex used to parse #include directives in pages
        /// </summary>
        private static Regex includeRegex = new Regex(@"\<!--#include\s(?<directive>virtual|file)=\x22(?<filename>[^\x22]+)\x22\s+-->", RegexOptions.IgnoreCase);

        /// <summary>
        /// Local path for parsing the location of #include file="" directives
        /// </summary>
        private string localPath = null;

        /// <summary>
        /// Variables to replace in page
        /// </summary>
        protected Dictionary<string, string> variables = new Dictionary<string, string>();

        /// <summary>
        /// Read the contents of a file and parse include directives
        /// </summary>
        /// <param name="file">File to read</param>
        /// <returns>Parsed document content as a string</returns>
        protected string ReadFile(FileInfo file)
        {
            if (file.Exists)
            {
                return ParseDocument(file.Directory.FullName, File.ReadAllText(file.FullName));
            }

            return "";
        }

        /// <summary>
        /// Read the contents of a file, parse include directives and replace variables
        /// </summary>
        /// <param name="file">File to read</param>
        /// <returns>Parsed document content as a string</returns>
        protected string ReadFileAndParse(FileInfo file)
        {
            return ReplaceVariables(ReadFile(file));
        }

        /// <summary>
        /// Process #include directives
        /// </summary>
        /// <param name="input">Source document with includes</param>
        /// <returns>Parsed document content as a string</returns>
        protected string ParseDocument(string localPath, string input)
        {
            this.localPath = localPath;
            return includeRegex.Replace(input, new MatchEvaluator(this.IncludeCallback));
        }

        /// <summary>
        /// Callback function for replacing includes
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        private string IncludeCallback(Match match)
        {
            if (match.Groups["filename"].Value != "")
            {
                switch(match.Groups["directive"].Value.ToLower())
                {
                    case "virtual":
                        FileInfo includeFileVirtual = new FileInfo(Path.Combine(WebServer.WebRoot, match.Groups["filename"].Value.Replace("__SKIN__", WebServer.Skin)));
                        if (includeFileVirtual.Exists) return File.ReadAllText(includeFileVirtual.FullName);
                        break;

                    case "file":
                        if (localPath != null && Directory.Exists(localPath))
                        {
                            FileInfo includeFile = new FileInfo(Path.Combine(localPath, match.Groups["filename"].Value.Replace("__SKIN__", WebServer.Skin)));
                            if (includeFile.Exists) return File.ReadAllText(includeFile.FullName);
                        }
                        break;
                }
            }

            return match.Value;
        }

        /// <summary>
        /// Replace variables in a page with the values in the variables list
        /// </summary>
        /// <param name="input">Input string to replace variables in</param>
        /// <param name="clear">Clear variables immediately after performing the replacement</param>
        /// <returns>Page with all valid variables replaced</returns>
        protected string ReplaceVariables(string input, bool clear)
        {
            string result = varRegex.Replace(input, new MatchEvaluator(this.VariableReplacementCallback));
            variables.Clear();
            return result;
        }

        /// <summary>
        /// Replace variables in a page with the values in the variables list
        /// </summary>
        /// <param name="input">Input string to replace variables in</param>
        /// <returns>Page with all valid variables replaced</returns>
        protected string ReplaceVariables(string input)
        {
            return ReplaceVariables(input, false);
        }

        /// <summary>
        /// Callback function to replace variables in a page with their values
        /// </summary>
        /// <param name="match">Matched variable</param>
        /// <returns></returns>
        private string VariableReplacementCallback(Match match)
        {
            if (match.Groups["var"].Value != "" && variables.ContainsKey(match.Groups["var"].Value.ToLower()))
                return variables[match.Groups["var"].Value.ToLower()];

            return match.Value;
        }

        /// <summary>
        /// Replaces < and > brackets with codes
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        protected string EncodeBrackets(string input)
        {
            return input.Replace("<", "_x003c_").Replace(">", "_x003e_");
        }

        /// <summary>
        /// Helper function which formats a HTML IMG tag with the specified attributes
        /// </summary>
        /// <param name="url">Image URL</param>
        /// <param name="alt">Alt text</param>
        /// <param name="title">Title text (hover)</param>
        /// <param name="width">Width attribute</param>
        /// <param name="height">Height attribute</param>
        /// <returns>HTML IMG tag as text</returns>
        public static string Image(string url, string alt, string title, int width, int height)
        {
            return String.Format("<img src=\"{0}\" alt=\"{1}\" title=\"{2}\" width=\"{3}\" height=\"{4}\" />", url, alt, title, width, height);
        }
    }
}
