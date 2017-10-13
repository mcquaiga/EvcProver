﻿using System;
using System.Reactive.Linq;
using System.Windows.Controls;
using ReactiveUI;

namespace Prover.GUI.Modules.ClientManager.Screens
{
    /// <summary>
    ///     Interaction logic for ClientManager.xaml
    /// </summary>
    public partial class ClientManagerView : UserControl, IViewFor<ClientManagerViewModel>
    {
        public ClientManagerView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                ViewModel = (ClientManagerViewModel)DataContext;
                d(this.WhenAnyValue(x => x.ViewModel.LoadClientsCommand)
                    .SelectMany(x => x.Execute())
                    .Subscribe());
            });
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (ClientManagerViewModel) value; }
        }

        public ClientManagerViewModel ViewModel { get; set; }
    }
}