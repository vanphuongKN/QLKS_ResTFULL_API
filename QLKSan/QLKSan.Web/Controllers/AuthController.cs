using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Enums;
using QLKSan.Infrastructure;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    // Đăng nhập/đăng ký thông qua QLKSan.API, lưu JWT trong cookie đăng nhập của Web
    public class AuthController : Controller
    {
        private readonly IAuthApiClient _authApi;

        public AuthController(IAuthApiClient authApi)
        {
            _authApi = authApi;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.IsLoggedIn()) return RedirectToHome(User.GetRole());

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập tên đăng nhập và mật khẩu.";
                return View();
            }

            try
            {
                var login = await _authApi.LoginAsync(new LoginRequest { TenDangNhap = username, MatKhau = password });
                await SignInAsync(login);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);
                return RedirectToHome(login.VaiTro.ToString());
            }
            catch (ApiException ex)
            {
                ViewBag.Error = ex.UserMessage;
                return View();
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string ho, string ten, string sdt, string cccd,
            DateTime ngaySinh, string email, string diaChi, string username, string password, string confirmPassword)
        {
            // Lưu lại dữ liệu đã nhập để hiển thị lại khi có lỗi
            ViewBag.Ho = ho;
            ViewBag.Ten = ten;
            ViewBag.SDT = sdt;
            ViewBag.CCCD = cccd;
            ViewBag.NgaySinh = ngaySinh.ToString("yyyy-MM-dd");
            ViewBag.Email = email;
            ViewBag.DiaChi = diaChi;
            ViewBag.Username = username;

            var request = new RegisterRequest
            {
                Ho = ho?.Trim(),
                Ten = ten?.Trim(),
                SDT = sdt?.Trim(),
                CCCD_Passport = cccd?.Trim(),
                NgaySinh = ngaySinh,
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                DiaChi = diaChi?.Trim(),
                TenDangNhap = username?.Trim(),
                MatKhau = password,
                XacNhanMatKhau = confirmPassword
            };

            try
            {
                // Tự động đăng nhập sau khi đăng ký thành công
                var login = await _authApi.RegisterAsync(request);
                await SignInAsync(login);
                return RedirectToAction("MyStays", "Customer");
            }
            catch (ApiException ex)
            {
                ViewBag.Error = ex.UserMessage;
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            ViewBag.Error = "Tài khoản của bạn không có quyền truy cập trang này. Vui lòng đăng nhập bằng tài khoản khác.";
            return View("Login");
        }

        private async Task SignInAsync(LoginResponse login)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, login.TenDangNhap),
                new Claim(ClaimTypes.Role, login.VaiTro.ToString()),
                new Claim(WebClaimTypes.MaDangNhap, login.MaDangNhap),
                new Claim(WebClaimTypes.HoTen, login.HoTen ?? login.TenDangNhap),
                new Claim(WebClaimTypes.AccessToken, login.AccessToken)
            };
            if (!string.IsNullOrEmpty(login.MaNhanVien)) claims.Add(new Claim(WebClaimTypes.MaNhanVien, login.MaNhanVien));
            if (!string.IsNullOrEmpty(login.MaKhachHang)) claims.Add(new Claim(WebClaimTypes.MaKhachHang, login.MaKhachHang));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

            // Cookie hết hạn cùng lúc với JWT
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { ExpiresUtc = login.ExpiresAt, IsPersistent = false });
        }

        // Về trang làm việc mặc định của vai trò đang đăng nhập (nút "Trang nhân viên" ở giao diện khách)
        [Authorize]
        [HttpGet]
        public IActionResult Portal() => RedirectToHome(User.GetRole());

        private IActionResult RedirectToHome(string role)
        {
            return role switch
            {
                nameof(Role.Customer) => RedirectToAction("MyStays", "Customer"),
                nameof(Role.Receptionist) => RedirectToAction("Index", "Reception"),
                nameof(Role.Housekeeping) => RedirectToAction("MyTasks", "Housekeeping"),
                nameof(Role.Accountant) => RedirectToAction("Reports", "Accounting"),
                _ => RedirectToAction("Index", "Admin")
            };
        }
    }
}
