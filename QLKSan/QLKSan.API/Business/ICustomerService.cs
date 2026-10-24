using System.Collections.Generic;
using System.Threading.Tasks;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public interface ICustomerService
    {
        Task<KhachHang> GetCustomerByAccountIdAsync(string maDangNhap);

        // Cập nhật liên hệ chính của khách (tạo mới nếu chưa có), trả về hồ sơ sau khi cập nhật
        Task<KhachHang> UpdateContactAsync(string maKhachHang, Models.Requests.UpdateContactRequest request);
        Task<IEnumerable<PhieuDatPhong>> GetBookingsByCustomerAsync(string maKhachHang);
        // Trả về mã phiếu đặt phòng vừa tạo; ném BusinessException nếu phòng không hợp lệ
        Task<string> CreateOnlineBookingAsync(string maKhachHang, string maPhong, int soNguoi, System.DateTime checkIn, System.DateTime checkOut, string? maKhuyenMai = null);

        // Trả về khuyến mãi nếu đang có hiệu lực; ném NotFoundException/BusinessException nếu không
        Task<KhuyenMai> GetActivePromotionAsync(string code);
        Task<DanhGia> SubmitReviewAsync(string maKhachHang, string maPhong, int soSao, string noiDung, bool anDanh);
        Task<IEnumerable<DanhGia>> GetReviewsByCustomerAsync(string maKhachHang);
    }
}
