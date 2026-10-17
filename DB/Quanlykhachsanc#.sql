USE master;
GO

IF EXISTS (SELECT * FROM sys.databases WHERE name = N'QuanLyKhachSanc#')
BEGIN
    EXECUTE sp_MSforeachdb 'IF ''?'' = ''QuanLyKhachSanc#'' 
    BEGIN
        DECLARE @sql AS NVARCHAR(MAX) = ''USE [?]; ALTER DATABASE [?] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;''
        EXEC (@sql)
    END'
    DROP DATABASE QuanLyKhachSanc#;
END
GO

CREATE DATABASE QuanLyKhachSanc#;
GO

USE QuanLyKhachSanc#;
GO

-- 1. NHÓM QUẢN LÝ TÀI KHOẢN & NHÂN SỰ
CREATE TABLE TaiKhoan (
    MaDangNhap VARCHAR(10) PRIMARY KEY,
    TenDangNhap VARCHAR(50) UNIQUE NOT NULL,
    MatKhau VARCHAR(255) NOT NULL, 
    VaiTro NVARCHAR(50) DEFAULT N'Khách hàng'
);

CREATE TABLE NhanVien (
    MaNhanVien VARCHAR(10) PRIMARY KEY,
    MaDangNhap VARCHAR(10) UNIQUE NULL,
    TenNhanVien NVARCHAR(100) NOT NULL,
    ChucVu NVARCHAR(50),
    SDT VARCHAR(15),
    Email VARCHAR(100),
    NgayVaoLam DATE DEFAULT GETDATE(),
    TrangThai NVARCHAR(50) DEFAULT N'Đang làm việc',
    CONSTRAINT FK_NhanVien_TaiKhoan FOREIGN KEY (MaDangNhap) REFERENCES TaiKhoan(MaDangNhap)
);

-- 2. NHÓM QUẢN LÝ KHÁCH HÀNG
CREATE TABLE KhachHang (
    MaKhachHang VARCHAR(10) PRIMARY KEY,
    MaDangNhap VARCHAR(10) UNIQUE NULL,
    Ho NVARCHAR(50) NOT NULL,
    Ten NVARCHAR(50) NOT NULL,
    NgaySinh DATE,
    CCCD_Passport VARCHAR(20) UNIQUE NOT NULL,
    DiemTichLuy INT DEFAULT 0,
    CONSTRAINT FK_KhachHang_TaiKhoan FOREIGN KEY (MaDangNhap) REFERENCES TaiKhoan(MaDangNhap)
);

CREATE TABLE LienHe_DiaChi (
    MaLienHe VARCHAR(10) PRIMARY KEY,
    MaKhachHang VARCHAR(10) NOT NULL,
    SDT VARCHAR(15) NOT NULL,
    Email VARCHAR(100),
    DiaChi NVARCHAR(255),
    QuocGia NVARCHAR(50) DEFAULT N'Việt Nam',
    CONSTRAINT FK_LienHe_KhachHang FOREIGN KEY (MaKhachHang) REFERENCES KhachHang(MaKhachHang)
);

-- 3. NHÓM QUẢN LÝ PHÒNG & DỊCH VỤ
-- ĐÃ CẬP NHẬT: Đưa giá thuê về đúng bảng Loại Phòng
CREATE TABLE LoaiPhong (
    MaLoai VARCHAR(10) PRIMARY KEY,
    TenLoai NVARCHAR(50) NOT NULL, 
    SoNguoiToiDa INT CHECK (SoNguoiToiDa > 0),
    GiaThueNgay DECIMAL(18,0) CHECK (GiaThueNgay >= 0), 
    GiaThueGio DECIMAL(18,0) CHECK (GiaThueGio >= 0),
    MoTa NVARCHAR(500)
);

-- ĐÃ CẬP NHẬT: Loại bỏ cột giá thuê khỏi bảng Phòng vật lý
CREATE TABLE Phong (
    MaPhong VARCHAR(10) PRIMARY KEY,
    MaLoai VARCHAR(10) NOT NULL,
    SoPhong VARCHAR(10) UNIQUE NOT NULL,
    TrangThai NVARCHAR(50) DEFAULT N'Trống', 
    CONSTRAINT FK_Phong_LoaiPhong FOREIGN KEY (MaLoai) REFERENCES LoaiPhong(MaLoai)
);

