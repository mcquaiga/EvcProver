﻿using System.Reactive.Disposables;
using System.Reactive.Linq;
using Devices.Core.Items;
using Devices.Core.Items.ItemGroups;
using Prover.Application.ViewModels.Corrections;
using Prover.Calculations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Prover.Application.ViewModels.Volume
{
    public class PulseOutputTestViewModel : DeviationTestViewModel<PulseOutputItems.ChannelItems>
    {
        private readonly VolumeTestRunViewModelBase _volumeTest;

        public PulseOutputTestViewModel(PulseOutputItems.ChannelItems pulseChannelItems) 
            : base(Tolerances.PULSE_VARIANCE_THRESHOLD)
        {
            Items = pulseChannelItems;
        }

        public PulseOutputTestViewModel(PulseOutputItems.ChannelItems pulseChannelItems, VolumeTestRunViewModelBase volumeTest) 
            : this(pulseChannelItems)
        {
            _volumeTest = volumeTest;
            var multiplier = (Items as IVolumeUnits)?.Units.Multiplier ?? volumeTest.Multiplier;

            this.WhenAnyValue(x => x._volumeTest.ExpectedValue)
                .Select(expectedVolume => VolumeCalculator.PulseCount(expectedVolume, multiplier))
                .ToPropertyEx(this, x => x.ExpectedValue)
                .DisposeWith(Cleanup);
        }
    }
}