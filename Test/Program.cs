using System.Diagnostics;
using Downloader.Models;
using Downloader.Services;

//var items = new List<DownloadItem>();
//for (var i = 0; i < 256; i++)
//{
//    items.Add(new() { Url = @"https://www.baidu.com/img/PCtm_d9c8750bed0b3c7d089fa7d55720d6cf.png", FilePath = @$"D:\DownloadTest\{i}.png" });
//}
var item = new DownloadItem
{
    Url = @"https://pvz2apk-cdn.ditwan.cn/1720/baokai_3.8.7_1720_350_dj2.0-2.0.0.apk",
    FilePath = @"D:\DownloadTest\pvz.apk",
};
var downloader = new DownloadService();
var core = downloader.InitCore(item, 8);
var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(10));

core.OnDownloadProgressChanged += (s, e) =>
{
    Console.WriteLine($"已下载 {e.BytesReceived}/{e.TotalBytes} 字节，进度：{e.ProgressPercentage:F2}%");
};

Console.WriteLine("开始下载");
var sw = Stopwatch.StartNew();
await downloader.FileDownloadAsync(core,cts.Token);
Console.WriteLine($"下载完成,用时{sw.Elapsed.TotalSeconds}秒");
