using QLKSan.Business;
using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;

namespace QLKSan.Mappings
{
    public static class PricingMappings
    {
        public static PricingRuleDto ToDto(this QuyTacGia entity)
        {
            if (entity == null) return null;
            return new PricingRuleDto
            {
                MaQuyTac = entity.MaQuyTac,
                TenQuyTac = entity.TenQuyTac,
                MaLoai = entity.MaLoai,
                TenLoai = entity.LoaiPhong?.TenLoai,
                TuNgay = entity.TuNgay,
                DenNgay = entity.DenNgay,
                CacThu = PricingCalculator.ParseThu(entity.CacThu),
                PhanTramDieuChinh = entity.PhanTramDieuChinh,
                UuTien = entity.UuTien,
                KichHoat = entity.KichHoat
            };
        }
    }
}
