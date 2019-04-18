﻿using System.Reactive.Subjects;
using System.Threading.Tasks;
using Prover.CommProtocol.Common;
using Prover.Core.Models.Instruments;

namespace Prover.Core.VerificationTests.TestActions
{
    public interface IPostTestAction
    {
        Task Execute(EvcCommunicationClient commClient, Instrument instrument, Subject<string> statusUpdates = null);
    }

    public interface IPreTestValidation
    {
        Task Validate(EvcCommunicationClient commClient, Instrument instrument, Subject<string> statusUpdates = null);
    }
}