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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using XMPMS.Core;

namespace XMPMS.Service
{
    [RunInstaller(true)]
    public partial class MasterServerServiceProjectInstaller : Installer
    {
        public MasterServerServiceProjectInstaller()
        {
            InitializeComponent();

            // Set display name to assembly description
            MasterServerServiceInstaller.DisplayName = MasterServer.Title;

            // Get short service name from service class
            MasterServerServiceInstaller.ServiceName = MasterServerService.ShortServiceName;
        }
    }
}
