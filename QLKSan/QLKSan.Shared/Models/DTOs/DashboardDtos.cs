using System;
using System.Collections.Generic;

namespace QLKSan.Models.DTOs
{
    // GET /api/dashboard: bảng điều khiển điều hành — hôm nay, 30 ngày qua (so với 30 ngày trước), dự báo 14 ngày, việc cần xử lý
    public class DashboardDto
    {
        public DateTime NgayCapNhat { get; set; }
        public int TongSoPhong { get; set; }

        // ===== Hôm nay =====
        public int PhongCoKhach { get; set; }
        public decimal CongSuatHomNay { get; set; }
        public int KhachDangLuuTru { get; set; }
        public int SoKhachDen { get; set; }
        public int SoKhachDenDaNhan { get; set; }
        public int SoKhachDi { get; set; }
        public int SoKhachDiDaTra { get; set; }
        public List<DashboardStayDto> KhachDen { get; set; } = new List<DashboardStayDto>();
        public List<DashboardStayDto> KhachDi { get; set; } = new List<DashboardStayDto>();

        // ===== 30 ngày qua và kỳ trước liền kề =====
        public DashboardPeriodDto KyNay { get; set; } = new DashboardPeriodDto();
        public DashboardPeriodDto KyTruoc { get; set; } = new DashboardPeriodDto();

        // Công suất đã đặt (gồm phiếu chờ duyệt / đã duyệt / đang ở) cho 14 ngày tới
        public List<OccupancyByDayDto> DuBao14Ngay { get; set; } = new List<OccupancyByDayDto>();

        // Doanh thu thuần từng tháng của năm hiện tại
        public List<DashboardMonthDto> DoanhThuTheoThang { get; set; } = new List<DashboardMonthDto>();

        // ===== Việc cần xử lý =====
        public int PhieuChoDuyet { get; set; }
        public int PhongBaoTri { get; set; }
        public int PhongDangDon { get; set; }
        public int ViecBuongPhongChuaXong { get; set; }
        public int SuCoBuongPhong { get; set; }
        public List<PromotionDto> KhuyenMaiSapHetHan { get; set; } = new List<PromotionDto>();

        // ===== Đánh giá =====
        public double? DiemDanhGiaTrungBinh { get; set; }
        public int TongSoDanhGia { get; set; }
        public List<ReviewDto> DanhGiaMoi { get; set; } = new List<ReviewDto>();
    }

    public class DashboardPeriodDto
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public decimal DoanhThuThuan { get; set; }
        public decimal TienPhong { get; set; }
        public int DemPhongDaBan { get; set; }
        public decimal CongSuat { get; set; }

        // ADR: tiền phòng / đêm phòng đã bán; RevPAR: tiền phòng / (số phòng × số ngày)
        public decimal ADR { get; set; }
        public decimal RevPAR { get; set; }
    }

    public class DashboardStayDto
    {
        public string MaDatPhong { get; set; }
        public string HoTenKhachHang { get; set; }
        public string SoPhong { get; set; }
        public string TenLoaiPhong { get; set; }
        public int SoKhach { get; set; }
        public string GioDenDuKien { get; set; }
        public string YeuCauDacBiet { get; set; }
        public bool DaXuLy { get; set; }
    }

    public class DashboardMonthDto
    {
        public int Thang { get; set; }
        public decimal DoanhThuThuan { get; set; }
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
