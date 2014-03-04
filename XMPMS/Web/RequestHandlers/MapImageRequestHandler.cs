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
using System.IO;
using System.Text;

namespace XMPMS.Web.RequestHandlers
{
    /// <summary>
    /// Handles requests for map images for the server details page, this is a simple override to
    /// support showing the "unknown" image if an image doesn't exist for the specified map.
    /// </summary>
    public class MapImageRequestHandler : SupportFileRequestHandler
    {
        /// <summary>
        /// Overridden so that when an image doesn't exist we can return "unknown.jpg"
        /// </summary>
        /// <param name="requestPath"></param>
        /// <returns></returns>
        protected override FileInfo GetRealFile(string requestPath)
        {
            if (requestPath.ToLower().StartsWith("img/maps/"))
            {
                FileInfo mapImage = new FileInfo(Path.Combine(WebServer.WebRoot, requestPath));
                return (mapImage.Exists) ? mapImage : new FileInfo(Path.Combine(WebServer.WebRoot, "img/maps/unknown.jpg"));
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Needs to be higher than that of SupportFileRequestHandler so that this gets called first
        /// </summary>
        public override int Priority
        {
            get { return 75; }
        }
    }
}
