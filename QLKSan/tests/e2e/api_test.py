import os
import json, ssl, sys, urllib.request, urllib.error

BASE = os.environ.get('QLKSAN_API', 'https://localhost:7001')  # đổi bằng biến môi trường QLKSAN_API
CTX = ssl._create_unverified_context()
results = []

def call(method, path, token=None, body=None):
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(BASE + path, data=data, method=method)
    req.add_header('Content-Type', 'application/json')
    if token: req.add_header('Authorization', 'Bearer ' + token)
    try:
        with urllib.request.urlopen(req, context=CTX) as r:
            raw = r.read().decode('utf-8')
            return r.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as e:
        raw = e.read().decode('utf-8')
        try: return e.code, json.loads(raw) if raw else None
        except Exception: return e.code, raw

def check(name, method, path, expect, token=None, body=None, show=None):
    status, data = call(method, path, token, body)
    ok = status == expect
    extra = ''
    if isinstance(data, dict) and 'detail' in data and data.get('detail'): extra = ' | ' + str(data['detail'])
    elif isinstance(data, dict) and 'errors' in data: extra = ' | ' + '; '.join(m for v in data['errors'].values() for m in v)
    elif isinstance(data, list): extra = f' | {len(data)} mục'
    if show and ok and data is not None: extra += ' | ' + show(data)
    results.append(ok)
    print(f"{'OK  ' if ok else 'FAIL'} {status} (cần {expect}) {method:6} {path:45} {name}{extra}")
    return data

def login(u):
    s, d = call('POST', '/api/auth/login', body={'TenDangNhap': u, 'MatKhau': '123456'})
    assert s == 200, (u, s, d)
    return d

print('--- Xác thực ---')
check('Sai mật khẩu', 'POST', '/api/auth/login', 401, body={'TenDangNhap': 'admin1', 'MatKhau': 'sai'})
check('Thiếu dữ liệu → validation', 'POST', '/api/auth/login', 400, body={'TenDangNhap': ''})
admin, letan, khach = login('admin1'), login('letan1'), login('khach1')
print(f"     admin1 → {admin['VaiTro']} {admin['MaNhanVien']} {admin['HoTen']}; letan1 → {letan['VaiTro']} {letan['MaNhanVien']}; khach1 → {khach['VaiTro']} {khach['MaKhachHang']} {khach['HoTen']}")
A, L, K = admin['AccessToken'], letan['AccessToken'], khach['AccessToken']
check('Không có token', 'GET', '/api/rooms', 401)
check('Token sai', 'GET', '/api/rooms', 401, token='abc.def.ghi')
check('/me', 'GET', '/api/auth/me', 200, token=K, show=lambda d: f"{d['TenDangNhap']} {d['VaiTro']}")
check('Đăng ký: mật khẩu xác nhận sai + SĐT sai', 'POST', '/api/auth/register', 400,
      body={'Ho': 'A', 'Ten': 'B', 'SDT': '123', 'CCCD_Passport': '123456789', 'NgaySinh': '2000-01-01', 'DiaChi': 'x', 'TenDangNhap': 'zz', 'MatKhau': '123456', 'XacNhanMatKhau': '654321'})
check('Đăng ký: trùng tên đăng nhập', 'POST', '/api/auth/register', 409,
      body={'Ho': 'A', 'Ten': 'B', 'SDT': '0911111119', 'CCCD_Passport': '999999999001', 'NgaySinh': '2000-01-01', 'DiaChi': 'x', 'TenDangNhap': 'khach1', 'MatKhau': '123456', 'XacNhanMatKhau': '123456'})

print('--- Phân quyền ---')
check('Khách gọi API của lễ tân', 'GET', '/api/rooms', 403, token=K)
check('Lễ tân gọi API của admin', 'GET', '/api/staff', 403, token=L)
check('Lễ tân gọi /customers/me (chỉ khách)', 'GET', '/api/customers/me', 403, token=L)

