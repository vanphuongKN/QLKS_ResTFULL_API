using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Enums;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;

        public AuthController(IAuthService authService, ITokenService tokenService)
        {
            _authService = authService;
            _tokenService = tokenService;
        }

        /// <summary>Đăng nhập, trả về JWT và thông tin người dùng.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [Audit("Đăng nhập", "Tài khoản")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            var account = await _authService.LoginAsync(request.TenDangNhap.Trim(), request.MatKhau);
            if (account == null)
            {
                return Problem(statusCode: StatusCodes.Status401Unauthorized,
                    title: "Đăng nhập thất bại",
                    detail: "Thông tin đăng nhập không chính xác. Vui lòng thử lại.");
            }

            if (account.NhanVien != null && account.NhanVien.TrangThai == EmployeeStatus.Resigned)
            {
                return Problem(statusCode: StatusCodes.Status403Forbidden,
                    title: "Tài khoản đã ngừng hoạt động",
                    detail: "Tài khoản nhân viên đã ngừng hoạt động (nghỉ việc). Vui lòng liên hệ quản lý.");
            }

            return Ok(_tokenService.CreateToken(account));
        }

        /// <summary>Khách hàng đăng ký tài khoản mới (tự động đăng nhập sau khi đăng ký).</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Register(RegisterRequest request)
        {
            var account = await _authService.RegisterCustomerAsync(request);
            return CreatedAtAction(nameof(Me), _tokenService.CreateToken(account));
        }

        /// <summary>Người dùng đang đăng nhập (khách hàng hoặc nhân viên) tự đổi mật khẩu.</summary>
        [HttpPost("change-password")]
        [Authorize]
        [Audit("Đổi mật khẩu", "Tài khoản")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            await _authService.ChangePasswordAsync(User.GetMaDangNhap(), request.MatKhauCu, request.MatKhauMoi);
            return NoContent();
        }

        /// <summary>Thông tin người dùng đang đăng nhập.</summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> Me()
        {
            var account = await _authService.GetAccountAsync(User.GetMaDangNhap());
            if (account == null) return NotFound();
            return account.ToUserInfoDto();
        }
    }
}
