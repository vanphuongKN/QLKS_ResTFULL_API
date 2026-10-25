namespace QLKSan.Common
{
    // Băm và kiểm tra mật khẩu bằng BCrypt (có salt ngẫu nhiên, mỗi lần băm cho kết quả khác nhau)
    public static class PasswordHasher
    {
        private const int WorkFactor = 11;

        public static string Hash(string password)
            => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

        // Chuỗi BCrypt luôn bắt đầu bằng "$2a$", "$2b$" hoặc "$2y$" và dài 60 ký tự
        public static bool IsHashed(string storedPassword)
            => storedPassword != null && storedPassword.Length == 60 && storedPassword.StartsWith("$2");

        // Hỗ trợ cả mật khẩu cũ lưu dạng chữ thường (dữ liệu trước khi áp dụng BCrypt)
        public static bool Verify(string password, string storedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedPassword)) return false;
            return IsHashed(storedPassword)
                ? BCrypt.Net.BCrypt.Verify(password, storedPassword)
                : password == storedPassword;
        }
    }
}
