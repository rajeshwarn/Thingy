﻿using AppLib.Common;
using AppLib.MVVM.MessageHandler;
using AppLib.WPF;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Thingy.Db;
using Thingy.Infrastructure;
using Thingy.MusicPlayerCore;

namespace Thingy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IApplication
    {
        public static AppLib.Common.IOC.IContainer IoCContainer { get; private set; }
        public static AppLib.Common.Log.ILogger Log { get; private set; }
        public static string[] Accents { get; private set; }

        public static IApplication Instance
        {
            get { return App.Current as IApplication; }
        }

        public ITabManager TabManager
        {
            get;
            private set;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Log = new AppLib.Common.Log.Logger();
            Log.Info("Application startup");

            var trayIcon = new Infrastructure.Tray.TrayIcon();

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            Accents = new string[]
            {
                "Red", "Green", "Blue",
                "Purple", "Orange", "Lime",
                "Emerald", "Teal", "Cyan",
                "Cobalt", "Indigo", "Violet",
                "Pink", "Magenta", "Crimson",
                "Amber", "Yellow", "Brown",
                "Olive", "Steel", "Mauve",
                "Taupe", "Sienna"
            };
            IoCContainer = new AppLib.Common.IOC.Container();
            IoCContainer.RegisterSingleton<IDataBase>(() =>
            {
                var dbfile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ThingyDataBase.litedb");
                return new DataBase(dbfile);
            });
            Log.Info("Database initialized");
            IoCContainer.RegisterSingleton<IAudioEngine>(() =>
            {
                return new AudioEngine();
            });
            Log.Info("Audio Engine loaded");
            IoCContainer.RegisterSingleton<IModuleLoader>(() =>
            {
                return new ModuleLoader(Log);
            });
            Log.Info("Module loader initialized");

            var accent = Thingy.Properties.Settings.Default.SelectedAccent;
            ThemeManager.ChangeAppStyle(Current,
                              ThemeManager.GetAccent(accent),
                              ThemeManager.GetAppTheme("BaseLight"));

            var dload = BingPhotoOfDay.WasSuccesfull;

            this.TabManager = new TabManager(Instance, IoCContainer.ResolveSingleton<IModuleLoader>());

            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception);
            var desktop = Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
            var log = System.IO.Path.Combine(desktop, "Thingy crash log.xml");
            Log.SaveToFile(log);
            AppLib.WPF.Dialogs.Dialogs.ShowErrorDialog(e.Exception);
        }

        #region IApplication implementation
        public void Close()
        {
            Thingy.Properties.Settings.Default.Save();
            Current.Shutdown();
        }

        public async Task<bool> ShowDialog(UserControl control, string Title, INotifyPropertyChanged model = null, bool ShowOverlay = true)
        {
            ModalDialog modalDialog = new ModalDialog();
            if (ShowOverlay == false)
                modalDialog.OverlayBrush = null;
            if (model != null)
                control.DataContext = model;
            modalDialog.DailogContent = control;
            modalDialog.Title = Title;

            var result = await (Current.MainWindow as MainWindow).ShowChildWindowAsync<bool>(modalDialog);

            return result;
        }

        public Task<MessageDialogResult> ShowMessageBox(string title, string content, MessageDialogStyle style)
        {
            var mainwindow = (Current.MainWindow as MainWindow);
            return mainwindow.ShowMessageAsync(title, content, style);
        }

        public Task ShowMessageBox(CustomDialog messageBoxContent)
        {
            var mainwindow = (Current.MainWindow as MainWindow);
            return mainwindow.ShowMetroDialogAsync(messageBoxContent);
        }

        public Task HideMessageBox(CustomDialog messageBoxContent)
        {
            var mainwindow = (Current.MainWindow as MainWindow);
            return mainwindow.HideMetroDialogAsync(messageBoxContent);
        }

        public void ShowStatusBarMenu(UserControl control, string title, bool AutoClose = true, int AutoCloseTimeMs = 5000)
        {
            var mainwindow = (Current.MainWindow as MainWindow);
            mainwindow.StatusFlyOut.Content = control;
            mainwindow.StatusFlyOut.AutoCloseInterval = AutoCloseTimeMs;
            mainwindow.StatusFlyOut.IsAutoCloseEnabled = AutoClose;
            mainwindow.StatusFlyOut.Header = title;
            mainwindow.StatusFlyOut.IsOpen = true;
        }

        public void Restart()
        {
            Thingy.Properties.Settings.Default.Save();
            System.Diagnostics.Process.Start(ResourceAssembly.Location);
            Current.Shutdown();
        }

        public async void HandleFiles(IList<string> files)
        {
            var loader = IoCContainer.ResolveSingleton<IModuleLoader>();
            foreach (var file in files)
            {
                var module = loader.GetModuleForFile(file);
                if (module == null) continue;

                if (module.IsSingleInstance)
                {
                    int tabIndex = TabManager.GetTabIndexByTitle(module.ModuleName);
                    if (tabIndex == -1)
                    {
                        var id =  await TabManager.StartModule(module);
                        await Task.Delay(25);
                        Messager.Instance.SendMessage(id, new HandleFileMessage
                        {
                            File = file
                        });
                    }
                    else
                    {
                        TabManager.FocusTabByIndex(tabIndex);
                        Messager.Instance.SendMessage(module.RunModule().GetType(), new HandleFileMessage
                        {
                            File = file
                        });
                    }
                }
                else
                {
                    var id = await TabManager.StartModule(module);
                    await Task.Delay(25);
                    Messager.Instance.SendMessage(id, new HandleFileMessage
                    {
                        File = file
                    });
                }
            }
        }
        #endregion
    }
}
