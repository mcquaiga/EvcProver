﻿using System.Reactive.Linq;
using System.Threading.Tasks;
using Devices.Core.Interfaces;
using Devices.Core.Items;
using Prover.Application.Interactions;
using Prover.Application.Interfaces;
using Prover.Application.ViewModels;

namespace Prover.Application.Verifications.CustomActions
{

    public abstract class ItemFileValidation<TValue> : IVerificationAction
    {
        private readonly IDeviceSessionManager _deviceManager;

        protected ItemFileValidation(IDeviceSessionManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public abstract VerificationTestStep RunOnStep { get; }

        public async Task<bool> Execute(EvcVerificationViewModel verification)
        {
            var device = verification.Device;
            var itemValue = GetDeviceValue(device);
            
            var isValid = await CheckIfValid(device, itemValue);
            
            while (!isValid)
            {
                var newValue = await GetUpdatedValue();

                if (newValue == null || (newValue.IsTypeOf(typeof(string)) && string.IsNullOrEmpty(newValue.ToString())))
                    return false;

                itemValue.SetValue(newValue);

                isValid =
                    await CheckIfValid(device, itemValue);

                if (isValid) await UpdateDeviceValue(_deviceManager, device, newValue);
            };

            return true;
        }

        protected virtual async Task<TValue> GetUpdatedValue() => (TValue) await MessageInteractions.GetInput.Handle(InputMessage);

        protected abstract string InputMessage { get; }

        protected abstract ItemValue GetDeviceValue(DeviceInstance device);

        protected abstract Task<bool> CheckIfValid(DeviceInstance device, ItemValue itemValueToValidate);

        protected virtual async Task<bool> UpdateDeviceValue(IDeviceSessionManager deviceManager, DeviceInstance device,
            TValue updateValue)
        {
            var item = GetDeviceValue(device).Metadata;

            var itemValue = await deviceManager.WriteItemValue(item, updateValue.ToString());
            if (itemValue != null) device.SetItemValues(new []{itemValue});

            return itemValue != null;
        }
    }
}