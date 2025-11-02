using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Downloader.DownloadEventArgs;

public class DownloadProgressChangedArgs : EventArgs
{
    public long? TotalBytes { get; }
    public long BytesReceived { get; }
    public double ProgressPercentage { get; }
    public DownloadProgressChangedArgs(long? totalBytes, long bytesReceived, double progressPercentage)
    {
        TotalBytes = totalBytes;
        BytesReceived = bytesReceived;
        ProgressPercentage = progressPercentage;
    }
}
