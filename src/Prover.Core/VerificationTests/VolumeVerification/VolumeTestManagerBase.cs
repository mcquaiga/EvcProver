﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using MccDaq;
using NLog;
using Prover.CommProtocol.Common;
using Prover.Core.ExternalDevices.DInOutBoards;
using Prover.Core.Models.Instruments;
using LogManager = NLog.LogManager;

namespace Prover.Core.VerificationTests.VolumeVerification
{
    public abstract class VolumeTestManagerBase
    {
        protected Logger Log;
        protected IEventAggregator EventAggreator;
        protected IDInOutBoard FirstPortAInputBoard;
        protected IDInOutBoard FirstPortBInputBoard;
        protected EvcCommunicationClient InstrumentCommunicator;
        protected bool IsFirstVolumeTest = true;
        protected bool RequestStopTest;
        protected CancellationTokenSource TestCancellationToken;

        protected VolumeTestManagerBase(
            IEventAggregator eventAggregator,
            IPulseInputService pulseInputService)
        {
            Log = LogManager.GetCurrentClassLogger();
            EventAggreator = eventAggregator;

            //DaqService = daqService;
            FirstPortAInputBoard = DInOutBoardFactory.CreateBoard(0, DigitalPortType.FirstPortA, 0);
            FirstPortBInputBoard = DInOutBoardFactory.CreateBoard(0, DigitalPortType.FirstPortB, 1);
        }

        public async Task RunTest(EvcCommunicationClient commClient, VolumeTest volumeTest, IEvcItemReset evcTestItemReset)
        {
            if (!RunningTest)
            {
                try
                {
                    TestCancellationToken = new CancellationTokenSource();
                    RunningTest = true;

                    await Task.Run(async () =>
                    {
                        Log.Info("Volume test started!");

                        await ExecuteSyncTest(commClient, volumeTest);
                        await PreTest(commClient, volumeTest, evcTestItemReset);
                        await ExecutingTest(volumeTest);
                        await PostTest(commClient, volumeTest, evcTestItemReset);

                        Log.Info("Volume test finished!");
                    }, TestCancellationToken.Token);
                    
                }
                finally
                {
                    RunningTest = false;
                }
            }
        }

        public virtual void CancelTest()
        {
            TestCancellationToken.Cancel();
        }

        public bool RunningTest { get; set; }

        protected abstract Task ExecuteSyncTest(EvcCommunicationClient commClient, VolumeTest volumeTest);
        protected abstract Task PreTest(EvcCommunicationClient commClient, VolumeTest volumeTest, IEvcItemReset evcTestItemReset);
        protected abstract Task ExecutingTest(VolumeTest volumeTest);
        protected abstract Task PostTest(EvcCommunicationClient commClient, VolumeTest volumeTest, IEvcItemReset evcPostTestItemReset);

    }

    public interface IPulseInputService
    {

    }
}