print('--- Phòng ---')
rooms = check('Danh sách phòng', 'GET', '/api/rooms', 200, token=L)
check('Lọc trạng thái Available', 'GET', '/api/rooms?trangThai=Available', 200, token=L)
check('Phòng trống (khách gọi)', 'GET', '/api/rooms/available', 200, token=K)
check('Chi tiết P101', 'GET', '/api/rooms/P101', 200, token=L, show=lambda d: f"{d['SoPhong']} {d['LoaiPhong']['TenLoai']} | {len(d['TienIchs'])} tiện ích")
check('Phòng không tồn tại', 'GET', '/api/rooms/P999', 404, token=L)
occupied = [r['MaPhong'] for r in rooms if r['TrangThai'] == 'Occupied']
if occupied:
    stay = check(f'Phiếu đang ở của {occupied[0]}', 'GET', f'/api/rooms/{occupied[0]}/current-stay', 200, token=L,
                 show=lambda d: f"{d['MaDatPhong']} {d['KhachHang']['HoTen']} | HĐ: {(d['HoaDon'] or {}).get('MaHoaDon')}")
check('Phòng trống không có phiếu đang ở', 'GET', '/api/rooms/P101/current-stay', 404, token=L)
check('Đổi trạng thái: giá trị enum sai', 'PATCH', '/api/rooms/P101', 400, token=L, body={'TrangThai': 'XYZ'})
check('Đổi trạng thái: body thiếu TrangThai → 400 (không mặc định "Trống")', 'PATCH', '/api/rooms/P101', 400, token=L, body={})

print('--- Danh mục công khai cho trang chủ ---')
check('Danh sách loại phòng: không cần đăng nhập', 'GET', '/api/room-types', 200)
check('Danh sách dịch vụ: không cần đăng nhập', 'GET', '/api/services', 200)
check('Thêm loại phòng khi chưa đăng nhập → 401', 'POST', '/api/room-types', 401, body={'TenLoai': 'X', 'SoNguoiToiDa': 2})
check('Chi tiết dịch vụ vẫn cần đăng nhập → 401', 'GET', '/api/services/DV01', 401)

print('--- CRUD tự dọn: loại phòng + phòng ---')
rt = check('Thêm loại phòng', 'POST', '/api/room-types', 201, token=A,
           body={'TenLoai': 'TEST API', 'SoNguoiToiDa': 2, 'GiaThueNgay': 100000, 'GiaThueGio': 20000, 'MoTa': 'test'}, show=lambda d: d['MaLoai'])
check('Thêm loại phòng: số người 0', 'POST', '/api/room-types', 400, token=A, body={'TenLoai': 'X', 'SoNguoiToiDa': 0})
if rt:
    ml = rt['MaLoai']
    room = check('Thêm phòng 9901', 'POST', '/api/rooms', 201, token=A, body={'SoPhong': '9901', 'MaLoai': ml}, show=lambda d: d['MaPhong'])
    check('Thêm phòng trùng số', 'POST', '/api/rooms', 409, token=A, body={'SoPhong': '9901', 'MaLoai': ml})
    check('Thêm phòng: loại không tồn tại', 'POST', '/api/rooms', 400, token=A, body={'SoPhong': '9902', 'MaLoai': 'LP99'})
    check('Xoá loại đang có phòng → 409', 'DELETE', f'/api/room-types/{ml}', 409, token=A)
    check('Sửa phòng', 'PUT', '/api/rooms/P9901', 204, token=A, body={'SoPhong': '9901', 'MaLoai': ml})
    check('Đổi trạng thái → Maintenance', 'PATCH', '/api/rooms/P9901', 204, token=L, body={'TrangThai': 'Maintenance'})
    check('Đánh dấu dọn xong khi không đang dọn → 409', 'POST', '/api/housekeeping/rooms/P9901/clean', 409, token=L)
    check('Đổi trạng thái → Cleaning', 'PATCH', '/api/rooms/P9901', 204, token=L, body={'TrangThai': 'Cleaning'})
    check('Đánh dấu dọn xong', 'POST', '/api/housekeeping/rooms/P9901/clean', 204, token=L)
    check('Xoá phòng', 'DELETE', '/api/rooms/P9901', 204, token=A)
    check('Xoá phòng lần 2', 'DELETE', '/api/rooms/P9901', 404, token=A)
    check('Sửa loại phòng', 'PUT', f'/api/room-types/{ml}', 204, token=A, body={'TenLoai': 'TEST API 2', 'SoNguoiToiDa': 3, 'GiaThueNgay': 1, 'GiaThueGio': 1})
    check('Xoá loại phòng', 'DELETE', f'/api/room-types/{ml}', 204, token=A)
