﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Akka.Util.Internal;
using Caliburn.Micro;
using Prover.CommProtocol.Common;
using Prover.CommProtocol.Common.IO;
using Prover.CommProtocol.MiHoneywell;
using Prover.Core.DriveTypes;
using Prover.Core.Events;
using Prover.Core.Models.Instruments;
using Prover.Core.Settings;
using Prover.Core.VerificationTests;
using Prover.GUI.Common;
using Prover.GUI.Common.Screens;
using Prover.GUI.Screens.QAProver.PTVerificationViews;
using Prover.GUI.Screens.Settings;
using ReactiveUI;
using Splat;
using SerialPort = System.IO.Ports.SerialPort;

namespace Prover.GUI.Screens.QAProver
{
    public class TestRunViewModel : ViewModelBase, IDisposable
    {
        private const string NewQaTestViewContext = "NewTestView";
        private const string EditQaTestViewContext = "EditTestView";

        private IQaRunTestManager _qaRunTestManager;

        public ReactiveCommand<object> StartTestCommand { get; private set; }

        public TestRunViewModel(ScreenManager screenManager, IEventAggregator eventAggregator)
            : base(screenManager, eventAggregator)
        {
            SettingsManager.RefreshSettings();

            /***  Setup Instruments list  ***/
            InstrumentTypes = new ReactiveList<SelectableInstrumentType>
            {
                ChangeTrackingEnabled = true,
            };

            Instruments.GetAll().ForEach(
                x => InstrumentTypes.Add(new SelectableInstrumentType
                    {
                        Instrument = x,
                        IsSelected = x.Name == SettingsManager.SettingsInstance.LastInstrumentTypeUsed
                    }));
            
            InstrumentTypes.ItemChanged
                .Where(x => x.PropertyName == "IsSelected" && x.Sender.IsSelected)
                .Select(x => x.Sender)
                .Subscribe(x => {
                    SettingsManager.SettingsInstance.LastInstrumentTypeUsed = x.Instrument.Name;
                    SettingsManager.Save();
                });

            /***  Setup Comm Ports and Baud Rate settings ***/
            CommPort = PortsWatcherObservable().CreateCollection();
            
            _selectedCommPort = CommPort.Contains(SettingsManager.SettingsInstance.InstrumentCommPort) ? SettingsManager.SettingsInstance.InstrumentCommPort : string.Empty;
            _selectedBaudRate = BaudRate.Contains(SettingsManager.SettingsInstance.InstrumentBaudRate) ? SettingsManager.SettingsInstance.InstrumentBaudRate : -1;
            _selectedTachCommPort = TachCommPort.Contains(SettingsManager.SettingsInstance.TachCommPort) ? SettingsManager.SettingsInstance.TachCommPort : string.Empty;

            this.WhenAnyValue(x => x.SelectedBaudRate, x => x.SelectedCommPort, x => x.SelectedTachCommPort)
                .Subscribe(_ =>
                {
                    SettingsManager.SettingsInstance.InstrumentBaudRate = _.Item1;
                    SettingsManager.SettingsInstance.InstrumentCommPort = _.Item2;
                    SettingsManager.SettingsInstance.TachCommPort = _.Item3;
                    SettingsManager.Save();
                });

            /*** Commands ***/
            var canStartNewTest = this.WhenAnyValue(x => x.SelectedBaudRate, x => x.SelectedCommPort, x => x.SelectedTachCommPort,
                (baud, instrumentPort, tachPort) => 
                    BaudRate.Contains(baud) && !string.IsNullOrEmpty(instrumentPort) && !string.IsNullOrEmpty(tachPort));

            StartTestCommand = ReactiveCommand.Create(canStartNewTest);
            StartTestCommand.Subscribe(async _ => await StartNewQaTest());

            _viewContext = NewQaTestViewContext;
        }

