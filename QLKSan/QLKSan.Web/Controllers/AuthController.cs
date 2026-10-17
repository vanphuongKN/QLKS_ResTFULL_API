using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Data;
using QLKSan.Models.Entities;
using QLKSan.Enums;
using System.Linq;

namespace QLKSan.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập tên đăng nhập và mật khẩu.";
                return View();
            }

            var account = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.TenDangNhap == username && t.MatKhau == password);
            
            if (account != null)
            {
                Response.Cookies.Append("UserRole", account.VaiTro.ToString());
                Response.Cookies.Append("Username", account.TenDangNhap);

                if (account.VaiTro == Enums.Role.Customer)
                {
                    return RedirectToAction("MyStays", "Customer");
                }
                else if (account.VaiTro == Enums.Role.Receptionist)
                {
                    return RedirectToAction("Index", "Reception");
                }
                else if (account.VaiTro == Enums.Role.Admin)
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "Admin");
                }
            }

            ViewBag.Error = "Thông tin đăng nhập không chính xác. Vui lòng thử lại.";
            return View();
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

            // Validate required fields
            if (string.IsNullOrWhiteSpace(ho) || string.IsNullOrWhiteSpace(ten) || 
                string.IsNullOrWhiteSpace(sdt) || string.IsNullOrWhiteSpace(cccd) ||
                string.IsNullOrWhiteSpace(diaChi) ||
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ các thông tin bắt buộc.";
                return View();
            }

            // Validate SDT length & format
            var sdtTrim = sdt.Trim();
            var phoneRegex = new System.Text.RegularExpressions.Regex(@"^(0[3|5|7|8|9])+([0-9]{8})\b");
            if (!phoneRegex.IsMatch(sdtTrim))
            {
                ViewBag.Error = "Số điện thoại không hợp lệ (gồm 10 số, bắt đầu bằng 03, 05, 07, 08, 09).";
                return View();
            }

            // Validate CCCD/Passport length
            var cccdTrim = cccd.Trim();
            var cccdRegex = new System.Text.RegularExpressions.Regex(@"^[0-9]{9,12}$");
            if (!cccdRegex.IsMatch(cccdTrim))
            {
                ViewBag.Error = "CCCD/Passport phải bao gồm 9 đến 12 chữ số.";
                return View();
            }

            // Validate ngày sinh không được ở tương lai
            if (ngaySinh.Date > DateTime.Today)
            {
                ViewBag.Error = "Ngày sinh không được lớn hơn ngày hiện tại.";
                return View();
            }

            // Validate password match
            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            // Validate password length
            if (password.Length < 6)
            {
                ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự.";
                return View();
            }

            // Check unique username
            if (await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == username))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.";
                return View();
            }

            // Check unique CCCD/Passport
            if (await _context.KhachHangs.AnyAsync(k => k.CCCD_Passport == cccd))
            {
                ViewBag.Error = "Số CCCD/Passport đã được đăng ký. Vui lòng kiểm tra lại.";
                return View();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var rnd = new Random();

                // 1. Tạo TaiKhoan
                string maDangNhap = "TK" + rnd.Next(10000, 99999).ToString();
                while (await _context.TaiKhoans.AnyAsync(t => t.MaDangNhap == maDangNhap))
                {
                    maDangNhap = "TK" + rnd.Next(10000, 99999).ToString();
                }

                var taiKhoan = new TaiKhoan
                {
                    MaDangNhap = maDangNhap,
                    TenDangNhap = username,
                    MatKhau = password,
                    VaiTro = Role.Customer
                };
                _context.TaiKhoans.Add(taiKhoan);

                // 2. Tạo KhachHang
                string maKH = "KH" + rnd.Next(100, 999).ToString();
                while (await _context.KhachHangs.AnyAsync(k => k.MaKhachHang == maKH))
                {
                    maKH = "KH" + rnd.Next(1000, 9999).ToString();
                }

                var khachHang = new KhachHang
                {
                    MaKhachHang = maKH,
                    MaDangNhap = maDangNhap,
                    Ho = ho,
                    Ten = ten,
                    NgaySinh = ngaySinh,
                    CCCD_Passport = cccd,
                    DiemTichLuy = 0
                };
                _context.KhachHangs.Add(khachHang);

                // 3. Tạo LienHe_DiaChi
                string maLH = "LH" + rnd.Next(100, 999).ToString();
                while (await _context.LienHeDiaChis.AnyAsync(l => l.MaLienHe == maLH))
                {
                    maLH = "LH" + rnd.Next(1000, 9999).ToString();
                }

                var lienHe = new LienHeDiaChi
                {
                    MaLienHe = maLH,
                    MaKhachHang = maKH,
                    SDT = sdt,
                    Email = email ?? "",
                    DiaChi = diaChi
                };
                _context.LienHeDiaChis.Add(lienHe);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Tự động đăng nhập sau khi đăng ký thành công
                Response.Cookies.Append("UserRole", Role.Customer.ToString());
                Response.Cookies.Append("Username", username);

                return RedirectToAction("MyStays", "Customer");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                ViewBag.Error = $"Đã xảy ra lỗi khi đăng ký: {innerMsg}";
                return View();
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("UserRole");
            Response.Cookies.Delete("Username");
            return RedirectToAction("Index", "Home");
        }
    }
}
