using QLKSan.Business;
using QLKSan.Common;

namespace QLKSan.Tests
{
    // Kiểm thử tính tiền phòng theo ngày thực tế, phụ thu trả muộn, khuyến mãi và tiền cọc.
    // Chính sách chuẩn: trả phòng 12:00; trả muộn đến 15:00 = 30%, đến 18:00 = 50%, sau 18:00 = 100% giá 1 đêm.
    public class BillingCalculatorTests
    {
        private static readonly HotelPolicy Policy = HotelPolicy.MacDinh();
        private const decimal Gia = 500_000m;

        private static BillingResult Tinh(string nhan, string tra, decimal dichVu = 0, string loaiGiam = null, decimal giaTriGiam = 0, decimal coc = 0, decimal gia = Gia, int giamThanhVien = 0)
            => BillingCalculator.Calculate(new BillingInput
            {
                GiaMotDem = gia,
                NgayNhanPhong = DateTime.Parse(nhan),
                ThoiDiemTraPhong = DateTime.Parse(tra),
                TongTienDichVu = dichVu,
                LoaiGiam = loaiGiam,
                GiaTriGiam = giaTriGiam,
                PhanTramGiamThanhVien = giamThanhVien,
                TienCoc = coc
            }, Policy);

        [Fact]
        public void UuDaiThanhVien_GiamPhanTramTienPhong_KhongGiamDichVu()
        {
            // 2 đêm 1.000.000đ + dịch vụ 200.000đ, hạng Vàng giảm 5% tiền phòng = 50.000đ
            var r = Tinh("2026-10-18 14:00", "2026-10-20 11:00", dichVu: 200_000m, giamThanhVien: 5);

            Assert.Equal(50_000m, r.GiamGiaThanhVien);
            Assert.Equal(1_150_000m, r.TienTruocThue);
            Assert.Equal(1_242_000m, r.TongTien);
        }

        [Fact]
        public void UuDaiThanhVien_CongDonKhuyenMai_KhongLamTongAm()
        {
            // Khuyến mãi cố định 990.000đ trên 1.000.000đ tiền phòng: ưu đãi 10% chỉ còn trừ được 10.000đ
            var r = Tinh("2026-10-18 14:00", "2026-10-20 11:00", loaiGiam: "Cố định", giaTriGiam: 990_000m, giamThanhVien: 10);

            Assert.Equal(990_000m, r.GiamGia);
            Assert.Equal(10_000m, r.GiamGiaThanhVien);
            Assert.Equal(0m, r.TienTruocThue);
        }

        [Fact]
        public void TraDungHan_TinhTheoSoDemThucTe_KhongPhuThu()
        {
            var r = Tinh("2026-10-18 14:00", "2026-10-20 11:00");

            Assert.Equal(2, r.SoDem);
            Assert.Equal(1_000_000m, r.TienPhong);
            Assert.Equal(0m, r.PhuThuTraMuon);
            Assert.Equal(80_000m, r.ThueVAT);
            Assert.Equal(1_080_000m, r.TongTien);
        }

        [Fact]
        public void TraPhongSom_ChiTinhSoDemDaO()
        {
            // Đặt 3 đêm nhưng trả sau 1 đêm
            var r = Tinh("2026-10-18 14:00", "2026-10-19 10:00");

            Assert.Equal(1, r.SoDem);
            Assert.Equal(500_000m, r.TienPhong);
        }

        [Fact]
        public void NhanVaTraTrongNgay_TinhToiThieu1Dem_KhongPhuThu()
        {
            var r = Tinh("2026-10-18 08:00", "2026-10-18 20:00");

            Assert.Equal(1, r.SoDem);
            Assert.Equal(0m, r.PhuThuTraMuon);
        }

        [Theory]
        [InlineData("2026-10-20 12:00", 0)]    // đúng giờ trả phòng
        [InlineData("2026-10-20 13:30", 30)]   // trước 15:00
        [InlineData("2026-10-20 15:00", 30)]   // đúng mốc 15:00
        [InlineData("2026-10-20 16:00", 50)]   // trước 18:00
        [InlineData("2026-10-20 18:00", 50)]   // đúng mốc 18:00
        [InlineData("2026-10-20 18:30", 100)]  // sau 18:00 tính thêm 1 đêm
        public void TraMuon_PhuThuTheoMocGio(string thoiDiemTra, int phanTramMongDoi)
        {
            var r = Tinh("2026-10-18 14:00", thoiDiemTra);

            Assert.Equal(2, r.SoDem);
            Assert.Equal(phanTramMongDoi, r.PhanTramPhuThu);
            Assert.Equal(Gia * phanTramMongDoi / 100m, r.PhuThuTraMuon);
        }

        [Fact]
        public void KhuyenMaiPhanTram_ChiGiamTrenTienPhong()
        {
            // 5% của 1.000.000 tiền phòng = 50.000 (không giảm trên dịch vụ / phụ thu)
            var r = Tinh("2026-10-18 14:00", "2026-10-20 13:00", dichVu: 250_000m, loaiGiam: "Phần trăm", giaTriGiam: 5);

            Assert.Equal(50_000m, r.GiamGia);
            Assert.Equal(150_000m, r.PhuThuTraMuon);
            Assert.Equal(1_000_000m + 250_000m + 150_000m - 50_000m, r.TienTruocThue);
            Assert.Equal(r.TienTruocThue * 1.08m, r.TongTien);
        }

        [Fact]
        public void KhuyenMaiSoTien_KhongVuotQuaTongTien()
        {
            var r = Tinh("2026-10-18 14:00", "2026-10-19 10:00", loaiGiam: "Số tiền", giaTriGiam: 2_000_000m);

            Assert.Equal(500_000m, r.GiamGia);
            Assert.Equal(0m, r.TongTien);
        }

        [Fact]
        public void TienCoc_DuocTruVaoSoTienConLai()
        {
            // Khách vãng lai: phòng 2.500.000/đêm, cọc 50% = 1.250.000
            var r = Tinh("2026-10-17 15:00", "2026-10-18 11:00", coc: 1_250_000m, gia: 2_500_000m);

            Assert.Equal(2_700_000m, r.TongTien);
            Assert.Equal(1_250_000m, r.TienCoc);
            Assert.Equal(1_450_000m, r.SoTienConLai);
        }

        [Fact]
        public void TienCocNhieuHonTongTien_SoTienConLaiAm_DeHoanLaiKhach()
        {
            var r = Tinh("2026-10-17 15:00", "2026-10-18 11:00", coc: 1_000_000m);

            Assert.Equal(540_000m, r.TongTien);
            Assert.Equal(-460_000m, r.SoTienConLai);
        }

        [Fact]
        public void KhongCoMucPhuThuCauHinh_TraMuonTinhThem1Dem()
        {
            var policy = new HotelPolicy();  // không có mốc phụ thu nào
            Assert.Equal(100, BillingCalculator.TinhPhanTramPhuThu(DateTime.Parse("2026-10-18 14:00"), DateTime.Parse("2026-10-19 13:00"), policy));
        }
    }
}
