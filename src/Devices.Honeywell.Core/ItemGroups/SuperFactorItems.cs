using Devices.Core.Interfaces.Items;
using Devices.Honeywell.Core.Attributes;

namespace Devices.Honeywell.Core.ItemGroups
{
    internal class SuperFactorItems : ItemGroupBase, ISuperFactorItems
    {
        [ItemInfo(55)]
        public decimal Co2 { get; protected set; }

        [ItemInfo(54)]
        public decimal N2 { get; protected set; }

        [ItemInfo(53)]
        public decimal SpecGr { get; protected set; }
    }
}