﻿using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Extensions;

namespace Prover.UI.Desktop.Dialogs
{
    public class InputDialogViewModel : DialogViewModel, IValidatableViewModel
    {
        public InputDialogViewModel()
        {

        }

        public InputDialogViewModel(string message, string title = null)
        {
            Message = message;
            Title = title;

            this.ValidationRule(
                viewModel => viewModel.InputValue,
                value => !string.IsNullOrWhiteSpace(value),
                "Value cannot be empty.");
        }

        public string Message { get; }
        public string Title { get; }
        [Reactive] public string InputValue { get; set; }

    }
}