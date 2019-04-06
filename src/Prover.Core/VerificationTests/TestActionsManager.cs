﻿namespace Prover.Core.VerificationTests
{
    using Prover.CommProtocol.Common;
    using Prover.Core.Models.Instruments;
    using Prover.Core.VerificationTests.TestActions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="TestActionsManager" />
    /// </summary>
    public class TestActionsManager : ITestActionsManager
    {
        public enum TestActionStep
        {
            PreVolume,
            PostVolume,
            PreVerification,
            PostVerification
        }

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TestActionsManager"/> class.
        /// </summary>
        /// <param name="preVolumeTestActions">The preVolumeTestActions<see cref="IEnumerable{IPreVolumeTestAction}"/></param>
        /// <param name="postVolumeTestActions">The postVolumeTestActions<see cref="IEnumerable{IPostVolumeTestAction}"/></param>
        /// <param name="preVerificationActions">The preVerificationActions<see cref="IEnumerable{IPreVerificationAction}"/></param>
        /// <param name="postVerificationActions">The postVerificationActions<see cref="IEnumerable{IPostVerificationAction}"/></param>
        public TestActionsManager(IEnumerable<IPreVolumeTestAction> preVolumeTestActions,
            IEnumerable<IPostVolumeTestAction> postVolumeTestActions,
            IEnumerable<IPreVerificationAction> preVerificationActions,
            IEnumerable<IPostVerificationAction> postVerificationActions)
        {
            PreVolumeTestActions = preVolumeTestActions;
            PostVolumeTestActions = postVolumeTestActions;
            PreVerificationActions = preVerificationActions;
            PostVerificationActions = postVerificationActions;
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets the PostVerificationActions
        /// </summary>
        public IEnumerable<IPostVerificationAction> PostVerificationActions { get; }

        /// <summary>
        /// Gets the PostVolumeTestActions
        /// </summary>
        public IEnumerable<IPostVolumeTestAction> PostVolumeTestActions { get; }

        /// <summary>
        /// Gets the PreVerificationActions
        /// </summary>
        public IEnumerable<IPreVerificationAction> PreVerificationActions { get; }

        /// <summary>
        /// Gets the PreVolumeTestActions
        /// </summary>
        public IEnumerable<IPreVolumeTestAction> PreVolumeTestActions { get; }

        public List<Tuple<TestActionStep, Func<EvcCommunicationClient, Instrument, Task>>> TestActions { get; } 
            = new List<Tuple<TestActionStep, Func<EvcCommunicationClient, Instrument, Task>>>();

        #endregion

        #region Methods

        /// <summary>
        /// The RunPostVerificationActions
        /// </summary>
        /// <param name="commClient">The commClient<see cref="EvcCommunicationClient"/></param>
        /// <param name="instrument">The instrument<see cref="Instrument"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public async Task RunVerificationCompleteActions(EvcCommunicationClient commClient, Instrument instrument)
        {
            await ExecuteActions(PostVerificationActions, commClient, instrument);
            await ExecuteActions(TestActionStep.PostVerification, commClient, instrument);
        }

        /// <summary>
        /// The ExecuteTestInitializationActions
        /// </summary>
        /// <param name="commClient">The commClient<see cref="EvcCommunicationClient"/></param>
        /// <param name="instrument">The instrument<see cref="Instrument"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public async Task RunVerificationInitActions(EvcCommunicationClient commClient, Instrument instrument)
        {
            await ExecuteActions(PreVerificationActions, commClient, instrument);
            await ExecuteActions(TestActionStep.PreVerification, commClient, instrument);
        }

        /// <summary>
        /// The RunPostVolumeActions
        /// </summary>
        /// <param name="commClient">The commClient<see cref="EvcCommunicationClient"/></param>
        /// <param name="instrument">The instrument<see cref="Instrument"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public async Task RunVolumeTestCompleteActions(EvcCommunicationClient commClient, Instrument instrument)
        {
            await ExecuteActions(PostVolumeTestActions, commClient, instrument);
            await ExecuteActions(TestActionStep.PostVolume, commClient, instrument);
        }

        /// <summary>
        /// The RunPreVolumeActions
        /// </summary>
        /// <param name="commClient">The commClient<see cref="EvcCommunicationClient"/></param>
        /// <param name="instrument">The instrument<see cref="Instrument"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public async Task RunVolumeTestInitActions(EvcCommunicationClient commClient, Instrument instrument)
        {
            await ExecuteActions(PreVolumeTestActions, commClient, instrument);
            await ExecuteActions(TestActionStep.PreVolume, commClient, instrument);
        }

        public void RegisterAction(TestActionStep actionStep, Func<EvcCommunicationClient, Instrument, Task> testAction)
        {
            TestActions.Add(new Tuple<TestActionStep, Func<EvcCommunicationClient, Instrument, Task>>(actionStep, testAction));
        }

        public void UnregisterActions(TestActionStep actionStep, Func<EvcCommunicationClient, Instrument, Task> testAction)
        {
            TestActions.RemoveAll(x => x.Item1 == actionStep && x.Item2 == testAction);
        }

        /// <summary>
        /// The ExecuteActions
        /// </summary>
        /// <param name="actions">The actions<see cref="IEnumerable{IInstrumentAction}"/></param>
        /// <param name="commClient">The commClient<see cref="EvcCommunicationClient"/></param>
        /// <param name="instrument">The instrument<see cref="Instrument"/></param>
        /// <returns>The <see cref="Task"/></returns>
        private async Task ExecuteActions(IEnumerable<IInstrumentAction> actions, EvcCommunicationClient commClient, Instrument instrument)
        {
            if (!commClient.IsConnected)
                await commClient.Connect(new CancellationToken());

            foreach (var testAction in actions)
            {
                await testAction.Execute(commClient, instrument);
            }
        }

        public async Task ExecuteActions(TestActionStep testStep, EvcCommunicationClient commClient, Instrument instrument)
        {
             if (!commClient.IsConnected)
                await commClient.Connect();

            foreach (var a in TestActions.Where(x => x.Item1 == testStep))
            {
                await a.Item2(commClient, instrument);
            }
        }

        #endregion
    }
}
