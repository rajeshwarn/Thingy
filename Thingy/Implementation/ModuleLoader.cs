﻿using AppLib.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Thingy.API;

namespace Thingy.Implementation
{
    internal class ModuleLoader: IModuleLoader
    {
        private List<IModule> _modules;
        private Dictionary<string, int> _moudleCounter;
        private ILog _log;

        public ModuleLoader(ILog log)
        {
            _log = log;
            _modules = new List<IModule>();
            _moudleCounter = new Dictionary<string, int>
            {
                { "All", 0 }
            };
            LoadModules();
        }

        private void LoadModules()
        {
            _log.Info("Searching for loadable modules...");
            var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, ".module.dll");
            _log.Info("Found {0} loadable modules", files.Length);
            var imodule = typeof(IModule);
            foreach (var file in files)
            {
                try
                {
                    _log.Info("Atempting to load: {0}", file);
                    var assembly = Assembly.LoadFile(file);

                    var modules = from module in assembly.GetTypes()
                                  where imodule.IsAssignableFrom(module) &&
                                        module.IsInterface == false &&
                                        module.IsAbstract == false &&
                                        module.IsVisible == true
                                  select module;

                    foreach (var module in modules)
                    {
                        try
                        {
                            var instance = (IModule)Activator.CreateInstance(module);
                            if (instance.CanLoad)
                            {
                                SetCount(instance.Category);
                                _modules.Add(instance);
                                _log.Info($"Module load was succesfull: {module.Name}");
                            }
                            else
                            {
                                _log.Info($"Module load was succesfull, but it was not cached: {module.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }
            }
        }

        private void SetCount(string category)
        {
            if (_moudleCounter.ContainsKey(category))
            {
                ++_moudleCounter[category];
            }
            else
            {
                _moudleCounter.Add(category, 1);
            }
            ++_moudleCounter["All"];
        }

        public IDictionary<string, int> CategoryModuleCount
        {
            get { return _moudleCounter; }
        }

        public IEnumerable<IModule> GetModulesForCategory(string category = null)
        {
            if (string.IsNullOrEmpty(category) || category == "All")
            {
                return _modules.OrderBy(module => module.ModuleName);
            }
            else
            {
                return _modules.Where(module => module.Category == category).OrderBy(module => module.ModuleName);
            }

        }

        public IEnumerable<IModule> GetModulesByName(string searchname)
        {
            if (string.IsNullOrEmpty(searchname))
            {
                return _modules.OrderBy(module => module.ModuleName);
            }
            else
            {
                return _modules.Where(m => m.ModuleName.Contains(searchname, StringComparison.CurrentCultureIgnoreCase)).OrderBy(m => m.ModuleName);
            }
        }

        public IModule GetModuleByName(string name)
        {
            var moduleToRun = (from module in _modules
                               where module.ModuleName == name
                               select module).FirstOrDefault();

            if (moduleToRun == null)
            {
                _log.Error("couldn't find module: {0}", name);
                return null;
            }

            _log.Info("Found Module: {0}", name);

            return moduleToRun;
        }

        public IModule GetModuleForFile(string file)
        {
            var extension = System.IO.Path.GetExtension(file);

            var moduleToRun = (from module in _modules
                               where module.SupportedExtensions != null &&
                               module.SupportedExtensions.Contains(extension)
                               select module).FirstOrDefault();

            if (moduleToRun == null)
            {
                if (!string.IsNullOrEmpty(extension))
                    _log.Error("couldn't find module for extension: {0}", extension);
                return null;
            }

            return moduleToRun;
        }
    }
}
