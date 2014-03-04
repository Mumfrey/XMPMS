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
using System.Diagnostics;
using System.Text;
using XMPMS.Interfaces;
using XMPMS.Core;

namespace XMPMS.GameStats
{
    /// <summary>
    /// GameStats LogWriter which just dumps stats to the debug console
    /// </summary>
    public class GameStatsLogNull : IGameStatsLog
    {
        /// <summary>
        /// IMasterServerModule Interface
        /// </summary>
        public bool AutoLoad
        {
            get { return false; }
        }

        /// <summary>
        /// IMasterServerModule Interface
        /// </summary>
        /// <param name="masterServer"></param>
        public virtual void Initialise(MasterServer masterServer)
        {
        }

        /// <summary>
        /// IMasterServerModule Interface
        /// </summary>
        public virtual void Shutdown()
        {
        }

        /// <summary>
        /// LogWriter stats
        /// </summary>
        /// <param name="server"></param>
        /// <param name="statLine"></param>
        public virtual void Log(DateTime timeStamp, Server server, string statLine)
        {
            Debug.WriteLine(String.Format("[{0:yyyy-MM-dd HH:mm:ss}] STATS [{1}] {2}", timeStamp, server, statLine));
        }
    }
}
