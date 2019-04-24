using Devices.Core.Items;
using System.Collections.Generic;

namespace Devices.Core.Interfaces
{
    public interface IEvcDeviceType
    {
        #region Properties

        bool? CanUseIrDaPort { get; set; }

        IEnumerable<ItemMetadata> Definitions { get; set; }

        bool IsHidden { get; set; }

        string ItemFilePath { get; set; }

        int? MaxBaudRate { get; set; }

        string Name { get; set; }

        #endregion
    }
}