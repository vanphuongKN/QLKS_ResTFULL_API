using System;
using System.Collections.Generic;
using System.Linq;
using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    // Tính giá từng đêm theo quy tắc giá (mùa, cuối tuần, lễ) — hàm thuần, kiểm thử được bằng unit test.
    // Mỗi đêm chỉ áp dụng 1 quy tắc: quy tắc khớp có độ ưu tiên cao nhất (vd: Lễ ưu tiên hơn Cuối tuần).
    public static class PricingCalculator
    {
        public static List<int> ParseThu(string cacThu)
            => string.IsNullOrWhiteSpace(cacThu)
                ? new List<int>()
                : cacThu.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(int.Parse).Where(d => d >= 0 && d <= 6).Distinct().OrderBy(d => d).ToList();

        public static string FormatThu(IEnumerable<int> cacThu)
        {
            var list = (cacThu ?? Enumerable.Empty<int>()).Where(d => d >= 0 && d <= 6).Distinct().OrderBy(d => d).ToList();
            return list.Count == 0 ? null : string.Join(",", list);
        }

        public static bool AppliesTo(QuyTacGia rule, string maLoai, DateTime dem)
        {
            if (!rule.KichHoat) return false;
            if (!string.IsNullOrEmpty(rule.MaLoai) && rule.MaLoai != maLoai) return false;
            if (rule.TuNgay != null && dem.Date < rule.TuNgay.Value.Date) return false;
            if (rule.DenNgay != null && dem.Date > rule.DenNgay.Value.Date) return false;
            var thu = ParseThu(rule.CacThu);
            return thu.Count == 0 || thu.Contains((int)dem.DayOfWeek);
        }

        // Giá các đêm bắt đầu từ ngày nhận phòng (đêm của ngày d = ở từ ngày d sang ngày d+1)
        public static List<NightPriceDto> PriceNights(decimal giaGoc, string maLoai, DateTime ngayNhan, int soDem, IEnumerable<QuyTacGia> rules)
        {
            var ruleList = rules?.ToList() ?? new List<QuyTacGia>();
            var result = new List<NightPriceDto>();
            for (int i = 0; i < soDem; i++)
            {
                var dem = ngayNhan.Date.AddDays(i);
                var rule = ruleList
                    .Where(r => AppliesTo(r, maLoai, dem))
                    .OrderByDescending(r => r.UuTien)
                    .ThenByDescending(r => Math.Abs(r.PhanTramDieuChinh))
                    .ThenBy(r => r.MaQuyTac)
                    .FirstOrDefault();
                var phanTram = rule?.PhanTramDieuChinh ?? 0;
                result.Add(new NightPriceDto
                {
                    Dem = dem,
                    Gia = Math.Round(giaGoc * (100 + phanTram) / 100m, 0, MidpointRounding.AwayFromZero),
                    QuyTac = rule?.TenQuyTac,
                    PhanTramDieuChinh = phanTram
                });
            }
            return result;
        }
    }
}
