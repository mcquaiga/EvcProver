﻿using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Prover.CommProtocol.Common;
using Prover.CommProtocol.Common.IO;
using Prover.CommProtocol.Common.Items;
using Prover.Core.ExternalIntegrations.Validators;
using Prover.Core.Models.Instruments;
using Prover.Core.Settings;
using Prover.Core.Storage;
using Prover.Core.VerificationTests.VolumeVerification;

namespace Prover.Core.VerificationTests
{
    public interface IQaRunTestManager : IDisposable
    {
        Instrument Instrument { get; }
        Task InitializeTest(InstrumentType instrumentType, CommPort commPort);
        Task RunTest(int level, CancellationToken ct = new CancellationToken());
        Task SaveAsync();
        Task RunVerifier();

        IObservable<string> TestStatus { get; }
    }

    public class QaRunTestManager : IQaRunTestManager
    {
        private const int VolumeTestNumber = 0;
        protected static Logger Log = LogManager.GetCurrentClassLogger();
        private EvcCommunicationClient _communicationClient;
        private readonly IInstrumentStore<Instrument> _instrumentStore;
        private readonly IReadingStabilizer _readingStabilizer;
        private readonly IValidator _validator;
        private readonly IEvcItemReset _itemResetter;
        private readonly Subject<string> _testStatus = new Subject<string>();

        public QaRunTestManager(
            IInstrumentStore<Instrument> instrumentStore,
            IReadingStabilizer readingStabilizer,
            VolumeTestManager volumeTestManager,
            IValidator validator,
            IEvcItemReset itemResetter = null
        )
        {
            VolumeTestManager = volumeTestManager;
            _instrumentStore = instrumentStore;           
            _readingStabilizer = readingStabilizer;
            _validator = validator;
            _itemResetter = itemResetter;
        }

        public IObservable<string> TestStatus => _testStatus.AsObservable();

        public VolumeTestManager VolumeTestManager { get; set; }

        public void Dispose()
        {
            _communicationClient.Dispose();
            VolumeTestManager.Dispose();
        }

        public Instrument Instrument { get; private set; }

        public async Task InitializeTest(InstrumentType instrumentType, CommPort commPort)
        {
            _communicationClient = instrumentType.ClientFactory.Invoke(commPort);

            _testStatus.OnNext($"Connecting to {instrumentType.Name}...");
            await _communicationClient.Connect();

            _testStatus.OnNext("Downloading items...");
            var items = await _communicationClient.GetAllItems();

            Instrument = new Instrument(instrumentType, items);

            await RunVerifier();

            _testStatus.OnNext($"Disconnecting from {instrumentType.Name}...");
            await _communicationClient.Disconnect();
            
            await SaveAsync();
        }

        public async Task RunTest(int level, CancellationToken ct)
        {
            try
            {
                if (Instrument == null) throw new NullReferenceException("Call InitializeTest before runnning a test");

                _testStatus.OnNext($"Stabilizing live readings...");
                //await _readingStabilizer.WaitForReadingsToStabilizeAsync(_communicationClient, Instrument, level, ct);

                _testStatus.OnNext($"Downloading items...");
                await DownloadVerificationTestItems(level, ct);

                if (Instrument.VerificationTests.FirstOrDefault(x => x.TestNumber == level)?.VolumeTest != null)
                {
                    _testStatus.OnNext($"Running volume test...");
                    await VolumeTestManager.RunTest(_communicationClient, Instrument.VolumeTest, _itemResetter, ct);
                }
                _testStatus.OnNext($"Saving test...");
                await SaveAsync();
            }
            catch (OperationCanceledException ex)
            {
                Log.Info("Test run cancelled.");
            }            
        }

        private async Task DownloadVerificationTestItems(int level, CancellationToken ct)
        {
            if (!_communicationClient.IsConnected)
                await _communicationClient.Connect();

            if (Instrument.CompositionType == CorrectorType.PTZ)
            {
                await DownloadTemperatureTestItems(level);
                await DownloadPressureTestItems(level);
            }

            if (Instrument.CompositionType == CorrectorType.T)
                await DownloadTemperatureTestItems(level);

            if (Instrument.CompositionType == CorrectorType.P)
                await DownloadPressureTestItems(level);

            await _communicationClient.Disconnect();
        }

        public async Task DownloadTemperatureTestItems(int levelNumber)
        {
            var firstOrDefault = Instrument.VerificationTests.FirstOrDefault(x => x.TestNumber == levelNumber);
            var test = firstOrDefault?.TemperatureTest;

            if (test != null)
                test.Items =
                    await _communicationClient.GetTemperatureTestItems();
        }

        public async Task DownloadPressureTestItems(int level)
        {
            var firstOrDefault = Instrument.VerificationTests.FirstOrDefault(x => x.TestNumber == level);
            var test = firstOrDefault?.PressureTest;
            if (test != null)
                test.Items = await _communicationClient.GetPressureTestItems();
        }

        public async Task SaveAsync()
        {
            try
            {
                await _instrumentStore.UpsertAsync(Instrument);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public async Task RunVerifier()
        {
            if (_validator != null)
            {
                _testStatus.OnNext($"Verifying items...");
                await _validator.Validate(_communicationClient, Instrument);  
            }       
        }
    }
}