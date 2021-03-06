﻿using AppLib.WPF;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Thingy.API;

namespace Thingy.MediaModules.ModuleDefinitions
{
    public class MpvModule : ModuleBase
    {
        public override string ModuleName
        {
            get { return "MVP Player"; }
        }

        public override ImageSource Icon
        {
            get { return BitmapHelper.FrozenBitmap("pack://application:,,,/Thingy.Resources;component/Icons/mpv-logo-128.png"); }
        }

        public override string Category
        {
            get { return ModuleCategories.Media; }
        }

        public override UserControl RunModule()
        {
            return new Views.MpvPlayerView
            {
                DataContext = new ViewModels.MpvPlayerViewModel(App)
            };
        }

        public override bool CanHadleFile(string pathOrExtension)
        {
            var extension = System.IO.Path.GetExtension(pathOrExtension);
            if (Formats.IsYoutubeUrl(pathOrExtension))
            {
                return true;
            }
            else if (Formats.SupportedVideoFormats.Contains(extension))
            {
                return true;
            }
            return Formats.SupportedAudioFormats.Contains(extension);
        }

        public override bool CanLoad
        {
            get
            {
                string mpv = null;
                var setpath = App.Settings.Get("MPVPath", string.Empty);
                if (string.IsNullOrEmpty(setpath))
                {
                    mpv = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Apps\x64\mpv.exe");
                    App.Settings.Set("MPVPath", mpv);
                }
                else
                {
                    mpv = setpath;
                }
                bool canLoad = System.IO.File.Exists(mpv);
                if (!canLoad)
                    App.Log.Warning("Mpv Module not shown, because file not exists: {0}", mpv);

                return canLoad;
            }
        }

        public override OpenParameters OpenParameters
        {
            get
            {
                return new OpenParameters
                {
                    DialogButtons = DialogButtons.OkCancel
                };
            }
        }
    }
}
