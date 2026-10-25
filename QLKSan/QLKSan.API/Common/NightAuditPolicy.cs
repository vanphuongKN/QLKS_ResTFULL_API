namespace QLKSan.Common
{
    // Chốt ngày, ánh xạ từ mục "NightAudit" trong appsettings.json
    public class NightAuditPolicy
    {
        public const string SectionName = "NightAudit";

        // Ngày đầu tiên phải chốt; các ngày trước mốc này không cần chốt
        public DateTime NgayBatDau { get; set; } = new DateTime(2026, 10, 25);
    }
}
