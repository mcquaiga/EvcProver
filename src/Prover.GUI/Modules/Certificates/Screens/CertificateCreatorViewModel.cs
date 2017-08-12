﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Caliburn.Micro;
using Prover.Core.Models.Certificates;
using Prover.Core.Models.Clients;
using Prover.Core.Models.Instruments;
using Prover.Core.Services;
using Prover.Core.Storage;
using Prover.GUI.Common;
using Prover.GUI.Common.Screens;
using Prover.GUI.Modules.Certificates.Common;
using Prover.GUI.Modules.Certificates.Reports;
using Prover.GUI.Modules.ClientManager.Screens.CsvExporter;
using ReactiveUI;

namespace Prover.GUI.Modules.Certificates.Screens
{
    public class CertificateCreatorViewModel : ViewModelBase, IDisposable
    {

        public List<string> VerificationType => new List<string>
        {
            "New",
            "Re-Verified"
        };
  
        private readonly IClientStore _clientStore;
        private readonly ICertificateService _certificateService;
        private readonly Client _allClient = new Client {Id = Guid.Empty, Name = "(No client)"};

        public CertificateCreatorViewModel(ScreenManager screenManager, IEventAggregator eventAggregator,
            IClientStore clientStore, ICertificateService certificateService, ExportToCsvViewModel exportToCsvViewModel)
            : base(screenManager, eventAggregator)
        {
            ExportToCsvViewModel = exportToCsvViewModel;
            _clientStore = clientStore;
            _certificateService = certificateService;

            var canCreateCertificate = this.WhenAnyValue(x => x.TestedBy, x => x.SelectedVerificationType, x => x.SelectedClient,
                (testedBy, vt, client)
                    => !string.IsNullOrEmpty(testedBy) && !string.IsNullOrEmpty(SelectedVerificationType) && client != null && client.Id != Guid.Empty);
            CreateCertificateCommand = ReactiveCommand.CreateFromTask(CreateCertificate, canCreateCertificate);

            LoadClientInstrumentsCommand = ReactiveCommand.CreateFromTask<Client, ReactiveList<CreateVerificationViewModel>>(LoadClientInstruments);                 
            LoadClientInstrumentsCommand
                .ToProperty(this, x => x.RootResults, out _rootResults, new ReactiveList<CreateVerificationViewModel>());           

            UpdateFilter = ReactiveCommand.Create<string>(filter =>
            {                
                ResultFilteredItems = RootResults.CreateDerivedCollection(x => x,
                    x => filter == "All" || string.IsNullOrEmpty(filter) || (filter == "Passed" && x.Instrument.HasPassed) || (filter == "Failed" && !x.Instrument.HasPassed),
                    (x, y) => x.Instrument.TestDateTime.CompareTo(y.Instrument.TestDateTime));       
            });

            this.WhenAnyValue(x => x.ResultFilteredItems)
                .Select(x => x != null && x.Any())
                .ToProperty(this, x => x.ShowTestViewListBox, out _showTestViewListBox);

            LoadClientInstrumentsCommand.IsExecuting
                .CombineLatest(UpdateFilter.IsExecuting, (clients, filter) => (clients || filter))
                //.Delay(x => !x ? TimeSpan.FromMilliseconds(500).Ticks : 0)
                .ToProperty(this, x => x.ShowLoadingIndicator, out _showLoadingIndicator);

            this.WhenAnyValue(x => x.RootResults)
                .Select(s => "All")
                .InvokeCommand(UpdateFilter);            

            LoadClientsCommand = ReactiveCommand.CreateFromTask(LoadClients);
            LoadClientsCommand
                .ToProperty(this, x => x.Clients, out _clientsList,  new List<Client>());

            FetchNextCertificateNumberCommand = ReactiveCommand.CreateFromTask(_certificateService.GetNextCertificateNumber);
            FetchNextCertificateNumberCommand
                .ToProperty(this, x => x.NextCertificateNumber, out _nextCertificateNumber);

            FetchExistingClientCertificatesCommand =
                ReactiveCommand.CreateFromTask<Client, IEnumerable<Certificate>>(_certificateService.GetAllCertificates);
            FetchExistingClientCertificatesCommand
                .ToProperty(this, x => x.ExistingClientCertificates, out _existingClientCertificates, new List<Certificate>());

            var canExecutePrintCommand = this.WhenAnyValue(x => x.SelectedExistingClientCertificate)
                .Select(c => c != null);
            PrintExistingCertificateCommand =
                ReactiveCommand.CreateFromTask<Certificate>(PrintExistingCertificate, canExecutePrintCommand);

            var selectedClientChange = this.WhenAnyValue(x => x.SelectedClient)
                .Do(c =>
                {
                    if (c == null)
                    {
                        RootResults.Clear();                        
                    }
                })
                .Where(c => c != null);

            selectedClientChange
                .Delay(TimeSpan.FromMilliseconds(25))
                .InvokeCommand(LoadClientInstrumentsCommand);
            selectedClientChange
                .Delay(TimeSpan.FromMilliseconds(150))
                .InvokeCommand(FetchExistingClientCertificatesCommand);
            selectedClientChange
                .Subscribe(c => ExportToCsvViewModel.Client = c);            
        }

