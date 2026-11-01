using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using QLKSan.ChatBot.Business;
using QLKSan.ChatBot.Common;
using QLKSan.ChatBot.Data;
using QLKSan.ChatBot.Providers;
using QLKSan.ChatBot.Tools;

namespace QLKSan.ChatBot
{
    // Trợ lý ảo cho khách: REST /api/chat/conversations (đi qua Gateway), trả lời bằng provider cấu hình trong ChatBot:Provider
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var options = builder.Configuration.GetSection(ChatBotOptions.SectionName).Get<ChatBotOptions>() ?? new ChatBotOptions();
            builder.Services.Configure<ChatBotOptions>(builder.Configuration.GetSection(ChatBotOptions.SectionName));

            builder.Services.AddControllers().AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy = null;
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            builder.Services.AddProblemDetails();

            // CSDL riêng (SQLite): đường dẫn tương đối tính từ thư mục chạy của service
            var chuoiKetNoi = builder.Configuration.GetConnectionString("ChatDb") ?? "Data Source=data/chatbot.db";
            var file = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(chuoiKetNoi).DataSource;
            if (!Path.IsPathRooted(file))
                chuoiKetNoi = $"Data Source={Path.Combine(builder.Environment.ContentRootPath, file)}";
            Directory.CreateDirectory(Path.GetDirectoryName(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(chuoiKetNoi).DataSource)!);
            builder.Services.AddDbContext<ChatDbContext>(o => o.UseSqlite(chuoiKetNoi));

            builder.Services.AddMemoryCache();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient<HotelDataClient>(c =>
            {
                c.BaseAddress = new Uri(options.ApiBaseUrl);
                c.Timeout = TimeSpan.FromSeconds(15);
            });
            builder.Services.AddSingleton<BookingLinks>();
            builder.Services.AddScoped<HotelToolbox>();

            // Các provider; thêm Claude / Gemini: viết lớp ILlmProvider mới và đăng ký ở đây
            builder.Services.AddScoped<ILlmProvider, RuleBasedProvider>();
            builder.Services.AddHttpClient<OpenAiProvider>(c => c.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.OpenAI.TimeoutGiay)));
            builder.Services.AddScoped<ILlmProvider>(sp => sp.GetRequiredService<OpenAiProvider>());
            builder.Services.AddScoped<IChatService, ChatService>();

            // Chạy sau Gateway: IP thật của khách (để Gateway giới hạn tần suất theo khách khi ChatBot gọi lại API lõi)
            builder.Services.Configure<ForwardedHeadersOptions>(o =>
            {
                o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                foreach (var mang in builder.Configuration.GetSection("ForwardedHeaders:MangTinCay").Get<string[]>() ?? Array.Empty<string>())
                    if (!string.IsNullOrWhiteSpace(mang)) o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(mang.Trim()));
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<ChatDbContext>().Database.EnsureCreated();
                var providers = scope.ServiceProvider.GetServices<ILlmProvider>().Select(p => p.Ten).ToList();
                if (!providers.Contains(options.Provider, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"ChatBot:Provider '{options.Provider}' không hợp lệ. Hỗ trợ: {string.Join(", ", providers)}.");
            }

            app.UseForwardedHeaders();
            app.UseExceptionHandler();
            app.UseStatusCodePages();
            app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "QLKSan.ChatBot", provider = options.Provider, time = DateTime.Now }));
            app.MapControllers();
            app.Run();
        }
    }
}
