﻿using System.Reflection;
using Autofac;
using NLog;
using Prover.Core.ExternalIntegrations;
using Prover.Core.ExternalIntegrations.Validators;
using Prover.Core.Login;
using Prover.GUI.Common.Screens.MainMenu;
using Prover.GUI.Common.Screens.Toolbar;
using ReactiveUI.Autofac;
using UnionGas.MASA.DCRWebService;
using UnionGas.MASA.Exporter;
using UnionGas.MASA.Screens.Toolbars;
using UnionGas.MASA.Validators.CompanyNumber;

namespace UnionGas.MASA
{
    public static class Startup
    {
        public static void Initialize(ContainerBuilder builder)
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            builder.RegisterInstance<DCRWebServiceSoap>(new DCRWebServiceSoapClient("DCRWebServiceSoap"));

            //Login service
            builder.RegisterType<LoginService>().As<ILoginService<EmployeeDTO>>().SingleInstance();

            builder.RegisterType<CompanyNumberValidator>().As<IValidator>();
            builder.RegisterType<CompanyNumberUpdater>().As<IUpdater>();
            builder.RegisterType<NewCompanyNumberPopupRequestor>().As<IGetValue>();

            builder.RegisterType<ExportManager>().As<IExportTestRun>();
            
        }
    }
}