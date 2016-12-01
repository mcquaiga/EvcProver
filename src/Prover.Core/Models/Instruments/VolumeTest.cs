﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Newtonsoft.Json;
using Prover.CommProtocol.Common;
using Prover.CommProtocol.Common.Items;
using Prover.CommProtocol.MiHoneywell.Items;
using Prover.Core.DriveTypes;
using Prover.Core.Extensions;

namespace Prover.Core.Models.Instruments
{
    public sealed class VolumeTest : BaseVerificationTest
    {
        private string _testInstrumentData;

        public VolumeTest()
        {
        }

        public VolumeTest(VerificationTest verificationTest)
        {
            Items = verificationTest.Instrument.Items.Where(i => i.Metadata.IsVolumeTest == true);
            VerificationTest = verificationTest;
            VerificationTestId = VerificationTest.Id;

            CreateDriveType();
        }

        public int PulseACount { get; set; }
        public int PulseBCount { get; set; }
        public decimal AppliedInput { get; set; }

        public IDriveType DriveType { get; set; }

        public string TestInstrumentData
        {
            get { return AfterTestItems.Serialize(); }
            set { _testInstrumentData = value; }
        }

        public Instrument Instrument => VerificationTest.Instrument;

        public Guid VerificationTestId { get; set; }

        [Required]
        public VerificationTest VerificationTest { get; set; }

        [NotMapped]
        public IEnumerable<ItemValue> AfterTestItems { get; set; }

        [NotMapped]
        public decimal? UnCorrectedPercentError
        {
            get
            {
                if ((EvcUncorrected != null) && (TrueUncorrected != 0) && TrueUncorrected.HasValue)
                {
                    var o = (EvcUncorrected - TrueUncorrected)/TrueUncorrected;
                    if (o != null)
                        return Math.Round((decimal) o*100, 2);
                }

                return null;
            }
        }

        [NotMapped]
        public decimal? TrueUncorrected
        {
            get { return DriveType?.UnCorrectedInputVolume(AppliedInput); }
        }

        [NotMapped]
        public decimal? CorrectedPercentError
        {
            get
            {
                if ((EvcCorrected != null) && (TrueCorrected != 0) && (TrueCorrected != null))
                {
                    var o = (EvcCorrected - TrueCorrected)/TrueCorrected*100;
                    if (o != null)
                        return Math.Round((decimal) o, 2);
                }

                return null;
            }
        }

        [NotMapped]
        public bool CorrectedHasPassed => CorrectedPercentError?.IsBetween(Global.COR_ERROR_THRESHOLD) ?? false;

        [NotMapped]
        public bool UnCorrectedHasPassed => UnCorrectedPercentError?.IsBetween(Global.UNCOR_ERROR_THRESHOLD) ?? false;

        [NotMapped]
        public bool HasPassed => CorrectedHasPassed && UnCorrectedHasPassed && DriveType.HasPassed;

        public override decimal? PercentError { get; }
        public override decimal? ActualFactor { get; }

        [NotMapped]
        public int UncPulseCount
        {
            get
            {
                if (Instrument.PulseASelect() == "UncVol")
                    return PulseACount;

                return PulseBCount;
            }
        }

        [NotMapped]
        public int CorPulseCount
        {
            get
            {
                if (Instrument.PulseASelect() == "CorVol")
                    return PulseACount;

                return PulseBCount;
            }
        }

        [NotMapped]
        public decimal? TrueCorrected
        {
            get
            {
                if (VerificationTest == null) return null;

                if ((VerificationTest.Instrument.CompositionType == CorrectorType.T) &&
                    (VerificationTest.TemperatureTest != null))
                    return VerificationTest.TemperatureTest.ActualFactor*
                           DriveType.UnCorrectedInputVolume(AppliedInput);

                if ((VerificationTest.Instrument.CompositionType == CorrectorType.P) &&
                    (VerificationTest.PressureTest != null))
                    return VerificationTest.PressureTest.ActualFactor*
                           DriveType.UnCorrectedInputVolume(AppliedInput);

                if (VerificationTest.Instrument.CompositionType == CorrectorType.PTZ)
                    return VerificationTest.PressureTest?.ActualFactor*VerificationTest.TemperatureTest?.ActualFactor*
                           VerificationTest.SuperFactorTest.SuperFactorSquared*
                           DriveType.UnCorrectedInputVolume(AppliedInput);

                return null;
            }
        }

        [NotMapped]
        public decimal? EvcCorrected => VerificationTest.Instrument.EvcCorrected(Items, AfterTestItems);

        [NotMapped]
        public decimal? EvcUncorrected => VerificationTest.Instrument.EvcUncorrected(Items, AfterTestItems);

        public string DriveTypeDiscriminator { get; set; }

        [NotMapped]
        public override InstrumentType InstrumentType => Instrument.InstrumentType;

        private void CreateDriveType()
        {
            if (string.IsNullOrEmpty(DriveTypeDiscriminator))
                DriveTypeDiscriminator = Instrument.Items?.GetItem(98)?.Description.ToLower() == "rotary" ? "Rotary" : "Mechanical";
            
            if ((DriveType == null) && (DriveTypeDiscriminator != null) && (VerificationTest != null))
                switch (DriveTypeDiscriminator)
                {
                    case "Rotary":
                        DriveType = new RotaryDrive(this.Instrument);
                        break;
                    case "Mechanical":
                        DriveType = new MechanicalDrive(Instrument);
                        AppliedInput = DriveType.MaxUncorrectedPulses();
                        break;
                    default:
                        throw new NotSupportedException($"Drive type {DriveTypeDiscriminator} is not supported.");
                }
            else
            {
                throw new ArgumentNullException($"Could not determine drive type {DriveTypeDiscriminator}.");
            }
        }

        public override void OnInitializing()
        {
            base.OnInitializing();

            CreateDriveType();

            if (!string.IsNullOrEmpty(_testInstrumentData))
            {
                var afterItemValues = JsonConvert.DeserializeObject<Dictionary<int, string>>(_testInstrumentData);
                AfterTestItems = ItemHelpers.LoadItems(Instrument.InstrumentType, afterItemValues);
            }
        }
    }
}