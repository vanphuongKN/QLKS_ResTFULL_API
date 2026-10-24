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
    }
}
