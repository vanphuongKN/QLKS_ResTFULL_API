using QLKSan.Models.Events;
using QLKSan.Notification.Business;
using QLKSan.Notification.Common;

namespace QLKSan.Tests
{
    // Service thông báo: soạn email theo loại sự kiện phiếu đặt phòng nhận từ RabbitMQ
    public class NotificationTests
    {
        private static readonly EmailOptions Options = new EmailOptions { TenKhachSan = "LUXE RESERVE", Hotline = "1900 1234" };

        private static BookingEvent SuKien(string loai, Action<BookingEvent> sua = null)
        {
            var su = new BookingEvent
            {
                MaSuKien = Guid.NewGuid(), Loai = loai, MaDatPhong = "PDP0000001", HoTenKhachHang = "Võ Văn A", Email = " khach@example.com ",
                TenLoaiPhong = "Deluxe", SoPhong = "P302", NgayNhan = new DateTime(2026, 11, 20), NgayTra = new DateTime(2026, 11, 22),
                SoLuongKhach = 2, TienCoc = 500_000
            };
            sua?.Invoke(su);
            return su;
        }

        [Fact]
        public void DatPhongMoi_TieuDeXacNhan_DuThongTinPhieu()
        {
            var email = EmailComposer.Compose(SuKien(BookingEventTypes.DatPhongMoi), Options);

            Assert.Equal("khach@example.com", email.NguoiNhan);
            Assert.Equal("Xác nhận đặt phòng PDP0000001 - LUXE RESERVE", email.TieuDe);
            foreach (var s in new[] { "Võ Văn A", "Deluxe · phòng P302", "20/11/2026", "22/11/2026", "Tiền cọc", "Hotline: 1900 1234" })
                Assert.Contains(s, email.NoiDungHtml);
        }

        [Fact]
        public void HuyPhieu_GhiLyDo_VaTienCocGiuLai()
        {
            var email = EmailComposer.Compose(SuKien(BookingEventTypes.HuyPhieu, s => s.LyDo = "Khách không đến"), Options);

            Assert.StartsWith("Phiếu đặt phòng PDP0000001 đã huỷ", email.TieuDe);
            Assert.Contains("Tiền cọc giữ lại", email.NoiDungHtml);
            Assert.Contains("Khách không đến", email.NoiDungHtml);
        }

        [Fact]
        public void NhanPhong_LoiChaoMung()
        {
            var email = EmailComposer.Compose(SuKien(BookingEventTypes.NhanPhong, s => s.TienCoc = 0), Options);

            Assert.Equal("Chào mừng quý khách đến LUXE RESERVE", email.TieuDe);
            Assert.DoesNotContain("Tiền cọc", email.NoiDungHtml);
        }

        [Fact]
        public void DuLieuKhach_DuocMaHoaHtml()
        {
            var email = EmailComposer.Compose(SuKien(BookingEventTypes.DatPhongMoi, s => { s.HoTenKhachHang = "<script>x</script>"; s.LyDo = "a & b"; }), Options);

            Assert.DoesNotContain("<script>", email.NoiDungHtml);
            Assert.Contains("&lt;script&gt;", email.NoiDungHtml);
            Assert.Contains("a &amp; b", email.NoiDungHtml);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(" ")]
        public void KhachKhongCoEmail_KhongGui(string email)
            => Assert.Null(EmailComposer.Compose(SuKien(BookingEventTypes.DatPhongMoi, s => s.Email = email), Options));

        [Fact]
        public void SuKienKhongCanEmail_KhongGui()
            => Assert.Null(EmailComposer.Compose(SuKien("booking.room-changed"), Options));
    }
}
