using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using QLKSan.ChatBot.Common;
using QLKSan.ChatBot.Tools;
using QLKSan.Models.DTOs;

namespace QLKSan.ChatBot.Providers
{
    // Trả lời bằng OpenAI Chat Completions có tool calling: mô hình tự gọi các công cụ trong HotelToolbox
    // (chỉ dữ liệu công khai), tối đa vài vòng, rồi trả lời bằng tiếng Việt. Khoá API chỉ nằm ở service này.
    public class OpenAiProvider : ILlmProvider
    {
        public const string TenProvider = "OpenAI";
        private const int SoVongCongCuToiDa = 4;

        private readonly HttpClient _http;
        private readonly HotelToolbox _tools;
        private readonly OpenAiOptions _options;

        public OpenAiProvider(HttpClient http, HotelToolbox tools, IOptions<ChatBotOptions> options)
        {
            _http = http;
            _tools = tools;
            _options = options.Value.OpenAI;
        }

        public string Ten => TenProvider;

        public async Task<BotReply> ReplyAsync(IReadOnlyList<ChatTurn> lichSu, string cauHoi, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("Chưa cấu hình ChatBot:OpenAI:ApiKey.");

            var messages = new JsonArray { new JsonObject { ["role"] = "system", ["content"] = HuongDan() } };
            foreach (var t in lichSu)
                messages.Add(new JsonObject { ["role"] = t.VaiTro == ChatRoles.Khach ? "user" : "assistant", ["content"] = t.NoiDung });
            messages.Add(new JsonObject { ["role"] = "user", ["content"] = cauHoi });

            var reply = new BotReply();
            for (var vong = 0; vong <= SoVongCongCuToiDa; vong++)
            {
                var body = new JsonObject
                {
                    ["model"] = _options.Model,
                    ["messages"] = messages.DeepClone(),
                    ["temperature"] = 0.3
                };
                // Vòng cuối không cho gọi công cụ nữa để mô hình buộc phải trả lời
                if (vong < SoVongCongCuToiDa)
                    body["tools"] = new JsonArray(HotelToolbox.Definitions.Select(d => (JsonNode)new JsonObject
                    {
                        ["type"] = "function",
                        ["function"] = new JsonObject { ["name"] = d.Ten, ["description"] = d.MoTa, ["parameters"] = d.ThamSo.DeepClone() }
                    }).ToArray());

                var message = await CompleteAsync(body, ct);
                var toolCalls = message["tool_calls"] as JsonArray;
                if (toolCalls == null || toolCalls.Count == 0)
                {
                    reply.NoiDung = message["content"]?.GetValue<string>()?.Trim();
                    if (string.IsNullOrWhiteSpace(reply.NoiDung)) throw new InvalidOperationException("OpenAI trả về câu trả lời rỗng.");
                    reply.LienKet = reply.LienKet.GroupBy(l => l.Url).Select(g => g.First()).Take(4).ToList();
                    return reply;
                }

                messages.Add(message.DeepClone());
                foreach (var call in toolCalls)
                {
                    var fn = call?["function"];
                    var (ketQua, lienKet) = await _tools.ExecuteAsync(fn?["name"]?.GetValue<string>(), fn?["arguments"]?.GetValue<string>(), ct);
                    reply.LienKet.AddRange(lienKet);
                    messages.Add(new JsonObject { ["role"] = "tool", ["tool_call_id"] = call?["id"]?.GetValue<string>(), ["content"] = ketQua });
                }
            }
            throw new InvalidOperationException("OpenAI gọi công cụ quá số vòng cho phép.");
        }

        private async Task<JsonNode> CompleteAsync(JsonObject body, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl.TrimEnd('/') + "/chat/completions")
            {
                Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            using var response = await _http.SendAsync(request, ct);
            var text = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"OpenAI trả {(int)response.StatusCode}: {(text.Length > 300 ? text[..300] : text)}");
            return JsonNode.Parse(text)?["choices"]?[0]?["message"] ?? throw new InvalidOperationException("Phản hồi OpenAI không có choices[0].message.");
        }

        private static string HuongDan() =>
            $"Bạn là trợ lý ảo lễ tân của khách sạn, trả lời khách bằng tiếng Việt, lịch sự, ngắn gọn (tối đa khoảng 120 từ). Hôm nay là {DateTime.Today.ToString("dddd, dd/MM/yyyy", new System.Globalization.CultureInfo("vi-VN"))} ({DateTime.Today:yyyy-MM-dd}).\n"
            + "Quy tắc:\n"
            + "- Chỉ dùng thông tin lấy từ công cụ; không tự bịa giá, phòng trống, dịch vụ hay chính sách. Thiếu thông tin thì nói rõ và mời khách liên hệ hotline.\n"
            + "- Hỏi phòng trống / giá cho kỳ nghỉ: gọi tim_phong_trong (đổi ngày khách nói sang yyyy-MM-dd; thiếu ngày thì hỏi lại; thiếu số khách thì dùng 2 và nói rõ).\n"
            + "- Bạn không thể đặt, giữ, sửa hay huỷ phòng. Kết quả tìm phòng tự kèm nút đặt phòng điền sẵn bên dưới câu trả lời: hãy mời khách bấm nút, không tự viết đường dẫn.\n"
            + "- Không hỏi hay ghi nhận thông tin cá nhân (CCCD, thẻ, mật khẩu). Câu hỏi ngoài phạm vi khách sạn thì từ chối nhẹ nhàng.";
    }
}
