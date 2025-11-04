using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Downloader.DownloadEventArgs;
using Downloader.Models;

namespace Downloader.Services;

public class DownloadCore
{
    private readonly SemaphoreSlim? _semaphore;
    private readonly DownloadItem[]? _downloadItems;
    private readonly DownloadItem? _downloadItem;
    private readonly int _chunkCount;
    private readonly bool _isMultiple;

    private HttpClient _httpClient;
    private long _totalBytesReceived;
    private long _totalBytes;
    private double _progressPercentage => 100 * _totalBytesReceived / (double)_totalBytes;

    public event EventHandler<DownloadProgressChangedArgs> OnDownloadProgressChanged;
    public event EventHandler OnDownloadCompleted;
    public event EventHandler OnDownloadFailed;

    public DownloadCore(DownloadItem[] downloadItems, int maxConcurrentDownloads)
    {
        _isMultiple = true;
        _semaphore = new SemaphoreSlim(maxConcurrentDownloads);
        _downloadItems = downloadItems;
    }

    public DownloadCore(DownloadItem item, int chunkCount)
    {
        _isMultiple = false;
        _downloadItem = item;
        _semaphore = new SemaphoreSlim(chunkCount);
        _chunkCount = chunkCount;
    }

    public void SetHttpClient(HttpClient httpClient)
    {
         _httpClient = httpClient;
    }

    public async Task StartDownloadAsync(CancellationToken token)
    {
        if(_httpClient == null)
        {
            InitializeHttpClient();
        }

        if ( _isMultiple)
        {
            await DownloadMultipleFilesAsync(_downloadItems, token);
        }
        else
        {
            await DownloadSingleFileAsync(_downloadItem, _chunkCount, token);
        }
    }

    private async Task DownloadMultipleFilesAsync(DownloadItem[] items, CancellationToken token)
    {
        Console.WriteLine($"开始批量下载 {items.Length} 个文件...");
        await InitInfoAsync(items, token);
        var tasks = new List<Task>();
       
        foreach (var item in items)
        {
            tasks.Add(Task.Run(async () => await OpenDownloadTask(item, token)));
        }
        Task.WaitAll(tasks.ToArray());
        OnDownloadCompleted?.Invoke(this, new DownloadCompltedArgs(null));
    }

    private async Task DownloadSingleFileAsync(DownloadItem item, int chunkCount, CancellationToken token)
    {
        await InitInfoAsync(item, token);

        var chunkSize = _totalBytes / chunkCount;

        var tempDir = Path.Combine(Path.GetDirectoryName(new DirectoryInfo(item.FilePath).FullName), "temp_chunks");
        Directory.CreateDirectory(tempDir);

        try
        {
            var tasks = new Task[chunkCount];
            for (var i = 0; i < chunkCount; i++)
            {
                long start = i * chunkSize;
                long end = Math.Min((i + 1) * chunkSize - 1, _totalBytes - 1);
                string chunkFile = Path.Combine(tempDir, $"{i}.chunk");

                tasks[i] = Task.Run(async()=> await OpenDownloadTask(new() { Url=item.Url, FilePath=chunkFile}, token,start, end));
            }

            await Task.WhenAll(tasks);

            //合并块
            MergeChunks(item.FilePath, tempDir, chunkCount);
        }
        finally
        {
            OnDownloadCompleted?.Invoke(this, new DownloadCompltedArgs(item.FilePath));
            Directory.Delete(tempDir, true);
        }
    }

    private async Task InitInfoAsync(DownloadItem item, CancellationToken token)
    {
        _totalBytesReceived = 0;
        if (item.FileSize is not null)
            _totalBytes= item.FileSize.Value;
        else
            _totalBytes = await GetFileSizeAsync(item.Url, token);
    }

    private async Task InitInfoAsync(DownloadItem[] items, CancellationToken token)
    {
        foreach (var item in items)
        {
        if (item.FileSize.HasValue && item.FileSize.Value != 0)
            _totalBytes += item.FileSize.Value;
        else
            _totalBytes += await GetFileSizeAsync(item.Url, token);
    }
    }

    private void MergeChunks(string outputPath, string tempDir, int totalChunks)
    {
        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        for (var i = 0; i < totalChunks; i++)
        {
            var chunkFile = Path.Combine(tempDir, $"{i}.chunk");
            using var chunkStream = new FileStream(chunkFile, FileMode.Open, FileAccess.Read);
            chunkStream.CopyTo(outputStream);
        }
    }

    private async Task<long> GetFileSizeAsync(string url, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, url);
        var response = await _httpClient.SendAsync(request, token);
        if (response.IsSuccessStatusCode)
        {
            return response.Content.Headers.ContentLength ?? 0;
        }
        return 0;
    }

    private async Task OpenDownloadTask(DownloadItem item, CancellationToken token,long? start = null, long? end = null)
    {
        await _semaphore.WaitAsync(); // 等待信号量
        try
        {
            await InitDownloadTask(item).StartAsync(token ,start, end);
        }
        finally
        {
            _semaphore.Release(); // 释放信号量
        }
    }

    private DownloadTask InitDownloadTask(DownloadItem item)
    {
        var task = new DownloadTask(item.Url, item.FilePath, new());
        task.ProgressChanged += (s, BytesReceived) =>
        {
            Interlocked.Add(ref _totalBytesReceived, BytesReceived);
            OnDownloadProgressChanged?.Invoke(this, new(_totalBytes, _totalBytesReceived, _progressPercentage));
        };
        return task;
    }

    private void InitializeHttpClient()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("zh-CN,zh;q=0.9,en;q=0.8");
    }
}