CREATE TABLE TienIch (
    MaTienIch VARCHAR(10) PRIMARY KEY,
    TenTienIch NVARCHAR(100) NOT NULL,
    Icon VARCHAR(50) 
);

CREATE TABLE Phong_TienIch (
    MaPhong VARCHAR(10),
    MaTienIch VARCHAR(10),
    PRIMARY KEY (MaPhong, MaTienIch),
    CONSTRAINT FK_PTI_Phong FOREIGN KEY (MaPhong) REFERENCES Phong(MaPhong),
    CONSTRAINT FK_PTI_TienIch FOREIGN KEY (MaTienIch) REFERENCES TienIch(MaTienIch)
);

CREATE TABLE HinhAnh (
    MaHinhAnh VARCHAR(10) PRIMARY KEY,
    MaPhong VARCHAR(10) NOT NULL,
    DuongDanAnh VARCHAR(255) NOT NULL,
    CONSTRAINT FK_HinhAnh_Phong FOREIGN KEY (MaPhong) REFERENCES Phong(MaPhong)
);

CREATE TABLE DichVu (
    MaDichVu VARCHAR(10) PRIMARY KEY,
    TenDichVu NVARCHAR(100) NOT NULL,
    GiaDichVu DECIMAL(18,0) CHECK (GiaDichVu >= 0),
    DonViTinh NVARCHAR(20),
    TrangThai NVARCHAR(50) DEFAULT N'Sẵn sàng'
);

-- 4. NHÓM KHUYẾN MÃI
CREATE TABLE KhuyenMai (
    MaKhuyenMai VARCHAR(10) PRIMARY KEY,
    TenKhuyenMai NVARCHAR(100) NOT NULL,
    LoaiGiam NVARCHAR(20), 
    GiaTriGiam DECIMAL(18,2) CHECK (GiaTriGiam >= 0), 
    NgayBatDau DATE,
    NgayKetThuc DATE,
    TrangThai AS (CASE WHEN GETDATE() BETWEEN NgayBatDau AND NgayKetThuc THEN 1 ELSE 0 END)
);

-- 5. NHÓM GIAO DỊCH
CREATE TABLE PhieuDatPhong (
    MaDatPhong VARCHAR(10) PRIMARY KEY,
    MaKhachHang VARCHAR(10) NOT NULL,
    MaNhanVien VARCHAR(10) NULL,
    NgayDat DATETIME DEFAULT GETDATE(),
    
    NgayCheckInDuKien DATETIME NOT NULL,
    NgayCheckOutDuKien DATETIME NOT NULL,
    NgayCheckInThucTe DATETIME NULL,
    NgayCheckOutThucTe DATETIME NULL,
    
    SoLuongKhach INT CHECK (SoLuongKhach > 0),
    TrangThai NVARCHAR(50) DEFAULT N'Chờ duyệt', 
    TienCoc DECIMAL(18,0) DEFAULT 0,
    GhiChu NVARCHAR(MAX),
    CONSTRAINT FK_PhieuDat_KhachHang FOREIGN KEY (MaKhachHang) REFERENCES KhachHang(MaKhachHang),
    CONSTRAINT FK_PhieuDat_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien)
);

CREATE TABLE ChiTietDatPhong (
    MaCTDP VARCHAR(10) PRIMARY KEY,
    MaDatPhong VARCHAR(10) NOT NULL,
    MaPhong VARCHAR(10) NOT NULL,
    HinhThucThue NVARCHAR(20) CHECK (HinhThucThue IN (N'Theo ngày', N'Theo giờ')),
    GiaThueThucTe DECIMAL(18,0) NOT NULL, 
    CONSTRAINT FK_CTDP_PhieuDat FOREIGN KEY (MaDatPhong) REFERENCES PhieuDatPhong(MaDatPhong),
    CONSTRAINT FK_CTDP_Phong FOREIGN KEY (MaPhong) REFERENCES Phong(MaPhong)
);

