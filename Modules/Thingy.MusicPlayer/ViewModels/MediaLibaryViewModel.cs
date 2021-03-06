﻿using AppLib.Common.Extensions;
using AppLib.MVVM;
using AppLib.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Thingy.MediaLibary.Models;
using Thingy.API;
using Thingy.API.Capabilities;
using Thingy.Db;
using Thingy.Db.Entity;
using Thingy.Db.Entity.MediaLibary;
using Thingy.Db.Factories;
using Thingy.MusicPlayerCore;
using Thingy.MusicPlayerCore.Formats;
using Thingy.Resources;

namespace Thingy.MusicPlayer.ViewModels
{
    public class MediaLibaryViewModel: ViewModel, ICanImportExportXMLData
    {
        private IApplication _app;
        private IDataBase _db;
        private IExtensionProvider _extensions;

        public ObservableCollection<NavigationItem> Tree { get; }
        public DelegateCommand AddFilesCommand { get; }
        public DelegateCommand AddDirectoryCommand { get; }
        public DelegateCommand<string[]> CategoryQueryCommand { get; }
        public DelegateCommand<string[]> DeleteQueryCommand { get; }
        public DelegateCommand CreateQueryCommand { get; }
        public DelegateCommand<IList> SendToPlayerCommand { get; }

        public ObservableCollection<Song> QueryResults { get; }
        public ObservableCollection<RadioStation> RadioItems { get; }


        public MediaLibaryViewModel(IApplication app, IDataBase db)
        {
            _app = app;
            _db = db;
            _extensions = new ExtensionProvider();
            Tree = new ObservableCollection<NavigationItem>();
            QueryResults = new ObservableCollection<Song>();
            RadioItems = new ObservableCollection<RadioStation>();
            CreateQueryCommand = Command.CreateCommand(CreateQuery);
            AddFilesCommand = Command.CreateCommand(AddFiles);
            AddDirectoryCommand = Command.CreateCommand(AddDirectory);
            CategoryQueryCommand = Command.CreateCommand<string[]>(CategoryQuery);
            DeleteQueryCommand = Command.CreateCommand<string[]>(DeleteQuery);
            SendToPlayerCommand = Command.CreateCommand<IList>(SendToPlayer, CanSendToPlayer);
            BuildTree();
            LoadRadios();
        }

        private void LoadRadios()
        {
            RadioItems.UpdateWith(_db.MediaLibary.GetRadioStations());
        }

        private bool CanSendToPlayer(IList songs)
        {
            return (songs != null && songs.Count > 0);
        }

        private void SendToPlayer(IList songs)
        {
            if (songs[0] is Song)
            {
                var files = songs.Cast<Song>().Select(s => s.Filename).ToList();
                _app.HandleFiles(files);
            }
            else
            {
                var files = songs.Cast<RadioStation>().Select(s => s.Url).ToList();
                _app.HandleFiles(files);
            }
        }

        private async void CreateQuery()
        {
            var editor = new MediaLibary.Views.QueryEditor();
            var modell = new Db.Entity.MediaLibary.SongQuery();
            if (await _app.ShowDialog("Query Editor", editor, DialogButtons.OkCancel, false, modell))
            {
                var results = _db.MediaLibary.DoQuery(modell);
                if (results != null)
                    QueryResults.UpdateWith(results);

                if (!string.IsNullOrEmpty(modell.Name))
                {
                    _db.MediaLibary.SaveQuery(modell);
                    BuildTree();
                }
            }
        }

        private IEnumerable<Song> ExecuteQuery(string[] obj)
        {
            if (obj == null || obj.Length < 2) return null;

            var category = obj[1];

            SongQuery q = null;

            switch (category)
            {
                case "Albums":
                    q = QueryFactory.AlbumQuery(obj[0]);
                    break;
                case "Artists":
                    q = QueryFactory.ArtistQuery(obj[0]);
                    break;
                case "Years":
                    q = QueryFactory.YearQuery(Convert.ToInt32(obj[0]));
                    break;
                case "Genres":
                    q = QueryFactory.GenreQuery(obj[0]);
                    break;
                case "Queries":
                    q = _db.MediaLibary.GetQuery(obj[0]);
                    break;
            }

            return _db.MediaLibary.DoQuery(q);
        }

