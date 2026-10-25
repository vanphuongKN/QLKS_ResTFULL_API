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

        // Đổi phòng đã xếp cho phiếu chưa nhận phòng; giá theo hạng của phòng mới
        Task ChangeRoomAsync(string maDatPhong, string maPhongMoi);
        // request (tuỳ chọn): số khách thực tế, tiền cọc thu thêm khi nhận phòng
        Task CheckInOnlineBookingAsync(string maDatPhong, string maNhanVien, CheckInBookingRequest request = null);

        // Huỷ/từ chối phiếu chưa nhận phòng. laKhachHang = true: áp dụng chính sách khách tự huỷ (trước ngày nhận phòng).
        // hoanCoc (lễ tân): hoàn toàn bộ tiền cọc đã thu, ghi vào sổ cọc
        Task CancelBookingAsync(string maDatPhong, string lyDo, string nguoiHuy, bool laKhachHang, bool hoanCoc = false, string maNhanVien = null);

        // Lễ tân đặt phòng cho ngày tới (điện thoại, email, tại quầy, đại lý); phiếu được xác nhận ngay. Trả về mã phiếu
        Task<string> CreateFrontDeskBookingAsync(FrontDeskBookingRequest request, string maNhanVien);

        // Thu thêm tiền cọc cho phiếu chưa trả phòng
        Task AddDepositAsync(string maDatPhong, DepositRequest request, string maNhanVien);

        // Walk-in Check-in
        Task<KhachHang> GetCustomerByCccdAsync(string cccd);

        // Tra khách cũ theo đúng số CCCD/Passport hoặc số điện thoại (tối đa 5 hồ sơ)
        Task<List<KhachHang>> FindCustomersAsync(string q);

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
