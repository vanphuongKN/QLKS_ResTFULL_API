using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace QLKSan.ChatBot.Business
{
    // Ngày ở và số khách đọc được từ câu hỏi; null = khách chưa nói
    public record StaySlots(DateTime? CheckIn, DateTime? CheckOut, int? SoNguoi)
    {
        public bool CoNgay => CheckIn != null && CheckOut != null;
    }

    // Đọc câu tiếng Việt (có hoặc không dấu): ngày nhận / trả phòng, số đêm, số khách — hàm thuần, kiểm thử được bằng unit test
    public static class StayParser
    {
        private static readonly Dictionary<string, int> So = new()
        {
            ["mot"] = 1, ["hai"] = 2, ["ba"] = 3, ["bon"] = 4, ["tu"] = 4, ["nam"] = 5, ["sau"] = 6, ["bay"] = 7
        };

        private const string SoChu = @"(\d{1,2}|mot|hai|ba|bon|tu|nam|sau|bay)";

        // Chữ thường, bỏ dấu tiếng Việt, gộp khoảng trắng
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var d = text.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (var c in d)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return Regex.Replace(sb.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ");
        }

        public static StaySlots Parse(string text, DateTime today)
        {
            var s = Normalize(text);
            today = today.Date;
            var ngay = new List<DateTime>();

            // "20 den 22/11", "20-22/11": hai ngày chung tháng
            // (số đầu không được là phần tháng của một ngày đầy đủ, vd "20/11-22/11")
            var khoang = Regex.Match(s, @"(?<![/.\-]\s*)\b(\d{1,2})\s*(?:-|den|toi)\s*(\d{1,2})\s*[/.](\d{1,2})(?:[/.](\d{4}))?\b");
            if (khoang.Success)
            {
                var thang = int.Parse(khoang.Groups[3].Value);
                int? nam = khoang.Groups[4].Success ? int.Parse(khoang.Groups[4].Value) : null;
                Add(ngay, int.Parse(khoang.Groups[1].Value), thang, nam, today);
                Add(ngay, int.Parse(khoang.Groups[2].Value), thang, nam, today);
            }
            else
            {
                // dd/MM, dd-MM, dd.MM, dd/MM/yyyy; "ngay 20 thang 11"
                foreach (Match m in Regex.Matches(s, @"\b(\d{1,2})\s*[/.\-]\s*(\d{1,2})(?:\s*[/.\-]\s*(\d{4}|\d{2})(?!\s*[/.]\d|\d))?\b|\bngay (\d{1,2}) thang (\d{1,2})(?: nam (\d{4}))?"))
                {
                    if (m.Groups[1].Success)
                        Add(ngay, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), m.Groups[3].Success ? Year(m.Groups[3].Value) : null, today);
                    else
                        Add(ngay, int.Parse(m.Groups[4].Value), int.Parse(m.Groups[5].Value), m.Groups[6].Success ? int.Parse(m.Groups[6].Value) : null, today);
                }
            }

            // Ngày tương đối
            if (ngay.Count == 0)
            {
                if (Regex.IsMatch(s, @"\bhom nay\b|\btoi nay\b|\bdem nay\b")) ngay.Add(today);
                else if (Regex.IsMatch(s, @"\bngay kia\b|\bngay mot\b")) ngay.Add(today.AddDays(2));
                else if (Regex.IsMatch(s, @"\b(ngay )?mai\b")) ngay.Add(today.AddDays(1));
                else if (Regex.IsMatch(s, @"\bcuoi tuan\b"))
                    ngay.Add(today.AddDays(((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7));
            }

            int? soDem = null;
            var dem = Regex.Match(s, $@"\b{SoChu}\s*dem\b");
            if (dem.Success) soDem = ToInt(dem.Groups[1].Value);

            int? soNguoi = null;
            var nguoi = Regex.Match(s, $@"\b{SoChu}\s*(?:nguoi|khach)\b");
            if (nguoi.Success) soNguoi = ToInt(nguoi.Groups[1].Value);
            else if (Regex.IsMatch(s, @"\bvo chong\b|\bcap doi\b|\bhai dua\b")) soNguoi = 2;
            else if (Regex.IsMatch(s, @"\bmot minh\b")) soNguoi = 1;

            DateTime? checkIn = ngay.Count > 0 ? ngay[0] : null;
            DateTime? checkOut = null;
            if (ngay.Count > 1)
            {
                checkOut = ngay[1];
                // "30/12 - 02/01": ngày trả sang năm sau
                while (checkOut <= checkIn) checkOut = checkOut.Value.AddYears(1);
            }
            else if (checkIn != null)
                checkOut = checkIn.Value.AddDays(soDem is > 0 ? soDem.Value : 1);

            return new StaySlots(checkIn, checkOut, soNguoi is > 0 and <= 20 ? soNguoi : null);
        }

        private static int? Year(string y) => y.Length == 2 ? 2000 + int.Parse(y) : int.Parse(y);

        private static int ToInt(string v) => int.TryParse(v, out var n) ? n : So[v];

        // Không ghi năm: ngày đã qua hiểu là năm sau
        private static void Add(List<DateTime> list, int day, int month, int? year, DateTime today)
        {
            if (month < 1 || month > 12 || day < 1) return;
            var y = year ?? today.Year;
            if (day > DateTime.DaysInMonth(y, month)) return;
            var date = new DateTime(y, month, day);
            if (year == null && date < today) date = date.AddYears(1);
            list.Add(date);
        }
    }
}
