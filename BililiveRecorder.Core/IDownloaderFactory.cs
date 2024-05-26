namespace BililiveRecorder.Core
{
    public interface IDownloaderFactory
    {
        IDownloader CreateDownloader(DownloaderConfig downloaderConfig);
    }
}
