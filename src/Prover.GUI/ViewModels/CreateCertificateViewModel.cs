﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using Caliburn.Micro.ReactiveUI;
using Microsoft.Practices.Unity;
using Prover.Core.Models.Certificates;
using Prover.Core.Models.Instruments;
using Prover.Core.Storage;
using Prover.GUI.ViewModels.CertificateReport;
using Prover.GUI.ViewModels.InstrumentsList;
using Prover.GUI.Views.CertificateReport;
using MessageBox = Xceed.Wpf.Toolkit.MessageBox;
using Prover.GUI.Reporting;

namespace Prover.GUI.ViewModels
{
    public class CreateCertificateViewModel: ReactiveScreen
    {
        private IUnityContainer _container;

        public List<string> VerificationTypes
        {
            get
            {
                return new List<string> {"Verification", "Re-Verification"};
            }
        } 

        public CreateCertificateViewModel(IUnityContainer container)
        {
            _container = container;
            InstrumentsListViewModel = new InstrumentsListViewModel(_container);
        }

        public InstrumentsListViewModel InstrumentsListViewModel { get; private set; }

        public Certificate Certificate { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public string VerificationType { get; set; }
        public string TestedBy { get; set; }

        //public string SealExpirationDate
        //{
        //    get
        //    {
        //        var period = 10; //Re-Verification
        //        if (VerificationType == "Verification")
        //        {
        //            period = 12;
        //        }

        //        return DateTime.Now.AddYears(period).ToString("yyyy-MM-dd");
        //    }
        //}

        public void OneWeekFilter()
        {
            InstrumentsListViewModel.GetInstrumentsWithNoCertificateLastWeek();
        }

        public void OneMonthFilter()
        {
            InstrumentsListViewModel.GetInstrumentsWithNoCertificateLastMonth();
        }

        public void ResetFilter()
        {
            InstrumentsListViewModel.GetInstrumentsByCertificateId(null);
        }

        public int InstrumentCount 
        {
            get { return InstrumentsListViewModel.InstrumentItems.Count(x => x.IsSelected);  }
        }

        public void CreateCertificate()
        {
            var instruments = InstrumentsListViewModel.InstrumentItems.Where(x => x.IsSelected).Select(i => i.Instrument).ToList();

            if (instruments.Count() > 8)
            {
                MessageBox.Show("Maximum 8 instruments allowed per certificate.");
                return;
            }

            if (!instruments.Any())
            {
                MessageBox.Show("Please select at least one instrument.");
                return;
            }

            if (VerificationType == null || TestedBy == null)
            {
                MessageBox.Show("Please enter a tested by and verificate type.");
                return;
            }

            

            var cert = Certificate.CreateCertificate(_container, TestedBy, VerificationType, instruments);

            var generator = new CertificateGenerator(cert, _container);
            generator.Generate();

            InstrumentsListViewModel.GetInstrumentsByCertificateId(null);
        }
    }
}