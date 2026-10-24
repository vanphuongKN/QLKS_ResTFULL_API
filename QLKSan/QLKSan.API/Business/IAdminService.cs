using System.Collections.Generic;
using System.Threading.Tasks;
using QLKSan.Enums;
using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    // Các thao tác thêm/sửa/xoá ném NotFoundException, BusinessException hoặc ConflictException khi thất bại
    public interface IAdminService
    {
        // Dashboard Stats
        Task<decimal> GetTotalRevenueAsync();
        Task<List<decimal>> GetWeeklyRevenueAsync();
        Task<List<MonthlyRevenueDto>> GetYearlyRevenueHistoryAsync(int year);
        Task<int> GetTotalBookingsAsync();
        Task<int> GetTotalCustomersAsync();

        // Room Types
        Task<IEnumerable<LoaiPhong>> GetAllRoomTypesAsync();
        Task<LoaiPhong> GetRoomTypeByIdAsync(string maLoai);
        Task<string> GenerateNextRoomTypeIdAsync();
        Task AddRoomTypeAsync(LoaiPhong roomType);
        Task UpdateRoomTypeAsync(LoaiPhong roomType);
        Task DeleteRoomTypeAsync(string maLoai);

        // Rooms
        Task<IEnumerable<Phong>> GetAllRoomsAsync();
        Task AddRoomAsync(Phong room);
        Task UpdateRoomAsync(Phong room);
        Task DeleteRoomAsync(string maPhong);

        // Services
        Task<IEnumerable<DichVu>> GetAllServicesAsync();
        Task<DichVu> GetServiceByIdAsync(string maDichVu);
        Task<string> GenerateNextServiceIdAsync();
        Task AddServiceAsync(DichVu service);
        Task UpdateServiceAsync(DichVu service);
        Task DeleteServiceAsync(string maDichVu);
        Task<DichVu> SetServiceStatusAsync(string maDichVu, Enums.ServiceStatus trangThai);

        // Staff
        Task<IEnumerable<NhanVien>> GetAllStaffAsync();
        Task<NhanVien> GetStaffByIdAsync(string maNhanVien);
        Task<string> GenerateNextStaffIdAsync();
        Task AddStaffAsync(NhanVien staff, string tenDangNhap, string matKhau, Role vaiTro);

        // vaiTro/trangThai null = giữ nguyên; maNhanVienThucHien: người đang thao tác (không được tự khoá/hạ quyền mình)
        Task UpdateStaffAsync(NhanVien staff, Role? vaiTro, EmployeeStatus? trangThai, string maNhanVienThucHien);
        Task ResetPasswordAsync(string maNhanVien, string matKhauMoi);
        Task DeleteStaffAsync(string maNhanVien, string maNhanVienThucHien);

        // Promotions
        Task<IEnumerable<KhuyenMai>> GetAllPromotionsAsync();
        Task AddPromotionAsync(KhuyenMai promo);

        // Reviews
        Task<IEnumerable<DanhGia>> GetAllReviewsAsync();
    }
}
