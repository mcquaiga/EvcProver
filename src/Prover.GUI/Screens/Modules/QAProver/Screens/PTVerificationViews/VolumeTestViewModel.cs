﻿using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Caliburn.Micro;
using Prover.Core.Extensions;
using Prover.Core.Models.Instruments;
using Prover.Core.Models.Instruments.DriveTypes;
using Prover.Core.VerificationTests;
using Prover.Core.VerificationTests.VolumeVerification;
using Prover.GUI.Events;
using Prover.GUI.Screens.Dialogs;
using ReactiveUI;

namespace Prover.GUI.Screens.Modules.QAProver.Screens.PTVerificationViews
{
    public class VolumeTestViewModel : TestRunViewModelBase<Core.Models.Instruments.VolumeTest>
    {
        internal enum TestStep
        {
            PreTest,
            PostTest
        }

        public VolumeTestViewModel(ScreenManager screenManager, IEventAggregator eventAggregator, Prover.Core.Models.Instruments.VolumeTest volumeTest, IQaRunTestManager qaRunTestManager = null)
            : base(screenManager, eventAggregator, volumeTest)
        {
            Volume = volumeTest;
            TestManager = qaRunTestManager;

            AppliedInput = (long)Volume.AppliedInput;
            UncorrectedPulseCount = Volume.UncPulseCount;
            CorrectedPulseCount = Volume.CorPulseCount;

            var canRunTestCommand = this.WhenAny(x => x.TestManager, tm => tm != null);
            canRunTestCommand.ToProperty(this, model => model.DisplayButtons, out _displayButtons);

            CreateDriveSpecificViews();

            if (TestManager != null)
            {
                if (TestManager?.VolumeTestManager is ManualVolumeTestManager)
                {
                    var canRunPreTest = this.WhenAnyValue(x => x.ManualVolumeTestStep)
                        .Select(x => x == TestStep.PreTest);
                    PreVolumeTestCommand = DialogDisplayHelpers.ProgressStatusDialogCommand(eventAggregator,
                        "Starting Volume Test...", RunPreVolumeTest, canRunPreTest);

                    var canRunPostTest = this.WhenAnyValue(x => x.ManualVolumeTestStep)
                        .Select(x => x == TestStep.PostTest || x == TestStep.PreTest);
                    PostVolumeTestCommand = DialogDisplayHelpers.ProgressStatusDialogCommand(eventAggregator,
                        "Finishing Volume Test...", RunPostVolumeTest, canRunPostTest);

                    ManualVolumeTestStep = TestStep.PreTest;
                }

                if (TestManager?.VolumeTestManager is AutoVolumeTestManager)
                    RunVolumeTestCommand = DialogDisplayHelpers.ProgressStatusDialogCommand(eventAggregator,
                        "Running Volume Test...", RunTest, canRunTestCommand);

                this.WhenAnyValue(x => x.AppliedInput)
                    .Subscribe(value =>
                    {
                        Volume.AppliedInput = value;
                        EventAggregator.PublishOnUIThread(VerificationTestEvent.Raise(TestRun.VerificationTest));
                    });

                this.WhenAnyValue(x => x.UncorrectedPulseCount)
                    .Subscribe(value =>
                    {
                        Volume.UncPulseCount = value;
                        EventAggregator.PublishOnUIThread(VerificationTestEvent.Raise(TestRun.VerificationTest));
                    });

                this.WhenAnyValue(x => x.CorrectedPulseCount)
                    .Subscribe(value =>
                    {
                        Volume.CorPulseCount = value;
                        EventAggregator.PublishOnUIThread(VerificationTestEvent.Raise(TestRun.VerificationTest));
                    });
            }
        }

        public ReactiveCommand RunVolumeTestCommand { get; set; }
        public ReactiveCommand PreVolumeTestCommand { get; set; }
        public ReactiveCommand PostVolumeTestCommand { get; set; }

        #region Methods

        private async Task RunPreVolumeTest(IObserver<string> status, CancellationToken ct)
        {
            TestManager.VolumeTestManager.StatusMessage.Subscribe(status);
            await TestManager.DownloadPreVolumeTest(ct);
            ManualVolumeTestStep = TestStep.PostTest;
            EventAggregator.PublishOnUIThread(VerificationTestEvent.Raise(Volume.VerificationTest));
        }

        private async Task RunPostVolumeTest(IObserver<string> status, CancellationToken ct)
        {
            try
            {
                TestManager.VolumeTestManager.StatusMessage
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(status);

                await TestManager.DownloadPostVolumeTest(ct);
                ManualVolumeTestStep = TestStep.PreTest;
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    $"An error occured during the verification test. See exception for details. {ex.Message}");
            }
            finally
            {
                EventAggregator.PublishOnUIThread(VerificationTestEvent.Raise(Volume.VerificationTest));
            }
        }

