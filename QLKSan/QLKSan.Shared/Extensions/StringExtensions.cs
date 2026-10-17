using System.Text;
using System.Text.RegularExpressions;

namespace QLKSan.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Loại bỏ dấu tiếng Việt khỏi chuỗi (Ví dụ: "Nguyễn Văn A" -> "Nguyen Van A")
        /// Dùng để hỗ trợ chức năng tìm kiếm linh hoạt hơn
        /// </summary>
        public static string ToUnsignedString(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            string normalizedString = text.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            string result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
            // Replace đ
            result = result.Replace('đ', 'd').Replace('Đ', 'D');
            
            return result;
        }
        
        /// <summary>
        /// Kiểm tra chuỗi có chứa từ khóa (không phân biệt hoa thường và không dấu)
        /// </summary>
        public static bool ContainsIgnoreCaseAndAccents(this string source, string keyword)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(keyword))
                return false;
                
            return source.ToUnsignedString().ToLower().Contains(keyword.ToUnsignedString().ToLower());
        }
    }
}
