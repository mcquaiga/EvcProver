using Devices.Core.Interfaces;
using DynamicData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Devices.Core.Repository
{
    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<T> valueFactory) :
                base(() => Task.Factory.StartNew(valueFactory))
        { }

        public AsyncLazy(Func<Task<T>> taskFactory) :
                base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap())
        { }

        public TaskAwaiter<T> GetAwaiter() { return Value.GetAwaiter(); }
    }

    public interface IDeviceRepository
    {
        ReadOnlyObservableCollection<DeviceType> Devices { get; }
        IObservableCache<DeviceType, Guid> All { get; }
        IObservable<IChangeSet<DeviceType, Guid>> Connect();
        void Dispose();
        IEnumerable<T> FilterCacheByType<T>() where T : DeviceType;
        T Find<T>(Func<T, bool> predicate) where T : DeviceType;
        IEnumerable<T> GetAll<T>(bool includeHidden = false) where T : DeviceType;
        IEnumerable<DeviceType> GetAll(bool includeHidden = false);
        DeviceType GetById(Guid id);
        DeviceType GetByName(string name);
        void Save();
        Task<DeviceRepository> Load(ICollection<IDeviceTypeDataSource<DeviceType>> sources);
        Task<DeviceRepository> UpdateCachedTypes();
        Task<DeviceRepository> UpdateCachedTypes(IDeviceTypeDataSource<DeviceType> dataSource);
        Task<DeviceRepository> UpdateCachedTypes(IEnumerable<IDeviceTypeDataSource<DeviceType>> sources);
    }

    public static class DeviceRepositoryEx
    {

    }

    public class DeviceRepository : IDisposable, IDeviceRepository
    {
        private readonly IDeviceTypeCacheSource<DeviceType> _cacheSource;

        private readonly SourceCache<DeviceType, Guid>
            _deviceSourceCache = new SourceCache<DeviceType, Guid>(v => v.Id);

        private readonly CompositeDisposable _disposer;
        private readonly ILogger<DeviceRepository> _logger;

        private static DeviceRepository _instance;

        //private static AsyncLazy<IDeviceRepository> _lazy = new AsyncLazy<IDeviceRepository>(CreateRepository);
        private static Lazy<Task<IDeviceRepository>> _lazy = new Lazy<Task<IDeviceRepository>>(async () => await CreateRepository());
        //private static Lazy<Task<IDeviceRepository>> _lazy = new Lazy<Task<IDeviceRepository>>(async () => await Observable.StartAsync(CreateRepository));

        //IEnumerable<IDeviceTypeDataSource<DeviceType>> deviceRepositories,
        public DeviceRepository(ILogger<DeviceRepository> logger = null,
            IDeviceTypeCacheSource<DeviceType> cacheRepository = null) : this()
        {
            _logger = logger ?? NullLogger<DeviceRepository>.Instance;

            _cacheSource = cacheRepository;
        }

        public static DeviceType MiniMax => Instance.GetByName("Mini-Max");
        public static DeviceType MiniAt => Instance.GetByName("Mini-AT");
        public static DeviceType Adem => Instance.GetByName("Adem");

        private DeviceRepository()
        {
            _logger = NullLogger<DeviceRepository>.Instance;

            var devices = _deviceSourceCache.Connect().Publish();
            All = devices.AsObservableCache();

            var allDevice = All.Connect()
                .Filter(t => t.IsHidden == false)
                .Bind(out var deviceTypes)
                .Subscribe();
            Devices = deviceTypes;

            _disposer = new CompositeDisposable(All, _deviceSourceCache, devices.Connect(), allDevice);
        }

        private static async Task<IDeviceRepository> CreateRepository()
        {
            var instance = new DeviceRepository();
            await instance.FindDeviceTypeDataSources();
            return instance;
        }

        public static IDeviceRepository Instance { get; } = _lazy.Value.GetAwaiter().GetResult();

        public ReadOnlyObservableCollection<DeviceType> Devices { get; }

        public IObservableCache<DeviceType, Guid> All { get; }

        public IObservable<IChangeSet<DeviceType, Guid>> Connect() => _deviceSourceCache.Connect();

        public void Dispose()
        {
            _disposer?.Dispose();
        }

        public IEnumerable<T> FilterCacheByType<T>() where T : DeviceType => All.Items.OfType<T>();

        public T Find<T>(Func<T, bool> predicate) where T : DeviceType
            => FilterCacheByType<T>().FirstOrDefault(predicate);

        public IEnumerable<T> GetAll<T>(bool includeHidden = false) where T : DeviceType => FilterCacheByType<T>();

        public IEnumerable<DeviceType> GetAll(bool includeHidden = false) => All.Items.Where(d => includeHidden || d.IsHidden == false);

        public DeviceType GetById(Guid id) => All.Lookup(id).Value;

        public DeviceType GetByName(string name) => FindInSetByName(GetAll(), name);

        public async Task<DeviceRepository> Load(ICollection<IDeviceTypeDataSource<DeviceType>> sources)
        {
            _logger.LogDebug("Importing Device Types...");

            if (_cacheSource != null)
            {
                _logger.LogDebug("Cache source registered. Searching cached data first.");
                await UpdateCachedTypes();
            }

            if (Devices.Count == 0 && sources.Any())
            {
                _logger.LogDebug("No device types found in cache. Searching local data sources...");

                _deviceSourceCache.Clear();

                await UpdateCachedTypes(sources);

                if (_cacheSource != null)
                    Save();
            }

            if (sources == null || !sources.Any())
            {
                await FindDeviceTypeDataSources();
            }

            return this;
        }

        public void Save()
        {
            if (_cacheSource != null)
            {
                _logger.LogDebug("Saving device types to cache.");
                _cacheSource?.Save(_deviceSourceCache.Items);
            }
        }

        public async Task<DeviceRepository> UpdateCachedTypes() => await UpdateCachedTypes(_cacheSource);

        public async Task<DeviceRepository> UpdateCachedTypes(IDeviceTypeDataSource<DeviceType> dataSource)
        {
            if (dataSource == null)
                return this;

            _logger.LogDebug($"Scanning data from {dataSource.GetType().Name}.");

            await dataSource.GetDeviceTypes().ForEachAsync(t => _deviceSourceCache.AddOrUpdate(t));

            return this;
        }

        public async Task<DeviceRepository> UpdateCachedTypes(IEnumerable<IDeviceTypeDataSource<DeviceType>> sources)
        {
            foreach (var s in sources)
                await UpdateCachedTypes(s);

            return this;
        }

        private DeviceType FindInSetByName(IEnumerable<DeviceType> devices, string name)
        {
            var result = devices.FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));
            if (result == null)
                throw new KeyNotFoundException($"Device with name {name} not found.");

            return result;
        }

        private async Task FindDeviceTypeDataSources(IEnumerable<Assembly> assemblies = null)
        {
            var loaded = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "Devices.*.Core.dll");
            await loaded.Select(Assembly.LoadFrom)
                        .SelectMany(a
                                => a.DefinedTypes.Where(x
                                        => x.ImplementedInterfaces.Contains(typeof(IDeviceDataSourceInstance))))
                        .Select(t => (IDeviceTypeDataSource<DeviceType>)Activator.CreateInstance(t))
                        .ToObservable()
                        .ForEachAsync(async ds => await UpdateCachedTypes(ds));
        }
    }
}