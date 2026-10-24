using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Models.Entities;
using QLKSan.Models.Requests;

namespace QLKSan.Business
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TaiKhoan> LoginAsync(string tenDangNhap, string matKhau)
        {
            var account = await _context.TaiKhoans
                .Include(t => t.NhanVien)
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

            if (account == null || !PasswordHasher.Verify(matKhau, account.MatKhau))
                return null;

            // Tài khoản cũ còn lưu mật khẩu dạng chữ thường: băm lại ngay khi đăng nhập thành công
            if (!PasswordHasher.IsHashed(account.MatKhau))
            {
                account.MatKhau = PasswordHasher.Hash(matKhau);
                await _context.SaveChangesAsync();
            }

            return account;
        }

        public async Task<TaiKhoan> GetAccountAsync(string maDangNhap)
        {
            return await _context.TaiKhoans
                .Include(t => t.NhanVien)
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.MaDangNhap == maDangNhap);
        }

        public async Task<TaiKhoan> RegisterCustomerAsync(RegisterRequest request)
        {
            var username = request.TenDangNhap.Trim();
            var cccd = request.CCCD_Passport.Trim();

            if (request.NgaySinh.Date > DateTime.Today)
                throw new BusinessException("Ngày sinh không được lớn hơn ngày hiện tại.");

            if (await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == username))
                throw new ConflictException("Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");

            if (await _context.KhachHangs.AnyAsync(k => k.CCCD_Passport == cccd))
                throw new ConflictException("Số CCCD/Passport đã được đăng ký. Vui lòng kiểm tra lại.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Tạo TaiKhoan
                string maDangNhap = await IdGenerator.NextAsync(_context.TaiKhoans.Select(t => t.MaDangNhap), "TK", 4);

                var taiKhoan = new TaiKhoan
                {
                    MaDangNhap = maDangNhap,
                    TenDangNhap = username,
                    MatKhau = PasswordHasher.Hash(request.MatKhau),
                    VaiTro = Role.Customer
                };
                _context.TaiKhoans.Add(taiKhoan);

                // 2. Tạo KhachHang
                string maKH = await IdGenerator.NextAsync(_context.KhachHangs.Select(k => k.MaKhachHang), "KH", 4);

                var khachHang = new KhachHang
                {
                    MaKhachHang = maKH,
                    MaDangNhap = maDangNhap,
                    Ho = request.Ho.Trim(),
                    Ten = request.Ten.Trim(),
                    NgaySinh = request.NgaySinh,
                    CCCD_Passport = cccd,
                    DiemTichLuy = 0
                };
                _context.KhachHangs.Add(khachHang);

                // 3. Tạo LienHe_DiaChi
                string maLH = await IdGenerator.NextAsync(_context.LienHeDiaChis.Select(l => l.MaLienHe), "LH", 4);

                _context.LienHeDiaChis.Add(new LienHeDiaChi
                {
                    MaLienHe = maLH,
                    MaKhachHang = maKH,
                    SDT = request.SDT.Trim(),
                    Email = request.Email ?? "",
                    DiaChi = request.DiaChi
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                taiKhoan.KhachHang = khachHang;
                return taiKhoan;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
