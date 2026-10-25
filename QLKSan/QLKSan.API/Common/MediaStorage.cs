using System;
using System.IO;

namespace QLKSan.Common
{
    // Thư mục lưu ảnh tải lên (wwwroot/uploads của QLKSan.API), phục vụ công khai tại /uploads/...
    public class MediaStorage
    {
        public const string UrlPrefix = "/uploads";

        public string RootPath { get; }

        public MediaStorage(string rootPath)
        {
            RootPath = Path.GetFullPath(rootPath);
            Directory.CreateDirectory(RootPath);
        }

        // Đường dẫn /uploads/... → file thật; null nếu không thuộc thư mục tải lên (chặn ../)
        public string ToPhysicalPath(string url)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith(UrlPrefix + "/", StringComparison.OrdinalIgnoreCase)) return null;
            var path = Path.GetFullPath(Path.Combine(RootPath, url.Substring(UrlPrefix.Length + 1)));
            return path.StartsWith(RootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? path : null;
        }
    }
}
