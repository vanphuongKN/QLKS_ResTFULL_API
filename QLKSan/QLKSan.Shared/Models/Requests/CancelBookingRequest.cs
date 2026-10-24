using System.ComponentModel.DataAnnotations;

namespace QLKSan.Models.Requests
{
    // Huỷ (khách/lễ tân) hoặc từ chối (lễ tân) phiếu đặt chưa nhận phòng
    public class CancelBookingRequest
    {
        [StringLength(200, ErrorMessage = "Lý do huỷ tối đa 200 ký tự.")]
        public string LyDo { get; set; }
    }
}