check('Lễ tân thêm phòng (cấm)', 'POST', '/api/rooms', 403, token=L, body={'SoPhong': '9903', 'MaLoai': 'LP01'})

print('--- CRUD tự dọn: dịch vụ ---')
check('Danh sách dịch vụ (lễ tân)', 'GET', '/api/services', 200, token=L)
sv = check('Thêm dịch vụ', 'POST', '/api/services', 201, token=A, body={'TenDichVu': 'TEST API', 'GiaDichVu': 1000, 'DonViTinh': 'Lần'}, show=lambda d: d['MaDichVu'])
if sv:
    check('Đặt dịch vụ "Đã hết"', 'PATCH', f"/api/services/{sv['MaDichVu']}", 200, token=A, body={'TrangThai': 'Unavailable'}, show=lambda d: d['TrangThai'])
    r2 = check('Gọi lại cùng body → vẫn "Đã hết" (không đảo trạng thái)', 'PATCH', f"/api/services/{sv['MaDichVu']}", 200, token=A, body={'TrangThai': 'Unavailable'}, show=lambda d: d['TrangThai'])
    results.append(bool(r2) and r2['TrangThai'] == 'Unavailable')
    check('Đặt trạng thái thiếu body → 400', 'PATCH', f"/api/services/{sv['MaDichVu']}", 400, token=A, body={})
    check('Endpoint /toggle cũ không còn', 'PATCH', f"/api/services/{sv['MaDichVu']}/toggle", 404, token=A)
    check('Sửa dịch vụ', 'PUT', f"/api/services/{sv['MaDichVu']}", 204, token=A, body={'TenDichVu': 'TEST 2', 'GiaDichVu': 2000, 'DonViTinh': 'Lần'})
    check('Xoá dịch vụ', 'DELETE', f"/api/services/{sv['MaDichVu']}", 204, token=A)
check('Lễ tân thêm dịch vụ (cấm)', 'POST', '/api/services', 403, token=L, body={'TenDichVu': 'x', 'GiaDichVu': 1, 'DonViTinh': 'x'})

print('--- CRUD tự dọn: nhân viên ---')
check('Danh sách nhân viên', 'GET', '/api/staff', 200, token=A)
check('Thêm NV: SĐT sai, mật khẩu ngắn', 'POST', '/api/staff', 400, token=A, body={'TenNhanVien': 'X', 'SDT': '12', 'TenDangNhap': 'x', 'MatKhau': '1'})
check('Thêm NV: trùng tên đăng nhập', 'POST', '/api/staff', 409, token=A, body={'TenNhanVien': 'X', 'ChucVu': 'Lễ tân', 'SDT': '0912345678', 'Email': 'a@b.vn', 'TenDangNhap': 'letan1', 'MatKhau': '123456'})
nv = check('Thêm nhân viên', 'POST', '/api/staff', 201, token=A,
           body={'TenNhanVien': 'Test API', 'ChucVu': 'Lễ tân', 'SDT': '0912345678', 'Email': 'test@api.vn', 'TenDangNhap': 'testapi_zz', 'MatKhau': '123456'},
           show=lambda d: f"{d['MaNhanVien']} {d['TenDangNhap']}")
if nv:
    newtok = login('testapi_zz')
    print(f"     NV mới đăng nhập được → {newtok['VaiTro']} {newtok['MaNhanVien']}")
    check('Sửa nhân viên', 'PUT', f"/api/staff/{nv['MaNhanVien']}", 204, token=A, body={'TenNhanVien': 'Test API 2', 'ChucVu': 'Lễ tân', 'SDT': '0912345679', 'Email': 'test@api.vn'})
    check('Xoá nhân viên', 'DELETE', f"/api/staff/{nv['MaNhanVien']}", 204, token=A)
