﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Prover.Storage.SampleData
{
    public static class ItemFiles
    {
        public static Dictionary<string, string> MiniMaxItemFile 
            => _lazy.Value.Items;

        private static readonly Lazy<ItemAndTestFile> _lazy = new Lazy<ItemAndTestFile>(
            () => DeserializeItemFile(File.ReadAllText("SampleData\\MiniMax.json"))
        );

        public static Dictionary<string, string> PressureTest(int testNumber) => _lazy.Value.PressureTests.ElementAt(testNumber);

        public static Dictionary<string, string> TempLowItems => _lazy.Value.TemperatureTests.ElementAt(0);
        public static Dictionary<string, string> TempMidItems => _lazy.Value.TemperatureTests.ElementAt(1);
        public static Dictionary<string, string> TempHighItems => _lazy.Value.TemperatureTests.ElementAt(2);

        public static ItemAndTestFile DeserializeItemFile(string jsonString)
        {
            return JsonConvert.DeserializeObject<ItemAndTestFile>(jsonString);
        }

        public static ItemAndTestFile LoadFromFile(string filePath)
        {
            return DeserializeItemFile(File.ReadAllText(filePath));
        }

        public class ItemAndTestFile
        {
            [JsonConstructor]
            public ItemAndTestFile(Dictionary<string, string> items,
                IEnumerable<Dictionary<string, string>> pressureTests,
                IEnumerable<Dictionary<string, string>> temperatureTests)
            {
                Items = items;
                PressureTests = pressureTests.ToList();
                TemperatureTests = temperatureTests.ToList();
            }

            public Dictionary<string, string> Items { get; set; }

            public ICollection<Dictionary<string, string>> PressureTests { get; set; }
            public ICollection<Dictionary<string, string>> TemperatureTests { get; set; }
        }
    }
}
