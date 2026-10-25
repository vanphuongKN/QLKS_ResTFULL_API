using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    // Bộ ảnh của hạng phòng do quản trị tải lên; ảnh đầu tiên (theo thứ tự) là ảnh đại diện của hạng
    public interface IRoomTypeImageService
    {
        // Ném BusinessException nếu file không phải ảnh JPG/PNG/WebP hoặc vượt quá dung lượng cho phép
        Task<HinhAnhLoaiPhong> AddAsync(string maLoai, Stream content, string fileName);

        Task DeleteAsync(string maLoai, string maHinhAnh);
    }

    public class RoomTypeImageService : IRoomTypeImageService
    {
        public const long DungLuongToiDa = 5 * 1024 * 1024;
        private static readonly string[] DuoiHopLe = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly ApplicationDbContext _context;
        private readonly MediaStorage _storage;

        public RoomTypeImageService(ApplicationDbContext context, MediaStorage storage)
        {
            _context = context;
            _storage = storage;
        }

        public async Task<HinhAnhLoaiPhong> AddAsync(string maLoai, Stream content, string fileName)
        {
            var loai = await _context.LoaiPhongs.Include(l => l.HinhAnhs).FirstOrDefaultAsync(l => l.MaLoai == maLoai)
                ?? throw new NotFoundException($"Không tìm thấy hạng phòng '{maLoai}'.");

            var duoi = Path.GetExtension(fileName ?? "").ToLowerInvariant();
            if (!DuoiHopLe.Contains(duoi))
                throw new BusinessException("Chỉ nhận ảnh JPG, PNG hoặc WebP.");

            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer);
            if (buffer.Length == 0)
                throw new BusinessException("Vui lòng chọn ảnh cần tải lên.");
            if (buffer.Length > DungLuongToiDa)
                throw new BusinessException("Ảnh tối đa 5 MB.");
            if (!LaAnh(buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, 12))))
                throw new BusinessException("File không phải ảnh hợp lệ.");

            var tenFile = $"{Guid.NewGuid():N}{(duoi == ".jpeg" ? ".jpg" : duoi)}";
            var thuMuc = Path.Combine(_storage.RootPath, "room-types", maLoai);
            Directory.CreateDirectory(thuMuc);
            await File.WriteAllBytesAsync(Path.Combine(thuMuc, tenFile), buffer.ToArray());

            var anh = new HinhAnhLoaiPhong
            {
                MaHinhAnh = await NextIdAsync(),
                MaLoai = maLoai,
                DuongDan = $"{MediaStorage.UrlPrefix}/room-types/{maLoai}/{tenFile}",
                ThuTu = loai.HinhAnhs.Count == 0 ? 1 : loai.HinhAnhs.Max(h => h.ThuTu) + 1
            };
            loai.HinhAnhs.Add(anh);
            CapNhatAnhDaiDien(loai, null);
            await _context.SaveChangesAsync();
            return anh;
        }

        public async Task DeleteAsync(string maLoai, string maHinhAnh)
        {
            var loai = await _context.LoaiPhongs.Include(l => l.HinhAnhs).FirstOrDefaultAsync(l => l.MaLoai == maLoai)
                ?? throw new NotFoundException($"Không tìm thấy hạng phòng '{maLoai}'.");
            var anh = loai.HinhAnhs.FirstOrDefault(h => h.MaHinhAnh == maHinhAnh)
                ?? throw new NotFoundException($"Không tìm thấy ảnh '{maHinhAnh}' của hạng phòng {maLoai}.");

            loai.HinhAnhs.Remove(anh);
            _context.HinhAnhLoaiPhongs.Remove(anh);
            CapNhatAnhDaiDien(loai, anh.DuongDan);
            await _context.SaveChangesAsync();

            // Chỉ xoá file do hệ thống lưu (ảnh mẫu /images/... thuộc Web, không nằm ở đây)
            var path = _storage.ToPhysicalPath(anh.DuongDan);
            if (path != null && File.Exists(path)) File.Delete(path);
        }

        // Ảnh đại diện = ảnh đầu tiên của bộ ảnh; hết ảnh thì bỏ ảnh đại diện nếu nó là ảnh vừa xoá
        private static void CapNhatAnhDaiDien(LoaiPhong loai, string duongDanVuaXoa)
        {
            var dau = loai.HinhAnhs.OrderBy(h => h.ThuTu).ThenBy(h => h.MaHinhAnh).FirstOrDefault();
            if (dau != null) loai.AnhDaiDien = dau.DuongDan;
            else if (loai.AnhDaiDien == duongDanVuaXoa) loai.AnhDaiDien = null;
        }

        private async Task<string> NextIdAsync()
        {
            var ids = await _context.HinhAnhLoaiPhongs.Select(h => h.MaHinhAnh).ToListAsync();
            var max = ids.Where(i => i.StartsWith("HL")).Select(i => int.TryParse(i.Substring(2), out var n) ? n : 0).DefaultIfEmpty(0).Max();
            return "HL" + (max + 1).ToString("D2");
        }

        // Kiểm tra chữ ký đầu file: JPEG FF D8 FF, PNG 89 50 4E 47, WebP "RIFF....WEBP"
        private static bool LaAnh(ReadOnlySpan<byte> h)
        {
            if (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return true;
            if (h.Length >= 4 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47) return true;
            return h.Length >= 12 && h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F' && h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P';
        }
    }
}
