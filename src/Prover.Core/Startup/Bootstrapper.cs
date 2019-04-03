using System.Data.Entity;
using Autofac;
using Prover.CommProtocol.Common;
using Prover.CommProtocol.Common.IO;
using Prover.CommProtocol.MiHoneywell;
using Prover.Core.Communication;
using Prover.Core.ExternalDevices.DInOutBoards;
using Prover.Core.Migrations;
using Prover.Core.Models.Certificates;
using Prover.Core.Models.Instruments;
using Prover.Core.Settings;
using Prover.Core.Storage;
using Prover.Core.VerificationTests;
using Prover.Core.VerificationTests.TestActions;
using Prover.Core.VerificationTests.TestActions.PreTestActions;
using Prover.Core.VerificationTests.VolumeVerification;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Prover.Core.Startup
{
    public class CoreBootstrapper
    {
        public CoreBootstrapper()
        {
            //Database registrations
            Builder.RegisterInstance(new ProverContext());
            Builder.RegisterType<InstrumentStore>().As<IInstrumentStore<Instrument>>();
            Builder.RegisterType<CertificateStore>().As<ICertificateStore<Certificate>>();
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<ProverContext, Configuration>());

            ////QA Test Runs
            Builder.Register(c => DInOutBoardFactory.CreateBoard(0, 0, 1)).Named<IDInOutBoard>("TachDaqBoard");
            Builder.Register(c => new TachometerService(SettingsManager.SettingsInstance.TachCommPort, c.ResolveNamed<IDInOutBoard>("TachDaqBoard")))
                .As<TachometerService>();

            Builder.RegisterType<ManualVolumeTestManager>().As<ManualVolumeTestManager>();
            Builder.RegisterType<RotaryAutoVolumeTestManager>();
            Builder.RegisterType<MechanicalAutoVolumeTestManager>();

            Builder.RegisterType<AverageReadingStabilizer>().As<IReadingStabilizer>();
            Builder.RegisterType<QaRunTestManager>().As<IQaRunTestManager>();

            RegisterTestActions();

            Task.Run(SettingsManager.RefreshSettings);
        }

        private void RegisterTestActions()
        {
            Builder.RegisterType<TestActionsManager>().As<ITestActionsManager>();

            Builder.Register(c =>
            {
                var resetItems = SettingsManager.SettingsInstance.TocResetItems;
                return new TocItemUpdaterAction(resetItems);
            })
            .As<IPreVolumeTestAction>()
            .Named<IPreVolumeTestAction>("TocVolPulsesWaitingReset");

            Builder.Register(c =>
            {
                var resetItems = new Dictionary<int, string>
                {
                    {5, "0" },
                    {6, "0" },
                    {7, "0" }
                };
                return new ItemUpdaterAction(resetItems);
            })
            .As<IPreVolumeTestAction>()
            .Named<IPreVolumeTestAction>("PulseOutputWaitingReset");

            
        }

        public ContainerBuilder Builder { get; } = new ContainerBuilder();
    }
}