CREATE TABLE SuDungDichVu (
    MaSDDV VARCHAR(10) PRIMARY KEY,
    MaDatPhong VARCHAR(10) NOT NULL,
    MaDichVu VARCHAR(10) NOT NULL,
    SoLuong INT CHECK (SoLuong > 0),
    NgaySuDung DATETIME DEFAULT GETDATE(),
    GiaThucTe DECIMAL(18,0) NOT NULL,
    ThanhTien AS (SoLuong * GiaThucTe), 
    CONSTRAINT FK_SDDV_PhieuDat FOREIGN KEY (MaDatPhong) REFERENCES PhieuDatPhong(MaDatPhong),
    CONSTRAINT FK_SDDV_DichVu FOREIGN KEY (MaDichVu) REFERENCES DichVu(MaDichVu)
);

-- 6. NHÓM THANH TOÁN & ĐÁNH GIÁ
CREATE TABLE HoaDon (
    MaHoaDon VARCHAR(10) PRIMARY KEY,
    MaDatPhong VARCHAR(10) NOT NULL UNIQUE,
    MaNhanVienLap VARCHAR(10) NOT NULL,
    NgayLapHoaDon DATETIME DEFAULT GETDATE(),
    
    TongTienPhong DECIMAL(18,0) DEFAULT 0,
    TongTienDichVu DECIMAL(18,0) DEFAULT 0,
    MaKhuyenMai VARCHAR(10) NULL,
    GiamGiaKhuyenMai DECIMAL(18,0) DEFAULT 0,
    TienPhat DECIMAL(18,0) DEFAULT 0,
    
    ThueVAT AS ((TongTienPhong + TongTienDichVu + TienPhat - GiamGiaKhuyenMai) * 0.08),
    
    TongTienCanThanhToan AS ((TongTienPhong + TongTienDichVu + TienPhat - GiamGiaKhuyenMai) * 1.08),
    
    TrangThai NVARCHAR(50) DEFAULT N'Chưa thanh toán',
    CONSTRAINT FK_HoaDon_PhieuDat FOREIGN KEY (MaDatPhong) REFERENCES PhieuDatPhong(MaDatPhong),
    CONSTRAINT FK_HoaDon_KhuyenMai FOREIGN KEY (MaKhuyenMai) REFERENCES KhuyenMai(MaKhuyenMai),
    CONSTRAINT FK_HoaDon_NhanVien FOREIGN KEY (MaNhanVienLap) REFERENCES NhanVien(MaNhanVien)
);

CREATE TABLE ThanhToan (
    MaThanhToan VARCHAR(10) PRIMARY KEY,
    MaHoaDon VARCHAR(10) NOT NULL,
    PhuongThuc NVARCHAR(50), 
    NgayThanhToan DATETIME DEFAULT GETDATE(),
    SoTienDaTra DECIMAL(18,0) CHECK (SoTienDaTra >= 0),
    GhiChu NVARCHAR(255),
    CONSTRAINT FK_ThanhToan_HoaDon FOREIGN KEY (MaHoaDon) REFERENCES HoaDon(MaHoaDon)
);

CREATE TABLE LichSuGiaoDich (
    MaGiaoDich VARCHAR(50) PRIMARY KEY, 
    MaThanhToan VARCHAR(10),
    CongThanhToan NVARCHAR(50), 
    SoTien DECIMAL(18,0),
    TrangThaiGiaoDich NVARCHAR(50), 
    ThoiGianGiaoDich DATETIME DEFAULT GETDATE(),
    NoiDungChuyenKhoan NVARCHAR(255),
    CONSTRAINT FK_LSGD_ThanhToan FOREIGN KEY (MaThanhToan) REFERENCES ThanhToan(MaThanhToan)
);

