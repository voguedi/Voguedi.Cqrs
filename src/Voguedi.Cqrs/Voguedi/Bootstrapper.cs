using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Voguedi.Services;
using Voguedi.Stores;

namespace Voguedi
{
    class Bootstrapper : IBootstrapper
    {
        #region Private Fields

        readonly IEnumerable<IService> services;
        readonly IEnumerable<IStore> stores;
        readonly IApplicationLifetime applicationLifetime;
        readonly ILogger logger;
        readonly CancellationTokenSource cancellationTokenSource;
        readonly CancellationTokenRegistration cancellationTokenRegistration;
        Task task;

        #endregion

        #region Ctors

        public Bootstrapper(
            IEnumerable<IService> services,
            IEnumerable<IStore> stores,
            IApplicationLifetime applicationLifetime,
            ILogger<Bootstrapper> logger)
        {
            this.services = services;
            this.stores = stores;
            this.applicationLifetime = applicationLifetime;
            this.logger = logger;
            cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenRegistration = applicationLifetime.ApplicationStopping.Register(
                () =>
                {
                    cancellationTokenSource.Cancel();

                    try
                    {
                        task?.GetAwaiter().GetResult();
                    }
                    catch (OperationCanceledException ex)
                    {
                        logger.LogError(ex, "框架初始化操作取消！");
                    }
                });
        }

        #endregion

        #region Private Methods

        async Task DoBootstrapperAsync()
        {
            foreach (var store in stores)
                await store.InitializeAsync(cancellationTokenSource.Token);

            if (!cancellationTokenSource.IsCancellationRequested)
            {
                applicationLifetime.ApplicationStopping.Register(() =>
                {
                    foreach (var service in services)
                        service.Dispose();
                });

                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    foreach (var service in services)
                    {
                        try
                        {
                            service.Start();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"框架相关服务启动失败！");
                        }
                    }

                    cancellationTokenRegistration.Dispose();
                    cancellationTokenSource.Dispose();
                }
            }
        }

        #endregion

        #region IBootstrapper

        public Task BootstrapperAsync() => task = DoBootstrapperAsync();

        #endregion
    }
}
