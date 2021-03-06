﻿using System;
using Prover.Shared.Domain;

namespace Prover.Application.Settings
{
    /// <summary>
    /// Defines the <see cref="LocalSettings" />
    /// </summary>
    public class LocalSettings : IKeyValueEntity
    {

        #region Properties

        public bool AutoSave { get; set; }

        /// <summary>
        /// Gets or sets the InstrumentBaudRate
        /// </summary>
        public int InstrumentBaudRate { get; set; } = 0;

        /// <summary>
        /// Gets or sets the InstrumentCommPort
        /// </summary>
        public string InstrumentCommPort { get; set; } = "";

        /// <summary>
        /// Gets or sets a value indicating whether InstrumentUseIrDaPort
        /// </summary>
        public bool InstrumentUseIrDaPort { get; set; } = false;

        /// <summary>
        /// Gets or sets the LastClientSelected
        /// </summary>
        public string LastClientSelected { get; set; } = "";

        /// <summary>
        /// Gets or sets the LastInstrumentTypeUsed
        /// </summary>
        public Guid LastDeviceTypeUsed { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets the TachCommPort
        /// </summary>
        public string TachCommPort { get; set; } = "";

        /// <summary>
        /// Gets or sets a value indicating whether TachIsNotUsed
        /// </summary>
        public bool TachIsNotUsed { get; set; } = false;

        /// <summary>
        /// Gets or sets the WindowHeight
        /// </summary>
        public double WindowHeight { get; set; } = 800;

        /// <summary>
        /// Gets or sets the WindowState
        /// </summary>
        public string WindowState { get; set; } = "Normal";


        public string VerificationFilePath { get; set; } 

        /// <summary>
        /// Gets or sets the WindowWidth
        /// </summary>
        public double WindowWidth { get; set; } = 800;

        #endregion

        public LocalSettings()
        {
        }

        public string Key => "LocalSettings";

        //public event PropertyChangedEventHandler PropertyChanged;

        //[NotifyPropertyChangedInvocator]
        //protected virtual void OnPropertyChanged(string propertyName, object before, object after)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}
    }

}