CREATE TABLE DanhGia (
    MaDanhGia VARCHAR(10) PRIMARY KEY,
    MaKhachHang VARCHAR(10) NOT NULL,
    MaPhong VARCHAR(10) NOT NULL,
    SoSao INT CHECK (SoSao BETWEEN 1 AND 5),
    NoiDung NVARCHAR(1000),
    NgayDanhGia DATETIME DEFAULT GETDATE(),
    AnDanh BIT DEFAULT 0,
    CONSTRAINT FK_DanhGia_KhachHang FOREIGN KEY (MaKhachHang) REFERENCES KhachHang(MaKhachHang),
    CONSTRAINT FK_DanhGia_Phong FOREIGN KEY (MaPhong) REFERENCES Phong(MaPhong)
);
GO

-- ==========================================
-- BỔ SUNG CƠ CHẾ TỰ ĐỘNG (TRIGGERS)
-- ==========================================

CREATE TRIGGER trg_UpdateRoomStatus_CheckIn
ON PhieuDatPhong
AFTER UPDATE
AS
BEGIN
    IF UPDATE(TrangThai)
    BEGIN
        UPDATE Phong
        SET TrangThai = N'Đang ở'
        WHERE MaPhong IN (SELECT MaPhong FROM ChiTietDatPhong WHERE MaDatPhong IN (SELECT MaDatPhong FROM inserted WHERE TrangThai = N'Đã nhận phòng'));
        
        UPDATE Phong
        SET TrangThai = N'Chờ dọn dẹp'
        WHERE MaPhong IN (SELECT MaPhong FROM ChiTietDatPhong WHERE MaDatPhong IN (SELECT MaDatPhong FROM inserted WHERE TrangThai = N'Đã trả phòng'));
    END
END;
GO

-- ==========================================
-- CHÈN DỮ LIỆU MẪU (DATA INSERTS)
-- ==========================================

-- 1. NHÓM QUẢN LÝ TÀI KHOẢN & NHÂN SỰ
INSERT INTO TaiKhoan (MaDangNhap, TenDangNhap, MatKhau, VaiTro)
VALUES 
('TK01', 'admin1', '123456', N'Quản lý'),
('TK02', 'letan1', '123456', N'Lễ tân'),
('TK03', 'khach1','123456', N'Khách hàng'),
('TK04', 'khach2', '123456', N'Khách hàng'),
('TK05', 'khach3', '123456', N'Khách hàng'),
('TK06', 'letan2', '123456', N'Lễ tân'),
('TK07', 'letan3','123456', N'Lễ tân'),
('TK08', 'admin2', '123456', N'Quản lý'),
('TK09', 'khach4', '123456', N'Khách hàng'),
('TK10', 'khach5', '123456', N'Khách hàng');
GO

INSERT INTO NhanVien (MaNhanVien, MaDangNhap, TenNhanVien, ChucVu, SDT, Email, NgayVaoLam, TrangThai)
VALUES 
('NV01', 'TK01', N'Nguyễn Hoàng Đức', N'Quản lý', '0901234567', 'admin1@ute.udn.vn', '2023-01-15', N'Đang làm việc'),
('NV02', 'TK02', N'Trần Thị Bích', N'Lễ tân', '0901234568', 'letan1@ute.udn.vn', '2024-02-01', N'Đang làm việc'),
('NV03', 'TK06', N'Lê Văn Cường', N'Lễ tân', '0901234569', 'letan2@ute.udn.vn', '2024-03-10', N'Đang làm việc'),
('NV04', 'TK07', N'Phạm Thị Duyên', N'Lễ tân', '0901234570', 'letan3@ute.udn.vn', '2024-04-05', N'Đang làm việc'),
('NV05', 'TK08', N'Hoàng Văn Ân', N'Quản lý', '0901234571', 'admin2@ute.udn.vn', '2025-05-20', N'Đang làm việc');
GO

