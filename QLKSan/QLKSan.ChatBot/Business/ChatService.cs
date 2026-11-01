using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QLKSan.ChatBot.Common;
using QLKSan.ChatBot.Data;
using QLKSan.ChatBot.Providers;
using QLKSan.Models.DTOs;

namespace QLKSan.ChatBot.Business
{
    public interface IChatService
    {
        Task<ChatConversationDto> CreateAsync(CancellationToken ct);

        // null = không có hội thoại
        Task<ChatConversationDto> GetAsync(Guid maHoiThoai, CancellationToken ct);
        Task<ChatMessageDto> SendAsync(Guid maHoiThoai, string noiDung, CancellationToken ct);
    }

    // Điều phối hội thoại: lưu tin nhắn, gửi lịch sử gần nhất cho provider đã cấu hình;
    // provider chính lỗi (thiếu khoá, mạng, hết hạn mức) thì trả lời bằng RuleBased để khách vẫn được hỗ trợ
    public class ChatService : IChatService
    {
        private const string LoiChaoDau = "Xin chào quý khách, tôi là trợ lý ảo của khách sạn. Tôi có thể giúp tìm phòng trống, báo giá, giờ nhận/trả phòng, dịch vụ và đánh giá. Quý khách cần hỗ trợ gì ạ?";

        private readonly ChatDbContext _db;
        private readonly IEnumerable<ILlmProvider> _providers;
        private readonly ChatBotOptions _options;
        private readonly ILogger<ChatService> _logger;

        public ChatService(ChatDbContext db, IEnumerable<ILlmProvider> providers, IOptions<ChatBotOptions> options, ILogger<ChatService> logger)
        {
            _db = db;
            _providers = providers;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ChatConversationDto> CreateAsync(CancellationToken ct)
        {
            var now = DateTime.Now;
            var hoiThoai = new HoiThoai { MaHoiThoai = Guid.NewGuid(), ThoiGianTao = now, ThoiGianCuoi = now };
            hoiThoai.TinNhans.Add(ToEntity(hoiThoai.MaHoiThoai, ChatRoles.Bot, new BotReply { NoiDung = LoiChaoDau, GoiY = RuleBasedProvider.GoiYMacDinh }, now, null));
            _db.HoiThoais.Add(hoiThoai);
            await _db.SaveChangesAsync(ct);
            return ToDto(hoiThoai);
        }

        public async Task<ChatConversationDto> GetAsync(Guid maHoiThoai, CancellationToken ct)
        {
            var hoiThoai = await _db.HoiThoais.Include(h => h.TinNhans).FirstOrDefaultAsync(h => h.MaHoiThoai == maHoiThoai, ct);
            return hoiThoai == null ? null : ToDto(hoiThoai);
        }

        public async Task<ChatMessageDto> SendAsync(Guid maHoiThoai, string noiDung, CancellationToken ct)
        {
            var hoiThoai = await _db.HoiThoais.FirstOrDefaultAsync(h => h.MaHoiThoai == maHoiThoai, ct);
            if (hoiThoai == null) return null;
            noiDung = noiDung.Trim();

            var lichSu = (await _db.TinNhans.Where(t => t.MaHoiThoai == maHoiThoai)
                    .OrderByDescending(t => t.ThoiGian).ThenByDescending(t => t.Id).Take(Math.Max(0, _options.SoTinNhanNguCanh)).ToListAsync(ct))
                .AsEnumerable().Reverse().Select(t => new ChatTurn(t.VaiTro, t.NoiDung)).ToList();

            var (reply, provider) = await ReplyAsync(lichSu, noiDung, ct);
            var now = DateTime.Now;
            _db.TinNhans.Add(ToEntity(maHoiThoai, ChatRoles.Khach, new BotReply { NoiDung = noiDung }, now, null));
            var tinBot = ToEntity(maHoiThoai, ChatRoles.Bot, reply, now.AddMilliseconds(1), provider);
            _db.TinNhans.Add(tinBot);
            hoiThoai.ThoiGianCuoi = now;
            await _db.SaveChangesAsync(ct);
            return ToDto(tinBot);
        }

        private async Task<(BotReply, string)> ReplyAsync(List<ChatTurn> lichSu, string cauHoi, CancellationToken ct)
        {
            var chinh = Provider(_options.Provider);
            try
            {
                return (await chinh.ReplyAsync(lichSu, cauHoi, ct), chinh.Ten);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Provider {Provider} lỗi", chinh.Ten);
            }

            if (chinh.Ten != RuleBasedProvider.TenProvider)
            {
                try
                {
                    return (await Provider(RuleBasedProvider.TenProvider).ReplyAsync(lichSu, cauHoi, ct), RuleBasedProvider.TenProvider + " (dự phòng)");
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Provider dự phòng RuleBased lỗi");
                }
            }
            return (new BotReply { NoiDung = "Xin lỗi, hệ thống đang bận nên tôi chưa tra cứu được. Quý khách vui lòng thử lại sau ít phút hoặc liên hệ lễ tân." }, "Lỗi");
        }

        private ILlmProvider Provider(string ten)
            => _providers.FirstOrDefault(p => string.Equals(p.Ten, ten, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"Không có provider '{ten}'. Hỗ trợ: {string.Join(", ", _providers.Select(p => p.Ten))}.");

        private static TinNhan ToEntity(Guid maHoiThoai, string vaiTro, BotReply r, DateTime thoiGian, string provider) => new TinNhan
        {
            MaHoiThoai = maHoiThoai,
            VaiTro = vaiTro,
            NoiDung = r.NoiDung.Length > 4000 ? r.NoiDung[..4000] : r.NoiDung,
            ThoiGian = thoiGian,
            LienKet = r.LienKet.Count == 0 ? null : JsonSerializer.Serialize(r.LienKet),
            GoiY = r.GoiY.Count == 0 ? null : JsonSerializer.Serialize(r.GoiY),
            Provider = provider
        };

        private static ChatMessageDto ToDto(TinNhan t) => new ChatMessageDto
        {
            VaiTro = t.VaiTro,
            NoiDung = t.NoiDung,
            ThoiGian = t.ThoiGian,
            LienKet = t.LienKet == null ? new List<ChatLinkDto>() : JsonSerializer.Deserialize<List<ChatLinkDto>>(t.LienKet),
            GoiY = t.GoiY == null ? new List<string>() : JsonSerializer.Deserialize<List<string>>(t.GoiY),
            Provider = t.Provider
        };

        private static ChatConversationDto ToDto(HoiThoai h) => new ChatConversationDto
        {
            MaHoiThoai = h.MaHoiThoai,
            ThoiGianTao = h.ThoiGianTao,
            TinNhan = h.TinNhans.OrderBy(t => t.ThoiGian).ThenBy(t => t.Id).Select(ToDto).ToList()
        };
    }
}
