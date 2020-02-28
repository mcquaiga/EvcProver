﻿using System.ComponentModel;
using System.Threading.Tasks;
using Client.Desktop.Wpf.Screens.Dialogs;
using MvvmDialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Client.Desktop.Wpf.ViewModels
{
    public interface IScreenManager : IScreen
    {
        Task<IRoutableViewModel> ChangeView(IRoutableViewModel viewModel);
        Task<IRoutableViewModel> ChangeView<TViewModel>() where TViewModel : IRoutableViewModel;

        DialogServiceManager DialogManager { get; }

        Task GoHome();
        Task GoBack();
    }
}