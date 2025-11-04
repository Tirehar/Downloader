using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Downloader.Models;

namespace Downloader.Services;

public class DownloadService
{
    private readonly HttpClient _httpClient;

    public DownloadService()
    {
    }

    public DownloadService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public DownloadCore InitCore(DownloadItem item, int chunkCount)
    {
        var core = new DownloadCore(item, chunkCount);
        if (_httpClient is not null)
        {
            core.SetHttpClient(_httpClient);
        }
        return core;
    }

    public DownloadCore InitCore(DownloadItem[] items, int maxConcurrentDownloads)
    {
        var core = new DownloadCore(items, maxConcurrentDownloads);
        if (_httpClient is not null)
        {
            core.SetHttpClient(_httpClient);
        }
        return core;
    }

    public async Task FileDownloadAsync(DownloadCore core, CancellationToken? token = null)
    {
        await core.StartDownloadAsync(token??CancellationToken.None);
    }
}
