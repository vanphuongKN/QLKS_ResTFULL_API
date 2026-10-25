using QLKSan.Common;
using QLKSan.Models.DTOs;

namespace QLKSan.Mappings
{
    // Chuyển đổi giữa cấu hình nội bộ của API (Common) và hợp đồng dữ liệu dùng chung (Shared)
    public static class SettingsMappings
    {
        public static HotelInfoSettingsDto ToDto(this HotelInfo e) => new HotelInfoSettingsDto
        {
            Ten = e.Ten, MoTaNgan = e.MoTaNgan, DiaChi = e.DiaChi, Hotline = e.Hotline, Email = e.Email, BanDoUrl = e.BanDoUrl
        };

        public static HotelInfo ToEntity(this HotelInfoSettingsDto d) => new HotelInfo
        {
            Ten = d.Ten?.Trim(), MoTaNgan = d.MoTaNgan?.Trim(), DiaChi = d.DiaChi?.Trim(), Hotline = d.Hotline?.Trim(), Email = d.Email?.Trim(), BanDoUrl = d.BanDoUrl?.Trim()
        };

        public static HotelPolicySettingsDto ToDto(this HotelPolicy e) => new HotelPolicySettingsDto
        {
            GioNhanPhong = e.GioNhanPhong,
            GioTraPhong = e.GioTraPhong,
            PhuThuTraMuon = e.PhuThuTraMuon.OrderBy(m => m.DenGio).Select(m => new LateCheckoutRuleDto { DenGio = m.DenGio, PhanTram = m.PhanTram }).ToList(),
            TuDongXacNhanDatPhong = e.TuDongXacNhanDatPhong,
            PhanTramTienCocVangLai = e.PhanTramTienCocVangLai
        };

        public static HotelPolicy ToEntity(this HotelPolicySettingsDto d) => new HotelPolicy
        {
            GioNhanPhong = d.GioNhanPhong,
            GioTraPhong = d.GioTraPhong,
            PhuThuTraMuon = (d.PhuThuTraMuon ?? new()).Where(m => m.DenGio > 0).Select(m => new MucPhuThu { DenGio = m.DenGio, PhanTram = m.PhanTram }).ToList(),
            TuDongXacNhanDatPhong = d.TuDongXacNhanDatPhong,
            PhanTramTienCocVangLai = d.PhanTramTienCocVangLai
        };

        public static LoyaltySettingsDto ToDto(this LoyaltyPolicy e) => new LoyaltySettingsDto
        {
            SoTienMotDiem = e.SoTienMotDiem,
            GiaTriMotDiem = e.GiaTriMotDiem,
            DiemDoiToiThieu = e.DiemDoiToiThieu,
            Hangs = e.Hangs.OrderBy(h => h.DiemToiThieu)
                .Select(h => new MembershipTierDto { Ten = h.Ten, DiemToiThieu = h.DiemToiThieu, PhanTramGiamPhong = h.PhanTramGiamPhong, QuyenLoi = h.QuyenLoi.ToList() })
                .ToList()
        };

        public static LoyaltyPolicy ToEntity(this LoyaltySettingsDto d) => new LoyaltyPolicy
        {
            SoTienMotDiem = d.SoTienMotDiem,
            GiaTriMotDiem = d.GiaTriMotDiem,
            DiemDoiToiThieu = d.DiemDoiToiThieu,
            Hangs = (d.Hangs ?? new())
                .Select(h => new HangThanhVien { Ten = h.Ten, DiemToiThieu = h.DiemToiThieu, PhanTramGiamPhong = h.PhanTramGiamPhong, QuyenLoi = h.QuyenLoi ?? new() })
                .ToList()
        };
    }
}
