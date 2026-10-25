using QLKSan.Enums;
using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;
using QLKSan.Models.Requests;

namespace QLKSan.Mappings
{
    public static class RoomMappings
    {
        public static RoomTypeDto ToDto(this LoaiPhong entity)
        {
            if (entity == null) return null;
            return new RoomTypeDto
            {
                MaLoai = entity.MaLoai,
                TenLoai = entity.TenLoai,
                SoNguoiToiDa = entity.SoNguoiToiDa,
                GiaThueNgay = entity.GiaThueNgay,
                GiaThueGio = entity.GiaThueGio,
                MoTa = entity.MoTa,
                DienTich = entity.DienTich,
                LoaiGiuong = entity.LoaiGiuong,
                HuongNhin = entity.HuongNhin,
                MoTaChiTiet = entity.MoTaChiTiet,
                AnhDaiDien = entity.AnhDaiDien
            };
        }

        public static AmenityDto ToDto(this TienIch entity)
            => new AmenityDto
            {
                MaTienIch = entity.MaTienIch,
                TenTienIch = entity.TenTienIch,
                Icon = entity.Icon,
                MaPhongs = entity.PhongTienIchs?.Select(p => p.MaPhong).OrderBy(p => p).ToList() ?? new List<string>()
            };

        public static RoomTypeImageDto ToDto(this HinhAnhLoaiPhong entity)
            => new RoomTypeImageDto { MaHinhAnh = entity.MaHinhAnh, DuongDan = entity.DuongDan, ThuTu = entity.ThuTu };

        public static RoomBriefDto ToBriefDto(this Phong entity)
        {
            if (entity == null) return null;
            return new RoomBriefDto
            {
                MaPhong = entity.MaPhong,
                MaLoai = entity.MaLoai,
                SoPhong = entity.SoPhong,
                TrangThai = entity.TrangThai,
                LoaiPhong = entity.LoaiPhong.ToDto()
            };
        }

        public static RoomDto ToDto(this Phong entity)
        {
            if (entity == null) return null;
            var dto = new RoomDto();
            FillRoomDto(entity, dto);
            return dto;
        }

        public static RoomDetailDto ToDetailDto(this Phong entity)
        {
            if (entity == null) return null;
            var dto = new RoomDetailDto();
            FillRoomDto(entity, dto);
            dto.TienIchs = entity.PhongTienIchs?
                .Where(pti => pti.TienIch != null)
                .Select(pti => new AmenityDto
                {
                    MaTienIch = pti.TienIch.MaTienIch,
                    TenTienIch = pti.TienIch.TenTienIch,
                    Icon = pti.TienIch.Icon
                })
                .ToList() ?? new List<AmenityDto>();
            return dto;
        }

        private static void FillRoomDto(Phong entity, RoomDto dto)
        {
            dto.MaPhong = entity.MaPhong;
            dto.MaLoai = entity.MaLoai;
            dto.SoPhong = entity.SoPhong;
            dto.TrangThai = entity.TrangThai;
            dto.LoaiPhong = entity.LoaiPhong.ToDto();
            dto.HinhAnhs = entity.HinhAnhs?
                .Select(h => new RoomImageDto { MaHinhAnh = h.MaHinhAnh, DuongDanAnh = h.DuongDanAnh })
                .ToList() ?? new List<RoomImageDto>();

            var bookings = entity.ChiTietDatPhongs?
                .Select(c => c.PhieuDatPhong)
                .Where(p => p != null)
                .ToList() ?? new List<PhieuDatPhong>();

            dto.DangLuuTru = bookings
                .FirstOrDefault(p => p.TrangThai == BookingStatus.CheckedIn)
                .ToSummaryDto();
            // Phiếu sắp đến gần nhất (phòng có thể có nhiều phiếu đặt cho các khoảng ngày khác nhau)
            dto.DatPhongHienTai = bookings
                .Where(p => p.TrangThai == BookingStatus.Approved || p.TrangThai == BookingStatus.Pending)
                .OrderBy(p => p.NgayCheckInDuKien)
                .FirstOrDefault()
                .ToSummaryDto();

            // TrangThai trả về là trạng thái hiển thị: phòng trống nhưng có khách nhận phòng hôm nay/ngày mai thì hiện "Đã đặt"
            if (entity.TrangThai == RoomStatus.Available && dto.DatPhongHienTai != null
                && dto.DatPhongHienTai.NgayCheckInDuKien.Date <= DateTime.Today.AddDays(1))
            {
                dto.TrangThai = RoomStatus.Booked;
            }
            dto.LanDatGanNhat = bookings
                .OrderByDescending(p => p.NgayCheckOutDuKien)
                .FirstOrDefault()
                .ToSummaryDto();
        }

        public static LoaiPhong ToEntity(this RoomTypeRequest request, string maLoai)
        {
            return new LoaiPhong
            {
                MaLoai = maLoai,
                TenLoai = request.TenLoai,
                SoNguoiToiDa = request.SoNguoiToiDa,
                GiaThueNgay = request.GiaThueNgay,
                GiaThueGio = request.GiaThueGio,
                MoTa = request.MoTa,
                DienTich = request.DienTich,
                LoaiGiuong = request.LoaiGiuong,
                HuongNhin = request.HuongNhin,
                MoTaChiTiet = request.MoTaChiTiet
            };
        }

        public static Phong ToEntity(this RoomRequest request, string maPhong)
        {
            return new Phong
            {
                MaPhong = maPhong,
                SoPhong = request.SoPhong,
                MaLoai = request.MaLoai
            };
        }
    }
}
