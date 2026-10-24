using System;
using System.ComponentModel.DataAnnotations;
using QLKSan.Validation;

namespace QLKSan.Models.Requests
{
    // Khách đặt online: chọn HẠNG phòng (MaLoai) — hệ thống tự xếp phòng trống; MaPhong (đích danh) giữ cho tương thích
    public class BookRoomRequest
    {
        [StringLength(10)]
        public string? MaLoai { get; set; }

        [StringLength(10)]
        public string? MaPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số người")]
        [Range(1, 20, ErrorMessage = "Số lượng người phải từ 1 đến 20")]
        public int SoNguoi { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng")]
        [FutureDate(ErrorMessage = "Ngày nhận phòng không được ở trong quá khứ")]
        [Display(Name = "Ngày check-in")]
        public DateTime CheckIn { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng")]
        [DateGreaterThan("CheckIn", ErrorMessage = "Ngày trả phòng phải sau ngày nhận phòng")]
        [Display(Name = "Ngày check-out")]
        public DateTime CheckOut { get; set; } = DateTime.Today.AddDays(1);

        public string? MaKhuyenMai { get; set; }

        [StringLength(500, ErrorMessage = "Yêu cầu riêng tối đa 500 ký tự.")]
        public string? YeuCauDacBiet { get; set; }

        [StringLength(30)]
        public string? GioDenDuKien { get; set; }
    }

    // POST /api/bookings/{id}/change-room: lễ tân đổi phòng đã xếp trước khi nhận khách
    public class ChangeRoomRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn phòng mới.")]
        [StringLength(10)]
        public string MaPhong { get; set; }
    }
}
