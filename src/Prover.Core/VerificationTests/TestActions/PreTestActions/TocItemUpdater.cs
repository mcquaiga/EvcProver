﻿namespace Prover.Core.VerificationTests.TestActions.PreTestActions
{
    using Prover.CommProtocol.Common;
    using Prover.CommProtocol.MiHoneywell;
    using Prover.Core.Models.Instruments;
    using System.Collections.Generic;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="TocItemUpdater" />
    /// </summary>
    public class TocItemUpdaterAction : ItemUpdaterAction
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TocItemUpdater"/> class.
        /// </summary>
        /// <param name="itemsForUpdate">The itemsForUpdate<see cref="Dictionary{int, string}"/></param>
        public TocItemUpdaterAction(Dictionary<int, string> itemsForUpdate) : base(itemsForUpdate)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// The Execute
        /// </summary>
        /// <param name="commClient">The commClient<see cref="EvcCommunicationClient"/></param>
        /// <param name="instrument">The instrument<see cref="Instrument"/></param>
        /// <param name="statusUpdates">The statusUpdates<see cref="Subject{string}"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public override async Task Execute(EvcCommunicationClient commClient, Instrument instrument, Subject<string> statusUpdates = null)
        {
            if (instrument.InstrumentType == Instruments.Toc)
            {
                await base.Execute(commClient, instrument, statusUpdates);
                await commClient.Disconnect();
                Thread.Sleep(1000);

                commClient.InstrumentType = Instruments.TurboMonitor;
                await commClient.Connect();
                await base.Execute(commClient, instrument, statusUpdates);
                await commClient.Disconnect();

                commClient.InstrumentType = Instruments.Toc;
                await commClient.Connect();
            }                
        }

        #endregion
    }
}