        public async Task RunTest(IObserver<string> status, CancellationToken ct)
        {
            try
            {
                TestManager.VolumeTestManager.StatusMessage.Subscribe(status);
                await TestManager.RunVolumeTest(ct);
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    $"An error occured during the verification test. See exception for details. {ex.Message}");
            }
            finally
            {
                AppliedInput = (long)Volume.AppliedInput;
                UncorrectedPulseCount = Volume.UncPulseCount;
                CorrectedPulseCount = Volume.CorPulseCount;
                EventAggregator.PublishOnUIThread(VerificationTestEvent.Raise(TestRun.VerificationTest));
            }
        }

        private void CreateDriveSpecificViews()
        {
            if (Volume?.DriveType is MechanicalDrive)
                EnergyTestItem = new EnergyTestViewModel(EventAggregator, ((MechanicalDrive)Volume.DriveType).Energy);
            else if (Volume?.DriveType is RotaryDrive)
                MeterDisplacementItem = new RotaryMeterTestViewModel((RotaryDrive) Volume.DriveType);

            if (Volume?.VerificationTest.FrequencyTest != null)
            {
                FrequencyTestItem = new FrequencyTestViewModel(ScreenManager, EventAggregator, Volume.VerificationTest.FrequencyTest);
            }
        }

        #endregion

        #region Properties

        private TestStep _manualVolumeTestStep;

        internal TestStep ManualVolumeTestStep
        {
            get => _manualVolumeTestStep;
            set => this.RaiseAndSetIfChanged(ref _manualVolumeTestStep, value);
        }

        public bool IsAutoVolumeTest => TestManager?.VolumeTestManager is AutoVolumeTestManager;
        public bool IsManualVolumeTest => TestManager?.VolumeTestManager is ManualVolumeTestManager;
        public IQaRunTestManager TestManager { get; set; }
        public Instrument Instrument => Volume.Instrument;
        public Core.Models.Instruments.VolumeTest Volume { get; }

        private long _appliedInput;

        public long AppliedInput
        {
            get => _appliedInput;
            set => this.RaiseAndSetIfChanged(ref _appliedInput, value);
        }

        private int _uncorrectedPulseCount;

        public int UncorrectedPulseCount
        {
            get => _uncorrectedPulseCount;
            set => this.RaiseAndSetIfChanged(ref _uncorrectedPulseCount, value);
        }

        private int _correctedPulseCount;

        public int CorrectedPulseCount
        {
            get => _correctedPulseCount;
            set => this.RaiseAndSetIfChanged(ref _correctedPulseCount, value);
        }

        public EnergyTestViewModel EnergyTestItem { get; set; }
        public RotaryMeterTestViewModel MeterDisplacementItem { get; set; }
        public FrequencyTestViewModel FrequencyTestItem { get; set; }

        public string DriveRateDescription => Instrument.DriveRateDescription();
        public string UnCorrectedMultiplierDescription => Instrument.UnCorrectedMultiplierDescription();
        public string CorrectedMultiplierDescription => Instrument.CorrectedMultiplierDescription();
        public decimal? TrueUncorrected => decimal.Round(Volume.TrueUncorrected.Value, 4);

        public decimal? TrueCorrected
        {
            get
            {
                if (Volume.TrueCorrected != null) return decimal.Round(Volume.TrueCorrected.Value, 4);

                return null;
            }
        }

        public decimal? StartUncorrected => Volume.Items?.Uncorrected();
        public decimal? EndUncorrected => Volume.AfterTestItems.Uncorrected();
        public decimal? StartCorrected => Volume.Items?.Corrected();
        public decimal? EndCorrected => Volume.AfterTestItems.Corrected();
        public decimal? EvcUncorrected => Volume.EvcUncorrected;
        public decimal? EvcCorrected => Volume.EvcCorrected;

        public Brush UnCorrectedPercentColour
            =>
                Volume?.UnCorrectedHasPassed == true
                    ? Brushes.White
                    : (SolidColorBrush) new BrushConverter().ConvertFrom("#DC6156");

        public Brush CorrectedPercentColour
            =>
                Volume?.CorrectedHasPassed == true
                    ? Brushes.White
                    : (SolidColorBrush) new BrushConverter().ConvertFrom("#DC6156");

        public Brush MeterDisplacementPercentColour
        {
            get
            {
                var rotaryDrive = Volume?.DriveType as RotaryDrive;
                return rotaryDrive?.Meter.MeterDisplacementHasPassed == true ? Brushes.Green : Brushes.Red;
            }
        }

        #endregion

        private readonly ObservableAsPropertyHelper<bool> _displayButtons;
        public bool DisplayButtons => _displayButtons.Value;

        protected override void RaisePropertyChangeEvents()
        {
            NotifyOfPropertyChange(() => TrueCorrected);
            NotifyOfPropertyChange(() => TrueUncorrected);
            NotifyOfPropertyChange(() => StartUncorrected);
            NotifyOfPropertyChange(() => EndUncorrected);
            NotifyOfPropertyChange(() => StartCorrected);
            NotifyOfPropertyChange(() => EndCorrected);
            NotifyOfPropertyChange(() => EvcUncorrected);
            NotifyOfPropertyChange(() => EvcCorrected);
            NotifyOfPropertyChange(() => StartCorrected);
            NotifyOfPropertyChange(() => UnCorrectedPercentColour);
            NotifyOfPropertyChange(() => CorrectedPercentColour);
            NotifyOfPropertyChange(() => Volume);
        }
    }
}