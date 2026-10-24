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

        // Sai mật khẩu SoLanSaiToiDa lần liên tiếp thì khoá tạm PhutKhoa phút
        public const int SoLanSaiToiDa = 5;
        public const int PhutKhoa = 15;

        public async Task<TaiKhoan> LoginAsync(string tenDangNhap, string matKhau)
        {
            var account = await _context.TaiKhoans
                .Include(t => t.NhanVien)
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);
            if (account == null) return null;

            if (account.KhoaDen != null && account.KhoaDen > DateTime.Now)
                throw new AccountLockedException(account.KhoaDen.Value);

            if (!PasswordHasher.Verify(matKhau, account.MatKhau))
            {
                account.SoLanSai++;
                if (account.SoLanSai >= SoLanSaiToiDa)
                {
                    account.KhoaDen = DateTime.Now.AddMinutes(PhutKhoa);
                    account.SoLanSai = 0;
                    _context.NhatKyThaoTacs.Add(new NhatKyThaoTac
                    {
                        MaDangNhap = account.MaDangNhap,
                        TenDangNhap = account.TenDangNhap,
                        VaiTro = account.VaiTro.ToString(),
                        HanhDong = "Tạm khoá tài khoản",
                        DoiTuong = "Tài khoản",
                        MaDoiTuong = account.MaDangNhap,
                        ChiTiet = $"Đăng nhập sai {SoLanSaiToiDa} lần liên tiếp, khoá đến {account.KhoaDen:HH:mm dd/MM/yyyy}"
                    });
                    await _context.SaveChangesAsync();
                    throw new AccountLockedException(account.KhoaDen.Value);
                }
                await _context.SaveChangesAsync();
                return null;
            }

            if (account.SoLanSai != 0 || account.KhoaDen != null)
            {
                account.SoLanSai = 0;
                account.KhoaDen = null;
                await _context.SaveChangesAsync();
            }

            // Tài khoản cũ còn lưu mật khẩu dạng chữ thường: băm lại ngay khi đăng nhập thành công
            if (!PasswordHasher.IsHashed(account.MatKhau))
            {
                account.MatKhau = PasswordHasher.Hash(matKhau);
                await _context.SaveChangesAsync();
            }

            return account;
        }

        public async Task ChangePasswordAsync(string maDangNhap, string matKhauCu, string matKhauMoi)
        {
            var account = await _context.TaiKhoans.FindAsync(maDangNhap)
                ?? throw new NotFoundException("Không tìm thấy tài khoản.");
            if (!PasswordHasher.Verify(matKhauCu, account.MatKhau))
                throw new BusinessException("Mật khẩu hiện tại không đúng.");
            if (matKhauCu == matKhauMoi)
                throw new BusinessException("Mật khẩu mới phải khác mật khẩu hiện tại.");

            account.MatKhau = PasswordHasher.Hash(matKhauMoi);
            await _context.SaveChangesAsync();
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

            // Khách từng ở dạng vãng lai đã có hồ sơ (do lễ tân tạo) nhưng chưa có tài khoản:
            // cho liên kết tài khoản vào hồ sơ đó nếu SĐT khớp, để giữ lịch sử lưu trú và điểm tích luỹ
            var hoSoCu = await _context.KhachHangs.Include(k => k.LienHeDiaChis)
                .FirstOrDefaultAsync(k => k.CCCD_Passport == cccd);
            if (hoSoCu != null)
            {
                if (hoSoCu.MaDangNhap != null)
                    throw new ConflictException("Số CCCD/Passport này đã có tài khoản. Vui lòng đăng nhập hoặc liên hệ lễ tân để lấy lại mật khẩu.");
                if (!hoSoCu.LienHeDiaChis.Any(l => l.SDT == request.SDT.Trim()))
                    throw new ConflictException("Số CCCD/Passport đã có hồ sơ tại khách sạn nhưng số điện thoại không khớp. Vui lòng liên hệ lễ tân để được hỗ trợ.");
            }

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

                if (hoSoCu != null)
                {
                    // 2'. Gắn tài khoản vào hồ sơ có sẵn; ngày sinh lúc nhận khách vãng lai chỉ là giá trị tạm
                    hoSoCu.MaDangNhap = maDangNhap;
                    hoSoCu.NgaySinh = request.NgaySinh;
                    var lienHe = hoSoCu.LienHeDiaChis.First(l => l.SDT == request.SDT.Trim());
                    if (string.IsNullOrWhiteSpace(lienHe.Email)) lienHe.Email = request.Email ?? "";
                    if (string.IsNullOrWhiteSpace(lienHe.DiaChi)) lienHe.DiaChi = request.DiaChi;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    taiKhoan.KhachHang = hoSoCu;
                    return taiKhoan;
                }

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
