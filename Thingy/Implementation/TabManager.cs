﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Thingy.API;
using Thingy.API.Capabilities;

namespace Thingy.Implementation
{
    internal class TabManager : ITabManager
    {
        private IApplication _application;
        private IModuleLoader _moduleLoader;
        private Dictionary<Guid, IModule> _runningModules;

        private MainWindow MainWindow
        {
            get { return Application.Current.MainWindow as MainWindow; }
        }

        public TabManager(IApplication application, IModuleLoader moduleLoader)
        {
            _application = application;
            _moduleLoader = moduleLoader;
            _runningModules = new Dictionary<Guid, IModule>();
        }

        public int Count
        {
            get { return MainWindow.TabCount; }
        }

        public void CloseCurrentTab()
        {
            MainWindow.CloseCurrentTab();
        }

        public void CreateNewTabContent(string Title, UserControl control)
        {
            MainWindow.SetCurrentTabContent(Title, control, true);
        }

        public void FocusTabByIndex(int index)
        {
            MainWindow.FocusTabByIndex(index);
        }

        public int GetTabIndexByTitle(string Title)
        {
            return MainWindow.FindTabByTitle(Title);
        }

        public void ModuleClosed(Guid ModuleId)
        {
            _runningModules.Remove(ModuleId);
        }

        public void SetCurrentTabContent(string Title, UserControl control)
        {
            MainWindow.SetCurrentTabContent(Title, control, false);
        }

        public async Task<Guid> StartModule(IModule module, bool newTab = false)
        {
            if (module == null) return Guid.Empty;
            var control = module.RunModule();
            if (control != null)
            {
                if (module.OpenParameters != null)
                {
                    bool result = await _application.ShowDialog(module.ModuleName, control, module.OpenParameters.DialogButtons);
                    if (result && control is IHaveCloseTask closetask)
                    {
                        if (closetask.CanExecuteAsync)
                            await Task.Run(closetask.ClosingTask);
                        else
                            closetask.ClosingTask.Invoke();
                    }
                    return Guid.Empty;
                }
                else
                {
                    if (module.IsSingleInstance)
                    {
                        var index = GetTabIndexByTitle(module.ModuleName);
                        if (index != -1)
                        {
                            await _application.ShowMessageBox("Single instance module",
                                                              "This module can only run in one instance. Switching to running module",
                                                              DialogButtons.Ok);
                            FocusTabByIndex(index);
                            return Guid.Empty;
                        }
                        else
                        {
                            var mId = Guid.NewGuid();
                            _runningModules.Add(mId, module);
                            control.Tag = mId;
                            if (newTab)
                                CreateNewTabContent(module.ModuleName, control);
                            else
                                SetCurrentTabContent(module.ModuleName, control);
                            return mId;
                        }
                    }
                    else
                    {
                        var mId = Guid.NewGuid();
                        _runningModules.Add(mId, module);
                        control.Tag = mId;
                        if (newTab)
                            CreateNewTabContent(module.ModuleName, control);
                        else
                            SetCurrentTabContent(module.ModuleName, control);
                        return mId;
                    }
                }
            }
            else
            {
                return Guid.Empty;
            }
        }

        public Task<Guid> StartModule(string moduleName, bool newTab = false)
        {
            var module = _moduleLoader.GetModuleByName(moduleName);
            if (module != null)
            {
                return StartModule(module, newTab);
            }
            else
            {
                return Task.Run(() =>
                {
                    return Guid.Empty;
                });
            }
        }
    }
}
