using System;
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
        public readonly string Url;
        public readonly string OutputPath;
        public DownloaderConfig(string url, string outputPath)
        {
            Url = url;
            OutputPath = outputPath;
        }
    }
}
