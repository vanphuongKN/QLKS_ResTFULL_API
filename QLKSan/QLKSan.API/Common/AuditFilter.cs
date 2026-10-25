using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using QLKSan.Data;
using QLKSan.Models.Entities;

namespace QLKSan.Common
{
    // Đánh dấu action cần ghi nhật ký thao tác (chỉ ghi khi thao tác thành công)
    [AttributeUsage(AttributeTargets.Method)]
    public class AuditAttribute : Attribute
    {
        public string HanhDong { get; }
        public string DoiTuong { get; }

        public AuditAttribute(string hanhDong, string doiTuong)
        {
            HanhDong = hanhDong;
            DoiTuong = doiTuong;
        }
    }

    // Bộ lọc toàn cục: sau action có [Audit] trả kết quả thành công thì ghi một dòng NhatKyThaoTac
    // (ai, lúc nào, làm gì, trên đối tượng nào, dữ liệu gửi lên — đã bỏ mật khẩu)
    public class AuditFilter : IAsyncActionFilter
    {
        private const int ChiTietToiDa = 1000;
        private static readonly JsonSerializerOptions Json = new() { WriteIndented = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var audit = context.ActionDescriptor.EndpointMetadata.OfType<AuditAttribute>().FirstOrDefault();
            var executed = await next();
            if (audit == null || executed.Exception != null && !executed.ExceptionHandled) return;

            var status = executed.Result switch
            {
                IStatusCodeActionResult r => r.StatusCode ?? StatusCodes.Status200OK,
                _ => StatusCodes.Status200OK
            };
            if (status >= 400) return;

            var user = context.HttpContext.User;
            var args = context.ActionArguments;
            // DbContext riêng: không lưu kèm các thay đổi còn treo của action; ghi nhật ký lỗi cũng không làm hỏng thao tác đã thành công
            using var scope = context.HttpContext.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.NhatKyThaoTacs.Add(new NhatKyThaoTac
            {
                ThoiGian = DateTime.Now,
                MaDangNhap = user.FindFirstValue(AppClaimTypes.MaDangNhap),
                TenDangNhap = user.Identity?.IsAuthenticated == true ? user.Identity.Name : TenDangNhapTuBody(args),
                VaiTro = user.FindFirstValue(ClaimTypes.Role),
                HanhDong = audit.HanhDong,
                DoiTuong = audit.DoiTuong,
                MaDoiTuong = MaDoiTuong(context, executed.Result),
                ChiTiet = ChiTiet(args),
                DiaChiIP = context.HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            try
            {
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                context.HttpContext.RequestServices.GetRequiredService<ILogger<AuditFilter>>()
                    .LogError(ex, "Không ghi được nhật ký thao tác {HanhDong}", audit.HanhDong);
            }
        }

        // Mã đối tượng: tham số trên đường dẫn (vd maDatPhong), nếu không có thì lấy thuộc tính Ma... của kết quả trả về (thêm mới)
        private static string MaDoiTuong(ActionExecutingContext context, IActionResult result)
        {
            var route = context.RouteData.Values
                .Where(v => v.Key is not ("controller" or "action") && v.Value != null)
                .Select(v => v.Value.ToString())
                .ToList();
            if (route.Count > 0) return string.Join("/", route);

            var value = (result as ObjectResult)?.Value;
            var prop = value?.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.Name.StartsWith("Ma") && p.PropertyType == typeof(string));
            return prop?.GetValue(value) as string;
        }

        // Dữ liệu gửi lên (trừ tham số đường dẫn và file), bỏ mọi trường mật khẩu
        private static string ChiTiet(IDictionary<string, object> args)
        {
            var body = args.Values.FirstOrDefault(v => v != null && v is not string && v is not IFormFile && !v.GetType().IsPrimitive);
            if (body == null) return null;
            var dict = body.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !p.Name.Contains("MatKhau", StringComparison.OrdinalIgnoreCase) && !p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(p => p.Name, p => p.GetValue(body));
            var json = JsonSerializer.Serialize(dict, Json);
            return json.Length > ChiTietToiDa ? json[..ChiTietToiDa] : json;
        }

        private static string TenDangNhapTuBody(IDictionary<string, object> args)
            => args.Values.Select(v => v?.GetType().GetProperty("TenDangNhap")?.GetValue(v) as string).FirstOrDefault(s => s != null);
    }
}
