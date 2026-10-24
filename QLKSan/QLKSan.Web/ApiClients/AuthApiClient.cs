using QLKSan.Infrastructure;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.ApiClients
{
    public interface IAuthApiClient
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> RegisterAsync(RegisterRequest request);
    }

    // Gọi các endpoint /api/auth của QLKSan.API
    public class AuthApiClient : ApiClientBase, IAuthApiClient
    {
        public AuthApiClient(HttpClient http) : base(http) { }

        public Task<LoginResponse> LoginAsync(LoginRequest request)
            => HttpPostAsync<LoginResponse>("api/auth/login", request);

        public Task<LoginResponse> RegisterAsync(RegisterRequest request)
            => HttpPostAsync<LoginResponse>("api/auth/register", request);
    }
}
