using EmbedIO;
using HeyRed.Mime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanmakuWidgetServer.Server.Utils
{
    internal class ServerUtils
    {
        internal static async Task SendStaticFile(IHttpContext httpContext, string filePath)
        {
            string etag = "";
            string lastModified = "";
            try
            {
                var fileInfo = new FileInfo(filePath);
                etag = await CreateETag(fileInfo);
                lastModified = fileInfo.LastWriteTimeUtc.ToString("R");

                var ifNoneMatch = httpContext.Request.Headers["If-None-Match"];
                if (ifNoneMatch == etag)
                {
                    httpContext.Response.Headers.Add("ETag", etag);
                    httpContext.Response.Headers.Add("Last-Modified", lastModified);
                    httpContext.Response.StatusCode = 304; // Not Modified
                    return;
                }
            }
            catch (FileNotFoundException)
            {
                httpContext.Response.StatusCode = 404;
                await httpContext.SendStringAsync("File Not Found", "text/plain", Encoding.UTF8);
                return;
            }
            catch (UnauthorizedAccessException)
            {
                httpContext.Response.StatusCode = 403;
                await httpContext.SendStringAsync("Access Denied", "text/plain", Encoding.UTF8);
                return;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[WidgetServer] Error creating ETag: " + ex.Message);
            }

            var response = httpContext.Response;
            FileStream inStream = null;
            try
            {
                inStream = File.OpenRead(filePath);
                var fileLength = inStream.Length;

                var contentType = MimeTypesMap.GetMimeType(filePath);
                response.ContentType = contentType;
                response.Headers.Add("ETag", etag);
                response.Headers.Add("Last-Modified", lastModified);

                // Check for Range header
                var rangeHeader = httpContext.Request.Headers["Range"];

                if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes="))
                {
                    // Parse Range header
                    var range = ParseRangeHeader(rangeHeader, fileLength);

                    if (range.HasValue)
                    {
                        var (start, end) = range.Value;
                        var contentLength = end - start + 1;

                        // Set 206 Partial Content response
                        response.StatusCode = 206;
                        response.Headers.Add("Accept-Ranges", "bytes");
                        response.Headers.Add("Content-Range", $"bytes {start}-{end}/{fileLength}");
                        response.ContentLength64 = contentLength;
                        response.Headers.Add("Cache-Control", "public, max-age=3600");

                        // Seek to start position
                        inStream.Seek(start, SeekOrigin.Begin);

                        // Copy the requested range
                        await CopyStreamRangeAsync(inStream, response.OutputStream, contentLength);
                        await response.OutputStream.FlushAsync();
                    }
                    else
                    {
                        // Invalid range
                        response.StatusCode = 416; // Range Not Satisfiable
                        response.Headers.Add("Content-Range", $"bytes */{fileLength}");
                        return;
                    }
                }
                else
                {
                    // No Range header, return full content
                    response.StatusCode = 200;
                    response.Headers.Add("Accept-Ranges", "bytes");
                    response.Headers.Add("Cache-Control", "public, max-age=36000");
                    response.ContentLength64 = fileLength;

                    await inStream.CopyToAsync(response.OutputStream);
                    await response.OutputStream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[WidgetServer] Error sending resource: " + ex.Message);

                try
                {
                    response.StatusCode = 500;
                    response.ContentType = "text/plain";
                    await httpContext.SendStringAsync("Error reading resource: " + ex.Message, "text/plain", Encoding.UTF8);
                }
                catch (Exception exx)
                {
                    Console.Error.WriteLine("[WidgetServer] Error sending error response: " + exx.Message);
                }
            }
            finally
            {
                inStream?.Dispose();
            }
        }

        /// <summary>
        /// Parse Range header value
        /// </summary>
        /// <param name="rangeHeader">Range header value (e.g., "bytes=0-1023")</param>
        /// <param name="fileLength">Total file length</param>
        /// <returns>Tuple of (start, end) positions, or null if invalid</returns>
        public static (long start, long end)? ParseRangeHeader(string rangeHeader, long fileLength)
        {
            try
            {
                // Remove "bytes=" prefix
                var rangeValue = rangeHeader.Substring(6);

                // Handle multiple ranges (take first one only)
                if (rangeValue.Contains(','))
                {
                    rangeValue = rangeValue.Split(',')[0].Trim();
                }

                var parts = rangeValue.Split('-');

                if (parts.Length != 2)
                {
                    return null;
                }

                long start, end;

                if (string.IsNullOrEmpty(parts[0]))
                {
                    // Suffix range: bytes=-500 (last 500 bytes)
                    if (long.TryParse(parts[1], out var suffixLength))
                    {
                        start = Math.Max(0, fileLength - suffixLength);
                        end = fileLength - 1;
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (string.IsNullOrEmpty(parts[1]))
                {
                    // Open-ended range: bytes=500- (from 500 to end)
                    if (long.TryParse(parts[0], out start))
                    {
                        end = fileLength - 1;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    // Normal range: bytes=0-1023
                    if (!long.TryParse(parts[0], out start) || !long.TryParse(parts[1], out end))
                    {
                        return null;
                    }
                }

                // Validate range
                if (start < 0 || start >= fileLength || end < start || end >= fileLength)
                {
                    return null;
                }

                return (start, end);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Copy a specific range of bytes from input stream to output stream
        /// </summary>
        public static async Task CopyStreamRangeAsync(Stream input, Stream output, long length)
        {
            const int bufferSize = 81920; // 80KB buffer
            var buffer = new byte[bufferSize];
            long totalRead = 0;

            while (totalRead < length)
            {
                var toRead = (int)Math.Min(bufferSize, length - totalRead);
                var read = await input.ReadAsync(buffer, 0, toRead);

                if (read == 0)
                {
                    break; // End of stream
                }

                await output.WriteAsync(buffer, 0, read);
                totalRead += read;
            }
        }

        public static async Task<string> CreateETag(FileInfo fileInfo)
        {
            var lastModified = fileInfo.LastWriteTimeUtc.ToString("yyyyMMddHHmmss");
            var fileSize = fileInfo.Length.ToString();
            var eTag = $"\"{Convert.ToBase64String(Encoding.UTF8.GetBytes(lastModified + fileSize))}\"";
            return eTag;
        }

        internal static async Task SendDirectoryListing(IHttpContext httpContext, string absolutePath, string resourcePath)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(absolutePath);

                if (!directoryInfo.Exists)
                {
                    httpContext.Response.StatusCode = 404;
                    await httpContext.SendStringAsync("Directory Not Found", "text/plain", Encoding.UTF8);
                    return;
                }

                // Check if we have permission to read the directory
                try
                {
                    Directory.GetFileSystemEntries(absolutePath);
                }
                catch (UnauthorizedAccessException)
                {
                    httpContext.Response.StatusCode = 403;
                    await httpContext.SendStringAsync("Access Denied", "text/plain", Encoding.UTF8);
                    return;
                }

                var html = new StringBuilder();
                var displayPath = string.IsNullOrEmpty(resourcePath) ? "/" : resourcePath;

                // Build HTML header
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html lang=\"en\">");
                html.AppendLine("<head>");
                html.AppendLine("<meta charset=\"UTF-8\">");
                html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                html.AppendLine($"<title>Index of {EscapeHtml(displayPath)}</title>");
                html.AppendLine("<style>");
                html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
                html.AppendLine("h1 { color: #333; border-bottom: 1px solid #ddd; padding-bottom: 10px; }");
                html.AppendLine("table { border-collapse: collapse; width: 100%; }");
                html.AppendLine("th, td { text-align: left; padding: 8px; border-bottom: 1px solid #ddd; }");
                html.AppendLine("th { background-color: #f5f5f5; font-weight: bold; }");
                html.AppendLine("tr:hover { background-color: #f9f9f9; }");
                html.AppendLine("a { color: #0066cc; text-decoration: none; }");
                html.AppendLine("a:hover { text-decoration: underline; }");
                html.AppendLine(".icon { margin-right: 5px; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");
                html.AppendLine($"<h1>Index of {EscapeHtml(displayPath)}</h1>");

                // Build parent directory link
                if (displayPath != "/")
                {
                    html.AppendLine("<p><a href=\"../\">&larr; Parent Directory</a></p>");
                }

                html.AppendLine("<table>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th>Name</th>");
                html.AppendLine("<th>Type</th>");
                html.AppendLine("<th>Size</th>");
                html.AppendLine("<th>Modified</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");

                // Get all entries
                FileSystemInfo[] entries = new FileSystemInfo[0];
                try
                {
                    var dirEntries = Directory.GetFileSystemEntries(absolutePath);
                    var entryList = new List<FileSystemInfo>();

                    foreach (var entryPath in dirEntries)
                    {
                        try
                        {
                            var attr = File.GetAttributes(entryPath);
                            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                            {
                                entryList.Add(new DirectoryInfo(entryPath));
                            }
                            else
                            {
                                entryList.Add(new FileInfo(entryPath));
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Skip entries we don't have access to
                        }
                    }

                    entries = entryList
                        .OrderByDescending(x => x is DirectoryInfo)
                        .ThenBy(x => x.Name)
                        .ToArray();
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip entries if access is denied
                }

                // Add directory entries
                foreach (var entry in entries)
                {
                    try
                    {
                        if (entry is DirectoryInfo dirInfo)
                        {
                            var encodedName = Uri.EscapeDataString(entry.Name);
                            html.AppendLine("<tr>");
                            html.AppendLine($"<td><span class=\"icon\">📁</span><a href=\"{encodedName}/\">{EscapeHtml(entry.Name)}</a></td>");
                            html.AppendLine("<td>Directory</td>");
                            html.AppendLine("<td>-</td>");
                            html.AppendLine($"<td>{entry.LastWriteTime:yyyy-MM-dd HH:mm:ss}</td>");
                            html.AppendLine("</tr>");
                        }
                        else if (entry is FileInfo fileInfo)
                        {
                            var encodedName = Uri.EscapeDataString(entry.Name);
                            var sizeStr = FormatFileSize(fileInfo.Length);
                            html.AppendLine("<tr>");
                            html.AppendLine($"<td><span class=\"icon\">📄</span><a href=\"{encodedName}\">{EscapeHtml(entry.Name)}</a></td>");
                            html.AppendLine("<td>File</td>");
                            html.AppendLine($"<td>{sizeStr}</td>");
                            html.AppendLine($"<td>{entry.LastWriteTime:yyyy-MM-dd HH:mm:ss}</td>");
                            html.AppendLine("</tr>");
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip this entry if we don't have permission
                    }
                }

                html.AppendLine("</tbody>");
                html.AppendLine("</table>");
                html.AppendLine("</body>");
                html.AppendLine("</html>");

                httpContext.Response.StatusCode = 200;
                httpContext.Response.ContentType = "text/html";
                await httpContext.SendStringAsync(html.ToString(), "text/html", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[WidgetServer] Error sending directory listing: " + ex.Message);

                try
                {
                    httpContext.Response.StatusCode = 500;
                    httpContext.Response.ContentType = "text/plain";
                    await httpContext.SendStringAsync("Error listing directory: " + ex.Message, "text/plain", Encoding.UTF8);
                }
                catch (Exception exx)
                {
                    Console.Error.WriteLine("[WidgetServer] Error sending error response: " + exx.Message);
                }
            }
        }

        /// <summary>
        /// Escape HTML special characters
        /// </summary>
        private static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        /// <summary>
        /// Format file size to human-readable format
        /// </summary>
        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
