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
using System.Collections.Specialized;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using XMPMS.Core;
using XMPMS.Interfaces;
using XMPMS.Properties;

namespace XMPMS.Core
{
    /// <summary>
    /// The module manager manages classes which can be dynamically assigned at run time. All modules are
    /// loaded at startup and initialised on first use. If a module is requested multiple times it will only
    /// be Shutdown() after all requested instances have been released.
    /// </summary>
    public static class ModuleManager
    {
        /// <summary>
        /// Struct for storing active module references with a counter
        /// </summary>
        internal class ActiveModule
        {
            /// <summary>
            /// Reference to the active module
            /// </summary>
            internal IMasterServerModule Module;

            /// <summary>
            /// Number of active instances
            /// </summary>
            internal int Count;

            /// <summary>
            /// Create a new ActiveModule object with the specified module
            /// </summary>
            /// <param name="module"></param>
            internal ActiveModule(IMasterServerModule module)
            {
                Module = module;
                Count = 0;
            }
        }

        /// <summary>
        /// Lock to prevent concurrent access to the module repository
        /// </summary>
        private static object repositoryLock = new object();

        /// <summary>
        /// List of all modules which are loaded
        /// </summary>
        private static Dictionary<string, IMasterServerModule> loadedModules = new Dictionary<string, IMasterServerModule>();

        /// <summary>
        /// List of modules which are active along with their corresponding usage counters
        /// </summary>
        private static Dictionary<string, ActiveModule> activeModules = new Dictionary<string, ActiveModule>();

        /// <summary>
        /// List of objects that want to receive commands from the command interface
        /// </summary>
        private static List<ICommandListener> commandListeners = new List<ICommandListener>();

        /// <summary>
        /// Static constructor, loads modules and initialises the module repository
        /// </summary>
        static ModuleManager()
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            lock (repositoryLock)
            {
                // Load modules from this assembly using reflection
                foreach (Type moduleType in executingAssembly.GetTypes())
                {
                    TryLoadType(moduleType, executingAssembly);
                }

                // Try to load extension module assemblies
                if (Modules.Default.LoadAssemblies != null)
                {
                    foreach (string moduleAssemblyName in Modules.Default.LoadAssemblies)
                    {
                        TryLoadDll(moduleAssemblyName);
                    }
                }
                else
                {
                    Modules.Default.LoadAssemblies = new StringCollection();
                }

                // Load any modules which request autoload
                foreach (IMasterServerModule module in loadedModules.Values)
                {
                    if (module.AutoLoad)
                    {
                        GetModule<IMasterServerModule>(module.GetType());
                    }
                }
            }
        }

