﻿using AppLib.MVVM;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Input;
using Thingy.API;
using Thingy.InternalViewModels;

namespace Thingy.InternalModules
{
    public interface IBangView: IView
    {
        void Close();
    }

    /// <summary>
    /// Interaction logic for Bang.xaml
    /// </summary>
    public partial class Bang : CustomDialog, IBangView
    {
        private readonly IApplication _app;

        public Bang()
        {
            InitializeComponent();
        }

        public Bang(IApplication app) : this()
        {
            _app = app;
            DataContext = new BangViewModel(this, _app);
        }

        public async void Close()
        {
            await _app.CloseMessageBox(this);
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                (DataContext as BangViewModel).SearchCommand.Execute(null);
            }
        }
    }
}
