﻿using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Prover.GUI.Common;
using Prover.GUI.Common.Screens.MainMenu;
using Prover.GUI.Modules.Certificates.Screens;
using Prover.Modules.Certificates.Screens.Certificates;
using Action = System.Action;

namespace Prover.Modules.Certificates
{  
    public class CertificatesApp : AppMainMenuBase
    {
        public CertificatesApp(ScreenManager screenManager, IEventAggregator eventAggregator)
            : base(screenManager, eventAggregator)
        {
        }

        public override ImageSource IconSource
            => new BitmapImage(new Uri("pack://application:,,,/Prover.Modules.Certificates;component/Resources/certificate.png"));

        public override string AppTitle => "Create Certificates";
        public override Action ClickAction => async () => await ScreenManager.ChangeScreen<CertificateViewModel>();
    }
}