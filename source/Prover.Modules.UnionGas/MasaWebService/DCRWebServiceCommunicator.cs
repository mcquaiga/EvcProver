﻿//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.ServiceModel;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
//using Prover.Modules.UnionGas.DcrWebService;

//namespace Prover.Modules.UnionGas.MasaWebService
//{
//    /// <summary>
//    ///     Defines the <see cref="DCRWebServiceCommunicator" />
//    /// </summary>
//    public class DCRWebServiceCommunicator
//    {
//        /// <summary>
//        ///     Defines the _dcrWebService
//        /// </summary>
//        private readonly DCRWebServiceSoap _dcrWebService;

//        /// <summary>
//        ///     Defines the _log
//        /// </summary>
//        private readonly ILogger _log;

//        /// <summary>
//        ///     Initializes a new instance of the <see cref="DCRWebServiceCommunicator" /> class.
//        /// </summary>
//        /// <param name="dcrWebService">The dcrWebService<see cref="DCRWebServiceSoap" /></param>
//        public DCRWebServiceCommunicator(ILogger<DCRWebServiceCommunicator> log, DCRWebServiceSoap dcrWebService)
//        {
//            _dcrWebService = dcrWebService;
//            _log = log;
//        }

//        /// <summary>
//        ///     Gets the SoapClient
//        /// </summary>
//        public DCRWebServiceSoapClient SoapClient => (DCRWebServiceSoapClient) _dcrWebService;

//        /// <summary>
//        ///     The FindMeterByCompanyNumber
//        /// </summary>
//        /// <param name="companyNumber">The companyNumber<see cref="string" /></param>
//        /// <returns>The <see cref="Task{MeterDTO}" /></returns>
//        public async Task<MeterDTO> FindMeterByCompanyNumber(string companyNumber)
//        {
//            _log.LogDebug($"Finding meter with inventory number {companyNumber} in MASA.");

//            var request = new GetValidatedEvcDeviceByInventoryCodeRequest
//            {
//                Body = new GetValidatedEvcDeviceByInventoryCodeRequestBody(companyNumber)
//            };

//            var response =
//                await CallWebServiceMethod(() => _dcrWebService.GetValidatedEvcDeviceByInventoryCodeAsync(request))
//                    .ConfigureAwait(false);

//            return response.Body.GetValidatedEvcDeviceByInventoryCodeResult;
//        }

//        /// <summary>
//        ///     The GetOutstandingMeterTestsByJobNumber
//        /// </summary>
//        /// <param name="jobNumber">The jobNumber<see cref="int" /></param>
//        /// <returns>The <see cref="Task{IList{MeterDTO}}" /></returns>
//        public async Task<IList<MeterDTO>> GetOutstandingMeterTestsByJobNumber(int jobNumber)
//        {
//            var request = new GetMeterListByJobNumberRequest(new GetMeterListByJobNumberRequestBody(jobNumber));

//            var response = await CallWebServiceMethod(() => _dcrWebService.GetMeterListByJobNumberAsync(request))
//                .ConfigureAwait(false);

//            return response.Body.GetMeterListByJobNumberResult.ToList();
//        }

//        /// <summary>
//        ///     The SendResultsToWebService
//        /// </summary>
//        /// <param name="evcQaRuns">The evcQaRuns<see cref="IEnumerable{QARunEvcTestResult}" /></param>
//        /// <returns>The <see cref="Task{bool}" /></returns>
//        public async Task<bool> SendQaTestResults(IEnumerable<QARunEvcTestResult> evcQaRuns)
//        {
//            var items = evcQaRuns.ToArray();

//            if (items.Length == 0)
//                throw new ArgumentOutOfRangeException(nameof(evcQaRuns));

//            var request = new SubmitQAEvcTestResultsRequest(new SubmitQAEvcTestResultsRequestBody(items));

//            var response = await CallWebServiceMethod(() => _dcrWebService.SubmitQAEvcTestResultsAsync(request))
//                .ConfigureAwait(false);

//            var result = response.Body.SubmitQAEvcTestResultsResult;

//            if (string.Equals(result, "success", StringComparison.OrdinalIgnoreCase))
//            {
//                _log.LogInformation("Web service returned successfully!");
//                return true;
//            }

//            _log.LogInformation($"Web service return an error: {Environment.NewLine} {result}");
//            return false;
//        }

//        /// <summary>
//        ///     The CallWebServiceMethod
//        /// </summary>
//        /// <typeparam name="TResult"></typeparam>
//        /// <param name="webServiceMethod">The webServiceMethod<see cref="Func{Task{TResult}}" /></param>
//        /// <returns>The <see cref="Task{TResult}" /></returns>
//        private async Task<TResult> CallWebServiceMethod<TResult>(Func<Task<TResult>> webServiceMethod)
//        {
//            try
//            {
//                var tokenSource = new CancellationTokenSource(new TimeSpan(0, 0, 0, 3));
//                tokenSource.Token.ThrowIfCancellationRequested();

//                return await Task.Run(async () => await webServiceMethod.Invoke(), tokenSource.Token)
//                    .ConfigureAwait(false);
//            }
//            catch (OperationCanceledException)
//            {
//                _log.LogWarning("Timed out contacting the web service. Skipping company number verification.");
//                throw;
//            }

//            catch (EndpointNotFoundException ex)
//            {
//                _log.LogError(ex, $"MASA Web service could not be reached. {Environment.NewLine}" +
//                                  $" Endpoint: {SoapClient.Endpoint.Address} {Environment.NewLine}" +
//                                  $" State: {SoapClient.State.ToString()}");
//                throw;
//            }

//            catch (Exception ex)
//            {
//                _log.LogError(ex, "An error occured contacting the web service. Skipping company number verification.");
//                throw;
//            }
//        }
//    }
//}