        #region Commands

        public ReactiveCommand<Certificate, Unit> PrintExistingCertificateCommand { get; set; }

        public ReactiveCommand<Unit, long> FetchNextCertificateNumberCommand { get; set; }

        public ReactiveCommand<Unit, List<Client>> LoadClientsCommand { get; protected set; }

        public ReactiveCommand<Client, ReactiveList<CreateVerificationViewModel>> LoadClientInstrumentsCommand { get; }

        public ReactiveCommand<Client, IEnumerable<Certificate>> FetchExistingClientCertificatesCommand { get; set; }

        public ReactiveCommand CreateCertificateCommand { get; set; }

        public ReactiveCommand<string, Unit> UpdateFilter { get; set; }

        #endregion

        #region Fody Properties
        //public extern ReactiveList<Instrument> RootClientInstruments { [ObservableAsProperty] get; }
        //public extern ReactiveList<CreateVerificationViewModel> RootResults { [ObservableAsProperty] get; }
        //public extern bool ShowLoadingIndicator { [ObservableAsProperty] get; }
        //public extern bool ShowTestViewListBox { [ObservableAsProperty] get; }
        //public extern long NextCertificateNumber { [ObservableAsProperty] get; }      
        //public extern IEnumerable<Certificate> ExistingClientCertificates { [ObservableAsProperty] get; }
        //public extern IEnumerable<Client> Clients { [ObservableAsProperty] get; }

        //[Reactive]
        //public Certificate SelectedExistingClientCertificate { get; set; }

        //[Reactive]
        //public string SelectedVerificationType { get; set; }

        //[Reactive]
        //public Client SelectedClient { get; set; }

        //[Reactive]
        //public string TestedBy { get; set; }

        //[Reactive]
        //public ExportToCsvViewModel ExportToCsvViewModel { get; set; }      

        //[Reactive]
        //public IReactiveDerivedList<CreateVerificationViewModel> ResultFilteredItems { get; set; }

        #endregion

        #region Properties
        private readonly ObservableAsPropertyHelper<ReactiveList<CreateVerificationViewModel>> _rootResults;
        public ReactiveList<CreateVerificationViewModel> RootResults => _rootResults.Value;

        private readonly ObservableAsPropertyHelper<bool> _showLoadingIndicator;
        public bool ShowLoadingIndicator => _showLoadingIndicator.Value;

        private readonly ObservableAsPropertyHelper<bool> _showTestViewListBox;
        public bool ShowTestViewListBox => _showTestViewListBox.Value;

