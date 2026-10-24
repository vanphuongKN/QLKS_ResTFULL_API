using QLKSan.Business;
using QLKSan.Enums;

namespace QLKSan.Tests
{
    // Quy tắc trùng lịch: khoảng ngày [nhận, trả) — ngày trả của khách trước có thể là ngày nhận của khách sau
    public class RoomAvailabilityTests
    {
        private static DateTime D(string s) => DateTime.Parse(s);

        [Theory]
        [InlineData("2026-10-18", "2026-10-20", "2026-10-20", "2026-10-22", false)] // nối tiếp nhau: không trùng
        [InlineData("2026-10-18", "2026-10-20", "2026-10-16", "2026-10-18", false)] // kết thúc đúng ngày bắt đầu
        [InlineData("2026-10-18", "2026-10-20", "2026-10-19", "2026-10-21", true)]  // trùng một phần
        [InlineData("2026-10-18", "2026-10-25", "2026-10-20", "2026-10-21", true)]  // nằm trong
        [InlineData("2026-10-20", "2026-10-21", "2026-10-18", "2026-10-25", true)]  // bao trùm
        [InlineData("2026-10-18", "2026-10-20", "2026-11-16", "2026-11-18", false)] // cách xa nhau
        public void Overlaps_TheoKhoangNgay(string aIn, string aOut, string bIn, string bOut, bool trungLich)
        {
            Assert.Equal(trungLich, RoomAvailabilityService.Overlaps(D(aIn), D(aOut), D(bIn), D(bOut)));
        }

        [Fact]
        public void Overlaps_BoQuaGioPhut_ChiSoSanhNgay()
        {
            Assert.False(RoomAvailabilityService.Overlaps(D("2026-10-18 14:00"), D("2026-10-20 12:00"), D("2026-10-20 08:00"), D("2026-10-21 12:00")));
        }

        [Fact]
        public void KhachDangO_QuaNgayTraDuKien_VanChiemPhongDenNgayMai()
        {
            var today = D("2026-10-17");
            var ngayTra = RoomAvailabilityService.NgayTraHieuLuc(BookingStatus.CheckedIn, D("2026-10-15"), today);

            Assert.Equal(D("2026-10-18"), ngayTra);
            Assert.True(RoomAvailabilityService.Overlaps(D("2026-10-12"), ngayTra, today, D("2026-10-18")));
        }

        [Fact]
        public void PhieuChuaNhanPhong_GiuNguyenNgayTraDuKien()
        {
            var ngayTra = RoomAvailabilityService.NgayTraHieuLuc(BookingStatus.Approved, D("2026-10-25"), D("2026-10-17"));
            Assert.Equal(D("2026-10-25"), ngayTra);
        }
    }
}
