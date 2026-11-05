using System.ComponentModel.DataAnnotations;

namespace QLKSan.Models.Requests
{
    // POST /api/chat/conversations/{id}/messages: khách gửi câu hỏi cho trợ lý ảo
    public class SendChatMessageRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập câu hỏi.")]
        [StringLength(500, ErrorMessage = "Câu hỏi tối đa 500 ký tự.")]
        public string NoiDung { get; set; }
    }
}