        private void CategoryQuery(string[] obj)
        {
            var results = ExecuteQuery(obj);
            if (results!=null)
                QueryResults.UpdateWith(results);
        }

        private async void DeleteQuery(string[] obj)
        {
            var results = ExecuteQuery(obj);
            if (results != null)
            {
                var q = await _app.ShowMessageBox("Media Libary", "Remove songs from database?", DialogButtons.YesNo);
                if (q)
                {
                    _db.MediaLibary.DeleteSongs(results);
                    BuildTree();
                }
            }
        }

        private async void AddFiles()
        {
            var ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = _extensions.AllFormatsFilterString;
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var items = await DBFactory.CreateSongs(ofd.FileNames);
                _db.MediaLibary.AddSongs(items);
                BuildTree();
                _db.MediaLibary.SaveCache();
            }
        }

        private async void AddDirectory()
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.Description = "Select folder to add";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<string> Files = new List<string>(30);
                foreach (var filter in _extensions.AllSupportedFormats)
                {
                    Files.AddRange(Directory.GetFiles(fbd.SelectedPath, filter));
                }
                Files.Sort();
                var items = await DBFactory.CreateSongs(Files.ToArray());
                _db.MediaLibary.AddSongs(items);
                BuildTree();
                _db.MediaLibary.SaveCache();
            }
        }

        private void BuildTree()
        {
            Tree.Clear();

            Tree.Add(new NavigationItem
            {
                Name = "Albums",
                Icon = BitmapHelper.FrozenBitmap(ResourceLocator.GetIcon(IconCategories.Small, "icons8-music-record-48.png")),
                SubItems = new ObservableCollection<string>(_db.MediaLibary.GetAlbums())
            });
            Tree.Add(new NavigationItem
            {
                Name = "Artists",
                Icon = BitmapHelper.FrozenBitmap(ResourceLocator.GetIcon(IconCategories.Small, "icons8-dj-48.png")),
                SubItems = new ObservableCollection<string>(_db.MediaLibary.GetArtists())
            });
            Tree.Add(new NavigationItem
            {
                Name = "Years",
                Icon = BitmapHelper.FrozenBitmap(ResourceLocator.GetIcon(IconCategories.Small, "icons8-calendar-7-48.png")),
                SubItems = new ObservableCollection<string>(_db.MediaLibary.GetYears().Cast<string>())
            });
            Tree.Add(new NavigationItem
            {
                Name = "Genres",
                Icon = BitmapHelper.FrozenBitmap(ResourceLocator.GetIcon(IconCategories.Small, "icons8-musical-notes-48.png")),
                SubItems = new ObservableCollection<string>(_db.MediaLibary.GetGeneires())
            });
            Tree.Add(new NavigationItem
            {
                Name = "Queries",
                Icon = BitmapHelper.FrozenBitmap(ResourceLocator.GetIcon(IconCategories.Small, "icons8-questionnaire-48.png")),
                SubItems = new ObservableCollection<string>(_db.MediaLibary.GetQueryNames())
            });
            Tree.Add(new NavigationItem
            {
                Name = "Radio",
                Icon = BitmapHelper.FrozenBitmap(ResourceLocator.GetIcon(IconCategories.Small, "icons8-radio-station-48.png"))
            });
        }

        public Task Import(Stream xmlData, bool append)
        {
            return Task.Run(() =>
            {
                var import = EntitySerializer.Deserialize<Tuple<IEnumerable<Song>, IEnumerable<SongQuery>>>(xmlData);
                if (append)
                {
                    _db.MediaLibary.AddSongs(import.Item1);
                    _db.MediaLibary.SaveQuery(import.Item2);
                }
                else
                {
                    _db.MediaLibary.DeleteAll();
                    _db.MediaLibary.AddSongs(import.Item1);
                    _db.MediaLibary.SaveQuery(import.Item2);
                }
            });
        }

        public Task Export(Stream xmlData)
        {
            var export = Tuple.Create(_db.MediaLibary.DoQuery(), _db.MediaLibary.GetAllQuery());

            return Task.Run(() =>
            {
                EntitySerializer.Serialize(xmlData, export);
            });
        }
    }
}