check('Đã lập phiếu thì không xoá được (NV02)', 'DELETE', '/api/staff/NV02', 409, token=A)

print('--- Khuyến mãi, đánh giá, dashboard ---')
promos = check('Danh sách khuyến mãi', 'GET', '/api/promotions', 200, token=A)
check('Mã KM không tồn tại', 'GET', '/api/promotions/KHONGCO', 404, token=K)
active = [p for p in promos if p['TrangThai'] == 'Active']
expired = [p for p in promos if p['TrangThai'] != 'Active']
if active: check(f"Mã KM còn hạn {active[0]['MaKhuyenMai']}", 'GET', f"/api/promotions/{active[0]['MaKhuyenMai']}", 200, token=K)
if expired: check(f"Mã KM hết hạn {expired[0]['MaKhuyenMai']}", 'GET', f"/api/promotions/{expired[0]['MaKhuyenMai']}", 400, token=K)
check('Thêm KM trùng mã', 'POST', '/api/promotions', 409, token=A, body={'MaKhuyenMai': promos[0]['MaKhuyenMai'], 'TenKhuyenMai': 'x', 'LoaiGiam': 'Phần trăm', 'GiaTriGiam': 1, 'NgayBatDau': '2026-01-01', 'NgayKetThuc': '2026-12-31'})
check('Thêm KM: ngày kết thúc trước ngày bắt đầu', 'POST', '/api/promotions', 400, token=A, body={'MaKhuyenMai': 'ZZ', 'TenKhuyenMai': 'x', 'LoaiGiam': 'Phần trăm', 'GiaTriGiam': 1, 'NgayBatDau': '2026-12-31', 'NgayKetThuc': '2026-01-01'})
check('Tất cả đánh giá', 'GET', '/api/reviews', 200, token=A)
check('Đánh giá phòng chưa ở', 'POST', '/api/reviews', 400, token=K, body={'MaPhong': 'P999', 'SoSao': 5, 'NoiDung': 'x'})
check('Đánh giá: số sao 9', 'POST', '/api/reviews', 400, token=K, body={'MaPhong': 'P101', 'SoSao': 9, 'NoiDung': 'x'})
check('Dashboard', 'GET', '/api/dashboard', 200, token=A, show=lambda d: f"Doanh thu {d['TongDoanhThu']:,.0f} | {d['TongDatPhong']} phiếu | {d['TongKhachHang']} khách | {len(d['DoanhThuTheoThang'])} tháng")

print('--- Đặt phòng (chỉ kiểm tra đọc + các trường hợp lỗi) ---')
bookings = check('Lịch sử đặt phòng', 'GET', '/api/bookings', 200, token=L)
b0 = bookings[0]
print(f"     Phiếu đầu tiên: KhachHang.SDT = {b0['KhachHang']['SDT']} (trước đây bị null)")
check('Tìm kiếm không dấu "tran"', 'GET', '/api/bookings?search=tran', 200, token=L)
check('Phiếu online chờ duyệt', 'GET', '/api/bookings/online', 200, token=L)
check('Đang lưu trú', 'GET', '/api/bookings/active', 200, token=L)
check('Chi tiết phiếu', 'GET', f"/api/bookings/{b0['MaDatPhong']}", 200, token=L)
other = next((b for b in bookings if b['MaKhachHang'] != khach['MaKhachHang']), None)
if other: check('Khách xem phiếu người khác', 'GET', f"/api/bookings/{other['MaDatPhong']}", 403, token=K)
mine = next((b for b in bookings if b['MaKhachHang'] == khach['MaKhachHang']), None)
if mine: check('Khách xem phiếu của mình', 'GET', f"/api/bookings/{mine['MaDatPhong']}", 200, token=K)
check('Duyệt phiếu không tồn tại', 'POST', '/api/bookings/XXX/approve', 404, token=L)
done = next((b for b in bookings if b['TrangThai'] == 'CheckedOut'), None)
if done:
    check('Duyệt phiếu đã trả phòng → 409', 'POST', f"/api/bookings/{done['MaDatPhong']}/approve", 409, token=L)
    check('Check-in phiếu đã trả phòng → 409', 'POST', f"/api/bookings/{done['MaDatPhong']}/check-in", 409, token=L)
    check('Gọi DV cho phiếu đã trả phòng → 409', 'POST', f"/api/bookings/{done['MaDatPhong']}/services", 409, token=L, body={'Services': [{'MaDichVu': 'DV01', 'SoLuong': 1}]})
