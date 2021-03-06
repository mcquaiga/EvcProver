﻿using Autofac;
using Caliburn.Micro;
using NLog;
using Prover.CommProtocol.Common;
using Prover.CommProtocol.Common.IO;
using Prover.CommProtocol.Common.Items;
using Prover.CommProtocol.Common.Models;
using Prover.CommProtocol.Common.Models.Instrument;
using Prover.CommProtocol.MiHoneywell;
using Prover.Core.Models.Clients;
using Prover.Core.Settings;
using Prover.Core.VerificationTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using LogManager = NLog.LogManager;

namespace Prover.Core.Testing
{
    public class RotaryStressTest
    {
        public Subject<string> Status { get; } = new Subject<string>();

        public RotaryStressTest(ISettingsService testSettings, Func<string, int, ICommPort> commPortFactory)
        {
            _testSettings = testSettings;
            _commPortFactory = commPortFactory;
        }

        public async Task Run(IEvcDevice instrumentType, ICommPort commPort, Client client, CancellationToken ct = new CancellationToken())
        {
            _testSettings.Local.AutoSave = false;
            _testSettings.TestSettings.StabilizeLiveReadings = false;

            _testSettings.TestSettings.TestPoints.Clear();
            _testSettings.TestSettings.TestPoints = _testPointSettings;

            try
            {
                //await RunMechanicalTest(instrumentType, client, ct);
                await RunRotaryTest(instrumentType, client, ct);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            finally
            {
                await _testSettings.RefreshSettings();
            }
        }

        private readonly Func<string, int, ICommPort> _commPortFactory;

        private readonly Logger _log = LogManager.GetCurrentClassLogger();

        private readonly ISettingsService _testSettings;

        private List<TestSettings.TestPointSetting> _testPointSettings = new List<TestSettings.TestPointSetting>()
        {
            new TestSettings.TestPointSetting()
            {
                Level = 0,
                IsVolumeTest = true,
                TemperatureGauge = 32,
                PressureGaugePercent = 80
            },
            new TestSettings.TestPointSetting()
            {
                Level = 1,
                IsVolumeTest = false,
                TemperatureGauge = 32,
                PressureGaugePercent = 80
            },
            new TestSettings.TestPointSetting()
            {
                Level = 2,
                IsVolumeTest = false,
                TemperatureGauge = 32,
                PressureGaugePercent = 80
            }
        };

        private ICommPort GetCommPort()
        {
            return _commPortFactory.Invoke(_testSettings.Local.InstrumentCommPort, _testSettings.Local.InstrumentBaudRate);
        }

        private int GetMeterId(IEvcDevice instrumentType, MeterIndexItemDescription meterInfo)
        {
            if (instrumentType == HoneywellInstrumentTypes.Ec350)
            {
                return meterInfo.Ids.FirstOrDefault(i => i > 80);
            }

            return meterInfo.Ids.FirstOrDefault(i => i < 80);
        }

        private async Task RunMechanicalTest(IEvcDevice instrumentType, Client client, CancellationToken ct)
        {
            IEnumerable<ItemMetadata.ItemDescription> corrVolumeUnits = instrumentType.ItemsMetadata.GetItemDescriptions(90);

            int x = 1;
            foreach (ItemMetadata.ItemDescription corUnits in corrVolumeUnits)
            {
                _log.Info($"Smoke test #{x} of {corrVolumeUnits.Count()}");
                ICommPort commPort = GetCommPort();
                using (EvcCommunicationClient commClient = EvcCommunicationClient.Create(instrumentType, commPort))
                {
                    //commClient.Status.Subscribe(Status);
                    await commClient.Connect(ct);

                    await commClient.SetItemValue(90, corUnits.Id);

                    await commClient.Disconnect();
                }

                await Task.Delay(TimeSpan.FromSeconds(1));

                commPort = GetCommPort();
                using (IQaRunTestManager qaRunTestManager = IoC.Get<IQaRunTestManager>())
                {
                    await qaRunTestManager.InitializeTest(instrumentType, commPort, _testSettings, ct, client, false);
                    qaRunTestManager.Status.Subscribe(Status);
                    await qaRunTestManager.RunCorrectionTest(0, ct);
                    await qaRunTestManager.RunVolumeTest(ct);
                    await qaRunTestManager.SaveAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1));

                x++;
            }
        }

        private async Task RunRotaryTest(IEvcDevice instrumentType, Client client, CancellationToken ct)
        {
            IEnumerable<ItemValue> items;
            IEnumerable<ItemMetadata.ItemDescription> meterTypes = instrumentType.ItemsMetadata.GetItemDescriptions(432);

            ICommPort commPort = GetCommPort();
            using (EvcCommunicationClient commClient = EvcCommunicationClient.Create(instrumentType, commPort))
            {
                await commClient.Connect(ct);
                items = await commClient.GetAllItems();
                await commClient.Disconnect();
            }

            MeterIndexItemDescription mt = items.GetItem(432).ItemDescription as MeterIndexItemDescription;
            int x = 1;
            IEnumerable<ItemMetadata.ItemDescription> mountTypes = meterTypes.Where(m => (m as MeterIndexItemDescription).MountType == mt.MountType);
            foreach (MeterIndexItemDescription meter in mountTypes)
            {
                _log.Info($"Smoke test #{x} of {mountTypes.Count()}");
                commPort = GetCommPort();
                using (EvcCommunicationClient commClient = EvcCommunicationClient.Create(instrumentType, commPort))
                {
                    //commClient.Status.Subscribe(Status);
                    await commClient.Connect(ct);

                    await commClient.SetItemValue(432, GetMeterId(instrumentType, meter));
                    await commClient.SetItemValue(439, meter.MeterDisplacement.Value.ToString());

                    await commClient.Disconnect();
                }

                commPort = GetCommPort();
                using (IQaRunTestManager qaRunTestManager = IoC.Get<IQaRunTestManager>())
                {
                    //qaRunTestManager.Status.Subscribe(Status);
                    await qaRunTestManager.InitializeTest(instrumentType, commPort, _testSettings, ct, client);

                    await qaRunTestManager.RunCorrectionTest(0, ct);
                    await qaRunTestManager.RunCorrectionTest(1, ct);
                    await qaRunTestManager.RunCorrectionTest(2, ct);

                    await qaRunTestManager.RunVolumeTest(ct);
                    await qaRunTestManager.SaveAsync();
                    qaRunTestManager.VolumeTestManager.Dispose();
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
                x++;
            }
        }
    }
}