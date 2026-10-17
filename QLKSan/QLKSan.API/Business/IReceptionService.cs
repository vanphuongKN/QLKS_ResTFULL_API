using System.Collections.Generic;
using System.Threading.Tasks;
using QLKSan.Models.Entities;
using QLKSan.ViewModels;

namespace QLKSan.Business
{
    public interface IReceptionService
    {
        // Dashboard
        Task<IEnumerable<Phong>> GetAllRoomsAsync();
        Task<IEnumerable<PhieuDatPhong>> GetOnlineBookingsAsync();
        
        // Online Booking Actions
        Task<bool> ApproveOnlineBookingAsync(string maDatPhong);
        Task<bool> CheckInOnlineBookingAsync(string maDatPhong, string maNhanVien);

        // Walk-in Check-in
        Task<IEnumerable<Phong>> GetAvailableRoomsAsync();
        Task<KhachHang> GetCustomerByCccdAsync(string cccd);
        Task<TaiKhoan> GetAccountByUsernameAsync(string username);
        Task<string> ProcessCheckInAsync(CheckInViewModel model, string maNhanVien);
        Task<IEnumerable<PhieuDatPhong>> GetAllBookingsAsync(string search = null);
        Task<IEnumerable<KhachHang>> GetAllCustomersAsync(string search = null);
        Task<KhachHang> GetCustomerDetailsAsync(string id);
    }
}
