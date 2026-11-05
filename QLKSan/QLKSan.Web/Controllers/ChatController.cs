using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Infrastructure;

namespace QLKSan.Controllers
{
    // Widget trợ lý ảo trên trang khách: trình duyệt gọi Web (cùng nguồn, có antiforgery), Web gọi ChatBot qua Gateway.
    // JSON giữ PascalCase như API (chat.js đọc MaHoiThoai, TinNhan, ...)
    [AllowAnonymous]
    public class ChatController : Controller
    {
        private readonly IChatApiClient _chatApi;

        public ChatController(IChatApiClient chatApi)
        {
            _chatApi = chatApi;
        }

        // Mở lại hội thoại đang có (lưu ở sessionStorage); không còn thì tạo mới
        [HttpPost]
        public async Task<IActionResult> Conversations(Guid? maHoiThoai)
        {
            try
            {
                var hoiThoai = maHoiThoai == null ? null : await _chatApi.GetAsync(maHoiThoai.Value);
                return Json(hoiThoai ?? await _chatApi.CreateAsync(), ApiClientBase.JsonOptions);
            }
            catch (ApiException ex)
            {
                return Loi(ex);
            }
            catch (HttpRequestException)
            {
                return Loi(null);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Messages(Guid maHoiThoai, string noiDung)
        {
            if (string.IsNullOrWhiteSpace(noiDung))
                return BadRequest(new { loi = "Vui lòng nhập câu hỏi." });
            try
            {
                return Json(await _chatApi.SendAsync(maHoiThoai, noiDung.Trim()), ApiClientBase.JsonOptions);
            }
            catch (ApiException ex)
            {
                return Loi(ex);
            }
            catch (HttpRequestException)
            {
                return Loi(null);
            }
        }

        private IActionResult Loi(ApiException ex) => ex?.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => StatusCode(429, new { loi = "Quý khách hỏi hơi nhanh, vui lòng đợi một chút rồi thử lại." }),
            HttpStatusCode.NotFound => NotFound(new { loi = "Hội thoại đã kết thúc, vui lòng bắt đầu lại." }),
            HttpStatusCode.BadRequest => BadRequest(new { loi = ex.UserMessage }),
            _ => StatusCode(503, new { loi = "Trợ lý ảo tạm thời không phản hồi, quý khách vui lòng thử lại sau hoặc liên hệ lễ tân." })
        };
    }
}
