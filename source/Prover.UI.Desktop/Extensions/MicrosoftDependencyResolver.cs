﻿// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Splat;

namespace Prover.UI.Desktop.Extensions
{
	/// <summary>
	/// Microsoft DI implementation for <see cref="IDependencyResolver"/>.
	/// </summary>
	/// <seealso cref="IDependencyResolver" />
	[SuppressMessage("Globalization", "CA1303", Justification = "Unnecessary warning")]
	public class MicrosoftDependencyResolver : IDependencyResolver
	{
		private const string ImmutableExceptionMessage = "This container has already been built and cannot be modified.";
		private static readonly Type _dictionaryType = typeof(ContractDictionary<>);
		private readonly object _syncLock = new object();
		private IServiceCollection _serviceCollection;
		private bool _isImmutable;
		private IServiceProvider _serviceProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="MicrosoftDependencyResolver" /> class with an <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services">An instance of <see cref="IServiceCollection"/>.</param>
		public MicrosoftDependencyResolver(IServiceCollection services = null)
		{
			_serviceCollection = services ?? new ServiceCollection();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MicrosoftDependencyResolver" /> class with a configured service Provider.
		/// </summary>
		/// <param name="serviceProvider">A ready to use service provider.</param>
		public MicrosoftDependencyResolver(IServiceProvider serviceProvider) =>
			UpdateContainer(serviceProvider);

		/// <summary>
		/// Gets the internal Microsoft conainer,
		/// or build new if this instance was not initialized with one.
		/// </summary>
		protected virtual IServiceProvider ServiceProvider
		{
			get
			{
				lock (_syncLock)
				{
					if (_serviceProvider == null)
					{
						_serviceProvider = _serviceCollection.BuildServiceProvider();
					}

					return _serviceProvider;
				}
			}
		}

		/// <summary>
		/// Updates this instance with a configured service Provider.
		/// </summary>
		/// <param name="serviceProvider">A ready to use service provider.</param>
		public void UpdateContainer(IServiceProvider serviceProvider)
		{
			if (serviceProvider == null)
			{
				throw new ArgumentNullException(nameof(serviceProvider));
			}

			lock (_syncLock)
			{
				_serviceCollection = null;
				_serviceProvider = serviceProvider;
				_isImmutable = true;
			}
		}

		/// <inheritdoc />
		public virtual object GetService(Type serviceType, string contract = null) =>
			GetServices(serviceType, contract).LastOrDefault();

		/// <inheritdoc />
		public virtual IEnumerable<object> GetServices(Type serviceType, string contract = null)
		{
			var isNull = serviceType == null;
			if (isNull)
			{
				serviceType = typeof(NullServiceType);
			}

			IEnumerable<object> services;

			if (string.IsNullOrWhiteSpace(contract))
			{
				services = ServiceProvider.GetServices(serviceType);
				if (isNull)
				{
					services = services
						.Cast<NullServiceType>()
						.Select(nst => nst.Factory());
				}
			}
			else
			{
				var dic = GetContractDictionary(serviceType, false);
				services = dic?
					.GetFactories(contract)
					.Select(f => f())
					?? Enumerable.Empty<object>();
			}

			return services;
		}

		/// <inheritdoc />
		public virtual void Register(Func<object> factory, Type serviceType, string contract = null)
		{
			if (_isImmutable)
			{
				throw new InvalidOperationException(ImmutableExceptionMessage);
			}

			var isNull = serviceType == null;

			if (isNull)
			{
				serviceType = typeof(NullServiceType);
			}

			lock (_syncLock)
			{
				if (string.IsNullOrWhiteSpace(contract))
				{
					_serviceCollection.AddTransient(serviceType, _ =>
					isNull
					? new NullServiceType(factory)
					: factory());
				}
				else
				{
					var dic = GetContractDictionary(serviceType, true);

					dic.AddFactory(contract, factory);
				}

				// required so that it gets rebuilt if not injected externally.
				_serviceProvider = null;
			}
		}

		/// <inheritdoc/>
		public virtual void UnregisterCurrent(Type serviceType, string contract = null)
		{
			if (_isImmutable)
			{
				throw new InvalidOperationException(ImmutableExceptionMessage);
			}

			if (serviceType == null)
			{
				serviceType = typeof(NullServiceType);
			}

			lock (_syncLock)
			{
				if (contract == null)
				{
					var sd = _serviceCollection.LastOrDefault(s => s.ServiceType == serviceType);
					_serviceCollection.Remove(sd);
				}
				else
				{
					var dic = GetContractDictionary(serviceType, false);
					if (dic != null)
					{
						dic.RemoveLastFactory(contract);
						if (dic.IsEmpty)
						{
							RemoveContractService(serviceType);
						}
					}
				}

				// required so that it gets rebuilt if not injected externally.
				_serviceProvider = null;
			}
		}

		/// <summary>
		/// Unregisters all the values associated with the specified type and contract - or -
		/// If the container has already been built, removes the specified contract (scope) entirely,
		/// ignoring the <paramref name="serviceType"/> argument.
		/// </summary>
		/// <param name="serviceType">The service type to unregister.</param>
		/// <param name="contract">This parameter is ignored. Service will be removed from all contracts.</param>
		public virtual void UnregisterAll(Type serviceType, string contract = null)
		{
			if (_isImmutable)
			{
				throw new InvalidOperationException(ImmutableExceptionMessage);
			}

			if (serviceType == null)
			{
				serviceType = typeof(NullServiceType);
			}

			lock (_syncLock)
			{
				if (contract == null)
				{
					var sds = _serviceCollection
						.Where(s => s.ServiceType == serviceType)
						.ToList();

					foreach (var sd in sds)
					{
						_serviceCollection.Remove(sd);
					}
				}
				else
				{
					var dic = GetContractDictionary(serviceType, false);
					if (dic != null && dic.TryRemoveContract(contract) && dic.IsEmpty)
					{
						RemoveContractService(serviceType);
					}
				}

				// required so that it gets rebuilt if not injected externally.
				_serviceProvider = null;
			}
		}

		/// <inheritdoc />
		public virtual IDisposable ServiceRegistrationCallback(Type serviceType, string contract, Action<IDisposable> callback)
		{
			// this method is not used by RxUI
			throw new NotImplementedException(nameof(ServiceRegistrationCallback));
		}

		/// <inheritdoc/>
		public virtual bool HasRegistration(Type serviceType, string contract = null)
		{
			if (serviceType == null)
			{
				serviceType = typeof(NullServiceType);
			}

			if (!_isImmutable)
			{
				if (string.IsNullOrWhiteSpace(contract))
				{
					return _serviceCollection.Any(sd => sd.ServiceType == serviceType);
				}

				var dictionary = (ContractDictionary)_serviceCollection.FirstOrDefault(sd => sd.ServiceType == GetDictionaryType(serviceType))?.ImplementationInstance;

				if (dictionary == null)
				{
					return false;
				}

				return dictionary.GetFactories(contract).Select(f => f()).Any();
			}

			if (contract == null)
			{
				var service = _serviceProvider.GetService(serviceType);
				return service != null;
			}

			var dic = GetContractDictionary(serviceType, false);
			return dic?.IsEmpty == false;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes of the instance.
		/// </summary>
		/// <param name="disposing">Whether or not the instance is disposing.</param>
		protected virtual void Dispose(bool disposing)
		{
		}

		private static Type GetDictionaryType(Type serviceType) => _dictionaryType.MakeGenericType(serviceType);

		private void RemoveContractService(Type serviceType)
		{
			var dicType = GetDictionaryType(serviceType);
			var sd = _serviceCollection.SingleOrDefault(s => s.ServiceType == serviceType);
			_serviceCollection.Remove(sd);
		}

		[SuppressMessage("Naming Rules", "SA1300", Justification = "Intentional")]
		private ContractDictionary GetContractDictionary(Type serviceType, bool createIfNotExists)
		{
			var dicType = GetDictionaryType(serviceType);

			if (_isImmutable)
			{
				return (ContractDictionary)ServiceProvider.GetService(dicType);
			}

			var dic = getDictionary();
			if (createIfNotExists && dic == null)
			{
				lock (_syncLock)
				{
					if (createIfNotExists && dic == null)
					{
						dic = (ContractDictionary)Activator.CreateInstance(dicType);
						_serviceCollection.AddSingleton(dicType, dic);
					}
				}
			}

			return dic;

			ContractDictionary getDictionary() => _serviceCollection
					.Where(sd => sd.ServiceType == dicType)
					.Select(sd => sd.ImplementationInstance)
					.Cast<ContractDictionary>()
					.SingleOrDefault();
		}

		private class ContractDictionary
		{
			private readonly ConcurrentDictionary<string, IList<Func<object>>> _dictionary = new ConcurrentDictionary<string, IList<Func<object>>>();

			public bool IsEmpty => _dictionary.Count == 0;

			public bool TryRemoveContract(string contract) =>
				_dictionary.TryRemove(contract, out var _);

			public Func<object> GetFactory(string contract) =>
				GetFactories(contract)
					.LastOrDefault();

			public IEnumerable<Func<object>> GetFactories(string contract) =>
				_dictionary.TryGetValue(contract, out var collection)
				? collection ?? Enumerable.Empty<Func<object>>()
				: Enumerable.Empty<Func<object>>();

			public void AddFactory(string contract, Func<object> factory) =>
				_dictionary.AddOrUpdate(contract, _ => new List<Func<object>> { factory }, (_, list) =>
				{
					if (list == null)
					{
						list = new List<Func<object>>();
					}

					list.Add(factory);
					return list;
				});

			public void RemoveLastFactory(string contract) =>
				_dictionary.AddOrUpdate(contract, default(IList<Func<object>>), (_, list) =>
				{
					var lastIndex = list.Count - 1;
					if (lastIndex > 0)
					{
						list.RemoveAt(lastIndex);
					}

					// TODO if list empty remove contract entirely
					// need to find how to atomically update or remove
					// https://github.com/dotnet/corefx/issues/24246
					return list;
				});
		}

		[SuppressMessage("Design", "CA1812: Unused class.", Justification = "Used in reflection.")]
		private class ContractDictionary<T> : ContractDictionary
		{
		}

		private class NullServiceType
		{
			public NullServiceType(Func<object> factory)
			{
				Factory = factory;
			}

			public Func<object> Factory { get; }
		}
	}
}
