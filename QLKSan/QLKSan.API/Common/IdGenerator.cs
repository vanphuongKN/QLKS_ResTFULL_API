using Microsoft.EntityFrameworkCore;

namespace QLKSan.Common
{
    // Sinh mã tăng dần theo tiền tố (KH0976, DP0050...) thay cho Random — tránh trùng mã và cạn mã
    public static class IdGenerator
    {
        // existingIds: cột mã của bảng (vd: _context.KhachHangs.Select(k => k.MaKhachHang))
        public static async Task<string> NextAsync(IQueryable<string> existingIds, string prefix, int minDigits = 4, int maxLength = 10)
        {
            var ids = await existingIds.Where(id => id.StartsWith(prefix)).ToListAsync();
            var max = ids
                .Select(id => id.Substring(prefix.Length))
                .Where(suffix => suffix.Length > 0 && suffix.All(char.IsDigit))
                .Select(long.Parse)
                .DefaultIfEmpty(0)
                .Max();

            var next = prefix + (max + 1).ToString().PadLeft(minDigits, '0');
            if (next.Length > maxLength)
                throw new InvalidOperationException($"Đã hết dải mã cho tiền tố '{prefix}' (tối đa {maxLength} ký tự).");
            return next;
        }
    }
}
