//using System.Collections.Generic;
//using System.Linq;
//using Devices.Core.Items;
//using Devices.Core.Items.ItemGroups;
//using Newtonsoft.Json;
//using ItemGroup = Devices.Core.Items.ItemGroups.ItemGroup;

//namespace Devices.Honeywell.Core.Items.ItemGroups
//{
//    internal class FrequencyItems : HoneywellItemGroup, IFrequencyTestItems
//    {
//        public long MainAdjustedVolumeReading { get; set; }

//        public long MainUnadjustVolumeReading { get; set; }

//        public decimal TibAdjustedVolumeReading { get; set; }

//        public long TibUnadjustedVolumeReading { get; }

//        public FrequencyItems(IEnumerable<ItemValue> mainItemValues, IEnumerable<ItemValue> tibItemValues)
//        {
//            var mainItems = mainItemValues.ToList();

//            //TibAdjustedVolumeReading = GetHighResolutionValue(tibItemValues.ToList(), 850, 851);
//            //TibUnadjustedVolumeReading = (long)tibItemValues.GetItem(852).NumericValue;

//            //MainAdjustedVolumeReading = (long)mainItems.GetItem(850).NumericValue;
//            //MainUnadjustVolumeReading = (long)mainItems.GetItem(852).NumericValue;
//        }

//        [JsonConstructor]
//        private FrequencyItems() { }
//    }
//}