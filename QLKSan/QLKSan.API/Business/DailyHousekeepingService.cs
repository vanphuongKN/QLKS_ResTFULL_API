using Microsoft.Extensions.Options;
using QLKSan.Common;

namespace QLKSan.Business
{
    // Tác vụ nền: định kỳ tạo việc dọn hằng ngày (phòng có khách ở tiếp) và chỉnh trang buổi tối theo cấu hình Housekeeping.
    // Tạo lặp lại không trùng (mỗi phiếu mỗi loại việc một lần trong ngày)
    public class DailyHousekeepingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailyHousekeepingService> _logger;

        public DailyHousekeepingService(IServiceScopeFactory scopeFactory, ILogger<DailyHousekeepingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var chuKy = 30;
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var policy = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<HousekeepingPolicy>>().CurrentValue;
                    chuKy = Math.Max(5, policy.ChuKyTaoViecPhut);
                    if (policy.TuDongTaoViecHangNgay)
                    {
                        var (donPhong, chinhTrang) = await scope.ServiceProvider.GetRequiredService<IHousekeepingService>().GenerateDailyTasksAsync(DateTime.Now);
                        if (donPhong + chinhTrang > 0)
                            _logger.LogInformation("Đã tạo {DonPhong} việc dọn hằng ngày, {ChinhTrang} việc chỉnh trang buổi tối", donPhong, chinhTrang);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Lỗi khi tạo việc buồng phòng hằng ngày");
                }
                await Task.Delay(TimeSpan.FromMinutes(chuKy), stoppingToken);
            }
        }
    }
}
