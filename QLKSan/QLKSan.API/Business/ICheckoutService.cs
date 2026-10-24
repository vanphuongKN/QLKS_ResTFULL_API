using System.Threading.Tasks;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public interface ICheckoutService
    {
        Task<PhieuDatPhong> GetBookingDetailsByRoomAsync(string maPhong);
        // Tạo mới hoặc cập nhật hoá đơn chưa thanh toán của phiếu đặt phòng
        Task<HoaDon> GenerateInvoiceAsync(string maDatPhong, string maNhanVienLap, string? maKhuyenMai = null);

        // Thanh toán hoá đơn, trả phòng và chuyển phòng sang trạng thái đang dọn
        // diemSuDung > 0: đổi điểm của khách trừ vào số tiền còn phải trả
        Task<HoaDon> ProcessPaymentAsync(string maHoaDon, string phuongThucThanhToan, string maNhanVienThu, int diemSuDung = 0);
    }
}
