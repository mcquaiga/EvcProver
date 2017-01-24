﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prover.Core.Models.Instruments;

namespace Prover.Core.Models.Clients
{
    public class Client : BaseEntity
    {
        public Client()
        {
            CreatedDateTime = DateTime.UtcNow;                
        }

        public string Name { get; set; }
        public string Address { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime? ArchivedDateTime { get; set; }

        public virtual List<ClientItems> Items { get; set; } = new List<ClientItems>();
    }
}