paid = next((b['HoaDon'] for b in bookings if b['HoaDon'] and b['HoaDon']['TrangThai'] == 'Paid'), None)
if paid: check('Thanh toán hoá đơn đã thanh toán → 409', 'POST', f"/api/invoices/{paid['MaHoaDon']}/payments", 409, token=L, body={'PhuongThuc': 'Tiền mặt'})
check('Thanh toán hoá đơn không tồn tại', 'POST', '/api/invoices/HDXXX/payments', 404, token=L, body={'PhuongThuc': 'Tiền mặt'})
check('Lập hoá đơn phiếu không tồn tại', 'POST', '/api/invoices', 404, token=L, body={'MaDatPhong': 'XXX'})
import datetime as _dt
_t0, _t1 = _dt.date.today().isoformat(), (_dt.date.today() + _dt.timedelta(days=1)).isoformat()
check('Khách đặt phòng đang có khách ở (trùng lịch hôm nay) → 409', 'POST', '/api/bookings', 409, token=K,
      body={'MaPhong': occupied[0] if occupied else 'P999', 'SoNguoi': 1, 'CheckIn': _t0, 'CheckOut': _t1})
check('Khách đặt: ngày trả trước ngày nhận', 'POST', '/api/bookings', 400, token=K,
      body={'MaPhong': 'P101', 'SoNguoi': 1, 'CheckIn': '2027-01-05', 'CheckOut': '2027-01-03'})
check('Lễ tân dùng API đặt online (chỉ khách)', 'POST', '/api/bookings', 403, token=L, body={'MaPhong': 'P101', 'SoNguoi': 1, 'CheckIn': '2027-01-01', 'CheckOut': '2027-01-03'})
check('Walk-in: thiếu dữ liệu', 'POST', '/api/bookings/walk-in', 400, token=L, body={'Ho': ''})

print('--- Khách hàng ---')
check('Danh sách khách hàng', 'GET', '/api/customers', 200, token=L)
check('Tìm theo tên', 'GET', '/api/customers?search=phuong', 200, token=L)
check('Chi tiết khách', 'GET', f"/api/customers/{khach['MaKhachHang']}", 200, token=L, show=lambda d: f"{d['HoTen']} | {len(d['PhieuDatPhongs'])} phiếu")
check('Tìm theo CCCD không có', 'GET', '/api/customers/by-cccd/000', 404, token=L)
check('Hồ sơ của tôi', 'GET', '/api/customers/me', 200, token=K, show=lambda d: f"{d['HoTen']} | {d['DiemTichLuy']} điểm")
check('Phiếu của tôi', 'GET', '/api/customers/me/bookings', 200, token=K)
check('Đánh giá của tôi', 'GET', '/api/customers/me/reviews', 200, token=K)

print('--- Buồng phòng, loại phòng, health ---')
check('Tổng quan buồng phòng', 'GET', '/api/housekeeping', 200, token=L, show=lambda d: f"đang dọn {d['SoPhongDangDon']}, trống {d['SoPhongTrong']}, đang ở {d['SoPhongDangO']}")
check('Loại phòng (khách)', 'GET', '/api/room-types', 200, token=K)
check('Health (không cần token)', 'GET', '/api/health', 200)

print(f"\n==> {sum(results)}/{len(results)} kiểm tra đạt")
sys.exit(0 if all(results) else 1)
