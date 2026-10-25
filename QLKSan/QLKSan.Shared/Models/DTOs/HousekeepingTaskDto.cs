using System;
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
    }
}
