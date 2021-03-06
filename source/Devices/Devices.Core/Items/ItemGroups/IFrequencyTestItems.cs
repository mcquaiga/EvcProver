namespace Devices.Core.Items.ItemGroups
{
    public interface IFrequencyTestItems
    {
        #region Public Properties

        long MainAdjustedVolumeReading { get; set; }

        long MainUnadjustVolumeReading { get; set; }

        decimal TibAdjustedVolumeReading { get; set; }

        long TibUnadjustedVolumeReading { get; }

        #endregion Public Properties
    }

    public interface IHaveFrequency
    {
        #region Properties

        IFrequencyTestItems FrequencyItems { get; set; }

        #endregion
    }
}