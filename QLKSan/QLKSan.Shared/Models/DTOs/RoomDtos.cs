using System.Collections.Generic;
using QLKSan.Enums;

namespace QLKSan.Models.DTOs
{
    public class RoomTypeDto
    {
        public string MaLoai { get; set; }
        public string TenLoai { get; set; }
        public int SoNguoiToiDa { get; set; }
        public decimal GiaThueNgay { get; set; }
        public decimal GiaThueGio { get; set; }
        public string MoTa { get; set; }
    }

    public class RoomImageDto
    {
        public string MaHinhAnh { get; set; }
        public string DuongDanAnh { get; set; }
    }

    public class AmenityDto
    {
        public string MaTienIch { get; set; }
        public string TenTienIch { get; set; }
        public string Icon { get; set; }
    }

    // Thông tin phòng rút gọn, dùng khi lồng trong phiếu đặt phòng
    public class RoomBriefDto
    {
        public string MaPhong { get; set; }
        public string MaLoai { get; set; }
        public string SoPhong { get; set; }
        public RoomStatus TrangThai { get; set; }
        public RoomTypeDto LoaiPhong { get; set; }
    }

    // Dùng cho danh sách phòng (sơ đồ phòng, quản lý phòng, đặt phòng, buồng phòng)
    public class RoomDto : RoomBriefDto
    {
        public List<RoomImageDto> HinhAnhs { get; set; } = new List<RoomImageDto>();

        // Phiếu đang lưu trú (đã check-in) của phòng
        public BookingSummaryDto DangLuuTru { get; set; }

        // Phiếu đặt đang chờ duyệt/đã duyệt của phòng (API tính sẵn, View không phải tự lọc)
        public BookingSummaryDto DatPhongHienTai { get; set; }

        // Phiếu đặt có ngày check-out dự kiến muộn nhất (dùng cho màn hình buồng phòng)
        public BookingSummaryDto LanDatGanNhat { get; set; }

        // Chỉ có khi tra phòng trống theo khoảng ngày: tổng tiền phòng dự kiến (đã áp dụng giá mùa/cuối tuần)
        public decimal? TongTienDuKien { get; set; }
        public decimal? GiaTrungBinhDem { get; set; }
    }

    // Dùng cho trang chi tiết phòng
    public class RoomDetailDto : RoomDto
    {
        public List<AmenityDto> TienIchs { get; set; } = new List<AmenityDto>();
    }
}
