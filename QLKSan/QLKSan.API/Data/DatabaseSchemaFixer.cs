using Microsoft.EntityFrameworkCore;

namespace QLKSan.Data
{
    // Cập nhật schema CSDL khi API khởi động. Mỗi bước kiểm tra trước nên chạy lại nhiều lần không lỗi (idempotent).
    // Các bước chạy RIÊNG RẼ: một bước lỗi không làm các bước sau bị bỏ qua.
    public static class DatabaseSchemaFixer
    {
        private static readonly (string Ten, string Sql)[] Steps =
        {
            ("1. Bỏ UNIQUE trên KhachHang.MaDangNhap (cột cho phép NULL)", DropUniqueConstraintSql("KhachHang", "MaDangNhap")),
            ("2. Bỏ UNIQUE trên KhachHang.CCCD_Passport", DropUniqueConstraintSql("KhachHang", "CCCD_Passport")),
            ("3. Bỏ UNIQUE trên NhanVien.MaDangNhap (cột cho phép NULL)", DropUniqueConstraintSql("NhanVien", "MaDangNhap")),

            ("4. Thêm cột PhieuDatPhong.MaKhuyenMai", @"
                IF COL_LENGTH('PhieuDatPhong', 'MaKhuyenMai') IS NULL
                    ALTER TABLE PhieuDatPhong ADD MaKhuyenMai VARCHAR(10) NULL;"),

            // Cột có ràng buộc UNIQUE nên phải bỏ ràng buộc → đổi kiểu → tạo lại, trong một transaction
            ("5a. TaiKhoan.TenDangNhap VARCHAR → NVARCHAR (hỗ trợ tiếng Việt)", @"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME = 'TaiKhoan' AND COLUMN_NAME = 'TenDangNhap' AND DATA_TYPE = 'varchar')
                BEGIN
                    SET XACT_ABORT ON;
                    BEGIN TRANSACTION;
                    DECLARE @ConstraintName NVARCHAR(200), @Sql NVARCHAR(MAX);
                    SELECT @ConstraintName = tc.CONSTRAINT_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
                    WHERE tc.TABLE_NAME = 'TaiKhoan' AND tc.CONSTRAINT_TYPE = 'UNIQUE' AND ccu.COLUMN_NAME = 'TenDangNhap';
                    IF @ConstraintName IS NOT NULL
                    BEGIN
                        SET @Sql = 'ALTER TABLE TaiKhoan DROP CONSTRAINT ' + QUOTENAME(@ConstraintName);
                        EXEC sp_executesql @Sql;
                    END
                    ALTER TABLE TaiKhoan ALTER COLUMN TenDangNhap NVARCHAR(50) NOT NULL;
                    ALTER TABLE TaiKhoan ADD CONSTRAINT UQ_TaiKhoan_TenDangNhap UNIQUE (TenDangNhap);
                    COMMIT TRANSACTION;
                END"),

            ("5b. TaiKhoan.MatKhau VARCHAR → NVARCHAR", @"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_NAME = 'TaiKhoan' AND COLUMN_NAME = 'MatKhau' AND DATA_TYPE = 'varchar')
                    ALTER TABLE TaiKhoan ALTER COLUMN MatKhau NVARCHAR(255) NOT NULL;"),

            // Mức 1.1: tiền cọc trên hoá đơn + số tiền còn phải thu sau khi trừ cọc.
            // Cột tính toán không được tham chiếu cột tính toán khác nên SoTienConLai viết lại đầy đủ công thức.
            ("6a. Thêm cột HoaDon.TienCoc", @"
                IF COL_LENGTH('HoaDon', 'TienCoc') IS NULL
                    ALTER TABLE HoaDon ADD TienCoc DECIMAL(18,0) NOT NULL CONSTRAINT DF_HoaDon_TienCoc DEFAULT 0;"),
            ("6b. Thêm cột tính toán HoaDon.SoTienConLai", @"
                IF COL_LENGTH('HoaDon', 'SoTienConLai') IS NULL
                    EXEC('ALTER TABLE HoaDon ADD SoTienConLai AS ((TongTienPhong + TongTienDichVu + TienPhat - GiamGiaKhuyenMai) * 1.08 - TienCoc)');"),

            // Mức 1.6: khuyến mãi còn hiệu lực trọn ngày kết thúc — so sánh theo NGÀY thay vì thời điểm có giờ
            ("7. Sửa cột tính toán KhuyenMai.TrangThai", @"
                IF EXISTS (SELECT 1 FROM sys.computed_columns
                           WHERE object_id = OBJECT_ID('KhuyenMai') AND name = 'TrangThai' AND definition NOT LIKE '%CONVERT(%date%')
                BEGIN
                    ALTER TABLE KhuyenMai DROP COLUMN TrangThai;
                    EXEC('ALTER TABLE KhuyenMai ADD TrangThai AS (CASE WHEN CAST(GETDATE() AS DATE) BETWEEN NgayBatDau AND NgayKetThuc THEN 1 ELSE 0 END)');
                END"),

            // Mức 1.2: 'Đã đặt' không còn lưu trong Phong.TrangThai — được suy ra từ phiếu đặt theo khoảng ngày
            ("8. Chuyển phòng 'Đã đặt' về 'Trống'", @"
                UPDATE Phong SET TrangThai = N'Trống' WHERE TrangThai = N'Đã đặt';"),

            // Mức 2.4: công việc buồng phòng
            ("9. Tạo bảng PhanCongDonPhong", @"
                IF OBJECT_ID('PhanCongDonPhong') IS NULL
                CREATE TABLE PhanCongDonPhong (
                    MaPhanCong VARCHAR(10) PRIMARY KEY,
                    MaPhong VARCHAR(10) NOT NULL,
                    MaNhanVien VARCHAR(10) NULL,
                    MaDatPhong VARCHAR(10) NULL,
                    LoaiCongViec NVARCHAR(50) NOT NULL DEFAULT N'Dọn sau trả phòng',
                    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Chờ giao',
                    ThoiGianTao DATETIME NOT NULL DEFAULT GETDATE(),
                    ThoiGianGiao DATETIME NULL,
                    ThoiGianBatDau DATETIME NULL,
                    ThoiGianHoanThanh DATETIME NULL,
                    GhiChu NVARCHAR(500) NULL,
                    CONSTRAINT FK_PCDP_Phong FOREIGN KEY (MaPhong) REFERENCES Phong(MaPhong),
                    CONSTRAINT FK_PCDP_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien),
                    CONSTRAINT FK_PCDP_PhieuDat FOREIGN KEY (MaDatPhong) REFERENCES PhieuDatPhong(MaDatPhong)
                );"),

            // Mức 2.5: phân biệt giao dịch thu/hoàn và nhân viên thực hiện
            ("10a. Thêm cột ThanhToan.LoaiGiaoDich", @"
                IF COL_LENGTH('ThanhToan', 'LoaiGiaoDich') IS NULL
                    ALTER TABLE ThanhToan ADD LoaiGiaoDich NVARCHAR(20) NOT NULL CONSTRAINT DF_ThanhToan_LoaiGiaoDich DEFAULT N'Thu';"),
            ("10b. Thêm cột ThanhToan.MaNhanVien", @"
                IF COL_LENGTH('ThanhToan', 'MaNhanVien') IS NULL
                BEGIN
                    ALTER TABLE ThanhToan ADD MaNhanVien VARCHAR(10) NULL CONSTRAINT FK_ThanhToan_NhanVien REFERENCES NhanVien(MaNhanVien);
                    -- Giao dịch cũ: coi người lập hoá đơn là người thu tiền
                    EXEC('UPDATE t SET MaNhanVien = h.MaNhanVienLap FROM ThanhToan t JOIN HoaDon h ON h.MaHoaDon = t.MaHoaDon WHERE t.MaNhanVien IS NULL');
                END"),

            // Phòng đang "Chờ dọn dẹp" từ trước khi có bảng công việc: tạo việc "Chờ giao" để hiện trong bảng phân công
            ("11. Tạo công việc cho phòng đang chờ dọn", @"
                INSERT INTO PhanCongDonPhong (MaPhanCong, MaPhong, LoaiCongViec, TrangThai, ThoiGianTao)
                SELECT 'PC' + RIGHT('0000' + CAST(
                           ISNULL((SELECT MAX(CAST(SUBSTRING(MaPhanCong, 3, 10) AS INT)) FROM PhanCongDonPhong
                                   WHERE MaPhanCong LIKE 'PC%' AND SUBSTRING(MaPhanCong, 3, 10) NOT LIKE '%[^0-9]%'), 0)
                           + ROW_NUMBER() OVER (ORDER BY p.MaPhong) AS VARCHAR(10)), 4),
                       p.MaPhong, N'Dọn sau trả phòng', N'Chờ giao', GETDATE()
                FROM Phong p
                WHERE p.TrangThai = N'Chờ dọn dẹp'
                  AND NOT EXISTS (SELECT 1 FROM PhanCongDonPhong t
                                  WHERE t.MaPhong = p.MaPhong AND t.TrangThai IN (N'Chờ giao', N'Đã giao', N'Đang dọn'));"),

            // Mức 3.1: giá theo mùa / cuối tuần / lễ
            ("12. Tạo bảng QuyTacGia", @"
                IF OBJECT_ID('QuyTacGia') IS NULL
                CREATE TABLE QuyTacGia (
                    MaQuyTac VARCHAR(10) PRIMARY KEY,
                    TenQuyTac NVARCHAR(100) NOT NULL,
                    MaLoai VARCHAR(10) NULL,
                    TuNgay DATE NULL,
                    DenNgay DATE NULL,
                    CacThu VARCHAR(20) NULL,
                    PhanTramDieuChinh DECIMAL(5,2) NOT NULL,
                    UuTien INT NOT NULL DEFAULT 1,
                    KichHoat BIT NOT NULL DEFAULT 1,
                    CONSTRAINT FK_QuyTacGia_LoaiPhong FOREIGN KEY (MaLoai) REFERENCES LoaiPhong(MaLoai),
                    CONSTRAINT CK_QuyTacGia_PhanTram CHECK (PhanTramDieuChinh BETWEEN -90 AND 300)
                );"),
            ("13. Thêm cột HoaDon.SoDem", @"
                IF COL_LENGTH('HoaDon', 'SoDem') IS NULL
                BEGIN
                    ALTER TABLE HoaDon ADD SoDem INT NOT NULL CONSTRAINT DF_HoaDon_SoDem DEFAULT 0;
                    -- Hoá đơn cũ tính 1 giá cho mọi đêm nên suy ra được số đêm
                    EXEC('UPDATE h SET SoDem = ROUND(h.TongTienPhong / c.GiaThueThucTe, 0)
                          FROM HoaDon h CROSS APPLY (SELECT TOP 1 GiaThueThucTe FROM ChiTietDatPhong WHERE MaDatPhong = h.MaDatPhong AND GiaThueThucTe > 0) c');
                END"),

            // Kế hoạch khách hàng GĐ3: hạng phòng giới thiệu chi tiết, đặt theo hạng phòng, đánh giá theo lần lưu trú
            ("14. Thêm cột giới thiệu cho LoaiPhong", @"
                IF COL_LENGTH('LoaiPhong', 'DienTich') IS NULL ALTER TABLE LoaiPhong ADD DienTich DECIMAL(6,1) NULL;
                IF COL_LENGTH('LoaiPhong', 'LoaiGiuong') IS NULL ALTER TABLE LoaiPhong ADD LoaiGiuong NVARCHAR(100) NULL;
                IF COL_LENGTH('LoaiPhong', 'HuongNhin') IS NULL ALTER TABLE LoaiPhong ADD HuongNhin NVARCHAR(100) NULL;
                IF COL_LENGTH('LoaiPhong', 'MoTaChiTiet') IS NULL ALTER TABLE LoaiPhong ADD MoTaChiTiet NVARCHAR(MAX) NULL;
                IF COL_LENGTH('LoaiPhong', 'AnhDaiDien') IS NULL ALTER TABLE LoaiPhong ADD AnhDaiDien NVARCHAR(255) NULL;"),
            ("15. Tạo bảng HinhAnhLoaiPhong", @"
                IF OBJECT_ID('HinhAnhLoaiPhong') IS NULL
                CREATE TABLE HinhAnhLoaiPhong (
                    MaHinhAnh VARCHAR(10) PRIMARY KEY,
                    MaLoai VARCHAR(10) NOT NULL,
                    DuongDan NVARCHAR(255) NOT NULL,
                    ThuTu INT NOT NULL DEFAULT 0,
                    CONSTRAINT FK_HinhAnhLoaiPhong_LoaiPhong FOREIGN KEY (MaLoai) REFERENCES LoaiPhong(MaLoai) ON DELETE CASCADE
                );"),
            ("16. Thêm yêu cầu riêng / giờ đến cho PhieuDatPhong", @"
                IF COL_LENGTH('PhieuDatPhong', 'YeuCauDacBiet') IS NULL ALTER TABLE PhieuDatPhong ADD YeuCauDacBiet NVARCHAR(500) NULL;
                IF COL_LENGTH('PhieuDatPhong', 'GioDenDuKien') IS NULL ALTER TABLE PhieuDatPhong ADD GioDenDuKien NVARCHAR(30) NULL;"),
            ("17. Thêm DichVu.LaTraiNghiem / MoTa", @"
                IF COL_LENGTH('DichVu', 'LaTraiNghiem') IS NULL
                BEGIN
                    ALTER TABLE DichVu ADD LaTraiNghiem BIT NOT NULL CONSTRAINT DF_DichVu_LaTraiNghiem DEFAULT 0;
                    -- Trước đây trang chủ tự nhận diện Spa / ăn sáng / đưa đón là trải nghiệm: giữ nguyên hiển thị đó
                    EXEC('UPDATE DichVu SET LaTraiNghiem = 1 WHERE TenDichVu LIKE N''%Spa%'' OR TenDichVu LIKE N''%Massage%''
                          OR TenDichVu LIKE N''%Buffet%'' OR TenDichVu LIKE N''%Ăn sáng%'' OR TenDichVu LIKE N''%Đưa đón%''');
                END
                IF COL_LENGTH('DichVu', 'MoTa') IS NULL ALTER TABLE DichVu ADD MoTa NVARCHAR(500) NULL;"),
            ("18. Thêm DanhGia.MaDatPhong (đánh giá theo lần lưu trú)", @"
                IF COL_LENGTH('DanhGia', 'MaDatPhong') IS NULL
                BEGIN
                    ALTER TABLE DanhGia ADD MaDatPhong VARCHAR(10) NULL
                        CONSTRAINT FK_DanhGia_PhieuDatPhong FOREIGN KEY REFERENCES PhieuDatPhong(MaDatPhong);
                    -- Đánh giá cũ: gắn vào lần trả phòng gần nhất của khách với phòng đó
                    EXEC('UPDATE d SET MaDatPhong = x.MaDatPhong FROM DanhGia d
                          CROSS APPLY (SELECT TOP 1 p.MaDatPhong FROM PhieuDatPhong p JOIN ChiTietDatPhong c ON c.MaDatPhong = p.MaDatPhong
                                       WHERE p.MaKhachHang = d.MaKhachHang AND c.MaPhong = d.MaPhong AND p.TrangThai = N''Đã trả phòng''
                                       ORDER BY p.NgayCheckOutThucTe DESC) x');
                    EXEC('CREATE UNIQUE INDEX UX_DanhGia_MaDatPhong ON DanhGia(MaDatPhong) WHERE MaDatPhong IS NOT NULL');
                END"),

            // GĐ4: ưu đãi hạng thành viên trên hoá đơn — cột tính toán VAT / tổng / còn lại phải trừ thêm GiamGiaThanhVien
            ("19. Thêm HoaDon.GiamGiaThanhVien, HangThanhVien và cập nhật cột tính toán", @"
                IF COL_LENGTH('HoaDon', 'GiamGiaThanhVien') IS NULL
                BEGIN
                    BEGIN TRANSACTION;
                    ALTER TABLE HoaDon ADD GiamGiaThanhVien DECIMAL(18,0) NOT NULL CONSTRAINT DF_HoaDon_GiamGiaThanhVien DEFAULT 0,
                                           HangThanhVien NVARCHAR(50) NULL;
                    EXEC('ALTER TABLE HoaDon DROP COLUMN ThueVAT, TongTienCanThanhToan, SoTienConLai');
                    EXEC('ALTER TABLE HoaDon ADD
                        ThueVAT AS ((TongTienPhong + TongTienDichVu + TienPhat - GiamGiaKhuyenMai - GiamGiaThanhVien) * 0.08),
                        TongTienCanThanhToan AS ((TongTienPhong + TongTienDichVu + TienPhat - GiamGiaKhuyenMai - GiamGiaThanhVien) * 1.08),
                        SoTienConLai AS ((TongTienPhong + TongTienDichVu + TienPhat - GiamGiaKhuyenMai - GiamGiaThanhVien) * 1.08 - TienCoc)');
                    COMMIT TRANSACTION;
                END"),

            // GĐ4: điểm đã đổi — hạng thành viên xét theo điểm trọn đời (số dư + đã đổi)
            ("20. Thêm KhachHang.DiemDaDoi", @"
                IF COL_LENGTH('KhachHang', 'DiemDaDoi') IS NULL
                    ALTER TABLE KhachHang ADD DiemDaDoi INT NOT NULL CONSTRAINT DF_KhachHang_DiemDaDoi DEFAULT 0;"),

            // A2: khuyến mãi bật/tắt + điều kiện áp dụng (số lượt tối đa, số đêm tối thiểu, hạng phòng)
            ("21. Thêm KhuyenMai.KichHoat, SoLuotToiDa, SoDemToiThieu, MaLoai", @"
                IF COL_LENGTH('KhuyenMai', 'KichHoat') IS NULL
                    ALTER TABLE KhuyenMai ADD KichHoat BIT NOT NULL CONSTRAINT DF_KhuyenMai_KichHoat DEFAULT 1,
                                              SoLuotToiDa INT NULL,
                                              SoDemToiThieu INT NULL,
                                              MaLoai VARCHAR(10) NULL CONSTRAINT FK_KhuyenMai_LoaiPhong FOREIGN KEY REFERENCES LoaiPhong(MaLoai);"),

            // A2: phản hồi đánh giá + ẩn đánh giá
            ("22. Thêm DanhGia.PhanHoi, NgayPhanHoi, MaNhanVienPhanHoi, An", @"
                IF COL_LENGTH('DanhGia', 'PhanHoi') IS NULL
                    ALTER TABLE DanhGia ADD PhanHoi NVARCHAR(1000) NULL,
                                            NgayPhanHoi DATETIME NULL,
                                            MaNhanVienPhanHoi VARCHAR(10) NULL CONSTRAINT FK_DanhGia_NhanVien FOREIGN KEY REFERENCES NhanVien(MaNhanVien),
                                            An BIT NOT NULL CONSTRAINT DF_DanhGia_An DEFAULT 0;")
        };

        private static string DropUniqueConstraintSql(string table, string column) => $@"
            DECLARE @ConstraintName NVARCHAR(200), @Sql NVARCHAR(MAX);
            SELECT @ConstraintName = tc.CONSTRAINT_NAME
            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
            JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
            WHERE tc.TABLE_NAME = '{table}' AND tc.CONSTRAINT_TYPE = 'UNIQUE' AND ccu.COLUMN_NAME = '{column}';
            IF @ConstraintName IS NOT NULL
            BEGIN
                SET @Sql = 'ALTER TABLE {table} DROP CONSTRAINT ' + QUOTENAME(@ConstraintName);
                EXEC sp_executesql @Sql;
            END";

        public static void ApplySchemaFixes(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            foreach (var (ten, sql) in Steps)
            {
                try
                {
                    context.Database.ExecuteSqlRaw(sql);
                }
                catch (Exception ex)
                {
                    app.Logger.LogError(ex, "Cập nhật schema thất bại ở bước '{Buoc}': {Message}", ten, ex.Message);
                }
            }
        }
    }
}
