using System;
using BililiveRecorder.Core.Config.V3;
using Microsoft.Extensions.DependencyInjection;

namespace BililiveRecorder.Core
{
    internal class DownloaderFactory : IDownloaderFactory
    {
        private readonly IServiceProvider serviceProvider;

        public DownloaderFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IDownloader CreateDownloader(DownloaderConfig downloaderConfig)
        {
            var scope = this.serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;

            return ActivatorUtilities.CreateInstance<Downloader>(sp, scope, downloaderConfig);
        }
    }
}
