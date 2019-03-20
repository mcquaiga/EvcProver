﻿using System.Collections.Generic;
using Prover.CommProtocol.Common.Items;
using System;
using System.Reactive.Subjects;
using Prover.CommProtocol.Common.IO;

namespace Prover.CommProtocol.Common
{
    public class InstrumentType
    {
        public int AccessCode { get; set; }
        public string Name { get; set; }
        public int Id { get; set; }
        public string ItemFilePath { get; set; }

        public string CommClientType { get; set; }

        public Func<ICommPort, ISubject<string>, EvcCommunicationClient> ClientFactory { get; set; }

        public bool? CanUseIrDaPort { get; set; }

        public int? MaxBaudRate { get; set; }

        public IEnumerable<ItemMetadata> ItemsMetadata { get; set; }
    }
}