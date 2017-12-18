﻿using System;
using System.Reactive.Subjects;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using UnionGas.MASA.DCRWebService;
using UnionGas.MASA.Dialogs.CompanyNumberDialog;
using LogManager = NLog.LogManager;
using Prover.Core.VerificationTests.TestActions;
using Prover.Core.Models.Instruments;
using Prover.CommProtocol.Common;
using Prover.CommProtocol.Common.Items;
using Prover.Core.Login;
using Prover.Core.Services;
using Prover.GUI.Screens;

namespace UnionGas.MASA.Validators.CompanyNumber
{
    public class CompanyNumberValidationManager : IPreTestValidation
    {
        private readonly ScreenManager _screenManager;
        private readonly TestRunService _testRunService;
        private readonly DCRWebServiceSoap _webService;
        private readonly ILoginService<EmployeeDTO> _loginService;
        private readonly Logger _log = LogManager.GetCurrentClassLogger();

        public CompanyNumberValidationManager(ScreenManager screenManager, TestRunService testRunService, DCRWebServiceSoap webService, ILoginService<EmployeeDTO> loginService)
        {
            _screenManager = screenManager;
            _testRunService = testRunService;
            _webService = webService;
            _loginService = loginService;
        }

        public async Task Validate(EvcCommunicationClient commClient, Instrument instrument, Subject<string> statusUpdates = null)
        {
            var companyNumberItem = instrument.Items.GetItem(ItemCodes.SiteInfo.CompanyNumber);
            var companyNumber = companyNumberItem.RawValue.TrimStart('0');

            var serialNumberItem = instrument.Items.GetItem(ItemCodes.SiteInfo.SerialNumber);
            var serialNumber = serialNumberItem.RawValue.TrimStart('0');

            MeterDTO meterDto;
            do
            {
                meterDto = await VerifyWithWebService(companyNumber);

                if (meterDto != null 
                    && (
                            (meterDto?.InventoryCode == null || meterDto.InventoryCode != companyNumber)
                        ||  (meterDto?.SerialNumber.TrimStart('0') != serialNumber)
                       )
                   )
                {
                    _log.Warn($"Company number {companyNumber} not found in an open job.");
                    companyNumber = (string) await Update(commClient, instrument, new CancellationTokenSource().Token);
                }
                else
                {
                    break;
                }
            } while (!string.IsNullOrEmpty(companyNumber));
            
            if (meterDto != null)
                await UpdateInstrumentValues(instrument, meterDto);
        }

        private async Task UpdateInstrumentValues(Instrument instrument, MeterDTO meterDto)
        {
            instrument.JobId = meterDto?.JobNumber.ToString();
            instrument.EmployeeId = _loginService.User?.Id;
            await _testRunService.Save(instrument);
        }

        public async Task<object> Update(EvcCommunicationClient evcCommunicationClient, Instrument instrument, CancellationToken ct)
        {
            var newCompanyNumber = OpenCompanyNumberDialog();
            if (string.IsNullOrEmpty(newCompanyNumber)) return string.Empty;

            await evcCommunicationClient.Connect(ct);
            var response =
                await
                    evcCommunicationClient.SetItemValue(ItemCodes.SiteInfo.CompanyNumber, long.Parse(newCompanyNumber));

            await evcCommunicationClient.Disconnect();

            if (response)
            {
                instrument.Items.GetItem(ItemCodes.SiteInfo.CompanyNumber).RawValue = newCompanyNumber;
                await _testRunService.Save(instrument);
            }

            return newCompanyNumber;
        }

        public string OpenCompanyNumberDialog()
        {
            while (true)
            {
                var dialog = _screenManager.ResolveViewModel<CompanyNumberDialogViewModel>();
                var result = _screenManager.ShowDialog(dialog);

                if (result.HasValue && result.Value)
                {
                    _log.Debug($"New company number {dialog.CompanyNumber} was entered.");
                    if (string.IsNullOrEmpty(dialog.CompanyNumber))
                        continue;

                    return dialog.CompanyNumber;
                }

                _log.Debug($"Skipping inventory code verification.");
                return string.Empty;
            }
        }

        public async Task<MeterDTO> VerifyWithWebService(string companyNumber)
        {
            var tokenSource = new CancellationTokenSource(new TimeSpan(0, 0, 0, 3));
            tokenSource.Token.ThrowIfCancellationRequested();

            _log.Debug($"Verifying company number {companyNumber} with web service.");

            try
            {
                var request = new GetValidatedEvcDeviceByInventoryCodeRequest
                {
                    Body = new GetValidatedEvcDeviceByInventoryCodeRequestBody(companyNumber)
                };

                var response =
                    await Task.Run(async () => await _webService.GetValidatedEvcDeviceByInventoryCodeAsync(request),
                        tokenSource.Token);
                return response.Body.GetValidatedEvcDeviceByInventoryCodeResult;
            }
            catch (OperationCanceledException)
            {
                _log.Warn($"Timed out contacting the web service. Skipping company number verification.");
                return null;
            }
            catch (EndpointNotFoundException)
            {
                _log.Warn($"Web service not available. Skipping company number verification.");
                return null;
            }            
            catch (Exception ex)
            {
                _log.Error(ex, $"An error occured contacting the web service. Skipping company number verification.");
                return null;
            }
        }
    }
}