﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Devices.Core.Items
{
    public interface IVolumeUnits
    {
        VolumeUnit Units { get; set; }
    }

    public class VolumeUnit
    {
        public decimal Multiplier { get; set; }
        public string Description { get; set; }
        public bool IsMetric { get; set; }
    }
}