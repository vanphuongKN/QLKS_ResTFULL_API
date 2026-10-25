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

        // Thông tin giới thiệu cho khách (trang chi tiết hạng phòng)
        public decimal? DienTich { get; set; }
        public string LoaiGiuong { get; set; }
        public string HuongNhin { get; set; }
        public string MoTaChiTiet { get; set; }

        // Ảnh đại diện; đường dẫn bắt đầu bằng /uploads/ là ảnh do quản trị tải lên (lưu ở API)
        public string AnhDaiDien { get; set; }

        // Điểm đánh giá trung bình của các phòng thuộc hạng (null = chưa có đánh giá)
        public double? DiemDanhGia { get; set; }
        public int SoDanhGia { get; set; }
    }

    public class RoomTypeImageDto
    {
        public string MaHinhAnh { get; set; }
        public string DuongDan { get; set; }
        public int ThuTu { get; set; }
    }

    // GET /api/room-types/{id}: hạng phòng kèm bộ ảnh, tiện nghi và đánh giá gần đây
    public class RoomTypeDetailDto : RoomTypeDto
    {
        public List<RoomTypeImageDto> HinhAnhs { get; set; } = new List<RoomTypeImageDto>();
        public List<AmenityDto> TienIchs { get; set; } = new List<AmenityDto>();
        public List<ReviewDto> DanhGiaGanDay { get; set; } = new List<ReviewDto>();
    }

    // GET /api/room-types/available: hạng phòng còn trống trong khoảng ngày kèm báo giá từng đêm
    public class RoomTypeAvailabilityDto
    {
        public RoomTypeDto LoaiPhong { get; set; }
        public int SoPhongTrong { get; set; }
        public PriceQuoteDto BaoGia { get; set; }
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

        // Các phòng đang có tiện nghi này (chỉ trả ở danh mục tiện nghi)
        public List<string> MaPhongs { get; set; } = new List<string>();
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
