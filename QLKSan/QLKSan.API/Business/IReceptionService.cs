using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QLKSan.Models.Entities;
using QLKSan.Models.Requests;

namespace QLKSan.Business
{
    public interface IReceptionService
    {
        // Dashboard
        Task<IEnumerable<Phong>> GetAllRoomsAsync();
        Task<IEnumerable<PhieuDatPhong>> GetOnlineBookingsAsync();

        // Online Booking Actions (ném NotFoundException/BusinessException khi thất bại)
        Task ApproveOnlineBookingAsync(string maDatPhong);
        Task CheckInOnlineBookingAsync(string maDatPhong, string maNhanVien);

        // Huỷ/từ chối phiếu chưa nhận phòng. laKhachHang = true: áp dụng chính sách khách tự huỷ (trước ngày nhận phòng)
        Task CancelBookingAsync(string maDatPhong, string lyDo, string nguoiHuy, bool laKhachHang);

        // Walk-in Check-in
        Task<KhachHang> GetCustomerByCccdAsync(string cccd);

        // Trả về mã phiếu đặt phòng vừa tạo
        Task<string> ProcessCheckInAsync(CheckInRequest model, string maNhanVien);

        Task<IEnumerable<PhieuDatPhong>> GetAllBookingsAsync(string search = null);

        // Các phiếu (chờ duyệt/đã duyệt/đang ở/đã trả) có thời gian lưu trú giao với [tuNgay, denNgay]
        Task<List<Models.DTOs.TimelineBookingDto>> GetTimelineAsync(DateTime tuNgay, DateTime denNgay);
        Task<PhieuDatPhong> GetBookingByIdAsync(string maDatPhong);
        Task<IEnumerable<KhachHang>> GetAllCustomersAsync(string search = null);
        Task<KhachHang> GetCustomerDetailsAsync(string id);
    }
}
