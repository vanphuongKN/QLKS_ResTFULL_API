using System;
using System.ComponentModel.DataAnnotations;
using QLKSan.Validation;

namespace QLKSan.ViewModels
{
    public class BookRoomViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn phòng")]
        public string MaPhong { get; set; }

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
    }
}
