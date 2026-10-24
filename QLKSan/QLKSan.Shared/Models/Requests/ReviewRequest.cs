using System.ComponentModel.DataAnnotations;

namespace QLKSan.Models.Requests
{
    // Đánh giá một lần lưu trú: gửi MaDatPhong (cách mới). Chỉ gửi MaPhong thì hệ thống gắn vào lần ở gần nhất chưa đánh giá
    public class ReviewRequest
    {
        [StringLength(20)]
        public string MaDatPhong { get; set; }

        [StringLength(10)]
        public string MaPhong { get; set; }

        [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
        public int SoSao { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá.")]
        [StringLength(1000)]
        public string NoiDung { get; set; }

        public bool AnDanh { get; set; }
    }

    // PUT /api/reviews/{id}/reply: phản hồi công khai của khách sạn
    public class ReviewReplyRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập nội dung phản hồi.")]
        [StringLength(1000, ErrorMessage = "Phản hồi tối đa 1000 ký tự.")]
        public string NoiDung { get; set; }
    }

    // PATCH /api/reviews/{id}: ẩn / hiện đánh giá trên trang khách { "An": true | false }
    public class UpdateReviewVisibilityRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn trạng thái hiển thị.")]
        public bool? An { get; set; }
    }
}