        private IObservable<string> PortsWatcherObservable()
        {
            return Observable.Create<string>(observer =>
            {
                return Observable
                    .Interval(TimeSpan.FromSeconds(1))
                    .Subscribe(
                        _ =>
                        {
                            if (!_commPort.SequenceEqual(SerialPort.GetPortNames()))
                            {
                                SerialPort.GetPortNames().ForEach(observer.OnNext);
                            }
                        });
            });
        }

        private string _viewContext;
        public string ViewContext
        {
            get { return _viewContext; }
            set { this.RaiseAndSetIfChanged(ref _viewContext, value); }
        }

        public InstrumentType SelectedInstrument => InstrumentTypes?.FirstOrDefault(i => i.IsSelected)?.Instrument;
        private ReactiveList<SelectableInstrumentType> _instrumentTypes;
        public ReactiveList<SelectableInstrumentType> InstrumentTypes
        {
            get { return _instrumentTypes; }
            set { this.RaiseAndSetIfChanged(ref _instrumentTypes, value); }
        }

        private IReactiveDerivedList<string> _commPort;
        public IReactiveDerivedList<string> CommPort
        {
            get { return _commPort; }
            set { this.RaiseAndSetIfChanged(ref _commPort, value); }
        }

        private string _selectedCommPort;
        public string SelectedCommPort
        {
            get { return _selectedCommPort; }
            set { this.RaiseAndSetIfChanged(ref _selectedCommPort, value); }
        }

        public List<string> TachCommPort => SerialPort.GetPortNames().ToList();
        private string _selectedTachCommPort;
        public string SelectedTachCommPort
        {
            get { return _selectedTachCommPort; }
            set { this.RaiseAndSetIfChanged(ref _selectedTachCommPort, value); }
        }

        public List<int> BaudRate => CommProtocol.Common.IO.SerialPort.BaudRates;
        private int _selectedBaudRate;
        public int SelectedBaudRate
        {
            get { return _selectedBaudRate; }
            set { this.RaiseAndSetIfChanged(ref _selectedBaudRate, value); }
        }
        
        public async Task CancelCommand()
        {
            await ScreenManager.GoHome();
        }

        public async Task StartNewQaTest()
        {
            if (SelectedInstrument != null) 
            {
                _qaRunTestManager = Locator.Current.GetService<IQaRunTestManager>();

                SettingsManager.SettingsInstance.LastInstrumentTypeUsed = SelectedInstrument.Name;
                SettingsManager.Save();

                try
                {
                    await _qaRunTestManager.InitializeTest(SelectedInstrument);

                    await Task.Run(() =>
                    {
                        SiteInformationItem = ScreenManager.ResolveViewModel<InstrumentInfoViewModel>();
                        SiteInformationItem.Instrument = _qaRunTestManager.Instrument;

                        foreach (var x in _qaRunTestManager.Instrument.VerificationTests.OrderBy(v => v.TestNumber))
                        {
                            var item = ScreenManager.ResolveViewModel<VerificationSetViewModel>();
                            item.InitializeViews(x, _qaRunTestManager);
                            item.VerificationTest = x;

                            TestViews.Add(item);
                        }

                        if (_qaRunTestManager.Instrument.VolumeTest?.DriveType is RotaryDrive)
                            MeterDisplacementItem = 
                                new RotaryMeterTestViewModel((RotaryDrive)_qaRunTestManager.Instrument.VolumeTest.DriveType);
                    });

                    ViewContext = EditQaTestViewContext;
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
                }
            }
        }

        public RotaryMeterTestViewModel MeterDisplacementItem { get; set; }

        public InstrumentInfoViewModel SiteInformationItem { get; set; }

        public ObservableCollection<VerificationSetViewModel> TestViews { get; set; } =
            new ObservableCollection<VerificationSetViewModel>();

        public VolumeTestViewModel VolumeInformationItem { get; set; }

        public class SelectableInstrumentType
        {
            public InstrumentType Instrument { get; set; }
            public bool IsSelected { get; set; }
        }

        public void Dispose()
        {
            _qaRunTestManager?.Dispose();
        }
    }
}