using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Downloader.Services;

internal class DownloadTask
{
    private readonly string _url;
    private readonly string _filePath;
    private readonly HttpClient _httpClient;

    public event EventHandler<long>? ProgressChanged;

    public DownloadTask(string url, string filePath, HttpClient httpClient)
    {
        _url = url;
        _filePath = filePath;
        _httpClient = httpClient;
    }

    public async Task StartAsync(CancellationToken token, long? start = null, long? end = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _url);
        if(start.HasValue && end.HasValue)
            request.Headers.Range = new RangeHeaderValue(start, end);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        await ProcessDownloadAsync(response, token);
    }

    private async Task ProcessDownloadAsync(HttpResponseMessage response, CancellationToken token)
    {
        response.EnsureSuccessStatusCode();
        await using var contentStream = await response.Content.ReadAsStreamAsync(token);
        await using var fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write);

        var buffer = new byte[8192];
        long totalBytesRead = 0;
        int bytesRead;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while ((bytesRead = await contentStream.ReadAsync(buffer, token)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
            totalBytesRead += bytesRead;
            
            if(sw.ElapsedMilliseconds >= 500)
            {
                ProgressChanged?.Invoke(this, totalBytesRead);
                totalBytesRead = 0;
                sw.Restart();
            }
        }  
        ProgressChanged?.Invoke(this, totalBytesRead);
        Console.WriteLine($"下载完成{_filePath}");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
