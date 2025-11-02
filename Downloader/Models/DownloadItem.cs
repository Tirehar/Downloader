using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Downloader.Models;

public class DownloadItem
{
    public string Url { get; set;}
    public string FilePath { get; set;}
    public long? FileSize { get; set;
    }
}
