﻿using Devices.Communications.Status;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Prover.Application.Interactions;
using Prover.Application.Interfaces;
using Prover.Application.Services.LiveReadCorrections;
using Prover.Application.Verifications.Volume;
using Prover.UI.Desktop.Dialogs;
using Prover.UI.Desktop.Views.Devices;
using Prover.UI.Desktop.Views.QATests.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;

namespace Prover.UI.Desktop.ViewModels.Devices {
	public class DeviceSessionDialogManager : DialogViewModel {
		private readonly ILogger<DeviceSessionDialogManager> _logger;
		private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
		private CompositeDisposable _cleanup;
		private SessionDialogView _dialogView;
		private SessionStatusDialogView _sessionStatusView;

		protected SessionStatusDialogViewModel SessionStatusUpdates;

		public DeviceSessionDialogManager(ILogger<DeviceSessionDialogManager> logger,
			IDialogServiceManager dialogManager) {
			DialogManager = dialogManager;
			_logger = logger ?? NullLogger<DeviceSessionDialogManager>.Instance;

			RegisterDeviceInteractions();

			RequestCancellation = ReactiveCommand.CreateFromTask(async () => {
				_logger.LogDebug("Cancellation Requested.");
				_cancellationTokenSource?.Cancel();
				await dialogManager.Close();
			});
		}

		public IDialogServiceManager DialogManager { get; }
		public ReactiveCommand<Unit, Unit> RequestCancellation { get; protected set; }

		[Reactive] public IViewFor SessionDialogContent { get; protected set; }

		public void RegisterDeviceInteractions() {
			DeviceInteractions.Connecting.RegisterHandler(async context => {
				_cancellationTokenSource = new CancellationTokenSource();

				SetSessionStatusDialog("Connecting", context.Input);

				await DialogManager.Show(_dialogView);
				context.SetOutput(_cancellationTokenSource.Token);
			});

			DeviceInteractions.Disconnecting.RegisterHandler(async context => {
				SetSessionStatusDialog("Disconnecting");

				context.SetOutput(_cancellationTokenSource.Token);
			});

			DeviceInteractions.DownloadingItems.RegisterHandler(async context => {
				SetSessionStatusDialog("Downloading items");

				context.SetOutput(_cancellationTokenSource.Token);
			});

			DeviceInteractions.LiveReading.RegisterHandler(async context => {
				SessionDialogContent = new LiveReadDialogView { ViewModel = (LiveReadCoordinator)context.Input };

				context.SetOutput(_cancellationTokenSource.Token);
			});

			DeviceInteractions.Unlinked.RegisterHandler(async context => {
				await DialogManager.Close();
				context.SetOutput(Unit.Default);
			});

			DeviceInteractions.SyncingVolumeTest.RegisterHandler(async context => {
				SetSessionStatusDialog("Syncing Volume...");// = new VolumeTestDialogView { ViewModel = (AutomatedVolumeTestRunnerBase)context.Input };
				await DialogManager.Show(_dialogView);
				context.SetOutput(_cancellationTokenSource.Token);
			});

			DeviceInteractions.StartVolumeTest.RegisterHandler(async context => {
				SessionDialogContent = new VolumeTestDialogView { ViewModel = (AutomatedVolumeTestRunnerBase)context.Input };

				await DialogManager.Show(_dialogView);
				context.SetOutput(_cancellationTokenSource.Token);
			});

			DeviceInteractions.CompleteVolumeTest.RegisterHandler(async context => {
				await DialogManager.Close();
				context.SetOutput(_cancellationTokenSource.Token);
			});
		}

		private void SetSessionStatusDialog(string message, IObservable<StatusMessage> statusMessageObservable = null) {
			
			if (_dialogView == null)
				_dialogView = new SessionDialogView { ViewModel = this };
			if (_sessionStatusView == null) {
				SessionStatusUpdates =
					new SessionStatusDialogViewModel(statusMessageObservable, _cancellationTokenSource) { StatusText = message };
				_sessionStatusView = new SessionStatusDialogView { ViewModel = SessionStatusUpdates };
			}

			if (statusMessageObservable != null)
				SessionStatusUpdates.RegisterStatusStream(statusMessageObservable);

			RxApp.MainThreadScheduler.Schedule(() => {
				SessionStatusUpdates.StatusText = message;
				SessionDialogContent = _sessionStatusView;
			});
		}
	}
}