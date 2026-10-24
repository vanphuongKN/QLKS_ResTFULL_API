using System.Collections.Generic;

namespace QLKSan.Models.DTOs
{
    // Số liệu cho trang Bảng điều khiển Quản lý (Admin/Index)
    public class DashboardDto
    {
        public decimal TongDoanhThu { get; set; }
        public int TongDatPhong { get; set; }
        public int TongKhachHang { get; set; }
        public List<decimal> DoanhThuTuan { get; set; } = new List<decimal>();
        public List<MonthlyRevenueDto> DoanhThuTheoThang { get; set; } = new List<MonthlyRevenueDto>();
        public List<PromotionDto> KhuyenMais { get; set; } = new List<PromotionDto>();
    }

    // Số liệu cho màn hình Buồng phòng (Housekeeping/Tasks)
    public class HousekeepingDto
    {
        public List<RoomDto> PhongCanDon { get; set; } = new List<RoomDto>();
        public int SoPhongDangDon { get; set; }
        public int SoPhongTrong { get; set; }
        public int SoPhongDangO { get; set; }
        public List<StaffDto> NhanVienBuongPhong { get; set; } = new List<StaffDto>();

        // Công việc dọn phòng chưa hoàn thành (chờ giao / đã giao / đang dọn / sự cố)
        public List<HousekeepingTaskDto> CongViec { get; set; } = new List<HousekeepingTaskDto>();
    }
}
