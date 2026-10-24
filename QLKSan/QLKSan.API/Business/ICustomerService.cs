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
        // Đặt theo hạng phòng (tự xếp phòng trống) hoặc đích danh phòng; còn phòng thì xác nhận ngay theo chính sách
        Task<string> CreateOnlineBookingAsync(string maKhachHang, Models.Requests.BookRoomRequest request);

        // Trả về khuyến mãi nếu đang có hiệu lực; ném NotFoundException/BusinessException nếu không
        Task<KhuyenMai> GetActivePromotionAsync(string code);
        // Đánh giá 1 lần lưu trú đã trả phòng (maDatPhong); chỉ có maPhong thì gắn vào lần ở gần nhất của phòng chưa đánh giá
        Task<DanhGia> SubmitReviewAsync(string maKhachHang, string maDatPhong, string maPhong, int soSao, string noiDung, bool anDanh);
        Task<IEnumerable<DanhGia>> GetReviewsByCustomerAsync(string maKhachHang);
    }
}
