#if TOOLS
using System;

namespace GameLauncherCloud;

/// <summary>
/// Helper for multipart uploads matching CLI implementation.
/// </summary>
public static class GlcMultipartUploadHelper
{
    // Constants matching CLI implementation
    private const long MultipartThreshold = 500L * 1024 * 1024; // 500 MB
    private const long StandardPartSize = 500L * 1024 * 1024; // 500 MB parts
    private const int BufferSize = 81920; // 80 KB buffer (CLI standard)
    private const int MaxParts = 10000;

    /// <summary>
    /// Check if file should use multipart upload
    /// </summary>
    public static bool ShouldUseMultipart(long fileSize)
    {
        return fileSize > MultipartThreshold;
    }

    /// <summary>
    /// Calculate part size for multipart upload
    /// </summary>
    public static long CalculatePartSize(long fileSize)
    {
        if (fileSize <= StandardPartSize)
        {
            return fileSize;
        }

        var partSize = StandardPartSize;
        var partCount = (int)Math.Ceiling((double)fileSize / partSize);

        if (partCount > MaxParts)
        {
            throw new NotSupportedException(
                $"File is too large. With 500 MB parts and max {MaxParts} parts, " +
                $"maximum supported file size is {(StandardPartSize * MaxParts) / (1024L * 1024 * 1024)} GB. " +
                $"Your file would require {partCount} parts.");
        }

        return partSize;
    }

    /// <summary>
    /// Calculate total number of parts needed
    /// </summary>
    public static int CalculatePartCount(long fileSize, long partSize)
    {
        return (int)Math.Ceiling((double)fileSize / partSize);
    }

    /// <summary>
    /// Get buffer size for streaming
    /// </summary>
    public static int GetBufferSize()
    {
        return BufferSize;
    }

    /// <summary>
    /// Format file size for display
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        var order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
#endif
