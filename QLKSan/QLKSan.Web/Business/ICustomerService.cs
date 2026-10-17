using System.Collections.Generic;
using System.Threading.Tasks;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public interface ICustomerService
    {
        Task<KhachHang> GetCustomerByAccountIdAsync(string maDangNhap);
        Task<KhachHang> GetCustomerByUsernameAsync(string username);
        Task<IEnumerable<PhieuDatPhong>> GetBookingsByCustomerAsync(string maKhachHang);
        Task<IEnumerable<Phong>> GetAvailableRoomsForBookingAsync();
        Task<string?> CreateOnlineBookingAsync(string maKhachHang, string maPhong, int soNguoi, System.DateTime checkIn, System.DateTime checkOut, string? maKhuyenMai = null);
        Task<KhuyenMai?> GetPromotionAsync(string code);
        Task<bool> SubmitReviewAsync(string maKhachHang, string maPhong, int soSao, string noiDung, bool anDanh);
        Task<IEnumerable<DanhGia>> GetReviewsByCustomerAsync(string maKhachHang);
    }
}
