﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Microsoft.Practices.Unity;
using Prover.CommProtocol.Common;
using Prover.Core.Collections;
using Prover.Core.Events;
using Prover.Core.Models.Instruments;

namespace Prover.Core.VerificationTests
{
    public class ReadingStabilizer
    {
        private bool _cancelStabilize = false;
        private bool _isLiveReading;

        public ReadingStabilizer(IUnityContainer container, Instrument instrument)
        {
            Container = container;
            Instrument = instrument;
        }

        public IUnityContainer Container { get; set; }

        public Instrument Instrument { get; set; }

        public async Task WaitForReadingsToStabilizeAsync(EvcCommunicationClient commClient, int level)
        {
            var liveReadItems = GetLiveReadItemNumbers(level);

            await commClient.Connect();
            
            do
            {
                _isLiveReading = true;
                foreach (var item in liveReadItems)
                {
                    var liveValue = await commClient.LiveReadItemValue(item.Key);
                    item.Value.Add(liveValue.NumericValue);
                    Container.Resolve<IEventAggregator>()
                        .PublishOnBackgroundThread(new LiveReadEvent(item.Key, liveValue.NumericValue));
                }
            } while (liveReadItems.Any(x => !x.Value.IsStable) && !_cancelStabilize);

            _isLiveReading = false;
            _cancelStabilize = false;

            await commClient.Disconnect();
        }

        public void CancelLiveReading()
        {
            if (_isLiveReading)
            {
                _cancelStabilize = true;
            }
        }

        private Dictionary<int, AveragedReadingStabilizer> GetLiveReadItemNumbers(int level)
        {
            var liveReadItems = new Dictionary<int, AveragedReadingStabilizer>();
            if (Instrument.CompositionType == CorrectorType.PTZ)
            {
                liveReadItems.Add(8, new AveragedReadingStabilizer(Instrument.GetGaugePressure(level)));
                liveReadItems.Add(26, new AveragedReadingStabilizer(Instrument.GetGaugeTemp(level)));
            }

            if (Instrument.CompositionType == CorrectorType.T)
                liveReadItems.Add(26, new AveragedReadingStabilizer(Instrument.GetGaugeTemp(level)));

            if (Instrument.CompositionType == CorrectorType.P)
                liveReadItems.Add(8, new AveragedReadingStabilizer(Instrument.GetGaugePressure(level)));
            return liveReadItems;
        }
    }

    public class AveragedReadingStabilizer
    {
        private const decimal AverageThreshold = 1.0m;
        private const int FixedQueueSize = 20;
        private FixedSizedQueue<decimal> _valueQueue;

        public AveragedReadingStabilizer(decimal gaugeValue)
        {
            this.GaugeValue = gaugeValue;
            _valueQueue = new FixedSizedQueue<decimal>(FixedQueueSize);
        }

        public void Add(decimal value)
        {
            _valueQueue.Enqueue(value);
        }

        public void Clear()
        {
            _valueQueue = null;
            _valueQueue = new FixedSizedQueue<decimal>(FixedQueueSize);
        }

        public decimal GaugeValue { get; private set; }

        public bool IsStable
        {
            get
            {
                if (_valueQueue.Count == FixedQueueSize)
                {
                    var average = _valueQueue.Sum() / FixedQueueSize;
                    var difference = Math.Abs(GaugeValue - average);

                    if (difference <= AverageThreshold)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
