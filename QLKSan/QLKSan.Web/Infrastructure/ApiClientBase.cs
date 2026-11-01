using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QLKSan.Infrastructure
{
    // Lớp cơ sở cho các API client: gửi request JSON và chuyển lỗi HTTP thành ApiException
    public abstract class ApiClientBase
    {
        // Khớp với cấu hình JSON của QLKSan.API (PascalCase, enum dạng chuỗi)
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        protected HttpClient Http { get; }

        protected ApiClientBase(HttpClient http)
        {
            Http = http;
        }

        protected async Task<T> HttpGetAsync<T>(string url)
        {
            using var response = await Http.GetAsync(url);
            return await ReadAsync<T>(response);
        }

        protected async Task<T> HttpPostAsync<T>(string url, object body = null)
        {
            using var response = await Http.PostAsJsonAsync(url, body, JsonOptions);
            return await ReadAsync<T>(response);
        }

        protected async Task HttpPostAsync(string url, object body = null)
        {
            using var response = await Http.PostAsJsonAsync(url, body, JsonOptions);
            await EnsureSuccessAsync(response);
        }

        // Gửi file dạng multipart/form-data (vd: tải ảnh lên)
        protected async Task<T> HttpPostFileAsync<T>(string url, string fieldName, Stream content, string fileName, string contentType)
        {
            using var form = new MultipartFormDataContent();
            var file = new StreamContent(content);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrEmpty(contentType) ? "application/octet-stream" : contentType);
            form.Add(file, fieldName, fileName);
            using var response = await Http.PostAsync(url, form);
            return await ReadAsync<T>(response);
        }

        protected async Task HttpPutAsync(string url, object body)
        {
            using var response = await Http.PutAsJsonAsync(url, body, JsonOptions);
            await EnsureSuccessAsync(response);
        }

        protected async Task<T> HttpPutAsync<T>(string url, object body)
        {
            using var response = await Http.PutAsJsonAsync(url, body, JsonOptions);
            return await ReadAsync<T>(response);
        }

        protected async Task<T> HttpPatchAsync<T>(string url, object body = null)
        {
            using var response = await Http.PatchAsJsonAsync(url, body, JsonOptions);
            return await ReadAsync<T>(response);
        }

        protected async Task HttpPatchAsync(string url, object body = null)
        {
            using var response = await Http.PatchAsJsonAsync(url, body, JsonOptions);
            await EnsureSuccessAsync(response);
        }

        protected async Task HttpDeleteAsync(string url)
        {
            using var response = await Http.DeleteAsync(url);
            await EnsureSuccessAsync(response);
        }

        // Tải file (vd: Excel) — trả về nội dung, kiểu nội dung và tên file do API đặt
        protected async Task<(byte[] Content, string ContentType, string FileName)> HttpGetFileAsync(string url)
        {
            using var response = await Http.GetAsync(url);
            await EnsureSuccessAsync(response);
            var content = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
            return (content, contentType, fileName);
        }

        // Trả về default(T) khi API trả 404 — dùng cho các hàm "tìm theo mã"
        protected async Task<T> HttpGetOrDefaultAsync<T>(string url)
        {
            using var response = await Http.GetAsync(url);
            if (response.StatusCode == HttpStatusCode.NotFound) return default;
            return await ReadAsync<T>(response);
        }

        private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        {
            await EnsureSuccessAsync(response);
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;

            string message = null;
            var errors = new List<string>();
            try
            {
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = doc.RootElement;
                if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                    message = detail.GetString();
                else if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                    message = title.GetString();

                if (root.TryGetProperty("errors", out var errorDict) && errorDict.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in errorDict.EnumerateObject())
                        foreach (var item in field.Value.EnumerateArray())
                            errors.Add(item.GetString());
                }
            }
            catch (JsonException)
            {
                // Body không phải JSON (vd: 401/403 rỗng) — dùng thông điệp mặc định bên dưới
            }

            message ??= response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
                HttpStatusCode.Forbidden => "Bạn không có quyền thực hiện thao tác này.",
                HttpStatusCode.NotFound => "Không tìm thấy dữ liệu.",
                HttpStatusCode.TooManyRequests => "Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút.",
                // Gateway chạy nhưng API lõi / ChatBot phía sau chưa chạy
                HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout => ApiConnectionHandler.ThongBao,
                _ => "Đã xảy ra lỗi khi kết nối tới máy chủ API."
            };

            throw new ApiException(response.StatusCode, message, errors);
        }
    }
}
