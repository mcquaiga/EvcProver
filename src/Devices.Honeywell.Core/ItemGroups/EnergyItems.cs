﻿using Devices.Core.Interfaces.Items;
using Devices.Honeywell.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Devices.Honeywell.Core.ItemGroups
{
    internal class EnergyItems : ItemGroupBase, IEnergyItems
    {
        [ItemInfo(142)]
        public decimal EnergyGasValue { get; protected set; }

        [ItemInfo(140)]
        public decimal EnergyReading { get; protected set; }

        [ItemInfo(141)]
        public string EnergyUnits { get; protected set; }
    }
}