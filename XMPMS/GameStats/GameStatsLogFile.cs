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
using System.Reflection;
using XMPMS.Interfaces;
using XMPMS.Core;

namespace XMPMS.GameStats
{
    /// <summary>
    /// Simple GameStas log which writes stat log entries to flat files
    /// </summary>
    public class GameStatsLogFile : IGameStatsLog
    {
        /// <summary>
        /// Application path
        /// </summary>
        private string applicationPath;

        /// <summary>
        /// Base path to write stats files
        /// </summary>
        private string statsFilePath;

        /// <summary>
        /// IMasterServerModule Interface
        /// </summary>
        public bool AutoLoad
        {
            get { return false; }
        }

        /// <summary>
        /// Initialise this stats log
        /// </summary>
        /// <param name="masterServer"></param>
        public void Initialise(MasterServer masterServer)
        {
            applicationPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            statsFilePath = Path.Combine(applicationPath, MasterServer.Settings.StatsFolder);
        }

        /// <summary>
        /// Release resources
        /// </summary>
        public void Shutdown()
        {
        }

        /// <summary>
        /// LogWriter a stat line to file
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <param name="server"></param>
        /// <param name="statLine"></param>
        public void Log(DateTime timeStamp, Server server, string statLine)
        {
            string serverDirectory = Path.Combine(statsFilePath, server.StatsID);
            string serverFile = Path.Combine(serverDirectory, String.Format("stats{0:yyyyMMdd}match{1}.txt", DateTime.Now, server.MatchID));

            if (!Directory.Exists(serverDirectory))
                Directory.CreateDirectory(serverDirectory);

            File.AppendAllText(serverFile, statLine + "\r\n");
        }
    }
}
