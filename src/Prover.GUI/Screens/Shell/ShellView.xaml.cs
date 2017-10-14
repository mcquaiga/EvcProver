﻿using System;
using System.Reactive.Linq;
using System.Windows;
using ReactiveUI;

namespace Prover.GUI.Screens.Shell
{
    /// <summary>
    ///     Interaction logic for ShellView.xaml
    /// </summary>
    public partial class ShellView : IViewFor<ShellViewModel>
    {
        public ShellView()
        {
            InitializeComponent();
            
            Application.Current.MainWindow.WindowState = WindowState.Maximized;
            Style = (Style)FindResource(typeof(Window));
            
            this.WhenActivated(d =>
            {
                d(ViewModel = (ShellViewModel) DataContext);

                d(this.WhenAnyValue(x => x.ViewModel.GoHomeCommand)
                    .SelectMany(x => x.Execute())
                    .Subscribe());
            });
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (ShellViewModel) value; }
        }

        public ShellViewModel ViewModel { get; set; }

        public void Dispose()
        {
        }
    }
}