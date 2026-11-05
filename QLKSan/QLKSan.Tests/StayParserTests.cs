using QLKSan.ChatBot.Business;

namespace QLKSan.Tests
{
    // Trợ lý ảo (RuleBased): đọc ngày nhận / trả phòng và số khách từ câu tiếng Việt có hoặc không dấu
    public class StayParserTests
    {
        // 01/11/2026 là Chủ nhật
        private static readonly DateTime HomNay = new(2026, 11, 1);

        [Theory]
        [InlineData("Còn phòng từ 20/11 đến 22/11 cho 2 người không?", "2026-11-20", "2026-11-22", 2)]
        [InlineData("con phong 20/11-22/11 khong", "2026-11-20", "2026-11-22", null)]
        [InlineData("đặt phòng 20 đến 22/11, 3 khách", "2026-11-20", "2026-11-22", 3)]
        [InlineData("phòng ngày 20/11/2026 - 22/11/2026", "2026-11-20", "2026-11-22", null)]
        [InlineData("ngày 5 tháng 12, hai người", "2026-12-05", "2026-12-06", 2)]
        [InlineData("ngày mai 2 đêm", "2026-11-02", "2026-11-04", null)]
        [InlineData("hôm nay một đêm, mình tôi đi một mình", "2026-11-01", "2026-11-02", 1)]
        [InlineData("cuối tuần này hai vợ chồng", "2026-11-07", "2026-11-08", 2)]
        [InlineData("30/12 - 02/01", "2026-12-30", "2027-01-02", null)]
        public void DocNgayVaSoKhach(string cau, string nhan, string tra, int? soNguoi)
        {
            var kq = StayParser.Parse(cau, HomNay);
            Assert.Equal(DateTime.Parse(nhan), kq.CheckIn);
            Assert.Equal(DateTime.Parse(tra), kq.CheckOut);
            Assert.Equal(soNguoi, kq.SoNguoi);
        }

        [Fact]
        public void NgayDaQuaKhongGhiNam_HieuLaNamSau()
        {
            var kq = StayParser.Parse("phòng ngày 15/10", HomNay);
            Assert.Equal(new DateTime(2027, 10, 15), kq.CheckIn);
        }

        [Theory]
        [InlineData("giờ nhận phòng mấy giờ?")]
        [InlineData("có dịch vụ spa không")]
        [InlineData("31/02")]
        public void KhongCoNgay(string cau)
        {
            Assert.False(StayParser.Parse(cau, HomNay).CoNgay);
        }

        [Fact]
        public void BoDauTiengViet()
        {
            Assert.Equal("con phong trong o da nang khong", StayParser.Normalize("  Còn phòng   TRỐNG ở Đà Nẵng không "));
        }
    }
}
