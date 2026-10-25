using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Models.Entities;

namespace QLKSan.Tests
{
    // Giá theo mùa / cuối tuần / lễ: mỗi đêm áp dụng quy tắc khớp có độ ưu tiên cao nhất
    public class PricingCalculatorTests
    {
        private const decimal Gia = 500_000m;

        // 16/10/2026 là thứ Sáu, 17/10 thứ Bảy, 18/10 Chủ nhật
        private static readonly QuyTacGia CuoiTuan = new() { MaQuyTac = "QT001", TenQuyTac = "Cuối tuần", CacThu = "5,6", PhanTramDieuChinh = 20, UuTien = 1, KichHoat = true };
        private static readonly QuyTacGia Le = new()
        {
            MaQuyTac = "QT002", TenQuyTac = "Lễ", TuNgay = new DateTime(2026, 10, 17), DenNgay = new DateTime(2026, 10, 18),
            PhanTramDieuChinh = 50, UuTien = 5, KichHoat = true
        };

        [Fact]
        public void KhongCoQuyTac_MoiDemGiaGoc()
        {
            var nights = PricingCalculator.PriceNights(Gia, "LP01", new DateTime(2026, 10, 15), 3, Array.Empty<QuyTacGia>());
            Assert.All(nights, n => Assert.Equal(Gia, n.Gia));
        }

        [Fact]
        public void CuoiTuan_ChiTangDemThuSauVaThuBay()
        {
            // Đêm 15 (thứ Năm), 16 (thứ Sáu), 17 (thứ Bảy), 18 (Chủ nhật)
            var nights = PricingCalculator.PriceNights(Gia, "LP01", new DateTime(2026, 10, 15), 4, new[] { CuoiTuan });

            Assert.Equal(new[] { 500_000m, 600_000m, 600_000m, 500_000m }, nights.Select(n => n.Gia));
            Assert.Equal("Cuối tuần", nights[1].QuyTac);
            Assert.Null(nights[0].QuyTac);
        }

        [Fact]
        public void HaiQuyTacCungKhop_LayQuyTacUuTienCaoHon()
        {
            // Đêm 17 vừa là thứ Bảy vừa là Lễ → Lễ (ưu tiên 5) thắng Cuối tuần (ưu tiên 1)
            var nights = PricingCalculator.PriceNights(Gia, "LP01", new DateTime(2026, 10, 16), 3, new[] { CuoiTuan, Le });

            Assert.Equal(600_000m, nights[0].Gia);   // thứ Sáu: cuối tuần
            Assert.Equal(750_000m, nights[1].Gia);   // thứ Bảy + Lễ: lễ
            Assert.Equal("Lễ", nights[1].QuyTac);
            Assert.Equal(750_000m, nights[2].Gia);   // Chủ nhật trong dịp lễ
        }

        [Fact]
        public void QuyTacTheoLoaiPhong_KhongApDungChoLoaiKhac()
        {
            var suite = new QuyTacGia { MaQuyTac = "QT003", TenQuyTac = "Suite cao điểm", MaLoai = "LP04", PhanTramDieuChinh = 30, UuTien = 1, KichHoat = true };

            Assert.Equal(650_000m, PricingCalculator.PriceNights(Gia, "LP04", new DateTime(2026, 10, 14), 1, new[] { suite })[0].Gia);
            Assert.Equal(500_000m, PricingCalculator.PriceNights(Gia, "LP01", new DateTime(2026, 10, 14), 1, new[] { suite })[0].Gia);
        }

        [Fact]
        public void QuyTacBiTat_HoacGiamGia_ApDungDung()
        {
            var tat = new QuyTacGia { MaQuyTac = "QT004", TenQuyTac = "Đã tắt", PhanTramDieuChinh = 100, UuTien = 9, KichHoat = false };
            var thapDiem = new QuyTacGia { MaQuyTac = "QT005", TenQuyTac = "Thấp điểm", PhanTramDieuChinh = -10, UuTien = 1, KichHoat = true };

            var gia = PricingCalculator.PriceNights(Gia, "LP01", new DateTime(2026, 10, 14), 1, new[] { tat, thapDiem })[0];
            Assert.Equal(450_000m, gia.Gia);
            Assert.Equal("Thấp điểm", gia.QuyTac);
        }

        [Fact]
        public void ChuoiThu_DocVaGhiChuanHoa()
        {
            Assert.Equal(new List<int> { 5, 6 }, PricingCalculator.ParseThu(" 6,5 ,6"));
            Assert.Equal("0,6", PricingCalculator.FormatThu(new[] { 6, 0, 9 }));
            Assert.Null(PricingCalculator.FormatThu(Array.Empty<int>()));
        }

        [Fact]
        public void TinhTien_DungGiaTungDem_PhuThuTheoGiaGoc()
        {
            // Ở 2 đêm thứ Sáu + thứ Bảy (600k mỗi đêm), trả phòng Chủ nhật 13:00 → phụ thu 30% giá gốc
            var result = BillingCalculator.Calculate(new BillingInput
            {
                GiaMotDem = Gia,
                GiaTungDem = new[] { 600_000m, 600_000m },
                NgayNhanPhong = new DateTime(2026, 10, 16, 14, 0, 0),
                ThoiDiemTraPhong = new DateTime(2026, 10, 18, 13, 0, 0)
            }, HotelPolicy.MacDinh());

            Assert.Equal(2, result.SoDem);
            Assert.Equal(1_200_000m, result.TienPhong);
            Assert.Equal(150_000m, result.PhuThuTraMuon);
        }
    }
}
