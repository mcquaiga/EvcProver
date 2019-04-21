﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Prover.Core.Models.Instruments
{
    public abstract class BaseVerificationTest : ProverBaseEntity, IHavePercentError, IHaveVerificationTest
    {
        [NotMapped]
        public bool HasPassed => PercentError.HasValue && PercentError < PassTolerance && PercentError > -PassTolerance;

        protected virtual decimal PassTolerance => 1;

        public virtual decimal? PercentError => CalculatePercentError(EvcFactor, ActualFactor);

        public abstract decimal? ActualFactor { get; }

        [NotMapped]
        public abstract decimal? EvcFactor { get; }

        public virtual Guid VerificationTestId { get; set; }

        [Required]
        public virtual VerificationTest VerificationTest { get; set; }

        public override IEvcDevice InstrumentType => VerificationTest.Instrument.InstrumentType;

        protected decimal? CalculatePercentError(decimal? evcValue, decimal? actualValue)
        {
            if (actualValue == 0) return default(decimal?);

            var result = (evcValue - actualValue) / actualValue;
            return result != null 
                ? decimal.Round(result.Value * 100, 2) 
                : default(decimal?);
        }
    }

    public interface IHavePercentError
    {
        decimal? PercentError { get; }
        bool HasPassed { get; }
    }

    public interface IHaveVerificationTest
    {
        Guid VerificationTestId { get; set; }
        VerificationTest VerificationTest { get; set; }
    }
}