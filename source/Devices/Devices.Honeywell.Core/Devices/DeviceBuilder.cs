﻿using System.Collections.Generic;
using Devices.Core.Interfaces;
using Devices.Core.Items;

namespace Devices.Honeywell.Core.Devices
{
    public abstract class DeviceBuilder
    {
        protected readonly HoneywellDeviceType DeviceType;
        protected HoneywellDeviceInstance DeviceInstance;

        protected DeviceBuilder(HoneywellDeviceType deviceType)
        {
            DeviceType = deviceType;
            DeviceInstance = new HoneywellDeviceInstance(DeviceType);
        }

        #region Public Methods

        public DeviceInstance GetDeviceInstance()
        {
            return DeviceInstance;
        }

        protected virtual DeviceBuilder Reset()
        {
            DeviceInstance = new HoneywellDeviceInstance(DeviceType);
            return this;
        }

        //public abstract DeviceBuilder BuildAttributes();

        protected abstract DeviceBuilder SetItemValues(IEnumerable<ItemValue> values);

        #endregion
    }
}