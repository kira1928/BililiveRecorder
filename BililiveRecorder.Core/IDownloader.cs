using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BililiveRecorder.Core
{
    public interface IDownloader : IDisposable
    {
        DownloaderConfig DownloaderConfig { get; }

        Task StartRecord(IServiceProvider sp);
    }

    public class DownloaderConfig
    {
        public readonly string? Cookie;
        public readonly IEnumerable<string>? DownloadHeaders;
        public readonly string Url;
        public readonly string OutputPath;
        public DownloaderConfig(string url, string outputPath, string? cookie, IEnumerable<string>? downloadHeaders)
        {
            Url = url;
            OutputPath = outputPath;
            this.Cookie = cookie;
            this.DownloadHeaders = downloadHeaders;
        }
    }
}
