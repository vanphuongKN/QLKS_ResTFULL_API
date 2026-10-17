using System;
using System.Globalization;

namespace QLKSan.Helpers
{
    public static class FormatHelper
    {
        /// <summary>
        /// Định dạng số tiền tệ sang VNĐ (Ví dụ: 1000000 -> 1,000,000 VNĐ)
        /// </summary>
        public static string ToVND(decimal amount)
        {
            var culture = new CultureInfo("vi-VN");
            return string.Format(culture, "{0:N0} VNĐ", amount);
        }

        /// <summary>
        /// Định dạng ngày tháng thành chuỗi dễ đọc (Ví dụ: 25/05/2026 15:30)
        /// </summary>
        public static string FormatDateTime(DateTime date)
        {
            return date.ToString("dd/MM/yyyy HH:mm");
        }
        
        /// <summary>
        /// Rút gọn chuỗi nếu quá dài (Ví dụ: "Một chuỗi dài..." -> "Một chuỗi...")
        /// </summary>
        public static string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }
    }
}
