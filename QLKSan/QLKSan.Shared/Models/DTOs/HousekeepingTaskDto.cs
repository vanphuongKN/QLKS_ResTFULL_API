using System;
using System.Collections.Generic;
using QLKSan.Enums;

namespace QLKSan.Models.DTOs
{
    // Một công việc dọn phòng (bảng PhanCongDonPhong)
    public class HousekeepingTaskDto
    {
        public string MaPhanCong { get; set; }
        public string MaPhong { get; set; }
        public string SoPhong { get; set; }
        public string TenLoaiPhong { get; set; }
        public string MaNhanVien { get; set; }
        public string TenNhanVien { get; set; }
        public string MaDatPhong { get; set; }
        public string LoaiCongViec { get; set; }
        public HousekeepingTaskStatus TrangThai { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public DateTime? ThoiGianGiao { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianHoanThanh { get; set; }
        public string GhiChu { get; set; }

        public int Tang { get; set; }
        public int? MaYeuCau { get; set; }

        // 1 = gấp (khách VIP đến hôm nay), 2 = cao (khách đến hôm nay / khách đang chờ), 3 = thường, 4 = chờ kỹ thuật (sự cố)
        public int UuTien { get; set; } = 3;
        public string LyDoUuTien { get; set; }

        // Kiểm tra phòng: danh mục theo hạng phòng (khi chờ kiểm tra), kết quả lần kiểm tra gần nhất
        public List<string> DanhMucKiemTra { get; set; } = new List<string>();
        public DateTime? ThoiGianKiemTra { get; set; }
        public string TenNguoiKiemTra { get; set; }
        public string KetQuaKiemTra { get; set; }
        public int SoLanKhongDat { get; set; }
    }

    // Phiếu kỹ thuật (sự cố cần sửa trong phòng)
    public class MaintenanceTicketDto
    {
        public int MaPhieu { get; set; }
        public string MaPhong { get; set; }
        public string SoPhong { get; set; }
        public string MoTa { get; set; }
        public string MucDo { get; set; }
        public bool KhoaPhong { get; set; }
        public GuestRequestStatus TrangThai { get; set; }
        public string TenNguoiBao { get; set; }
        public DateTime ThoiGianBao { get; set; }
        public string TenNguoiXuLy { get; set; }
        public DateTime? ThoiGianXong { get; set; }
        public string KetQua { get; set; }
        public string MaPhanCong { get; set; }
        public int? MaYeuCau { get; set; }
    }

    // Một phòng trên bảng buồng phòng: trạng thái phòng, việc đang mở, khách đến / đi hôm nay
    public class HousekeepingRoomDto
    {
        public string MaPhong { get; set; }
        public string SoPhong { get; set; }
        public int Tang { get; set; }
        public string TenLoaiPhong { get; set; }
        public RoomStatus TrangThai { get; set; }
        public string KhachDen { get; set; }
        public string GioDen { get; set; }
        public bool KhachDenVIP { get; set; }
        public bool KhachDiHomNay { get; set; }
        public HousekeepingTaskDto ViecDangMo { get; set; }

        // Phiếu kỹ thuật đang mở của phòng (mô tả gần nhất)
        public string SuCo { get; set; }
    }
}
