namespace QLKSan.Business
{
    public static class BookingNotes
    {
        public const int MaxLength = 1000;

        // Nối thêm ghi chú vào phiếu, giữ phần mới nhất nếu vượt độ dài cột GhiChu
        public static string Append(string current, string note)
        {
            var combined = string.IsNullOrWhiteSpace(current) ? note : $"{current} | {note}";
            return combined.Length <= MaxLength ? combined : combined.Substring(combined.Length - MaxLength);
        }
    }
}
