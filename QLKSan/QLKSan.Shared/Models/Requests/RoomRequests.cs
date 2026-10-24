using System.ComponentModel.DataAnnotations;
using QLKSan.Enums;

namespace QLKSan.Models.Requests
{
    // Dùng cho cả thêm (POST) và sửa (PUT /{id}) loại phòng
    public class RoomTypeRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên loại phòng.")]
        [StringLength(50)]
        public string TenLoai { get; set; }

        [Range(1, 10, ErrorMessage = "Số người tối đa phải từ 1 đến 10")]
        public int SoNguoiToiDa { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá thuê ngày phải lớn hơn hoặc bằng 0")]
        public decimal GiaThueNgay { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá thuê giờ phải lớn hơn hoặc bằng 0")]
        public decimal GiaThueGio { get; set; }

        [StringLength(500)]
        public string MoTa { get; set; }
    }

    // Dùng cho cả thêm (POST) và sửa (PUT /{id}) phòng
    public class RoomRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập số phòng.")]
        [StringLength(10)]
        public string SoPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại phòng.")]
        [StringLength(10)]
        public string MaLoai { get; set; }
    }

    // PATCH /api/rooms/{id}: đổi trạng thái phòng thủ công (nullable để body thiếu trường bị báo lỗi thay vì mặc định "Trống")
    public class UpdateRoomStatusRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn trạng thái phòng.")]
        public RoomStatus? TrangThai { get; set; }
    }
}
