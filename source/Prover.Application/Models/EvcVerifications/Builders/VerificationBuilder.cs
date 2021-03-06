﻿using Devices.Core.Interfaces;
using Devices.Core.Items;
using System;
using System.Collections.Generic;

namespace Prover.Application.Models.EvcVerifications.Builders
{
    public class VerificationBuilder
    {
        private readonly DeviceInstance _device;
        private EvcVerificationTest _instance;
        private readonly VolumeInputTestBuilder _volumeBuilder;
        //private TestPointBuilder _testPointBuilder;

        private VerificationBuilder(DeviceInstance device)
        {
            _device = device;

            //_testPointBuilder = TestPointBuilder.Create(_device, _volumeBuilder);

            _instance = new EvcVerificationTest(device);
        }

        #region Public Methods

        public static VerificationBuilder CreateNew(DeviceInstance device)
        {
            return new VerificationBuilder(device);
            //.CreateVerificationTests(_instance);
        }

        public EvcVerificationTest Build()
        {

            var instance = _instance;
            _instance = null;
            return instance;
        }

        public VerificationBuilder AddTestPoint(Func<TestPointBuilder, TestPointBuilder> testDecoratorFunc, ICollection<ItemValue> deviceValues = null)
        {
            var correctionTests = testDecoratorFunc.Invoke(new TestPointBuilder(_device, _instance.Tests.Count, deviceValues ?? new List<ItemValue>()));
            _instance.AddTest(correctionTests.Build());
            return this;
        }

        #endregion

    }


}