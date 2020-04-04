﻿using System.Threading.Tasks;
using Devices.Core.Interfaces;
using Prover.Application.Services;

namespace Prover.Application.Interfaces
{
    public interface ITestManagerFactory
    {
        Task<ITestManager> StartNew(IVerificationTestService verificationService, DeviceType deviceType);
    }
}