-- 2. NHÓM QUẢN LÝ KHÁCH HÀNG
INSERT INTO KhachHang (MaKhachHang, MaDangNhap, Ho, Ten, NgaySinh, CCCD_Passport, DiemTichLuy)
VALUES 
('KH01', 'TK03', N'Võ', N'Văn Phương', '2004-10-15', '048204000123', 150),
('KH02', 'TK04', N'Lê', N'Thị Cẩm', '1995-05-20', '048195000456', 50),
('KH03', 'TK05', N'Nguyễn', N'Văn Hùng', '1988-12-05', '048188000789', 200),
('KH04', 'TK09', N'Trần', N'Đại', '1990-08-08', '048190000111', 0),
('KH05', 'TK10', N'Phạm', N'Hương', '2000-01-01', '048200000222', 10);
GO

INSERT INTO LienHe_DiaChi (MaLienHe, MaKhachHang, SDT, Email, DiaChi, QuocGia)
VALUES 
('LH01', 'KH01', '0911111111', 'phuong.vo@gmail.com', N'Hải Châu, Đà Nẵng', N'Việt Nam'),
('LH02', 'KH02', '0922222222', 'cam.le@gmail.com', N'Sơn Trà, Đà Nẵng', N'Việt Nam'),
('LH03', 'KH03', '0933333333', 'hung.nguyen@gmail.com', N'Liên Chiểu, Đà Nẵng', N'Việt Nam'),
('LH04', 'KH04', '0944444444', 'dai.tran@gmail.com', N'Ngũ Hành Sơn, Đà Nẵng', N'Việt Nam'),
('LH05', 'KH05', '0955555555', 'huong.pham@gmail.com', N'Cẩm Lệ, Đà Nẵng', N'Việt Nam');
GO

-- 3. NHÓM QUẢN LÝ PHÒNG & DỊCH VỤ
-- ĐÃ CẬP NHẬT: Thêm giá trị cho cột giá thuê ngày/giờ tại bảng Loại phòng
INSERT INTO LoaiPhong (MaLoai, TenLoai, SoNguoiToiDa, GiaThueNgay, GiaThueGio, MoTa)
VALUES 
('LP01', N'Standard (STD)', 2, 500000, 100000, N'Phòng tiêu chuẩn, thiết kế cơ bản'),
('LP02', N'Superior (SUP)', 2, 800000, 150000, N'Phòng chất lượng cao, có ban công'),
('LP03', N'Deluxe (DLX)', 3, 1200000, 200000, N'Phòng rộng rãi, view ngắm biển'),
('LP04', N'Suite (SUT)', 4, 2500000, 400000, N'Phòng cao cấp, có phòng khách riêng biệt'),
('LP05', N'Family', 4, 1500000, 250000, N'Phòng dành cho gia đình, 2 giường đôi');
GO

-- ĐÃ CẬP NHẬT: Loại bỏ các tham số giá thuê khi chèn vào bảng Phòng
INSERT INTO Phong (MaPhong, MaLoai, SoPhong, TrangThai)
VALUES 
('P101', 'LP01', '101', N'Trống'),
('P102', 'LP02', '102', N'Trống'),
('P201', 'LP03', '201', N'Đang ở'),
('P202', 'LP04', '202', N'Trống'),
('P301', 'LP05', '301', N'Chờ dọn dẹp');
GO

INSERT INTO TienIch (MaTienIch, TenTienIch, Icon)
VALUES 
('TI01', N'Wifi tốc độ cao', 'fa-wifi'),
('TI02', N'Tivi thông minh', 'fa-tv'),
('TI03', N'Điều hòa nhiệt độ', 'fa-snowflake'),
('TI04', N'Bồn tắm massage', 'fa-bath'),
('TI05', N'Minibar', 'fa-wine-glass');
GO

INSERT INTO Phong_TienIch (MaPhong, MaTienIch)
VALUES 
('P101', 'TI01'),
('P101', 'TI03'),
('P202', 'TI01'),
('P202', 'TI04'),
('P301', 'TI02');
GO

INSERT INTO HinhAnh (MaHinhAnh, MaPhong, DuongDanAnh)
VALUES 
('HA01', 'P101', '/assets/img/rooms/p101_front.jpg'),
('HA02', 'P101', '/assets/img/rooms/p101_bed.jpg'),
('HA03', 'P202', '/assets/img/rooms/p202_suite.jpg'),
('HA04', 'P301', '/assets/img/rooms/p301_family.jpg'),
('HA05', 'P301', '/assets/img/rooms/p301_bathroom.jpg');
GO