        private readonly ObservableAsPropertyHelper<long> _nextCertificateNumber;
        public long NextCertificateNumber => _nextCertificateNumber.Value;

        private readonly ObservableAsPropertyHelper<IEnumerable<Certificate>> _existingClientCertificates;
        public IEnumerable<Certificate> ExistingClientCertificates => _existingClientCertificates.Value;

        private Certificate _selectedExistingClientCertificate;
        public Certificate SelectedExistingClientCertificate
        {
            get => _selectedExistingClientCertificate;
            set => this.RaiseAndSetIfChanged(ref _selectedExistingClientCertificate, value);
        }

        private Client _selectedClient;
        public Client SelectedClient
        {
            get => _selectedClient;
            set => this.RaiseAndSetIfChanged(ref _selectedClient, value);
        }

        private string _testedBy;
        public string TestedBy
        {
            get => _testedBy;
            set => this.RaiseAndSetIfChanged(ref _testedBy, value);
        }

        private readonly ObservableAsPropertyHelper<IEnumerable<Client>> _clientsList;
        public IEnumerable<Client> Clients => _clientsList.Value;

        private string _selectedVerificationType;
        public string SelectedVerificationType
        {
            get => _selectedVerificationType;
            set => this.RaiseAndSetIfChanged(ref _selectedVerificationType, value);
        }

        private IReactiveDerivedList<CreateVerificationViewModel> _resultFilteredItems;
        public IReactiveDerivedList<CreateVerificationViewModel> ResultFilteredItems
        {
            get => _resultFilteredItems;
            set => this.RaiseAndSetIfChanged(ref _resultFilteredItems, value);
        }

        public ExportToCsvViewModel ExportToCsvViewModel { get; set; }
        #endregion

        #region Private Functions
        private async Task<ReactiveList<CreateVerificationViewModel>> LoadClientInstruments(Client client)
        {
            var instruments = await _certificateService.GetInstrumentsWithNoCertificate(client.Id);
            var viewModels = new ReactiveList<CreateVerificationViewModel>(
                instruments.Select(i =>
                {
                    var vvm = new VerificationViewModel(i);
                    var item = ScreenManager.ResolveViewModel<CreateVerificationViewModel>();
                    item.VerificationView = vvm;
                    return item;
                }));

            return viewModels;        
        }
    
        private async Task<List<Client>> LoadClients()
        {
            var clients = new List<Client> { _allClient };
            clients.AddRange(await _clientStore.GetAll());
            return clients;
        }

        private async Task CreateCertificate()
        {
            var instruments = ResultFilteredItems.Where(x => x.IsSelected)
                .Select(i => i.VerificationView.Instrument)
                .ToList();

            if (instruments.Count > 8)
            {
                MessageBox.Show("Maximum 8 instruments allowed per certificate.");
                return;
            }

            if (!instruments.Any())
            {
                MessageBox.Show("Please select at least one instrument.");
                return;
            }

            if (instruments.Select(x => x.ClientId).Distinct().Count() > 1)
                MessageBox.Show("Cannot have multiple clients on the same certificate.");

            var certificate =
                await _certificateService.CreateCertificate(TestedBy, SelectedVerificationType, instruments);

            var search = LoadClientInstrumentsCommand.Execute(SelectedClient);

            CertificateGenerator.GenerateXps(certificate);
            await FetchNextCertificateNumberCommand.Execute();
            await search;
        }

        private async Task PrintExistingCertificate(Certificate certificate)
        {
            if (certificate == null) return;

            var cert = await _certificateService.GetCertificate(certificate.Number);

            if (cert == null)
            {
                MessageBox.Show("No certificate found.", "Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CertificateGenerator.GenerateXps(cert);
        }
        #endregion

        public void Dispose()
        {
            ResultFilteredItems?.Dispose();
            LoadClientInstrumentsCommand?.Dispose();
            CreateCertificateCommand?.Dispose();
        }
    }
}