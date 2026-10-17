using System.Threading.Tasks;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public interface ICheckoutService
    {
        Task<PhieuDatPhong> GetBookingDetailsByRoomAsync(string maPhong);
        Task<HoaDon> GenerateInvoiceAsync(string maDatPhong, string maNhanVienLap, string? maKhuyenMai = null);
        Task<bool> ProcessPaymentAsync(string maHoaDon, string phuongThucThanhToan);
    }
}
