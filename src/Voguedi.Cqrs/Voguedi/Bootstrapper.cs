using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Voguedi.Domain.Caching;
using Voguedi.Services;

namespace Voguedi
{
    class Bootstrapper : BackgroundService, IBootstrapper
    {
        #region Private Fields

        readonly ICache cache;
        readonly IEnumerable<IBackgroundWorkerService> backgroundWorkerServices;
        readonly IEnumerable<IStoreService> storeServices;
        readonly IEnumerable<ISubscriberService> subscriberServices;
        readonly ILogger logger;

        #endregion

        #region Ctors

        public Bootstrapper(
            ICache cache,
            IEnumerable<IBackgroundWorkerService> backgroundWorkerServices,
            IEnumerable<IStoreService> storeServices,
            IEnumerable<ISubscriberService> subscriberServices,
            ILogger<Bootstrapper> logger)
        {
            this.cache = cache;
            this.backgroundWorkerServices = backgroundWorkerServices;
            this.storeServices = storeServices;
            this.subscriberServices = subscriberServices;
            this.logger = logger;
        }

        #endregion

        #region BackgroundService

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) => await BootstrapperAsync(stoppingToken);

        #endregion

        #region IBootstrapper

        public async Task BootstrapperAsync(CancellationToken cancellationToken)
        {
            logger.LogDebug("框架服务启动中...");

            foreach (var storeService in storeServices)
                await storeService.InitializeAsync(cancellationToken);

            cancellationToken.Register(() =>
            {
                logger.LogDebug("框架服务停止中...");
                cache.Dispose();

                foreach (var backgroundWorkerService in backgroundWorkerServices)
                    backgroundWorkerService.Stop();

                foreach (var subscriberService in subscriberServices)
                {
                    try
                    {
                        subscriberService.Dispose();
                    }
                    catch (OperationCanceledException ex)
                    {
                        logger.LogError(ex, $"订阅服务停止操作取消。 [SubscriberServiceType = {subscriberService.GetType()}]");
                    }
                }

                logger.LogDebug("框架服务已停止。");
            });

            cache.Start();

            foreach (var backgroundWorkerService in backgroundWorkerServices)
                backgroundWorkerService.Start();

            foreach (var subscriberService in subscriberServices)
            {
                try
                {
                    subscriberService.Start();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"订阅服务启动失败。 [SubscriberServiceType = {subscriberService.GetType()}]");
                }
            }

            logger.LogDebug("框架服务已启动。");
        }

        #endregion
    }
}
