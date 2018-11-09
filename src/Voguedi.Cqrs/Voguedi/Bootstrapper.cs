using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Voguedi.Messaging;

namespace Voguedi
{
    class Bootstrapper : IBootstrapper
    {
        #region Private Fields

        readonly IEnumerable<IMessageService> messageServices;
        readonly IEnumerable<IMessageStore> messageStores;
        readonly IApplicationLifetime applicationLifetime;
        readonly ILogger logger;
        readonly CancellationTokenSource cancellationTokenSource;
        readonly CancellationTokenRegistration cancellationTokenRegistration;
        Task task;

        #endregion

        #region Ctors

        public Bootstrapper(
            IEnumerable<IMessageService> messageServices,
            IEnumerable<IMessageStore> messageStores,
            IApplicationLifetime applicationLifetime,
            ILogger<Bootstrapper> logger)
        {
            this.messageServices = messageServices;
            this.messageStores = messageStores;
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
                        logger.LogError(ex, "操作取消！");
                    }
                });
        }

        #endregion

        #region Private Methods

        async Task DoBootstrapperAsync()
        {
            foreach (var store in messageStores)
                await store.InitializeAsync(cancellationTokenSource.Token);

            if (!cancellationTokenSource.IsCancellationRequested)
            {
                applicationLifetime.ApplicationStopping.Register(
                    () =>
                    {
                        foreach (var service in messageServices)
                            service.Dispose();
                    });

                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    foreach (var service in messageServices)
                    {
                        try
                        {
                            service.Start();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"消息相关服务启动异常！");
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
