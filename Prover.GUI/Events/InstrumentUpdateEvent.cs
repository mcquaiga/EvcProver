﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prover.Core.Communication;
using Prover.Core.Models.Instruments;

namespace Prover.GUI.Events
{
    public class InstrumentUpdateEvent
    {
        public InstrumentManager InstrumentManager { get; set; }

        public InstrumentUpdateEvent(InstrumentManager instrumentManager)
        {
            InstrumentManager = instrumentManager;
        } 
    }
}
