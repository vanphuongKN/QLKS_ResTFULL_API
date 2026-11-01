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
                                            An BIT NOT NULL CONSTRAINT DF_DanhGia_An DEFAULT 0;"),

            // A3: chăm sóc khách VIP + hạn chế đặt online
            ("23. Thêm KhachHang.LaVIP, SoThich, GhiChuNoiBo, HanChe, LyDoHanChe", @"
                IF COL_LENGTH('KhachHang', 'LaVIP') IS NULL
                    ALTER TABLE KhachHang ADD LaVIP BIT NOT NULL CONSTRAINT DF_KhachHang_LaVIP DEFAULT 0,
                                              SoThich NVARCHAR(500) NULL,
                                              GhiChuNoiBo NVARCHAR(1000) NULL,
                                              HanChe BIT NOT NULL CONSTRAINT DF_KhachHang_HanChe DEFAULT 0,
                                              LyDoHanChe NVARCHAR(255) NULL;"),

            // A3: lịch sử điểm; khách đang có điểm được ghi một dòng số dư đầu kỳ để tổng lịch sử = số điểm hiện có
            ("24. Tạo bảng LichSuDiem", @"
                IF OBJECT_ID('LichSuDiem', 'U') IS NULL
                BEGIN
                    CREATE TABLE LichSuDiem (
                        MaLichSu VARCHAR(12) PRIMARY KEY,
                        MaKhachHang VARCHAR(10) NOT NULL CONSTRAINT FK_LichSuDiem_KhachHang FOREIGN KEY REFERENCES KhachHang(MaKhachHang),
                        ThoiGian DATETIME NOT NULL CONSTRAINT DF_LichSuDiem_ThoiGian DEFAULT GETDATE(),
                        SoDiem INT NOT NULL,
                        Loai NVARCHAR(30) NOT NULL,
                        NoiDung NVARCHAR(255) NULL,
                        MaHoaDon VARCHAR(10) NULL,
                        MaNhanVien VARCHAR(10) NULL CONSTRAINT FK_LichSuDiem_NhanVien FOREIGN KEY REFERENCES NhanVien(MaNhanVien)
                    );
                    CREATE INDEX IX_LichSuDiem_KhachHang ON LichSuDiem(MaKhachHang, ThoiGian);
                    INSERT INTO LichSuDiem (MaLichSu, MaKhachHang, ThoiGian, SoDiem, Loai, NoiDung)
                    SELECT 'LSD' + RIGHT('000000' + CAST(ROW_NUMBER() OVER (ORDER BY MaKhachHang) AS VARCHAR(6)), 6),
                           MaKhachHang, GETDATE(), DiemTichLuy, N'Số dư đầu kỳ', N'Điểm có trước khi ghi lịch sử điểm'
                    FROM KhachHang WHERE DiemTichLuy > 0;
                END"),

            // A4: khoá tạm tài khoản khi đăng nhập sai nhiều lần
            ("25. Thêm TaiKhoan.SoLanSai, KhoaDen", @"
                IF COL_LENGTH('TaiKhoan', 'SoLanSai') IS NULL
                    ALTER TABLE TaiKhoan ADD SoLanSai INT NOT NULL CONSTRAINT DF_TaiKhoan_SoLanSai DEFAULT 0,
                                             KhoaDen DATETIME NULL;"),

            // A4: nhật ký thao tác
            ("26. Tạo bảng NhatKyThaoTac", @"
                IF OBJECT_ID('NhatKyThaoTac', 'U') IS NULL
                BEGIN
                    CREATE TABLE NhatKyThaoTac (
                        MaNhatKy BIGINT IDENTITY(1,1) PRIMARY KEY,
                        ThoiGian DATETIME NOT NULL CONSTRAINT DF_NhatKy_ThoiGian DEFAULT GETDATE(),
                        MaDangNhap VARCHAR(10) NULL,
                        TenDangNhap NVARCHAR(50) NULL,
                        VaiTro NVARCHAR(20) NULL,
                        HanhDong NVARCHAR(100) NOT NULL,
                        DoiTuong NVARCHAR(50) NULL,
                        MaDoiTuong NVARCHAR(100) NULL,
                        ChiTiet NVARCHAR(1000) NULL,
                        DiaChiIP VARCHAR(50) NULL
                    );
                    CREATE INDEX IX_NhatKy_ThoiGian ON NhatKyThaoTac(ThoiGian DESC);
                END"),

            // A4: cấu hình nghiệp vụ sửa trên web (chưa có dòng thì dùng appsettings.json)
            ("27. Tạo bảng CauHinh", @"
                IF OBJECT_ID('CauHinh', 'U') IS NULL
                    CREATE TABLE CauHinh (
                        Khoa NVARCHAR(50) PRIMARY KEY,
                        GiaTri NVARCHAR(MAX) NOT NULL,
                        NgayCapNhat DATETIME NOT NULL CONSTRAINT DF_CauHinh_NgayCapNhat DEFAULT GETDATE(),
                        MaNhanVien VARCHAR(10) NULL
                    );"),

            // L2: kênh đặt phòng + sổ tiền cọc (thu / hoàn, phương thức, người thu); cọc đang có được ghi một dòng đầu kỳ
            ("28. Thêm PhieuDatPhong.NguonDat", @"
                IF COL_LENGTH('PhieuDatPhong', 'NguonDat') IS NULL
                BEGIN
                    ALTER TABLE PhieuDatPhong ADD NguonDat NVARCHAR(30) NULL;
                    EXEC('UPDATE PhieuDatPhong SET NguonDat = CASE WHEN MaDatPhong LIKE ''PDP%'' THEN N''Online'' ELSE N''Vãng lai'' END;');
                END"),
            ("29. Tạo bảng GiaoDichCoc", @"
                IF OBJECT_ID('GiaoDichCoc', 'U') IS NULL
                BEGIN
                    CREATE TABLE GiaoDichCoc (
                        MaGiaoDichCoc INT IDENTITY(1,1) PRIMARY KEY,
                        MaDatPhong VARCHAR(10) NOT NULL CONSTRAINT FK_GiaoDichCoc_PhieuDat FOREIGN KEY REFERENCES PhieuDatPhong(MaDatPhong),
                        LoaiGiaoDich NVARCHAR(20) NOT NULL CONSTRAINT DF_GiaoDichCoc_Loai DEFAULT N'Thu',
                        SoTien DECIMAL(18,0) NOT NULL CHECK (SoTien >= 0),
                        PhuongThuc NVARCHAR(50) NULL,
                        ThoiGian DATETIME NOT NULL CONSTRAINT DF_GiaoDichCoc_ThoiGian DEFAULT GETDATE(),
                        MaNhanVien VARCHAR(10) NULL CONSTRAINT FK_GiaoDichCoc_NhanVien FOREIGN KEY REFERENCES NhanVien(MaNhanVien),
                        GhiChu NVARCHAR(255) NULL
                    );
                    CREATE INDEX IX_GiaoDichCoc_PhieuDat ON GiaoDichCoc(MaDatPhong);
                    CREATE INDEX IX_GiaoDichCoc_ThoiGian ON GiaoDichCoc(ThoiGian);
                    INSERT INTO GiaoDichCoc (MaDatPhong, LoaiGiaoDich, SoTien, PhuongThuc, ThoiGian, MaNhanVien, GhiChu)
                    SELECT MaDatPhong, N'Thu', TienCoc, N'Không rõ', ISNULL(NgayCheckInThucTe, NgayDat), MaNhanVien, N'Tiền cọc có trước khi ghi sổ cọc'
                    FROM PhieuDatPhong WHERE TienCoc > 0;
                END"),

            // L3: người đi cùng (sổ lưu trú đủ người) + lịch sử đổi phòng khi khách đang ở
            ("30. Tạo bảng KhachDiCung", @"
                IF OBJECT_ID('KhachDiCung', 'U') IS NULL
                BEGIN
                    CREATE TABLE KhachDiCung (
                        MaKhachDiCung INT IDENTITY(1,1) PRIMARY KEY,
                        MaDatPhong VARCHAR(10) NOT NULL CONSTRAINT FK_KhachDiCung_PhieuDat FOREIGN KEY REFERENCES PhieuDatPhong(MaDatPhong),
                        HoTen NVARCHAR(100) NOT NULL,
                        CCCD_Passport VARCHAR(20) NULL,
                        QuocTich NVARCHAR(50) NULL
                    );
                    CREATE INDEX IX_KhachDiCung_PhieuDat ON KhachDiCung(MaDatPhong);
                END"),
            ("31. Tạo bảng LichSuDoiPhong", @"
                IF OBJECT_ID('LichSuDoiPhong', 'U') IS NULL
                BEGIN
                    CREATE TABLE LichSuDoiPhong (
                        MaLichSu INT IDENTITY(1,1) PRIMARY KEY,
                        MaDatPhong VARCHAR(10) NOT NULL CONSTRAINT FK_LichSuDoiPhong_PhieuDat FOREIGN KEY REFERENCES PhieuDatPhong(MaDatPhong),
                        TuPhong VARCHAR(10) NOT NULL CONSTRAINT FK_LichSuDoiPhong_PhongCu FOREIGN KEY REFERENCES Phong(MaPhong),
                        DenPhong VARCHAR(10) NOT NULL CONSTRAINT FK_LichSuDoiPhong_PhongMoi FOREIGN KEY REFERENCES Phong(MaPhong),
                        ThoiGian DATETIME NOT NULL CONSTRAINT DF_LichSuDoiPhong_ThoiGian DEFAULT GETDATE(),
                        LyDo NVARCHAR(255) NULL,
                        GiaCu DECIMAL(18,0) NOT NULL,
                        GiaMoi DECIMAL(18,0) NOT NULL,
                        MaNhanVien VARCHAR(10) NULL CONSTRAINT FK_LichSuDoiPhong_NhanVien FOREIGN KEY REFERENCES NhanVien(MaNhanVien)
                    );
                    CREATE INDEX IX_LichSuDoiPhong_PhieuDat ON LichSuDoiPhong(MaDatPhong);
                END"),

            // L4: thông tin xuất hoá đơn VAT công ty
            ("32. Thêm HoaDon.TenCongTy, MaSoThue, DiaChiCongTy, EmailHoaDon", @"
                IF COL_LENGTH('HoaDon', 'MaSoThue') IS NULL
                    ALTER TABLE HoaDon ADD TenCongTy NVARCHAR(200) NULL,
                                           MaSoThue VARCHAR(20) NULL,
                                           DiaChiCongTy NVARCHAR(300) NULL,
                                           EmailHoaDon NVARCHAR(100) NULL;"),

            // L5: sổ yêu cầu khách + chốt ca / bàn giao
            ("33. Tạo bảng YeuCauKhach", @"
                IF OBJECT_ID('YeuCauKhach', 'U') IS NULL
                BEGIN
                    CREATE TABLE YeuCauKhach (
                        MaYeuCau INT IDENTITY(1,1) PRIMARY KEY,
                        MaDatPhong VARCHAR(10) NULL CONSTRAINT FK_YeuCauKhach_PhieuDat FOREIGN KEY REFERENCES PhieuDatPhong(MaDatPhong),
                        MaPhong VARCHAR(10) NULL CONSTRAINT FK_YeuCauKhach_Phong FOREIGN KEY REFERENCES Phong(MaPhong),
                        Loai NVARCHAR(30) NOT NULL,
                        NoiDung NVARCHAR(500) NOT NULL,
                        ThoiGianHen DATETIME NULL,
                        TrangThai NVARCHAR(20) NOT NULL CONSTRAINT DF_YeuCauKhach_TrangThai DEFAULT N'Mới',
                        MaNhanVienTao VARCHAR(10) NULL CONSTRAINT FK_YeuCauKhach_NVTao FOREIGN KEY REFERENCES NhanVien(MaNhanVien),
                        ThoiGianTao DATETIME NOT NULL CONSTRAINT DF_YeuCauKhach_ThoiGianTao DEFAULT GETDATE(),
                        MaNhanVienXuLy VARCHAR(10) NULL CONSTRAINT FK_YeuCauKhach_NVXuLy FOREIGN KEY REFERENCES NhanVien(MaNhanVien),
                        ThoiGianXuLy DATETIME NULL,
                        KetQua NVARCHAR(500) NULL
                    );
                    CREATE INDEX IX_YeuCauKhach_TrangThai ON YeuCauKhach(TrangThai, ThoiGianHen);
                END"),
            ("34. Tạo bảng GiaoCa", @"
                IF OBJECT_ID('GiaoCa', 'U') IS NULL
                BEGIN
                    CREATE TABLE GiaoCa (
                        MaGiaoCa INT IDENTITY(1,1) PRIMARY KEY,
                        MaNhanVien VARCHAR(10) NOT NULL CONSTRAINT FK_GiaoCa_NhanVien FOREIGN KEY REFERENCES NhanVien(MaNhanVien),
                        BatDau DATETIME NOT NULL,
                        KetThuc DATETIME NOT NULL,
                        TongThuRong DECIMAL(18,0) NOT NULL,
                        TienMatHeThong DECIMAL(18,0) NOT NULL,
                        TienMatThucTe DECIMAL(18,0) NOT NULL,
                        ChenhLech DECIMAL(18,0) NOT NULL,
                        GhiChuBanGiao NVARCHAR(1000) NULL,
                        ChiTiet NVARCHAR(MAX) NULL
                    );
                    CREATE INDEX IX_GiaoCa_KetThuc ON GiaoCa(KetThuc DESC);
                END"),

            // B1: việc buồng phòng xử lý yêu cầu của khách
            ("35. Thêm PhanCongDonPhong.MaYeuCau", @"
                IF COL_LENGTH('PhanCongDonPhong', 'MaYeuCau') IS NULL
                    ALTER TABLE PhanCongDonPhong ADD MaYeuCau INT NULL CONSTRAINT FK_PCDP_YeuCauKhach FOREIGN KEY REFERENCES YeuCauKhach(MaYeuCau);"),

            // B3: kiểm tra phòng, đồ minibar, phiếu kỹ thuật
            ("36. Thêm PhanCongDonPhong.ThoiGianKiemTra, MaNhanVienKiemTra, KetQuaKiemTra, SoLanKhongDat", @"
                IF COL_LENGTH('PhanCongDonPhong', 'ThoiGianKiemTra') IS NULL
                    ALTER TABLE PhanCongDonPhong ADD ThoiGianKiemTra DATETIME NULL,
                                                     MaNhanVienKiemTra VARCHAR(10) NULL CONSTRAINT FK_PCDP_NVKiemTra FOREIGN KEY REFERENCES NhanVien(MaNhanVien),
                                                     KetQuaKiemTra NVARCHAR(500) NULL,
                                                     SoLanKhongDat INT NOT NULL CONSTRAINT DF_PCDP_SoLanKhongDat DEFAULT 0;"),
            ("37. Thêm DichVu.LaMinibar", @"
                IF COL_LENGTH('DichVu', 'LaMinibar') IS NULL
                    ALTER TABLE DichVu ADD LaMinibar BIT NOT NULL CONSTRAINT DF_DichVu_LaMinibar DEFAULT 0;"),
            ("38. Tạo bảng PhieuKyThuat", @"
                IF OBJECT_ID('PhieuKyThuat', 'U') IS NULL
                BEGIN
                    CREATE TABLE PhieuKyThuat (
                        MaPhieu INT IDENTITY(1,1) PRIMARY KEY,
                        MaPhong VARCHAR(10) NOT NULL CONSTRAINT FK_PhieuKyThuat_Phong FOREIGN KEY REFERENCES Phong(MaPhong),
                        MoTa NVARCHAR(500) NOT NULL,
                        MucDo NVARCHAR(20) NOT NULL CONSTRAINT DF_PhieuKyThuat_MucDo DEFAULT N'Thường',
                        KhoaPhong BIT NOT NULL CONSTRAINT DF_PhieuKyThuat_KhoaPhong DEFAULT 0,
                        TrangThai NVARCHAR(20) NOT NULL CONSTRAINT DF_PhieuKyThuat_TrangThai DEFAULT N'Mới',
                        MaNhanVienBao VARCHAR(10) NULL CONSTRAINT FK_PhieuKyThuat_NVBao FOREIGN KEY REFERENCES NhanVien(MaNhanVien),
                        ThoiGianBao DATETIME NOT NULL CONSTRAINT DF_PhieuKyThuat_ThoiGianBao DEFAULT GETDATE(),
                        MaNhanVienXuLy VARCHAR(10) NULL CONSTRAINT FK_PhieuKyThuat_NVXuLy FOREIGN KEY REFERENCES NhanVien(MaNhanVien),
                        ThoiGianXong DATETIME NULL,
                        KetQua NVARCHAR(500) NULL,
                        MaPhanCong VARCHAR(10) NULL,
                        MaYeuCau INT NULL
                    );
                    CREATE INDEX IX_PhieuKyThuat_Phong ON PhieuKyThuat(MaPhong, TrangThai);
                END"),

            // B2: không làm phiền, giờ dọn khách muốn, từ chối dọn hằng ngày
            ("39. Thêm PhieuDatPhong.KhongLamPhienNgay, GioDonMongMuon, TuChoiDonHangNgay", @"
                IF COL_LENGTH('PhieuDatPhong', 'TuChoiDonHangNgay') IS NULL
                    ALTER TABLE PhieuDatPhong ADD KhongLamPhienNgay DATE NULL,
                                                  GioDonMongMuon NVARCHAR(30) NULL,
                                                  TuChoiDonHangNgay BIT NOT NULL CONSTRAINT DF_PhieuDat_TuChoiDon DEFAULT 0;"),

            // K2: số hoá đơn điện tử đã xuất cho công ty
            ("40. Thêm HoaDon.SoHoaDonDienTu, NgayXuatHoaDonDienTu", @"
                IF COL_LENGTH('HoaDon', 'SoHoaDonDienTu') IS NULL
                    ALTER TABLE HoaDon ADD SoHoaDonDienTu NVARCHAR(30) NULL,
                                           NgayXuatHoaDonDienTu DATETIME NULL;"),

            // K3: chốt ngày — thời điểm huỷ phiếu, ghi nhận cọc giữ lại, ảnh chụp ngày, doanh thu theo đêm
            ("41. Thêm PhieuDatPhong.NgayHuy, NgayGhiNhanCocGiuLai", @"
                IF COL_LENGTH('PhieuDatPhong', 'NgayHuy') IS NULL
                    ALTER TABLE PhieuDatPhong ADD NgayHuy DATETIME NULL,
                                                  NgayGhiNhanCocGiuLai DATE NULL;"),
            ("42. Tạo bảng ChotNgay", @"
                IF OBJECT_ID('ChotNgay', 'U') IS NULL
                    CREATE TABLE ChotNgay (
                        Ngay DATE PRIMARY KEY,
                        ThoiGianChot DATETIME NOT NULL CONSTRAINT DF_ChotNgay_ThoiGian DEFAULT GETDATE(),
                        MaNhanVien VARCHAR(10) NULL CONSTRAINT FK_ChotNgay_NhanVien FOREIGN KEY REFERENCES NhanVien(MaNhanVien),
                        TongSoPhong INT NOT NULL,
                        DemPhongBan INT NOT NULL,
                        CongSuat DECIMAL(5,1) NOT NULL,
                        TienPhong DECIMAL(18,0) NOT NULL,
                        ADR DECIMAL(18,0) NOT NULL,
                        RevPAR DECIMAL(18,0) NOT NULL,
                        DoanhThuKhac DECIMAL(18,0) NOT NULL,
                        TienDichVu DECIMAL(18,0) NOT NULL,
                        SoHoaDonThanhToan INT NOT NULL,
                        TongHoaDonThanhToan DECIMAL(18,0) NOT NULL,
                        KhachDen INT NOT NULL,
                        KhachDi INT NOT NULL,
                        KhachKhongDen INT NOT NULL,
                        GhiChu NVARCHAR(500) NULL,
                        ChiTiet NVARCHAR(MAX) NULL
                    );"),
            ("43. Tạo bảng DoanhThuDem", @"
                IF OBJECT_ID('DoanhThuDem', 'U') IS NULL
                BEGIN
                    CREATE TABLE DoanhThuDem (
                        MaDong INT IDENTITY(1,1) PRIMARY KEY,
                        Ngay DATE NOT NULL,
                        MaDatPhong VARCHAR(10) NOT NULL CONSTRAINT FK_DoanhThuDem_PhieuDat FOREIGN KEY REFERENCES PhieuDatPhong(MaDatPhong),
                        MaPhong VARCHAR(10) NULL,
                        Loai NVARCHAR(30) NOT NULL,
                        SoTien DECIMAL(18,0) NOT NULL,
                        GhiChu NVARCHAR(100) NULL
                    );
                    CREATE INDEX IX_DoanhThuDem_Ngay ON DoanhThuDem(Ngay);
                END"),

            // M1: outbox sự kiện gửi lên RabbitMQ
            ("44. Tạo bảng SuKienCho", @"
                IF OBJECT_ID('SuKienCho', 'U') IS NULL
                BEGIN
                    CREATE TABLE SuKienCho (
                        MaSuKien UNIQUEIDENTIFIER PRIMARY KEY,
                        Loai NVARCHAR(50) NOT NULL,
                        NoiDung NVARCHAR(MAX) NOT NULL,
                        ThoiGianTao DATETIME NOT NULL CONSTRAINT DF_SuKienCho_ThoiGianTao DEFAULT GETDATE(),
                        ThoiGianGui DATETIME NULL,
                        SoLanThu INT NOT NULL CONSTRAINT DF_SuKienCho_SoLanThu DEFAULT 0,
                        LoiCuoi NVARCHAR(500) NULL
                    );
                    CREATE INDEX IX_SuKienCho_ChuaGui ON SuKienCho(ThoiGianGui, ThoiGianTao);
                END")
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
