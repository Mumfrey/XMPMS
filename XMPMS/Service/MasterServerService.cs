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
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.ServiceProcess;
using System.Text;
using System.Configuration.Install;
using System.Reflection;
using XMPMS.Core;
using XMPMS.Validation;
using XMPMS.Interfaces;
using XMPMS.UserInterface.Telnet;

namespace XMPMS.Service
{
    /// <summary>
    /// Windows NT Service for Master Server
    /// </summary>
    partial class MasterServerService : ServiceBase
    {
        /// <summary>
        /// Current installation state of the service
        /// </summary>
        public enum ServiceState
        {
            NotInstalled,
            InstalledStopped,
            InstalledRunning
        }

        /// <summary>
        /// Short name for the service, used when registering and deregistering with SCM
        /// </summary>
        public const string ShortServiceName = "ue2masterserver";

        /// <summary>
        /// Basic constructor
        /// </summary>
        public MasterServerService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Service is being started
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            MasterServer.ParseCommandLineVars(String.Format("xmpms {0}", String.Join(" ", args)));

            ICommandInterface commandInterface = ModuleManager.GetModule<ICommandInterface>(typeof(TelnetCommandLine));

            if (!MasterServer.Start(null, commandInterface))
            {
                throw new InvalidOperationException("Error initialising service");
            }
        }

        /// <summary>
        /// Service is being stopped
        /// </summary>
        protected override void OnStop()
        {
            MasterServer.Stop();
        }

        /// <summary>
        /// Machine is shutting down - shut down the master server
        /// </summary>
        protected override void OnShutdown()
        {
            MasterServer.Stop();
        }

        /// <summary>
        /// Service entry point, called by the main entry point if running as a service
        /// </summary>
        public static void ServiceMain(MasterServerService service, string[] args)
        {
            ServiceBase.Run(new ServiceBase[] { service });
        }

        /// <summary>
        /// Gets the State of the service
        /// </summary>
        private static ServiceState State
        {
            get
            {
                using (ServiceController serviceController = new ServiceController(ShortServiceName))
                {
                    try
                    {
                        ServiceControllerStatus status = serviceController.Status;
                    }
                    catch (InvalidOperationException)
                    {
                        return ServiceState.NotInstalled;
                    }

                    return (serviceController.Status == ServiceControllerStatus.Running) ? ServiceState.InstalledRunning : ServiceState.InstalledStopped;
                }
            }
        }

        /// <summary>
        /// Attempt to install the service
        /// </summary>
        public static void InstallService()
        {
            Console.WriteLine("Attempting to install service '{0}'...", ShortServiceName);

            if (State != ServiceState.NotInstalled)
            {
                Console.WriteLine("Service '{0}' is already installed", ShortServiceName);
                return;
            }

            try
            {
                Console.WriteLine("Service '{0}' was not found. Creating assembly installer...", ShortServiceName);

                IDictionary savedState = new Hashtable();
                AssemblyInstaller installer = new AssemblyInstaller(Assembly.GetExecutingAssembly().Location, new string[] { });
                installer.UseNewContext = true;

                try
                {
                    Console.WriteLine("Installing service '{0}'...", ShortServiceName);
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    installer.Install(savedState);
                    Console.ResetColor();
                    Console.WriteLine("Committing changes...", ShortServiceName);
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    installer.Commit(savedState);
                    Console.ResetColor();
                    Console.WriteLine("Service '{0}' was sucessfully installed", ShortServiceName);
                }
                catch (Exception ex)
                {
                    Console.ResetColor();
                    installer.Rollback(savedState);
                    Console.WriteLine("Service '{0}' could not be installed\n{1}", ShortServiceName, ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Service '{0}' could not be installed\n{1}", ShortServiceName, ex.Message);
            }
        }

        /// <summary>
        /// Attempt to uninstall the service
        /// </summary>
        public static void UninstallService()
        {
            Console.WriteLine("Attempting to uninstall service '{0}'...", ShortServiceName);

            if (State == ServiceState.NotInstalled)
            {
                Console.WriteLine("Service '{0}' is not installed", ShortServiceName);
                return;
            }

            IDictionary mySavedState = new Hashtable();
            mySavedState.Clear();

            Console.WriteLine("Service '{0}' exists. Creating assembly installer...", ShortServiceName);

            AssemblyInstaller installer = new AssemblyInstaller(Assembly.GetExecutingAssembly().Location, new string[] { });
            installer.UseNewContext = true;

            try
            {
                Console.WriteLine("Uninstalling service '{0}'...", ShortServiceName);
                Console.ForegroundColor = ConsoleColor.Magenta;
                installer.Uninstall(mySavedState);
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ResetColor();
                Console.WriteLine("Service '{0}' could not be uninstalled\n{1}", ShortServiceName, ex.Message);
            }
        }
    }
}
