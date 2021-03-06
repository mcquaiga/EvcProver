﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prover.Application.Models.EvcVerifications;

namespace Prover.Application.Interfaces
{
    public interface IVolumeViewModelFactory
    {
        void CreateRelatedTests(EvcVerificationTest evcVerification);
    }

    public interface ICalculateTrueCorrectedFactor
    {
        IObservable<decimal> TrueCorrectedObservable { get; }
        decimal TotalCorrectionFactor { get; }
    }

    public interface IExportVerificationTest
    {
        Task<bool> Export(EvcVerificationTest verificationTest);
        Task<bool> Export(IEnumerable<EvcVerificationTest> verificationTests);
        //Task<bool> ExportFailedTest(string companyNumber);
    }
}