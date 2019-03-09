﻿namespace Prover.Core.Settings
{
    using Caliburn.Micro;
    using Prover.Core.Events;
    using Prover.Core.Storage;
    using System;
    using System.IO;
    using System.Threading.Tasks;

    #region Interfaces

    /// <summary>
    /// Defines the <see cref="ISettingsService" />
    /// </summary>
    public interface ISettingsService
    {
        #region Properties

        /// <summary>
        /// Gets the Local
        /// </summary>
        LocalSettings Local { get; }

        /// <summary>
        /// Gets the Shared
        /// </summary>
        SharedSettings Shared { get; }

        #endregion

        #region Methods

        /// <summary>
        /// The RefreshSettings
        /// </summary>
        Task RefreshSettings();

        /// <summary>
        /// The SaveSettings
        /// </summary>
        /// <returns>The <see cref="Task"/></returns>
        Task SaveSettings();

        #endregion
    }

    #endregion

    /// <summary>
    /// Defines the <see cref="SettingsService" />
    /// </summary>
    public class SettingsService : ISettingsService
    {
        #region Constants

        /// <summary>
        /// Defines the SettingsFileName
        /// </summary>
        private const string SettingsFileName = "settings.conf";

        #endregion

        #region Fields

        /// <summary>
        /// Defines the AppDirectory
        /// </summary>
        private static readonly string AppDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EvcProver");

        /// <summary>
        /// Defines the SettingsPath
        /// </summary>
        private static readonly string SettingsPath = Path.Combine(AppDirectory, SettingsFileName);

        /// <summary>
        /// Defines the _keyValueStore
        /// </summary>
        private readonly KeyValueStore _keyValueStore;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsService"/> class.
        /// </summary>
        /// <param name="keyValueStore">The keyValueStore<see cref="KeyValueStore"/></param>
        public SettingsService(KeyValueStore keyValueStore, IEventAggregator eventAggregator)
        {
            _keyValueStore = keyValueStore;
            EventAggregator = eventAggregator;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Local
        /// </summary>
        public LocalSettings Local { get; private set; }

        /// <summary>
        /// Gets the Shared
        /// </summary>
        public SharedSettings Shared { get; private set; }
        public IEventAggregator EventAggregator { get; }

        #endregion

        #region Methods

        /// <summary>
        /// The RefreshSettings
        /// </summary>
        public async Task RefreshSettings()
        {            
            Local = await LocalSettings.LoadLocalSettings(SettingsPath);
            Shared = SharedSettings.LoadSharedSettings(_keyValueStore);            
        }

        /// <summary>
        /// The SaveSettings
        /// </summary>
        /// <returns>The <see cref="Task"/></returns>
        public async Task SaveSettings()
        {
            await Shared.SaveSharedSettings(_keyValueStore).ConfigureAwait(false);
            await Local.SaveLocalSettingsAsync(SettingsPath).ConfigureAwait(false);

            await EventAggregator.PublishOnUIThreadAsync(new SettingsChangeEvent());
        }

        #endregion
    }
}
