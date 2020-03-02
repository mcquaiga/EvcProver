﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Prover.Core.Models.Clients;
using Prover.Core.Models.Instruments;
using Prover.Core.Shared.Domain;

namespace Prover.Core.Models.Certificates
{
    public class Certificate : EntityWithId
    {
        public DateTime CreatedDateTime { get; set; }

        public string VerificationType { get; set; }
        
        [Index]
        public string TestedBy { get; set; }
        
        public string Apparatus { get; set; }

        [Index]
        public Guid? ClientId { get; set; }
        public virtual Client Client { get; set; }

        public long Number { get; set; }

        public virtual ICollection<Instrument> Instruments { get; set; } = new List<Instrument>();

        [NotMapped]
        public string SealExpirationDate
        {
            get
            {
                var period = 10; //Re-Verification
                if (VerificationType == "Verification")
                    period = 12;

                return CreatedDateTime.AddYears(period).ToString("yyyy-MM-dd");
            }
        }

        [NotMapped]
        public int InstrumentCount => Instruments.Count();
    }
}