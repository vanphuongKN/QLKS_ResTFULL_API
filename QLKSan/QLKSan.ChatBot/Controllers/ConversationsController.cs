using Microsoft.AspNetCore.Mvc;
using QLKSan.ChatBot.Business;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.ChatBot.Controllers
{
    // Hội thoại với trợ lý ảo — công khai (khách chưa đăng nhập vẫn hỏi được); Gateway giới hạn tần suất
    [ApiController]
    [Route("api/chat/conversations")]
    public class ConversationsController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ConversationsController(IChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>Tạo hội thoại mới, trả kèm lời chào và câu hỏi gợi ý.</summary>
        [HttpPost]
        public async Task<ActionResult<ChatConversationDto>> Create(CancellationToken ct)
        {
            var created = await _chatService.CreateAsync(ct);
            return Created($"/api/chat/conversations/{created.MaHoiThoai}", created);
        }

        /// <summary>Lịch sử một hội thoại.</summary>
        [HttpGet("{maHoiThoai:guid}")]
        public async Task<ActionResult<ChatConversationDto>> Get(Guid maHoiThoai, CancellationToken ct)
        {
            return await _chatService.GetAsync(maHoiThoai, ct) is { } dto ? dto : NotFoundProblem();
        }

        /// <summary>Gửi câu hỏi, nhận câu trả lời của trợ lý (kèm liên kết đặt phòng điền sẵn khi tìm phòng trống).</summary>
        [HttpPost("{maHoiThoai:guid}/messages")]
        public async Task<ActionResult<ChatMessageDto>> Send(Guid maHoiThoai, SendChatMessageRequest request, CancellationToken ct)
        {
            return await _chatService.SendAsync(maHoiThoai, request.NoiDung, ct) is { } dto ? dto : NotFoundProblem();
        }

        private ObjectResult NotFoundProblem() => Problem(statusCode: StatusCodes.Status404NotFound, title: "Không tìm thấy", detail: "Không tìm thấy hội thoại.");
    }
}