INSERT INTO DichVu (MaDichVu, TenDichVu, GiaDichVu, DonViTinh)
VALUES 
('DV01', N'Dọn phòng thêm', 100000, N'Lần'),
('DV02', N'Giặt ủi quần áo', 50000, N'Kg'),
('DV03', N'Thuê xe máy', 150000, N'Ngày'),
('DV04', N'Spa & Massage', 500000, N'Lượt'),
('DV05', N'Ăn sáng Buffet', 200000, N'Vé');
GO

-- 4. NHÓM KHUYẾN MÃI
INSERT INTO KhuyenMai (MaKhuyenMai, TenKhuyenMai, LoaiGiam, GiaTriGiam, NgayBatDau, NgayKetThuc)
VALUES 
('KM01', N'Chào Hè Sôi Động', N'Phần trăm', 10, '2026-06-01', '2026-08-31'),
('KM02', N'Lễ Quốc Khánh 2/9', N'Số tiền', 200000, '2026-09-01', '2026-09-05'),
('KM03', N'Thành viên mới', N'Phần trăm', 5, '2026-01-01', '2026-12-31'),
('KM04', N'Flash Sale Tháng 5', N'Số tiền', 150000, '2026-05-15', '2026-05-20'),
('KM05', N'Black Friday', N'Phần trăm', 30, '2026-11-25', '2026-11-30');
GO

-- 5. NHÓM GIAO DỊCH
INSERT INTO PhieuDatPhong (MaDatPhong, MaKhachHang, MaNhanVien, NgayDat, NgayCheckInDuKien, NgayCheckOutDuKien, NgayCheckInThucTe, NgayCheckOutThucTe, SoLuongKhach, TrangThai, TienCoc, GhiChu)
VALUES 
('DP01', 'KH01', 'NV02', '2026-05-01', '2026-05-10', '2026-05-12', '2026-05-10 14:00', '2026-05-12 12:00', 2, N'Đã trả phòng', 500000, N'Khách check-in đúng giờ'),
('DP02', 'KH02', 'NV02', '2026-05-18', '2026-05-20', '2026-05-22', NULL, NULL, 1, N'Đã duyệt', 0, N'Ghi chú: cần phòng yên tĩnh'),
('DP03', 'KH03', 'NV05', '2026-05-15', '2026-05-17', '2026-05-19', '2026-05-17 15:00', NULL, 3, N'Đã nhận phòng', 1000000, N'Đang lưu trú'),
('DP04', 'KH04', NULL,   '2026-06-01', '2026-06-10', '2026-06-12', NULL, NULL, 2, N'Chờ duyệt', 0, N'Đặt online qua Web'),
('DP05', 'KH05', 'NV02', '2026-05-10', '2026-05-11', '2026-05-12', '2026-05-11 14:00', '2026-05-12 11:30', 4, N'Đã trả phòng', 500000, N'Gia đình đi nghỉ mát');
GO

INSERT INTO ChiTietDatPhong (MaCTDP, MaDatPhong, MaPhong, HinhThucThue, GiaThueThucTe)
VALUES 
('CT01', 'DP01', 'P101', N'Theo ngày', 500000),
('CT02', 'DP02', 'P102', N'Theo ngày', 800000),
('CT03', 'DP03', 'P201', N'Theo ngày', 1200000),
('CT04', 'DP04', 'P202', N'Theo ngày', 2500000),
('CT05', 'DP05', 'P301', N'Theo ngày', 1500000);
GO

INSERT INTO SuDungDichVu (MaSDDV, MaDatPhong, MaDichVu, SoLuong, NgaySuDung, GiaThucTe)
VALUES 
('SD01', 'DP01', 'DV05', 2, '2026-05-11 07:00', 200000),
('SD02', 'DP03', 'DV02', 3, '2026-05-18 09:00', 50000),
('SD03', 'DP03', 'DV03', 1, '2026-05-18 10:00', 150000),
('SD04', 'DP05', 'DV04', 2, '2026-05-11 19:00', 500000),
('SD05', 'DP05', 'DV01', 1, '2026-05-12 08:00', 100000);
GO

