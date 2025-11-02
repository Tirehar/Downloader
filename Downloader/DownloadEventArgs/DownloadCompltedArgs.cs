using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Downloader.DownloadEventArgs;

public class DownloadCompltedArgs : EventArgs
{
    public string? FilePath { get; }
    public DownloadCompltedArgs(string filePath)
    {
        FilePath = filePath;
    }
}