        /// <summary>
        /// Attempt to load an assembly .dll
        /// </summary>
        /// <param name="moduleAssemblyName"></param>
        private static bool TryLoadDll(string moduleAssemblyName)
        {
            string assemblyPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            string moduleAssemblyPath = Path.Combine(assemblyPath, moduleAssemblyName + ".dll");

            if (File.Exists(moduleAssemblyPath))
            {
                try
                {
                    Assembly moduleAssembly = Assembly.LoadFile(moduleAssemblyPath);

                    foreach (Type moduleType in moduleAssembly.GetTypes())
                    {
                        TryLoadType(moduleType, moduleAssembly);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(String.Format("Exception loading assembly {0}: {1} ", moduleAssemblyName, ex.Message));
                    MasterServer.LogMessage("Error loading assembly {0}.dll", moduleAssemblyName);
                    MasterServer.LogMessage(ex.Message);
                    return false;
                }
            }
            else
            {
                Debug.WriteLine(String.Format("Assembly {0}.dll was not found.", moduleAssemblyName));
                MasterServer.LogMessage("Assembly {0}.dll was not found", moduleAssemblyName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to load a module type if it is a valid master server module 
        /// </summary>
        /// <param name="moduleType"></param>
        /// <param name="assembly"></param>
        private static void TryLoadType(Type moduleType, Assembly assembly)
        {
            try
            {
                if (moduleType.IsClass && !moduleType.IsAbstract && moduleType.GetInterface(typeof(IMasterServerModule).Name, true) != null && !loadedModules.ContainsKey(moduleType.Name))
                {
                    Debug.WriteLine(String.Format("Loading module {0} from {1}", moduleType.Name, assembly.FullName));
                    loadedModules.Add(moduleType.Name, (IMasterServerModule)Activator.CreateInstance(moduleType));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("Exception loading module {0} from {1}: {2}", moduleType.Name, assembly.FullName, ex.Message));
                MasterServer.LogMessage("Error loading type {0} from {1}", moduleType.Name, assembly.FullName);
                MasterServer.LogMessage(ex.Message);
            }
        }

        /// <summary>
        /// Get the configured module of the specified type
        /// </summary>
        /// <typeparam name="T">Type of module to load, the module must be defined in the module configuration file</typeparam>
        /// <returns>New module or null if not configured</returns>
        public static T GetModule<T>()
            where T : IMasterServerModule
        {
            string moduleName = "";

            foreach (SettingsPropertyValue moduleNameProperty in Modules.Default.PropertyValues)
            {
                if (moduleNameProperty.Name == typeof(T).Name)
                {
                    moduleName = moduleNameProperty.PropertyValue.ToString();
                    break;
                }
            }

            if (moduleName == "")
            {
                Debug.WriteLine("Warning: no configured module was found for interface " + typeof(T).Name + ". Check configuration settings.");
            }

            return GetModule<T>(moduleName);
        }

        /// <summary>
        /// Get a module by class name
        /// </summary>
        /// <typeparam name="T">Specialised inner interface type for the module</typeparam>
        /// <param name="moduleType">Type of the module class to fetch</param>
        /// <returns>Module reference or null if the module was not found</returns>
        public static T GetModule<T>(Type moduleType)
            where T : IMasterServerModule
        {
            return GetModule<T>(moduleType.Name);
        }

        /// <summary>
        /// Get a module by class name
        /// </summary>
        /// <typeparam name="T">Specialised inner interface type for the module</typeparam>
        /// <param name="moduleName">Name of the module class to fetch</param>
        /// <returns>Module reference or null if the module was not found</returns>
        public static T GetModule<T>(string moduleName)
            where T : IMasterServerModule
        {
            lock (repositoryLock)
            {
                // If the module is already in use, increment the usage counter
                if (activeModules.ContainsKey(moduleName))
                {
                    activeModules[moduleName].Count++;
                    return (T)activeModules[moduleName].Module;
                }
                else if (loadedModules.ContainsKey(moduleName))
                {
                    // Mark the module as active and initialise it
                    activeModules[moduleName] = new ActiveModule(loadedModules[moduleName]);

                    try
                    {
                        activeModules[moduleName].Module.Initialise(MasterServer.Instance);
                        activeModules[moduleName].Count++;
                    }
                    catch
                    {
                        activeModules.Remove(moduleName);
                        return default(T);
                    }

                    return (T)activeModules[moduleName].Module;
                }
            }

            return default(T);
        }

        /// <summary>
        /// Get all modules that implement a particular interface
        /// </summary>
        /// <typeparam name="T">Interface of modules to fetch</typeparam>
        /// <returns>List of matching modules</returns>
        public static List<T> GetModules<T>()
            where T : IMasterServerModule
        {
            List<T> modules = new List<T>();

            lock (repositoryLock)
            {
                foreach (KeyValuePair<string, IMasterServerModule> module in loadedModules)
                {
                    if (module.Value.GetType().GetInterface(typeof(T).Name, true) != null)
                    {
                        modules.Add(GetModule<T>(module.Key));
                    }
                }
            }

            return modules;
        }

        /// <summary>
        /// Release the module configured for the specified interface type
        /// </summary>
        /// <param name="moduleType">Interface name to search for in the module configuration</param>
        public static void ReleaseModule(string moduleType)
        {
            string moduleName = "";

            // Search for the specified interface in the module configuration
            foreach (SettingsPropertyValue moduleNameProperty in Modules.Default.PropertyValues)
            {
                if (moduleNameProperty.Name == moduleType)
                {
                    moduleName = moduleNameProperty.PropertyValue.ToString();
                    break;
                }
            }

            ReleaseModuleByName(moduleName);
        }

        /// <summary>
        /// Release the specified module
        /// </summary>
        /// <param name="module">Module to release</param>
        public static void ReleaseModule(IMasterServerModule module)
        {
            ReleaseModuleByName(module.GetType().Name);
        }

        /// <summary>
        /// Release module which implements the specified interface
        /// </summary>
        /// <typeparam name="T">Type of module to release</typeparam>
        public static void ReleaseModule<T>()
            where T : IMasterServerModule
        {
            IMasterServerModule pendingRelease = null;

            lock (repositoryLock)
            {
                // Can't modify the activeModules collection within foreach, so add all modules to be removed to a list
                foreach (KeyValuePair<string, ActiveModule> activeModule in activeModules)
                {
                    if (activeModule.Value.Module.GetType().GetInterface(typeof(T).Name, true) != null)
                        pendingRelease = activeModule.Value.Module;
                }

                if (pendingRelease != null)
                    ReleaseModuleByName(pendingRelease.GetType().Name);
            }
        }

        /// <summary>
        /// Release all modules which implement the specified interface
        /// </summary>
        /// <typeparam name="T">Type of module to release</typeparam>
        public static void ReleaseModules<T>()
            where T : IMasterServerModule
        {
            List<IMasterServerModule> pendingRelease = new List<IMasterServerModule>();

            lock (repositoryLock)
            {
                // Can't modify the activeModules collection within foreach, so add all modules to be removed to a list
                foreach (KeyValuePair<string, ActiveModule> activeModule in activeModules)
                {
                    if (activeModule.Value.Module.GetType().GetInterface(typeof(T).Name, true) != null)
                        pendingRelease.Add(activeModule.Value.Module);
                }

                // Release the modules
                foreach (IMasterServerModule module in pendingRelease)
                {
                    ReleaseModuleByName(module.GetType().Name);
                }
            }
        }

        /// <summary>
        /// Releases the specified module
        /// </summary>
        /// <param name="assemblyName">Module name to release</param>
        private static void ReleaseModuleByName(string moduleName)
        {
            lock (repositoryLock)
            {
                if (activeModules.ContainsKey(moduleName))
                {
                    // Decrease the usage counter
                    activeModules[moduleName].Count--;

                    // If all active references have been released, shut down the module
                    if (activeModules[moduleName].Count < 1)
                    {
                        try
                        {
                            activeModules[moduleName].Module.Shutdown();
                        }
                        catch { }

                        activeModules.Remove(moduleName);
                    }
                }
            }
        }

        /// <summary>
        /// Releases all active modules
        /// </summary>
        public static void ReleaseAllModules()
        {
            lock (repositoryLock)
            {
                // Shut down any active modules
                foreach (KeyValuePair<string, ActiveModule> activeModule in activeModules)
                    activeModule.Value.Module.Shutdown();                    

                // Then clear the list
                activeModules.Clear();
            }
        }

        /// <summary>
        /// Register an object as a command listener
        /// </summary>
        /// <param name="listener"></param>
        public static void RegisterCommandListener(ICommandListener listener)
        {
            // Modules are already listeners
            if (listener == null || listener is IMasterServerModule || commandListeners.Contains(listener)) return;

            commandListeners.Add(listener);
        }

        /// <summary>
        /// Unregister a command listener
        /// </summary>
        /// <param name="listener"></param>
        public static void UnregisterCommandListener(ICommandListener listener)
        {
            commandListeners.Remove(listener);
        }

        /// <summary>
        /// Dispatch a console command to all listeners
        /// </summary>
        /// <param name="command"></param>
        public static void DispatchCommand(string[] command)
        {
            // Master server command handler
            if (MasterServer.Instance != null)
            {
                MasterServer.Instance.Command(command);
            }

            // Handle commands for the module manager
            Command(command);

            // Registered command listeners
            foreach (ICommandListener listener in commandListeners)
            {
                listener.Command(command);                
            }

            // Active modules which are command listeners
            foreach (ActiveModule module in activeModules.Values)
            {
                if (module.Module.GetType().GetInterface(typeof(ICommandListener).Name) != null)
                {
                    ICommandListener listener = (ICommandListener)module.Module;
                    if (listener != null) listener.Command(command);
                }
            }
        }

        /// <summary>
        /// Handle module commands
        /// </summary>
        /// <param name="command"></param>
        private static void Command(string[] command)
        {
            if (command.Length > 0 && command[0].Trim() != "")
            {
                switch (command[0].ToLower())
                {
                    case "module":
                        if (command.Length > 1)
                        {
                            switch (command[1].ToLower())
                            {
                                case "list":
                                    MasterServer.LogMessage("Loaded modules:");

                                    lock (repositoryLock)
                                    {
                                        foreach (string moduleName in loadedModules.Keys)
                                        {
                                            MasterServer.LogMessage("  {0}", moduleName);
                                        }
                                    }
                                    break;

                                case "show":
                                    MasterServer.LogMessage("Configured assemblies:");

                                    lock (repositoryLock)
                                    {
                                        if (Modules.Default.LoadAssemblies != null)
                                        {
                                            foreach (string assemblyName in Modules.Default.LoadAssemblies)
                                            {
                                                MasterServer.LogMessage("  {0}", assemblyName);
                                            }
                                        }
                                        else
                                        {
                                            MasterServer.Log("No configured module assemblies");
                                        }
                                    }
                                    break;

                                case "add":
                                    if (command.Length > 2)
                                    {
                                        MasterServer.LogMessage("Attempting to add {0}.dll", command[2]);

                                        if (TryLoadDll(command[2]) && !Modules.Default.LoadAssemblies.Contains(command[2]))
                                        {
                                            Modules.Default.LoadAssemblies.Add(command[2]);
                                            Modules.Default.Save();
                                            MasterServer.LogMessage("Assembly loaded ok, added assembly to configuration");
                                        }
                                    }
                                    else
                                    {
                                        MasterServer.LogMessage("module add <assemblyname>");
                                    }

                                    break;

                                case "remove":
                                    if (command.Length > 2)
                                    {
                                        if (Modules.Default.LoadAssemblies.Contains(command[2]))
                                        {
                                            Modules.Default.LoadAssemblies.Remove(command[2]);
                                            Modules.Default.Save();
                                            MasterServer.LogMessage("Removed assembly {0} from configuration", command[2]);
                                        }
                                    }
                                    else
                                    {
                                        MasterServer.LogMessage("module remove <assemblyname>");
                                    }

                                    break;
                            }
                        }
                        else
                        {
                            MasterServer.LogMessage("module list   List loaded modules");
                            MasterServer.LogMessage("module show   Show configured module .dlls");
                            MasterServer.LogMessage("module add    Load a module .dll");
                            MasterServer.LogMessage("module remove Remove a module .dll");
                        }
                        
                        break;

                    case "help": case "?":
                        MasterServer.LogMessage("module        Module manager commands");
                        break;                        
                }
            }
        }
    }
}
