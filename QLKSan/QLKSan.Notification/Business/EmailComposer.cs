using System.Text.Encodings.Web;
using System.Text.Unicode;
using QLKSan.Models.Events;
using QLKSan.Notification.Common;

namespace QLKSan.Notification.Business
{
    public record EmailMessage(string NguoiNhan, string TenNguoiNhan, string TieuDe, string NoiDungHtml);

    // Soạn email cho khách theo loại sự kiện phiếu đặt phòng; null = sự kiện không cần gửi email
    public static class EmailComposer
    {
        // Mã hoá HTML nhưng giữ nguyên chữ tiếng Việt (dễ đọc khi xem nguồn email)
        private static readonly HtmlEncoder Html = HtmlEncoder.Create(UnicodeRanges.All);

        public static EmailMessage Compose(BookingEvent su, EmailOptions options)
        {
            if (string.IsNullOrWhiteSpace(su.Email)) return null;
            var ten = Html.Encode(string.IsNullOrWhiteSpace(su.HoTenKhachHang) ? "Quý khách" : su.HoTenKhachHang);
            var ks = Html.Encode(options.TenKhachSan);

            (string TieuDe, string MoDau) = su.Loai switch
            {
                BookingEventTypes.DatPhongMoi => ($"Xác nhận đặt phòng {su.MaDatPhong} - {options.TenKhachSan}",
                    $"Cảm ơn {ten} đã chọn {ks}. Phiếu đặt phòng của quý khách đã được ghi nhận."),
                BookingEventTypes.NhanPhong => ($"Chào mừng quý khách đến {options.TenKhachSan}",
                    $"Chào mừng {ten} đã nhận phòng. Chúc quý khách một kỳ nghỉ trọn vẹn."),
                BookingEventTypes.HuyPhieu => ($"Phiếu đặt phòng {su.MaDatPhong} đã huỷ - {options.TenKhachSan}",
                    $"Phiếu đặt phòng của {ten} đã được huỷ."),
                _ => (null, null)
            };
            if (TieuDe == null) return null;

            var dong = new List<(string, string)>
            {
                ("Mã phiếu", su.MaDatPhong),
                ("Hạng phòng", su.TenLoaiPhong + (string.IsNullOrEmpty(su.SoPhong) ? "" : $" · phòng {su.SoPhong}")),
                ("Nhận phòng", su.NgayNhan.ToString("dd/MM/yyyy")),
                ("Trả phòng", su.NgayTra.ToString("dd/MM/yyyy")),
                ("Số khách", su.SoLuongKhach.ToString())
            };
            if (su.TienCoc > 0)
                dong.Add((su.Loai == BookingEventTypes.HuyPhieu ? "Tiền cọc giữ lại" : "Tiền cọc", su.TienCoc.ToString("N0") + " ₫"));
            if (!string.IsNullOrWhiteSpace(su.LyDo))
                dong.Add(("Lý do", su.LyDo));

            var bang = string.Concat(dong.Select(d =>
                $"<tr><td style=\"padding:6px 12px 6px 0;color:#777\">{Html.Encode(d.Item1)}</td><td style=\"padding:6px 0\"><strong>{Html.Encode(d.Item2)}</strong></td></tr>"));
            var lienHe = string.IsNullOrWhiteSpace(options.Hotline) ? "" : $" Hotline: {Html.Encode(options.Hotline)}.";
            var html = $@"<div style=""font-family:Segoe UI,Arial,sans-serif;color:#222;max-width:560px"">
<div style=""font-family:Georgia,serif;font-size:22px;letter-spacing:3px;color:#1f2a44;border-bottom:2px solid #c5a059;padding-bottom:8px"">{ks}</div>
<p style=""margin:18px 0"">{MoDau}</p>
<table style=""border-collapse:collapse;font-size:14px"">{bang}</table>
<p style=""margin:18px 0 0;color:#777;font-size:12px"">Email gửi tự động, vui lòng không trả lời.{lienHe}</p>
</div>";
            return new EmailMessage(su.Email.Trim(), su.HoTenKhachHang, TieuDe, html);
        }
    }
}
