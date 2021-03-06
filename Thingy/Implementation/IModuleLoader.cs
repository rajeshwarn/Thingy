﻿using System.Collections.Generic;
using Thingy.API;

namespace Thingy.Implementation
{
    public interface IModuleLoader
    {
        IModule GetModuleByName(string name);
        IEnumerable<IModule> GetModulesForCategory(string category = null);
        IEnumerable<IModule> GetModulesByName(string searchname);
        IDictionary<string, int> CategoryModuleCount { get; }
        IList<IModule> GetModulesForFiles(IEnumerable<string> files);
        IList<IModule> GetModulesWithDirectorySupport();
        void Add(IModule module);
        void AddRange(IEnumerable<IModule> modules);
    }
}
