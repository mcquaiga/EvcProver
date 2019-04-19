﻿using Newtonsoft.Json;
using Prover.CommProtocol.Common.Items;
using Prover.Core.Shared.Domain;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Prover.Core.Models.Instruments
{
    public class FrequencyTest : EntityWithId, IHaveVerificationTest, IHavePercentError
    {
        public FrequencyTest() 
        {
            //MainRotorPulseCount = 2600;
            //SenseRotorPulseCount = 260;
            //MechanicalOutputFactor = 70;
        }

        public FrequencyTest(VerificationTest verificationTest)
        {
            VerificationTest = verificationTest;
            VerificationTestId = verificationTest.Id;
        }       

        [Required]
        public virtual VerificationTest VerificationTest { get; set; }
        public Guid VerificationTestId { get; set; }

        public long MainRotorPulseCount { get; set; }
        public long SenseRotorPulseCount { get; set; }
        public long MechanicalOutputFactor { get; set; }

        private string _itemsType;
        public string ItemsType
        {
            get => PreTestItemValues?.GetType().AssemblyQualifiedName;
            set => _itemsType = value;
        }

        [NotMapped]
        public IFrequencyTestItems PreTestItemValues { get; set; }

        private string _preTestItemData;
        public string PreTestItemData
        {
            get => JsonConvert.SerializeObject(PreTestItemValues);
            set => _preTestItemData = value;
        }

        [NotMapped]
        public IFrequencyTestItems PostTestItemValues { get; set; }

        private string _postTestItemData;
        public string PostTestItemData
        {
            get => JsonConvert.SerializeObject(PostTestItemValues);
            set => _postTestItemData = value;
        }

        public decimal? AdjustedVolumePercentError
        {
            get
            {
                if (AdjustedVolume() == 0) return null;

                var result = (TibAdjustedVolume() - AdjustedVolume()) / AdjustedVolume() * 100;
                return result.HasValue ? decimal.Round(result.Value, 2) : default(decimal?);
            }
        }

        public decimal? UnadjustedVolumePercentError
        {
            get
            {
                if (UnadjustedVolume() == 0) return null;

                var result = (EvcUnadjustedVolume() - UnadjustedVolume()) / UnadjustedVolume() * 100;
                return result.HasValue ? decimal.Round(result.Value, 2) : default(decimal?);
            }
        }

        public decimal? CorrectedAdjustedVolumePercentError
        {
            get
            {
                return 100;
            }
        }

        public decimal? PercentError { get; }

        [NotMapped]
        public bool HasPassed => 
            (AdjustedVolumePercentError.HasValue && AdjustedVolumePercentError < 1 && AdjustedVolumePercentError > -1)
        && (UnadjustedVolumePercentError.HasValue && UnadjustedVolumePercentError < 1 && UnadjustedVolumePercentError > -1);

        public decimal? TotalCorrection()
        {
            return VerificationTest.SuperFactorTest.SuperFactorSquared * VerificationTest.PressureTest.ActualFactor
                * VerificationTest.TemperatureTest.ActualFactor;
        }       

        public decimal AdjustedVolume()
        {
            var mainAdjVol = MainRotorPulseCount / VerificationTest.Instrument.Items.GetItem(865).NumericValue;
            var senseAdjVol = SenseRotorPulseCount / VerificationTest.Instrument.Items.GetItem(866).NumericValue;
            return decimal.Round(mainAdjVol - senseAdjVol, 4);
        }      
        
        public decimal AdjustedCorrectedVolume()
        {
            return AdjustedVolume() * TotalCorrection().Value;
        }

        public long RoundedAdjustedVolume()
        {
            var indexRate = (long) VerificationTest.Instrument.Items.GetItem(98).NumericValue;
            var result = (long) (AdjustedVolume() / indexRate);
            return result * indexRate;
        }

        public decimal UnadjustedVolume()
        {
            if (MainRotorPulseCount == 0 || MechanicalOutputFactor == 0) return 0m;

            return decimal.Round((decimal) MainRotorPulseCount / MechanicalOutputFactor, 4);
        }

        public decimal? TibAdjustedVolume()
        {
            var result = (TibAdjustedEndReading - TibAdjustedStartReading) * VerificationTest.Instrument.Items.GetItem(98).NumericValue;

            return result != null ? decimal.Round(result.Value, 4) : default(decimal?);
        }

        public decimal? EvcUnadjustedVolume()
        {
            var result = (UnadjustedEndReading - UnadjustedStartReading) * VerificationTest.Instrument.Items.GetItem(98).NumericValue;           
            return result != null ? decimal.Round(result.Value, 4) : default(decimal?);
        }

        public decimal? TibAdjustedStartReading => PreTestItemValues?.TibAdjustedVolumeReading;
        public decimal? TibAdjustedEndReading => PostTestItemValues?.TibAdjustedVolumeReading;

        public decimal? EvcAdjustedStartReading => PreTestItemValues?.MainAdjustedVolumeReading;
        public decimal? EvcAdjustedEndReading => PostTestItemValues?.MainAdjustedVolumeReading;

        public decimal? UnadjustedStartReading => PreTestItemValues?.MainUnadjustVolumeReading;
        public decimal? UnadjustedEndReading => PostTestItemValues?.MainUnadjustVolumeReading;
        


        public override void OnInitializing()
        {
            base.OnInitializing();

            if (string.IsNullOrEmpty(_itemsType)) return;

            var type = Type.GetType(_itemsType, name => Assembly.Load(name), (assembly, s, arg3) => assembly.GetType(s)); 
            if (type != null && !string.IsNullOrEmpty(_preTestItemData) && !string.IsNullOrEmpty(_postTestItemData))
            {
                PreTestItemValues = (IFrequencyTestItems) JsonConvert.DeserializeObject(_preTestItemData, type);
                PostTestItemValues = (IFrequencyTestItems) JsonConvert.DeserializeObject(_postTestItemData, type);
            }
        }
    }
}
