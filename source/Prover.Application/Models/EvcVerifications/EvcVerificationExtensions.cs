using System.Collections.Generic;
using System.Linq;
using Devices.Core.Interfaces;
using Devices.Core.Items;
using Devices.Core.Items.ItemGroups;
using Prover.Application.Models.EvcVerifications.Builders;
using Prover.Application.Models.EvcVerifications.Verifications;
using Prover.Application.Models.EvcVerifications.Verifications.Volume;
using Prover.Calculations;

namespace Prover.Application.Models.EvcVerifications
{
    public static class VerificationTestFactoryEx
    {
        public static VerificationBuilder BuildVerification(this DeviceInstance deviceInstance)
        {
            return VerificationBuilder.CreateNew(deviceInstance);
        }

        public static EvcVerificationTest NewVerification(this DeviceInstance deviceInstance, VerificationTestOptions options = null)
        {
            options = options ?? VerificationTestOptions.Defaults;

            var builder = VerificationBuilder.CreateNew(deviceInstance);

            options.CorrectionTestDefinitions.ForEach(def =>
            {
                builder.AddTestPoint(tp =>
                {
                    if (deviceInstance.HasLivePressure())
                    {
                        var gaugePressure = PressureCalculator.GetGaugePressure(deviceInstance.Items.Pressure.Range, def.PressureGaugePercent);
                        tp.WithPressure(gaugePressure, deviceInstance.Items.Pressure.AtmosphericPressure);
                    }

                    if (deviceInstance.HasLiveTemperature())
                        tp.WithTemperature(def.TemperatureGauge);

                    if (deviceInstance.HasLiveSuper() 
                            || (deviceInstance.HasLivePressure() && deviceInstance.HasLiveTemperature()))
                        tp.WithSuperFactor();

                    if (def.IsVolumeTest)
                        tp.WithVolume();

                    return tp;
                });
            });

            return builder.Build();
        }

    }


    public static class EvcVerificationExtensions
    {
        public static T GetCorrectionTest<T>(this VerificationTestPoint vtp)
                where T : VerificationTestEntity<ItemGroup> => vtp.Tests.OfType<T>().FirstOrDefault();

        public static IEnumerable<VerificationTestEntity<T>> GetCorrectionTests<T>
                (this EvcVerificationTest evcVerification) where T : ItemGroup, ICorrectionFactor
        {
            return evcVerification.Tests.OfType<VerificationTestPoint>()
                                  .Select(v => v.Tests.OfType<VerificationTestEntity<T>>()
                                                .FirstOrDefault());
        }

        public static Dictionary<int, VerificationTestEntity<T>> GetCorrectionTests<T>
                (this ICollection<VerificationEntity> testPoints) where T : ItemGroup, ICorrectionFactor
        {
            return testPoints.OfType<VerificationTestPoint>().ToDictionary(k => k.TestNumber,
                    v => v.Tests.OfType<VerificationTestEntity<T>>()
                          .FirstOrDefault());
        }

        public static IEnumerable<VerificationTestEntity<ItemGroup>> GetCorrectionTests
                (this EvcVerificationTest evcVerification)
        {
            return evcVerification.Tests.OfType<VerificationTestPoint>()
                                  .SelectMany(v => v.Tests.OfType<VerificationTestEntity<ItemGroup>>());
        }

        public static T GetVerificationTest<T>(this VerificationTestPoint vtp)
                where T : VerificationEntity => vtp.GetTest<T>();

        public static VerificationTestPoint GetVolumeTest(this EvcVerificationTest evcVerification)
        {
            return evcVerification.Tests
                                  .Where(t => t.GetType() == typeof(VerificationTestPoint))
                                  .Select(t => (VerificationTestPoint) t)
                                  .FirstOrDefault(ct => ct.GetTest<CorrectedVolumeTestRun>() != null);
        }

        public static bool HasVolume(this VerificationTestPoint point)
            => point.Tests.OfType<CorrectedVolumeTestRun>()
                    .Any();

        public static void UpdateValues
        (this VerificationTestPoint vtp,
                IEnumerable<ItemValue> beforeValues,
                IEnumerable<ItemValue> afterValues,
                decimal appliedInput
        )
        {
        }
    }
}