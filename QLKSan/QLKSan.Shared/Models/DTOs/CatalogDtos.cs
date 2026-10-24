using System;
using QLKSan.Enums;

namespace QLKSan.Models.DTOs
{
    public class ServiceDto
    {
        public string MaDichVu { get; set; }
        public string TenDichVu { get; set; }
        public decimal GiaDichVu { get; set; }
        public string DonViTinh { get; set; }
        public ServiceStatus TrangThai { get; set; }

        // Dịch vụ được giới thiệu ở mục "Trải nghiệm" trang chủ (không ghi giá)
        public bool LaTraiNghiem { get; set; }
        public string MoTa { get; set; }
    }

    public class PromotionDto
    {
        public string MaKhuyenMai { get; set; }
        public string TenKhuyenMai { get; set; }
        public string LoaiGiam { get; set; }
        public decimal GiaTriGiam { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public PromotionStatus TrangThai { get; set; }
    }

    public class StaffDto
    {
        public string MaNhanVien { get; set; }
        public string MaDangNhap { get; set; }
        public string TenDangNhap { get; set; }
        public string TenNhanVien { get; set; }
        public string ChucVu { get; set; }
        public string SDT { get; set; }
        public string Email { get; set; }
        public DateTime NgayVaoLam { get; set; }
        public EmployeeStatus TrangThai { get; set; }

        // Vai trò của tài khoản đăng nhập (null nếu nhân viên không có tài khoản)
        public Role? VaiTro { get; set; }
    }

    public class ReviewDto
    {
        public string MaDanhGia { get; set; }
        public string MaKhachHang { get; set; }
        public string HoTenKhachHang { get; set; }
        public string MaPhong { get; set; }
        public string SoPhong { get; set; }
        public int SoSao { get; set; }
        public string NoiDung { get; set; }
        public DateTime NgayDanhGia { get; set; }
        public bool AnDanh { get; set; }

        // Mỗi lần lưu trú (phiếu đặt) được đánh giá 1 lần
        public string MaDatPhong { get; set; }
        public string TenLoaiPhong { get; set; }

        // Tên hiển thị công khai: "Khách ẩn danh" hoặc tên (không kèm họ) của khách
        public string TenHienThi { get; set; }
    }
}
