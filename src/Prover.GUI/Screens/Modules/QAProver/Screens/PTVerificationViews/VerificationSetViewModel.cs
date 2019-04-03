﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using Prover.Core.Models.Instruments;
using Prover.Core.Settings;
using Prover.Core.VerificationTests;
using Prover.GUI.Events;
using Prover.GUI.Screens.Dialogs;
using ReactiveUI;

namespace Prover.GUI.Screens.Modules.QAProver.Screens.PTVerificationViews
{
    public class VerificationSetViewModel : ViewModelBase, IDisposable
    {
        private readonly ISettingsService _settingsService;

        public VerificationSetViewModel(ScreenManager screenManager, IEventAggregator eventAggregator, ISettingsService settingsService)
            : base(screenManager, eventAggregator)
        {
            _settingsService = settingsService;
            RunTestCommand = DialogDisplayHelpers.ProgressStatusDialogCommand(eventAggregator, "Downloading data...", RunTest);
        }

        public IQaRunTestManager QaRunTestManager;

        public ColorZoneMode HeaderZoneColor => VerificationTest.TestNumber == 0 ? ColorZoneMode.PrimaryDark : ColorZoneMode.Accent;

        public Brush HeaderColour => VerificationTest.TestNumber == 0
                ? new SolidColorBrush(Colors.DarkRed)
                : new SolidColorBrush(Colors.Orange);

        public string Level => $"Level {VerificationTest.TestNumber + 1}";
        public bool ShowVolumeTestViewModel => VolumeTestViewModel != null;
        public TemperatureTestViewModel TemperatureTestViewModel { get; private set; }
        public PressureTestViewModel PressureTestViewModel { get; private set; }
        public SuperFactorTestViewModel SuperFactorTestViewModel { get; private set; }
        public VolumeTestViewModel VolumeTestViewModel { get; private set; }
        public VerificationTest VerificationTest { get; set; }

        public void InitializeViews(VerificationTest verificationTest, IQaRunTestManager qaTestRunTestManager = null)
        {
            VerificationTest = verificationTest;
            QaRunTestManager = qaTestRunTestManager;

            ShowDownloadButton = QaRunTestManager != null;

            if (VerificationTest.SuperFactorTest != null)
                SuperFactorTestViewModel = new SuperFactorTestViewModel(ScreenManager, EventAggregator, VerificationTest.SuperFactorTest);

            if (VerificationTest.TemperatureTest != null)
                TemperatureTestViewModel = new TemperatureTestViewModel(ScreenManager, EventAggregator, VerificationTest.TemperatureTest);

            if (VerificationTest.PressureTest != null)
                PressureTestViewModel = new PressureTestViewModel(ScreenManager, EventAggregator, VerificationTest.PressureTest, _settingsService);

            if (VerificationTest.VolumeTest != null)
                VolumeTestViewModel = new VolumeTestViewModel(ScreenManager, EventAggregator, VerificationTest.VolumeTest, QaRunTestManager);
        }

        public override void Dispose()
        {
            SuperFactorTestViewModel?.TryClose();
            TemperatureTestViewModel?.TryClose();
            PressureTestViewModel?.TryClose();
            VolumeTestViewModel?.TryClose();
            _cancelTestCommand?.Dispose();
            _runTestCommand?.Dispose();
        }

        #region Properties

        private bool _showDownloadButton;

        public bool ShowDownloadButton
        {
            get => _showDownloadButton;
            set => this.RaiseAndSetIfChanged(ref _showDownloadButton, value);
        }

        private bool _showProgressDialog;

        public bool ShowProgressDialog
        {
            get => _showProgressDialog;
            set => this.RaiseAndSetIfChanged(ref _showProgressDialog, value);
        }

        private string _testStatusMessage;

        public string TestStatusMessage
        {
            get => _testStatusMessage;
            set => this.RaiseAndSetIfChanged(ref _testStatusMessage, value);
        }

        private ReactiveCommand _cancelTestCommand;

        public ReactiveCommand CancelTestCommand
        {
            get => _cancelTestCommand;
            set => this.RaiseAndSetIfChanged(ref _cancelTestCommand, value);
        }

        private ReactiveCommand _runTestCommand;

        public ReactiveCommand RunTestCommand
        {
            get => _runTestCommand;
            set => this.RaiseAndSetIfChanged(ref _runTestCommand, value);
        }

        #endregion

        public async Task RunTest(IObserver<string> statusObserver, CancellationToken cancellationToken)
        {
            try
            {
                QaRunTestManager.TestStatus.Subscribe(statusObserver);
                await QaRunTestManager.RunCorrectionTest(VerificationTest.TestNumber, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    $"An error occured during the verification test. See exception for details. {ex.Message}");
            }
            finally
            {
                EventAggregator.PublishOnUIThread(VerificationTestEvent.Raise(VerificationTest));
            }
        }
    }
}