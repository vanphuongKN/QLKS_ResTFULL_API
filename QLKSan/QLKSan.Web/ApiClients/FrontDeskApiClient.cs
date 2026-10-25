using QLKSan.Infrastructure;
using QLKSan.Models.DTOs;

namespace QLKSan.ApiClients
{
    // ===== /api/front-desk: màn hình đầu ca của lễ tân =====
    public interface IFrontDeskApiClient
    {
        Task<FrontDeskTodayDto> GetTodayAsync();
    }

    public class FrontDeskApiClient : ApiClientBase, IFrontDeskApiClient
    {
        public FrontDeskApiClient(HttpClient http) : base(http) { }

        public Task<FrontDeskTodayDto> GetTodayAsync() => HttpGetAsync<FrontDeskTodayDto>("api/front-desk/today");
    }
}
