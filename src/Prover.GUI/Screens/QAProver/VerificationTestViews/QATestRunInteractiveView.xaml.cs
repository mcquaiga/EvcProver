﻿using System.Windows;
using System.Windows.Controls;

namespace Prover.GUI.Screens.QAProver.VerificationTestViews
{
    public partial class QaTestRunInteractiveView : UserControl
    {
        public QaTestRunInteractiveView()
        {
            InitializeComponent();
            Loaded += ControlLoaded;
        }

        private void ControlLoaded(object sender, RoutedEventArgs e)
        {
            var my = sender.ToString();
        }
    }
}