-- 6. NHÓM THANH TOÁN & ĐÁNH GIÁ
INSERT INTO HoaDon (MaHoaDon, MaDatPhong, MaNhanVienLap, NgayLapHoaDon, TongTienPhong, TongTienDichVu, MaKhuyenMai, GiamGiaKhuyenMai, TienPhat, TrangThai)
VALUES 
('HD01', 'DP01', 'NV02', '2026-05-12 12:05', 1000000, 400000, 'KM03', 70000, 0, N'Đã thanh toán'),
('HD02', 'DP05', 'NV02', '2026-05-12 11:45', 1500000, 1100000, NULL, 0, 0, N'Đã thanh toán'),
('HD03', 'DP03', 'NV05', '2026-05-19 12:00', 2400000, 300000, 'KM04', 150000, 0, N'Chưa thanh toán'),
('HD04', 'DP02', 'NV02', '2026-05-22 12:00', 1600000, 0, NULL, 0, 0, N'Chưa thanh toán'),
('HD05', 'DP04', 'NV02', '2026-06-12 12:00', 5000000, 0, 'KM01', 500000, 0, N'Chưa thanh toán');
GO

INSERT INTO ThanhToan (MaThanhToan, MaHoaDon, PhuongThuc, NgayThanhToan, SoTienDaTra, GhiChu)
VALUES 
('TT01', 'HD01', N'Tiền mặt', '2026-05-12 12:10', 1436400, N'Khách thanh toán trực tiếp'),
('TT02', 'HD02', N'Chuyển khoản', '2026-05-12 11:50', 2808000, N'Chuyển khoản Vietcombank'),
('TT03', 'HD03', N'Thẻ tín dụng', '2026-05-15 14:00', 1000000, N'Thanh toán tiền cọc trước'),
('TT04', 'HD04', N'Chuyển khoản', '2026-05-18 09:00', 0, N'Chưa thanh toán'),
('TT05', 'HD05', N'Tiền mặt', '2026-06-01 10:00', 0, N'Chưa thanh toán');
GO

INSERT INTO LichSuGiaoDich (MaGiaoDich, MaThanhToan, CongThanhToan, SoTien, TrangThaiGiaoDich, ThoiGianGiaoDich, NoiDungChuyenKhoan)
VALUES 
('GD_001', 'TT01', N'Tiền mặt', 1436400, N'Thành công', '2026-05-12 12:10', N'Thanh toán HD01'),
('GD_002', 'TT02', N'VNPay', 2808000, N'Thành công', '2026-05-12 11:50', N'KH05 thanh toan HD02'),
('GD_003', 'TT03', N'Máy POS', 1000000, N'Thành công', '2026-05-15 14:05', N'Quẹt thẻ cọc DP03'),
('GD_004', 'TT04', N'Momo', 0, N'Thất bại', '2026-05-18 09:05', N'Lỗi kết nối'),
('GD_005', 'TT05', N'Tiền mặt', 0, N'Chờ xử lý', '2026-06-01 10:05', N'Chờ khách đến');
GO

INSERT INTO DanhGia (MaDanhGia, MaKhachHang, MaPhong, SoSao, NoiDung, NgayDanhGia, AnDanh)
VALUES 
('DG01', 'KH01', 'P101', 5, N'Phòng sạch sẽ, nhân viên nhiệt tình!', '2026-05-13', 0),
('DG02', 'KH05', 'P301', 4, N'Phòng rộng nhưng wifi đôi lúc hơi chậm.', '2026-05-14', 0),
('DG03', 'KH03', 'P201', 5, N'View biển cực kỳ đẹp, rất đáng tiền.', '2026-05-18', 0),
('DG04', 'KH02', 'P102', 3, N'Bình thường, cách âm chưa tốt lắm.', '2026-05-25', 1),
('DG05', 'KH04', 'P202', 5, N'Tuyệt vời, sẽ quay lại lần sau.', '2026-06-15', 0);
GO