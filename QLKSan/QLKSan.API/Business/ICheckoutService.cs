using System.Collections.Generic;
using System.Threading.Tasks;
using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;
using QLKSan.Models.Requests;

namespace QLKSan.Business
{
    public interface ICheckoutService
    {
        Task<PhieuDatPhong> GetBookingDetailsByRoomAsync(string maPhong);
        // Tạo mới hoặc cập nhật hoá đơn chưa thanh toán của phiếu đặt phòng
        Task<HoaDon> GenerateInvoiceAsync(string maDatPhong, string maNhanVienLap, string? maKhuyenMai = null);

        // Thanh toán hoá đơn, trả phòng và chuyển phòng sang trạng thái đang dọn
        // diemSuDung > 0: đổi điểm của khách trừ vào số tiền còn phải trả
        // cacKhoan (tối đa 3): tách thanh toán, tổng = số tiền còn phải thu sau khi đổi điểm; null = một phương thức
        Task<HoaDon> ProcessPaymentAsync(string maHoaDon, string phuongThucThanhToan, string maNhanVienThu, int diemSuDung = 0, IList<PaymentPart> cacKhoan = null);

        // Bảng kê lưu trú: từng đêm, dịch vụ, phụ thu, ưu đãi, cọc, đã thu — không lưu gì
        Task<FolioDto> GetFolioAsync(string maDatPhong);

        // Thông tin xuất hoá đơn VAT công ty
        Task<HoaDon> SetCompanyInfoAsync(string maHoaDon, InvoiceCompanyRequest request);
    }
}
