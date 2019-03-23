﻿namespace Prover.Core.VerificationTests.VolumeVerification
{
    using Caliburn.Micro;
    using Prover.Core.ExternalDevices;
    using Prover.Core.Models.Instruments;
    using Prover.Core.Settings;
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class MechanicalAutoVolumeTestManager : AutoVolumeTestManager
    {
        public MechanicalAutoVolumeTestManager(IEventAggregator eventAggregator, TachometerService tachComm, ISettingsService settingsService) : base(eventAggregator, tachComm, settingsService)
        {
        }      

        protected override async Task WaitForTestComplete(VolumeTest volumeTest, CancellationToken ct)
        {
            await Task.Run(() =>
            {
                var tachCount = 0;

                using (Observable
                        .Interval(TimeSpan.FromMilliseconds(500))
                        .Subscribe(async _ => {
                            tachCount = await TachometerCommunicator.ReadTach();
                        }))
                {
                    while (tachCount < 100 && !ct.IsCancellationRequested) { }
                }
            });                 
        }
    }
}