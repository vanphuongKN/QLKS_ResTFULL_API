using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;

namespace QLKSan.Mappings
{
    public static class CustomerMappings
    {
        public static CustomerSummaryDto ToSummaryDto(this KhachHang entity)
        {
            if (entity == null) return null;
            var contact = entity.LienHeDiaChis?.FirstOrDefault();
            return new CustomerSummaryDto
            {
                MaKhachHang = entity.MaKhachHang,
                Ho = entity.Ho,
                Ten = entity.Ten,
                CCCD_Passport = entity.CCCD_Passport,
                SDT = contact?.SDT,
                Email = contact?.Email
            };
        }

        public static CustomerDto ToDto(this KhachHang entity)
        {
            if (entity == null) return null;
            var dto = new CustomerDto();
            FillCustomerDto(entity, dto);
            return dto;
        }

        public static CustomerDetailDto ToDetailDto(this KhachHang entity)
        {
            if (entity == null) return null;
            var dto = new CustomerDetailDto();
            FillCustomerDto(entity, dto);
            dto.PhieuDatPhongs = entity.PhieuDatPhongs?
                .OrderByDescending(p => p.NgayDat)
                .Select(p => p.ToDto())
                .ToList() ?? new List<BookingDto>();
            return dto;
        }

        private static void FillCustomerDto(KhachHang entity, CustomerDto dto)
        {
            dto.MaKhachHang = entity.MaKhachHang;
            dto.MaDangNhap = entity.MaDangNhap;
            dto.Ho = entity.Ho;
            dto.Ten = entity.Ten;
            dto.NgaySinh = entity.NgaySinh;
            dto.CCCD_Passport = entity.CCCD_Passport;
            dto.DiemTichLuy = entity.DiemTichLuy;
            dto.SoLanDatPhong = entity.PhieuDatPhongs?.Count ?? 0;
            dto.LienHeDiaChis = entity.LienHeDiaChis?.Select(l => l.ToDto()).ToList() ?? new List<ContactDto>();
        }

        public static ContactDto ToDto(this LienHeDiaChi entity)
        {
            if (entity == null) return null;
            return new ContactDto
            {
                MaLienHe = entity.MaLienHe,
                SDT = entity.SDT,
                Email = entity.Email,
                DiaChi = entity.DiaChi,
                QuocGia = entity.QuocGia
            };
        }
    }
}
