﻿using Devices.Core.Interfaces;
using Devices.Core.Items.DriveTypes;
using Prover.Application.ViewModels.Volume.Rotary;

namespace Prover.Application.ViewModels.Factories.Volume
{
    public partial class VolumeViewModelFactory
    {
        private void CreateRotaryVolume(DeviceInstance device, 
            EvcVerificationViewModel verificationViewModel,
            VerificationTestPointViewModel testPoint)
        {
            var rotaryViewModel = new RotaryVolumeViewModel(_startVolumeItems, _endVolumeItems);
            rotaryViewModel.DriveType = verificationViewModel.DriveType;
            
            rotaryViewModel.AddVerificationTest(CreateUncorrectedVolumeTest(verificationViewModel.DriveType));
            rotaryViewModel.AddVerificationTest(CreateCorrectedVolumeTest(verificationViewModel.DriveType, rotaryViewModel.Uncorrected));
            

            CreatePulseOutputTests(device, rotaryViewModel.Uncorrected, rotaryViewModel.Corrected);
            
            var rotaryMeterTest = new RotaryMeterTestViewModel(device.ItemGroup<RotaryMeterItems>());
            rotaryViewModel.AddVerificationTest(rotaryMeterTest);

            testPoint.AddTest(rotaryViewModel);
        }
    }
}