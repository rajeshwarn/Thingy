﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Thingy.Controls
{
    /// <summary>
    /// Interaction logic for CalculatorKeyboard.xaml
    /// </summary>
    public partial class CalculatorKeyboard : UserControl
    {
        private bool _sortAscending;

        public CalculatorKeyboard()
        {
            InitializeComponent();
            _sortAscending = true;
        }

        public static readonly DependencyProperty ClickCommandProperty =
            DependencyProperty.Register("ClickCommand", typeof(ICommand), typeof(CalculatorKeyboard), new FrameworkPropertyMetadata(null));

        public ICommand ClickCommand
        {
            get { return (ICommand)GetValue(ClickCommandProperty); }
            set { SetValue(ClickCommandProperty, value); }
        }

        public static readonly DependencyProperty NumSysCommandProperty =
            DependencyProperty.Register("NumSysCommand", typeof(ICommand), typeof(CalculatorKeyboard), new FrameworkPropertyMetadata(null));

        public ICommand NumSysCommand
        {
            get { return (ICommand)GetValue(NumSysCommandProperty); }
            set { SetValue(NumSysCommandProperty, value); }
        }

        public static readonly DependencyProperty ExecuteCommandProperty =
            DependencyProperty.Register("ExecuteCommand", typeof(ICommand), typeof(CalculatorKeyboard), new FrameworkPropertyMetadata(null));

        public ICommand ExecuteCommand
        {
            get { return (ICommand)GetValue(ExecuteCommandProperty); }
            set { SetValue(ExecuteCommandProperty, value); }
        }

        public static readonly DependencyProperty VisibleFunctionsProperty =
            DependencyProperty.Register("VisibleFunctions", typeof(IEnumerable<Tuple<string, string>>), typeof(CalculatorKeyboard));

        public IEnumerable<Tuple<string, string>> VisibleFunctions
        {
            get { return (IEnumerable<Tuple<string, string>>)GetValue(VisibleFunctionsProperty); }
            set { SetValue(VisibleFunctionsProperty, value); }
        }

        public static readonly DependencyProperty InputFunctionsProperty =
            DependencyProperty.Register("InputFunctions", typeof(IEnumerable<Tuple<string, string>>), typeof(CalculatorKeyboard), new PropertyMetadata(null, InputChanged));

        private static void InputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CalculatorKeyboard s)
            {
                s.DoFiltering();
            }
        }

        public IEnumerable<Tuple<string, string>> InputFunctions
        {
            get { return (IEnumerable<Tuple<string, string>>)GetValue(InputFunctionsProperty); }
            set { SetValue(InputFunctionsProperty, value); }
        }

        private void DoFiltering()
        {
            IEnumerable<Tuple<string, string>> draw;
            if (string.IsNullOrEmpty(FilterText.Text))
                draw = InputFunctions;
            else
                draw = InputFunctions.Where(f => f.Item1.StartsWith(FilterText.Text, StringComparison.InvariantCultureIgnoreCase));

            if (draw != null)
            {
                if (_sortAscending)
                    VisibleFunctions = draw.OrderBy(f => f.Item1);
                else
                    VisibleFunctions = draw.OrderByDescending(f => f.Item1);
            }
        }

        private void FilterText_TextChanged(object sender, TextChangedEventArgs e)
        {
            DoFiltering();
        }

        private void SortDesc_Click(object sender, RoutedEventArgs e)
        {
            _sortAscending = false;
            DoFiltering();
        }

        private void SortAsc_Click(object sender, RoutedEventArgs e)
        {
            _sortAscending = true;
            DoFiltering();
        }
    }
}
