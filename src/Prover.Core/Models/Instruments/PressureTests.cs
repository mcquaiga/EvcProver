﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prover.Core.Models.Instruments
{


    public class PressureTest : InstrumentTable
    {
        private int GAS_PRESSURE = 8;
        private int PRESSURE_FACTOR = 44;
        private int UNSQR_FACTOR = 47;

        //const decimal LOW_PERCENT = 20;
        //const decimal MID_PERCENT = 50;
        //const decimal HIGH_PERCENT = 80;

        public enum PressureLevel
        {
            Low = 20,
            Medium = 50,
            High = 80
        }
        
        public PressureTest(Pressure pressure, PressureLevel level) : 
            base(pressure.Instrument.Items.FindItems(x => x.IsPressureTest == true))
        {
            Pressure = pressure;
            PressureId = pressure.Id;
            TestLevel = level;
            IsVolumeTestPressure = TestLevel == PressureLevel.Low;
            SetDefaultGauge(TestLevel);
        }

        public Guid PressureId { get; set; }
        public Pressure Pressure { get; set; }

        public PressureLevel TestLevel { get; set; }
        public bool IsVolumeTestPressure { get; private set; }

        public decimal? GasGauge { get; set; }
        public decimal? AtmosphericGauge { get; set; }
        public decimal? PercentError
        {
            get
            {
                if (EvcFactor == null) return null;
                return Math.Round((decimal)((EvcFactor - ActualFactor) / ActualFactor) * 100, 2);
            }
        }

        [NotMapped]
        public decimal? ActualFactor
        {
            get
            {
                if (Pressure.EvcBase == 0) return 0;

                switch (Pressure.TransducerType)
                {
                    case TransducerType.Gauge:
                        return (GasGauge + Pressure.EvcAtmospheric) / Pressure.EvcBase;

                    case TransducerType.Absolute:
                        return (GasGauge + AtmosphericGauge) / Pressure.EvcBase;

                    default:
                        return 0;
                }
            }
        }

        /*
        ** EVC PRopertes
        */
        [NotMapped]
        public decimal? EvcGasPressure
        {
            get
            {
                return Items.GetItem(GAS_PRESSURE).GetNumericValue();
            }
        }

        [NotMapped]
        public decimal? EvcFactor
        {
            get
            {
                return Items.GetItem(PRESSURE_FACTOR).GetNumericValue();
            }
        }

        [NotMapped]
        public decimal? EvcUnsqrFactor
        {
            get
            {
                return Items.GetItem(UNSQR_FACTOR).GetNumericValue();
            }
        }

        [NotMapped]
        public bool HasPassed
        {
            get { return (PercentError < 1 && PercentError > -1); }
        }

        public void SetDefaultGauge(PressureLevel level)
        {
            GasGauge = ((int)level / 100) * Pressure.EvcPressureRange;
        }
    }
}
