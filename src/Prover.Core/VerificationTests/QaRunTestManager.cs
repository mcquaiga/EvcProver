﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Microsoft.Practices.Unity;
using NLog;
using Prover.CommProtocol.Common;
using Prover.CommProtocol.Common.Items;
using Prover.CommProtocol.MiHoneywell;
using Prover.Core.Events;
using Prover.Core.EVCTypes;
using Prover.Core.ExternalIntegrations;
using Prover.Core.Models.Instruments;
using Prover.Core.Settings;
using Prover.Core.Storage;
using Prover.Core.VerificationTests.VolumeTest;
using LogManager = NLog.LogManager;

namespace Prover.Core.VerificationTests
{
    public class QaRunTestManager : IDisposable
    {
        private const int VolumeTestNumber = 0;
        protected static Logger Log = LogManager.GetCurrentClassLogger();
        protected readonly IUnityContainer Container;

        public QaRunTestManager(
            IUnityContainer container,
            EvcCommunicationClient commClient,
            InstrumentType instrumentType,
            IDriveType driveType,            
            IVerifier verifier)
        {
            Container = container;
            CommunicationClient = commClient;
            Verifier = verifier;
            InstrumentType = instrumentType;
            DriveType = driveType;
        }

        public IDriveType DriveType { get; set; }
        public InstrumentType InstrumentType { get; set; }
        public IVerifier Verifier { get; private set; }
        public Instrument Instrument { get; private set; }
        public EvcCommunicationClient CommunicationClient { get; }
        public VolumeTestManager VolumeTestManager { get; set; }

        public void Dispose()
        {
            CommunicationClient.Dispose();
        }

        public async Task Init()
        {
            await CommunicationClient.Connect();
            var items =
                await CommunicationClient.GetItemValues(CommunicationClient.ItemDetails.GetAllItemNumbers());
            Instrument = new Instrument(InstrumentType, DriveType, items);
            ReadingStabilizer = new ReadingStabilizer(Container, Instrument);
            await CommunicationClient.Disconnect();

            await RunVerifier();
        }

        public ReadingStabilizer ReadingStabilizer { get; set; }

        public async Task RunTest(int level)
        {
            if (Instrument == null) throw new NullReferenceException("Call Init method before running a test.");

            await ReadingStabilizer.WaitForReadingsToStabilizeAsync(CommunicationClient, level);
            await DownloadVerificationTestItems(level);

            if (Instrument.VerificationTests.FirstOrDefault(x => x.TestNumber == level)?.VolumeTest != null)
                await VolumeTestManager.PreTest();
        }

        public async Task DownloadVerificationTestItems(int level)
        {
            if (!CommunicationClient.IsConnected)
                await CommunicationClient.Connect();

            if (Instrument.CompositionType == CorrectorType.PTZ)
            {
                await DownloadTemperatureTestItems(level);
                await DownloadPressureTestItems(level);
            }

            if (Instrument.CompositionType == CorrectorType.T)
                await DownloadTemperatureTestItems(level);

            if (Instrument.CompositionType == CorrectorType.P)
                await DownloadPressureTestItems(level);

            await CommunicationClient.Disconnect();
        }

        public async Task DownloadTemperatureTestItems(int levelNumber)
        {
            var test = Instrument.VerificationTests.FirstOrDefault(x => x.TestNumber == levelNumber).TemperatureTest;

            if (test != null)
                test.Items = await CommunicationClient.GetItemValues(CommunicationClient.ItemDetails.TemperatureItems());
        }

        public async Task DownloadPressureTestItems(int level)
        {
            var test = Instrument.VerificationTests.FirstOrDefault(x => x.TestNumber == level).PressureTest;
            if (test != null)
                test.Items = await CommunicationClient.GetItemValues(CommunicationClient.ItemDetails.PressureItems());
        }

        public async Task SaveAsync()
        {
            try
            {
                using (var store = new InstrumentStore(Container))
                {
                    await store.UpsertAsync(Instrument);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public async Task RunVerifier()
        {
            Verifier = Container.Resolve<IVerifier>(new ParameterOverride("commClient", CommunicationClient),
                new ParameterOverride("instrument", Instrument));
            var success = await Verifier.Verify();
        }
    }
}