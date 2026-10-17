using System.Collections.Generic;
using System.Threading.Tasks;
using QLKSan.Models.Entities;
using QLKSan.Models.DTOs;

namespace QLKSan.Business
{
    public interface IAdminService
    {
        // Dashboard Stats
        Task<decimal> GetTotalRevenueAsync();
        Task<List<decimal>> GetWeeklyRevenueAsync();
        Task<List<MonthlyRevenueDto>> GetYearlyRevenueHistoryAsync(int year);
        Task<int> GetTotalBookingsAsync();
        Task<int> GetTotalCustomersAsync();

        // Room CRUD
        Task<IEnumerable<Phong>> GetAllRoomsAsync();
        Task<IEnumerable<LoaiPhong>> GetAllRoomTypesAsync();
        Task<string> GenerateNextRoomTypeIdAsync();
        Task<bool> AddRoomTypeAsync(LoaiPhong roomType);
        Task<bool> UpdateRoomTypeAsync(LoaiPhong roomType);
        Task<bool> DeleteRoomTypeAsync(string maLoai);
        Task<bool> AddRoomAsync(Phong room);
        Task<bool> UpdateRoomAsync(Phong room);
        Task<bool> DeleteRoomAsync(string maPhong);

        // Service CRUD
        Task<IEnumerable<DichVu>> GetAllServicesAsync();
        Task<string> GenerateNextServiceIdAsync();
        Task<bool> AddServiceAsync(DichVu service);
        Task<bool> UpdateServiceAsync(DichVu service);
        Task<bool> DeleteServiceAsync(string maDichVu);
        Task<bool> ToggleServiceStatusAsync(string maDichVu);

        // Staff
        Task<IEnumerable<NhanVien>> GetAllStaffAsync();
        Task<string> GenerateNextStaffIdAsync();
        Task<bool> AddStaffAsync(NhanVien staff, string tenDangNhap, string matKhau);
        Task<bool> UpdateStaffAsync(NhanVien staff);
        Task<bool> DeleteStaffAsync(string maNhanVien);

        // Promotions
        Task<IEnumerable<KhuyenMai>> GetAllPromotionsAsync();
        Task<bool> AddPromotionAsync(KhuyenMai promo);

        // Reviews
        Task<IEnumerable<DanhGia>> GetAllReviewsAsync();
    }
}
