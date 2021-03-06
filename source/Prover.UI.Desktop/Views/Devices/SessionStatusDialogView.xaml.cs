﻿using System.Reactive.Disposables;
using Prover.UI.Desktop.ViewModels.Devices;
using ReactiveUI;

namespace Prover.UI.Desktop.Views.Devices
{
    /// <summary>
    /// Interaction logic for SessionStatusDialogView.xaml
    /// </summary>
    public partial class SessionStatusDialogView : ReactiveUserControl<SessionStatusDialogViewModel>
    {
        public SessionStatusDialogView()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {

                this.OneWayBind(ViewModel, vm => vm.TitleText, v => v.TitleText.Text)
                    .DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.StatusText, v => v.StatusText.Text)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.ProgressTotal, v => v.StatusProgressBar.Maximum).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.Progress, v => v.StatusProgressBar.Value).DisposeWith(d);
            });
        }
    }
}
