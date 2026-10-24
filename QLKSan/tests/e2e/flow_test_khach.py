"""Kiểm tra luồng khách hàng (kế hoạch nâng cấp khách hàng / giao diện hạng sang).
GĐ1: liên kết tài khoản cho khách vãng lai, đổi mật khẩu, khách tự sửa liên hệ, nhãn ô đăng nhập.
GĐ2: giao diện hạng sang — thông tin khách sạn công khai (/api/hotel), footer, trang chủ, đăng nhập chia đôi, lịch chọn ngày.
GĐ3: hạng phòng (ảnh, tiện nghi, đánh giá), đặt theo hạng + tự xác nhận, lễ tân đổi phòng, trang chi tiết phiếu, đánh giá theo kỳ nghỉ.
CHỈ chạy trên database vừa dựng từ DB/Quanlykhachsanc#.sql (script ghi dữ liệu: tạo tài khoản cho KH06, đổi liên hệ KH01)."""
import os
from csrfhelp import with_token
import html, http.cookiejar, json, re, ssl, sys, urllib.parse, urllib.request, urllib.error

WEB, API = os.environ.get('QLKSAN_WEB', 'https://localhost:7234'), os.environ.get('QLKSAN_API', 'https://localhost:7001')
CTX = ssl._create_unverified_context()
results = []

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k): return None

def web_session():
    return urllib.request.build_opener(urllib.request.HTTPSHandler(context=CTX), urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()), NoRedirect)

def web(op, method, path, form=None):
    form = with_token(op, method, form)
    data = urllib.parse.urlencode(form, doseq=True).encode() if form is not None else None
    try:
        with op.open(urllib.request.Request(WEB + path, data=data, method=method)) as r:
            return r.status, r.headers.get('Location'), html.unescape(r.read().decode('utf-8', 'replace'))
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get('Location'), html.unescape(e.read().decode('utf-8', 'replace'))

def web_post(op, path, form):
    s, loc, body = web(op, 'POST', path, form)
    if s == 302 and loc:
        u = urllib.parse.urlparse(loc); _, _, body = web(op, 'GET', u.path + ('?' + u.query if u.query else ''))
    return s, loc, body

def web_login(user, pwd='123456'):
    op = web_session()
    web(op, 'POST', '/Auth/Login', {'username': user, 'password': pwd})
    return op

def api(method, path, token=None, body=None):
    req = urllib.request.Request(API + path, data=json.dumps(body).encode() if body is not None else None, method=method)
    req.add_header('Content-Type', 'application/json')
    if token: req.add_header('Authorization', 'Bearer ' + token)
    try:
        with urllib.request.urlopen(req, context=CTX) as r:
            text = r.read().decode(); return r.status, json.loads(text) if text else None
    except urllib.error.HTTPError as e:
        text = e.read().decode()
        try: return e.code, json.loads(text) if text else None
        except Exception: return e.code, text

def login(user, pwd='123456'):
    return api('POST', '/api/auth/login', body={'TenDangNhap': user, 'MatKhau': pwd})

def tok(user, pwd='123456'): return login(user, pwd)[1]['AccessToken']

def alert(body):
    m = re.search(r'alert-(success|danger)[^>]*>\s*(?:<[^>]+>\s*)*([^<]+)', body)
    return (m.group(1), m.group(2).strip()) if m else (None, None)

def check(name, cond, info=''):
    results.append(bool(cond)); print(f"{'OK  ' if cond else 'FAIL'} {name}{' | ' + str(info) if info != '' else ''}")

def detail(r): return r.get('detail') if isinstance(r, dict) else r

A, L, K = tok('admin1'), tok('letan1'), tok('khach1')

print('===== 1.1 Liên kết tài khoản cho khách vãng lai =====')
# KH06 Đặng Minh Khoa: hồ sơ do lễ tân tạo khi nhận khách vãng lai, chưa có tài khoản, đã có 168 điểm
reg = {'Ho': 'Đặng', 'Ten': 'Minh Khoa', 'SDT': '0966666666', 'CCCD_Passport': '079085001234', 'NgaySinh': '1985-03-12',
       'Email': 'khoa.dang@gmail.com', 'DiaChi': 'Quận 1, TP. Hồ Chí Minh', 'TenDangNhap': 'khoadang', 'MatKhau': '123456', 'XacNhanMatKhau': '123456'}
s, r = api('POST', '/api/auth/register', body={**reg, 'SDT': '0912345678'})
check('CCCD có hồ sơ nhưng SĐT không khớp → 409, nhắc liên hệ lễ tân', s == 409 and 'không khớp' in detail(r), detail(r))
s, r = api('POST', '/api/auth/register', body=reg)
check('CCCD có hồ sơ + SĐT khớp → 201, liên kết tài khoản', s == 201 and r and r.get('AccessToken'), s)
KD = r['AccessToken'] if s == 201 else None
s, me = api('GET', '/api/customers/me', KD)
check('  Tài khoản mới gắn vào đúng hồ sơ KH06, giữ nguyên 168 điểm', s == 200 and me['MaKhachHang'] == 'KH06' and me['DiemTichLuy'] == 168, me and (me['MaKhachHang'], me['DiemTichLuy']))
check('  Ngày sinh cập nhật theo đăng ký, email/địa chỉ được bổ sung', me['NgaySinh'][:10] == '1985-03-12' and me['LienHeDiaChis'][0]['Email'] == 'khoa.dang@gmail.com'
      and me['LienHeDiaChis'][0]['DiaChi'] == 'Quận 1, TP. Hồ Chí Minh', (me['NgaySinh'][:10], me['LienHeDiaChis'][0]))
s, bks = api('GET', '/api/customers/me/bookings', KD)
check('  Thấy lịch sử lưu trú cũ (phiếu vãng lai DP05)', s == 200 and any(b['MaDatPhong'] == 'DP05' for b in bks), [b['MaDatPhong'] for b in bks])
s, r = api('POST', '/api/auth/register', body={**reg, 'TenDangNhap': 'khoadang2'})
check('Đăng ký lại cùng CCCD đã có tài khoản → 409', s == 409 and 'đã có tài khoản' in detail(r), detail(r))
s, r = api('POST', '/api/auth/register', body={**reg, 'CCCD_Passport': '079099998888', 'SDT': '0966000111', 'TenDangNhap': 'khach_moi_g1'})
check('Đăng ký CCCD mới → tạo hồ sơ mới như cũ', s == 201)

print('\n===== 1.3 Đổi mật khẩu =====')
s, r = api('POST', '/api/auth/change-password', KD, {'MatKhauCu': 'sai', 'MatKhauMoi': 'matkhau2', 'XacNhanMatKhauMoi': 'matkhau2'})
check('Sai mật khẩu hiện tại → 400', s == 400 and 'không đúng' in detail(r), detail(r))
s, r = api('POST', '/api/auth/change-password', KD, {'MatKhauCu': '123456', 'MatKhauMoi': 'matkhau2', 'XacNhanMatKhauMoi': 'khac'})
check('Xác nhận không khớp → 400', s == 400)
s, r = api('POST', '/api/auth/change-password', KD, {'MatKhauCu': '123456', 'MatKhauMoi': '123', 'XacNhanMatKhauMoi': '123'})
check('Mật khẩu mới dưới 6 ký tự → 400', s == 400)
s, r = api('POST', '/api/auth/change-password', KD, {'MatKhauCu': '123456', 'MatKhauMoi': '123456', 'XacNhanMatKhauMoi': '123456'})
check('Mật khẩu mới trùng mật khẩu cũ → 400', s == 400 and 'phải khác' in detail(r), detail(r))
s, r = api('POST', '/api/auth/change-password', KD, {'MatKhauCu': '123456', 'MatKhauMoi': 'matkhau2', 'XacNhanMatKhauMoi': 'matkhau2'})
check('Đổi mật khẩu hợp lệ → 204', s == 204, s)
check('  Mật khẩu cũ không đăng nhập được nữa', login('khoadang', '123456')[0] == 401)
check('  Mật khẩu mới đăng nhập được', login('khoadang', 'matkhau2')[0] == 200)
check('Chưa đăng nhập gọi đổi mật khẩu → 401', api('POST', '/api/auth/change-password', None, {'MatKhauCu': 'a', 'MatKhauMoi': 'bbbbbb', 'XacNhanMatKhauMoi': 'bbbbbb'})[0] == 401)
s, _ = api('POST', '/api/auth/change-password', tok('letan2'), {'MatKhauCu': '123456', 'MatKhauMoi': 'letan2pw', 'XacNhanMatKhauMoi': 'letan2pw'})
check('Nhân viên cũng tự đổi được mật khẩu', s == 204 and login('letan2', 'letan2pw')[0] == 200)
api('POST', '/api/auth/change-password', tok('letan2', 'letan2pw'), {'MatKhauCu': 'letan2pw', 'MatKhauMoi': '123456', 'XacNhanMatKhauMoi': '123456'})

print('\n===== 1.4 Khách tự sửa liên hệ =====')
s, r = api('PUT', '/api/customers/me/contact', K, {'SDT': '12345', 'DiaChi': 'x'})
check('SĐT sai định dạng → 400', s == 400, r and r.get('errors'))
s, r = api('PUT', '/api/customers/me/contact', K, {'SDT': '0911111111', 'Email': 'sai-email', 'DiaChi': 'x'})
check('Email sai định dạng → 400', s == 400)
s, r = api('PUT', '/api/customers/me/contact', K, {'SDT': '0911222333', 'Email': 'phuong.moi@gmail.com', 'DiaChi': 'Sơn Trà, Đà Nẵng', 'QuocGia': 'Việt Nam'})
check('Cập nhật hợp lệ → 200, trả về hồ sơ mới', s == 200 and r['LienHeDiaChis'][0]['SDT'] == '0911222333', s)
s, me = api('GET', '/api/customers/me', K)
check('  Hồ sơ hiển thị liên hệ mới, họ tên & CCCD giữ nguyên', me['LienHeDiaChis'][0]['DiaChi'] == 'Sơn Trà, Đà Nẵng' and me['CCCD_Passport'] == '048204000123' and me['Ten'] == 'Văn Phương')
check('Lễ tân gọi endpoint của khách → 403', api('PUT', '/api/customers/me/contact', L, {'SDT': '0911222333', 'DiaChi': 'x'})[0] == 403)
check('Chưa đăng nhập → 401', api('PUT', '/api/customers/me/contact', None, {'SDT': '0911222333', 'DiaChi': 'x'})[0] == 401)

print('\n===== Giao diện Web =====')
kw = web_login('khach1')
s, _, body = web(kw, 'GET', '/Customer/Profile')
check('Hồ sơ: có nút "Cập nhật liên hệ" và form sửa liên hệ', s == 200 and 'Cập nhật liên hệ' in body and 'name="SDT"' in body and '/Customer/UpdateContact' in body)
check('Hồ sơ: có lối vào "Đổi mật khẩu"', '/Auth/ChangePassword' in body)
s, loc, body = web_post(kw, '/Customer/UpdateContact', {'SDT': '0911111111', 'Email': 'phuong.vo@gmail.com', 'DiaChi': 'Hải Châu, Đà Nẵng', 'QuocGia': 'Việt Nam'})
check('Form sửa liên hệ → thông báo thành công, hiện địa chỉ mới', alert(body)[0] == 'success' and 'Hải Châu, Đà Nẵng' in body, alert(body))
s, loc, body = web_post(kw, '/Customer/UpdateContact', {'SDT': '999', 'DiaChi': 'x'})
check('Form: SĐT sai → báo lỗi', alert(body)[0] == 'danger', alert(body))
s, _, body = web(kw, 'GET', '/Auth/ChangePassword')
check('Trang đổi mật khẩu của khách dùng giao diện khách', s == 200 and 'lr-navbar' in body and 'sidebar' not in body)
s, loc, body = web_post(kw, '/Auth/ChangePassword', {'MatKhauCu': 'sai', 'MatKhauMoi': 'abcdef', 'XacNhanMatKhauMoi': 'abcdef'})
check('Form đổi mật khẩu: sai mật khẩu hiện tại → báo lỗi từ API', 'Mật khẩu hiện tại không đúng' in body)
lw = web_login('letan1')
s, _, body = web(lw, 'GET', '/Auth/ChangePassword')
check('Trang đổi mật khẩu của nhân viên dùng giao diện quản trị', s == 200 and 'sidebar' in body)
s, _, body = web(lw, 'GET', '/Reception')
check('Thanh trên cùng của nhân viên có lối vào đổi mật khẩu', '/Auth/ChangePassword' in body)
s, _, body = web(web_session(), 'GET', '/Auth/Login')
check('Ô đăng nhập không còn ghi "hoặc Email"', 'hoặc Email' not in body and 'Nhập tên đăng nhập' in body)
s, loc, _ = web(web_session(), 'GET', '/Auth/ChangePassword')
check('Chưa đăng nhập mở trang đổi mật khẩu → chuyển tới đăng nhập', s == 302 and '/Auth/Login' in (loc or ''), loc)

print('\n===== GĐ2: Giao diện hạng sang =====')
s, info = api('GET', '/api/hotel')
check('GET /api/hotel công khai: liên hệ + giờ nhận/trả + phụ thu + chính sách huỷ', s == 200 and info['Hotline'] and info['GioNhanPhong'] == '14:00'
      and info['GioTraPhong'] == '12:00' and len(info['PhuThuTraMuon']) == 2 and 'trước ngày nhận phòng' in info['ChinhSachHuy'], info and info.get('GioNhanPhong'))
anon = web_session()
s, _, body = web(anon, 'GET', '/')
check('Trang chủ: ảnh hero tràn màn hình + header trong suốt', s == 200 and 'lx-hero' in body and 'lr-navbar--overlay' in body)
text = re.sub(r'<[^>]+>', ' ', body)
check('  Không lộ giá theo giờ / mã hạng phòng nội bộ', '/giờ' not in text and 'LP0' not in text and '(DLX)' not in text)
check('  Giá dạng "Từ … ₫ / đêm"', re.search(r'Từ\s*<strong>[\d,\.]+ ₫</strong> / đêm', body) is not None)
check('  Không còn bảng giá dịch vụ (₫ / Kg, ₫ / Lần)', '₫ / Kg' not in body and '₫ / Lần' not in body)
svcs = api('GET', '/api/services')[1]
tn = [x for x in svcs if x['LaTraiNghiem'] and x['TrangThai'] == 'Available'][:3]
check('  Mục "Trải nghiệm" lấy từ dịch vụ được đánh dấu và đang phục vụ (kèm mô tả)', tn and all(x['TenDichVu'] in body and x['MoTa'] in body for x in tn)
      and all(x['TenDichVu'] not in text for x in svcs if x['LaTraiNghiem'] and x['TrangThai'] != 'Available'), [x['TenDichVu'] for x in tn])
check('  Thanh tìm phòng dùng lịch chọn khoảng ngày', 'id="stay-range"' in body and 'flatpickr' in body)
check('Footer: địa chỉ, hotline, giờ nhận/trả phòng, chính sách huỷ từ API', info['DiaChi'] in body and info['Hotline'] in body
      and 'Nhận phòng từ 14:00' in body and 'Huỷ miễn phí' in body)
s, _, body = web(anon, 'GET', '/Auth/Login')
check('Đăng nhập: màn hình chia đôi (ảnh + biểu mẫu)', s == 200 and 'lr-auth-visual' in body and 'lr-auth-form' in body)
s, _, body = web(kw, 'GET', '/Customer/BookRoom')
check('Đặt phòng: ô ngày dùng lịch tiếng Việt, nhãn "Ngày nhận phòng"', 'data-lr-date' in body and 'Ngày nhận phòng' in body and 'Check-in' not in body)
s, _, body = web(kw, 'GET', '/Customer/MyStays')
check('Lịch sử kỳ nghỉ: tên hạng phòng không kèm mã nội bộ', '(DLX)' not in body and '(STD)' not in body and 'Deluxe' in body)
check('Trang khách có footer mới', 'lr-footer-rich' in body)

print('\n===== GĐ3: Hạng phòng, đặt theo hạng, phiếu, đánh giá theo kỳ nghỉ =====')
import datetime as _dt
d = lambda n: (_dt.date.today() + _dt.timedelta(days=n)).isoformat()
s, types = api('GET', '/api/room-types')
check('GET /api/room-types công khai: kèm diện tích, ảnh đại diện, điểm đánh giá', s == 200 and len(types) == 5 and all(t['DienTich'] and t['AnhDaiDien'] for t in types)
      and any(t['DiemDanhGia'] for t in types), s)
s, lp3 = api('GET', '/api/room-types/LP03')
check('GET /api/room-types/LP03: bộ ảnh + tiện nghi + mô tả chi tiết', s == 200 and lp3['HinhAnhs'] and lp3['TienIchs'] and lp3['MoTaChiTiet'], s)
check('GET /api/room-types/XX → 404', api('GET', '/api/room-types/XX')[0] == 404)
s, av = api('GET', f'/api/room-types/available?checkIn={d(30)}&checkOut={d(32)}&soNguoi=4')
check('Hạng còn trống cho 4 khách: chỉ hạng đủ sức chứa, báo giá 2 đêm, rẻ trước', s == 200 and av and all(x['LoaiPhong']['SoNguoiToiDa'] >= 4 and x['BaoGia']['SoDem'] == 2 for x in av)
      and [x['BaoGia']['TongTien'] for x in av] == sorted(x['BaoGia']['TongTien'] for x in av), [x['LoaiPhong']['MaLoai'] for x in av])
check('Ngày trả trước ngày nhận → 400', api('GET', f'/api/room-types/available?checkIn={d(5)}&checkOut={d(4)}')[0] == 400)
s, fr = api('GET', '/api/reviews/featured?soLuong=3')
check('Đánh giá nổi bật công khai: từ 4 sao, không lộ mã / họ tên khách', s == 200 and fr and all(x['SoSao'] >= 4 and not x['MaKhachHang'] and not x['HoTenKhachHang'] and x['TenHienThi'] for x in fr))
check('Đánh giá theo hạng phòng (công khai)', api('GET', '/api/room-types/LP01/reviews')[0] == 200)

K2 = tok('khach2')
s, r = api('POST', '/api/bookings', K2, {'MaLoai': 'LP05', 'SoNguoi': 2, 'CheckIn': d(40), 'CheckOut': d(42), 'YeuCauDacBiet': 'Cần nôi em bé', 'GioDenDuKien': 'Sau 21:00'})
B1 = r.get('MaDatPhong') if s == 201 else None
check('Đặt theo hạng phòng LP05 → 201, tự xếp phòng thuộc hạng, xác nhận ngay', s == 201 and r['TrangThai'] == 'Approved' and r['ChiTietDatPhongs'][0]['Phong']['MaLoai'] == 'LP05', detail(r))
check('  Lưu yêu cầu riêng + giờ đến dự kiến', s == 201 and r['YeuCauDacBiet'] == 'Cần nôi em bé' and r['GioDenDuKien'] == 'Sau 21:00')
check('Gửi cả MaLoai và MaPhong → 400', api('POST', '/api/bookings', K2, {'MaLoai': 'LP05', 'MaPhong': 'P101', 'SoNguoi': 1, 'CheckIn': d(40), 'CheckOut': d(41)})[0] == 400)
check('Không gửi MaLoai lẫn MaPhong → 400', api('POST', '/api/bookings', K2, {'SoNguoi': 1, 'CheckIn': d(40), 'CheckOut': d(41)})[0] == 400)
s, r = api('POST', '/api/bookings', K2, {'MaLoai': 'LP04', 'SoNguoi': 2, 'CheckIn': d(60), 'CheckOut': d(62)})
check('Đặt Suite (hạng chỉ có 1 phòng) → 201', s == 201, detail(r))
s, r = api('POST', '/api/bookings', tok('khach4'), {'MaLoai': 'LP04', 'SoNguoi': 2, 'CheckIn': d(61), 'CheckOut': d(63)})
check('  Khách khác đặt cùng hạng, trùng ngày → 409 hết phòng', s == 409 and 'hết phòng' in detail(r), detail(r))
check('  Hạng hết phòng không còn trong danh sách trống', all(x['LoaiPhong']['MaLoai'] != 'LP04' for x in api('GET', f'/api/room-types/available?checkIn={d(60)}&checkOut={d(62)}')[1]))

free = [p for p in api('GET', f'/api/rooms/available?checkIn={d(40)}&checkOut={d(42)}&soNguoi=2')[1] if p['LoaiPhong']['MaLoai'] != 'LP05']
target = free[0]
check('Khách tự đổi phòng → 403', api('POST', f'/api/bookings/{B1}/change-room', K2, {'MaPhong': target['MaPhong']})[0] == 403)
s, r = api('POST', f'/api/bookings/{B1}/change-room', L, {'MaPhong': target['MaPhong']})
check('Lễ tân đổi phòng trước ngày đến → 200, giá theo hạng phòng mới', s == 200 and r['ChiTietDatPhongs'][0]['MaPhong'] == target['MaPhong']
      and r['ChiTietDatPhongs'][0]['GiaThueThucTe'] == target['LoaiPhong']['GiaThueNgay'], detail(r))
check('Đổi sang phòng không tồn tại → 404', api('POST', f'/api/bookings/{B1}/change-room', L, {'MaPhong': 'P999'})[0] == 404)
check('Đổi phòng cho phiếu đã trả phòng → 409', api('POST', '/api/bookings/DP01/change-room', L, {'MaPhong': target['MaPhong']})[0] == 409)

s, r = api('POST', '/api/reviews', KD, {'MaDatPhong': 'DP01', 'SoSao': 5, 'NoiDung': 'x', 'AnDanh': False})
check('Đánh giá kỳ nghỉ của người khác → 400', s == 400 and 'kỳ nghỉ của mình' in detail(r), detail(r))
s, r = api('POST', '/api/reviews', KD, {'MaDatPhong': 'DP05', 'SoSao': 5, 'NoiDung': 'Phòng sạch, lễ tân chu đáo.', 'AnDanh': True})
check('Đánh giá kỳ nghỉ DP05 đã trả phòng → thành công', s in (200, 201), detail(r))
s, r = api('POST', '/api/reviews', KD, {'MaDatPhong': 'DP05', 'SoSao': 4, 'NoiDung': 'lần 2', 'AnDanh': False})
check('  Đánh giá lại cùng kỳ nghỉ → 409', s == 409, detail(r))
s, fr = api('GET', '/api/reviews/featured?soLuong=10')
check('  Đánh giá ẩn danh hiển thị "Khách ẩn danh"', any(x['MaDatPhong'] == 'DP05' and x['TenHienThi'] == 'Khách ẩn danh' for x in fr))

anon = web_session()
s, _, body = web(anon, 'GET', f'/Customer/BookRoom?checkIn={d(30)}&checkOut={d(32)}&soNguoi=2')
check('Web: chưa đăng nhập vẫn xem được hạng còn trống + tổng giá', s == 200 and 'Còn trống từ' in body and 'Chọn phòng này' in body and 'bk-total' in body)
s, _, body = web(anon, 'GET', f'/Customer/BookRoom?checkIn={d(30)}&checkOut={d(32)}&soNguoi=2&maLoai=LP01')
check('  Bước xác nhận yêu cầu đăng nhập, giữ lại lựa chọn (returnUrl)', s == 200 and 'Đăng nhập để hoàn tất' in body and 'returnUrl=' in body and 'maLoai%3DLP01' in body)
s, loc, _ = web(anon, 'POST', '/Customer/BookRoom', {'MaLoai': 'LP01', 'SoNguoi': 1, 'CheckIn': d(30), 'CheckOut': d(32)})
check('  Gửi đặt phòng khi chưa đăng nhập → chuyển tới đăng nhập', s == 302 and '/Auth/Login' in (loc or ''), loc)
s, _, body = web(anon, 'GET', '/Home/RoomType/LP03')
check('Web: trang chi tiết hạng phòng (ảnh, thông số, tiện nghi, ô đặt phòng)', s == 200 and 'Phòng Deluxe Hướng Biển' in body and 'rt-gallery' in body
      and 'Tiện nghi trong phòng' in body and 'name="maLoai" value="LP03"' in body)
check('  Hạng phòng không tồn tại → 404', web(anon, 'GET', '/Home/RoomType/XX')[0] == 404)
s, _, body = web(anon, 'GET', '/')
check('Trang chủ: thẻ hạng phòng dẫn tới trang chi tiết + mục đánh giá khách', '/Home/RoomType/LP03' in body and 'Khách nói về chúng tôi' in body)

k2w = web_login('khach2')
s, loc, body = web_post(k2w, '/Customer/BookRoom', {'MaLoai': 'LP02', 'SoNguoi': 2, 'CheckIn': d(30), 'CheckOut': d(32),
                                                  'YeuCauDacBiet': 'Tầng cao, gần thang máy', 'GioDenDuKien': '18:00 – 21:00'})
B2 = (loc or '').rsplit('/', 1)[-1]
check('Web: đặt theo hạng → chuyển tới trang chi tiết phiếu', s == 302 and (loc or '').startswith('/Customer/Booking/') and alert(body)[0] == 'success', loc)
check('  Trang phiếu: đếm ngược, yêu cầu riêng, giá từng đêm, nút huỷ', 'Còn 30 ngày' in body and 'Tầng cao, gần thang máy' in body
      and '18:00 – 21:00' in body and body.count('bk-line night') == 2 and 'Huỷ đặt phòng' in body)
s, loc, _ = web(k2w, 'GET', '/Customer/Booking/DP01')
check('  Xem phiếu của khách khác → bị từ chối', s == 302 and 'AccessDenied' in (loc or ''), loc)
s, loc, body = web_post(k2w, '/Customer/CancelBooking', {'id': B2, 'lyDo': 'Đổi kế hoạch', 'veChiTiet': 'true'})
check('  Huỷ từ trang phiếu → quay lại trang phiếu, trạng thái đã huỷ', loc == f'/Customer/Booking/{B2}' and 'đã được huỷ' in body, loc)

k3w = web_login('khach3')
s, _, body = web(k3w, 'GET', '/Customer/Feedback')
check('Web: trang đánh giá liệt kê kỳ nghỉ đã trả phòng chưa đánh giá (DP06)', s == 200 and 'value="DP06"' in body and 'value="DP01"' not in body)
s, loc, body = web_post(k3w, '/Customer/SubmitFeedback', {'maDatPhong': 'DP06', 'rating': 5, 'comment': 'Bữa sáng rất ngon.', 'isAnonymous': 'false'})
check('  Gửi đánh giá → về trang phiếu, hiện đánh giá đã gửi', loc == '/Customer/Booking/DP06' and 'Bữa sáng rất ngon.' in body and 'Đã gửi ngày' in body, loc)
s, loc, body = web_post(k3w, '/Customer/SubmitFeedback', {'maDatPhong': 'DP06', 'rating': 3, 'comment': 'lần 2'})
check('  Gửi lại cùng kỳ nghỉ → báo lỗi', alert(body)[0] == 'danger', alert(body))
s, _, body = web(k3w, 'GET', '/Customer/MyStays')
check('  Lịch sử kỳ nghỉ: kỳ đã đánh giá hiện "ĐÃ ĐÁNH GIÁ", có lối vào chi tiết phiếu', 'ĐÃ ĐÁNH GIÁ' in body and '/Customer/Booking/DP06' in body)

print('\n===== GĐ3: Quản trị bộ ảnh / thông tin hạng phòng, dịch vụ trải nghiệm, lễ tân đổi phòng =====')
import struct, uuid, zlib
from csrfhelp import token as csrf_token

def png_bytes():
    # Ảnh PNG 1×1 hợp lệ
    chunk = lambda t, d: struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)
    return b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', 1, 1, 8, 2, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(b'\x00\xb0\x8d\x57')) + chunk(b'IEND', b'')

def multipart(fields, filename, content, ctype='image/png'):
    b = uuid.uuid4().hex
    parts = [f'--{b}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode() for k, v in fields.items()]
    parts.append(f'--{b}\r\nContent-Disposition: form-data; name="file"; filename="{filename}"\r\nContent-Type: {ctype}\r\n\r\n'.encode() + content + b'\r\n')
    return b''.join(parts) + f'--{b}--\r\n'.encode(), f'multipart/form-data; boundary={b}'

def api_upload(token_, maLoai, filename, content):
    data, ctype = multipart({}, filename, content)
    req = urllib.request.Request(f'{API}/api/room-types/{maLoai}/images', data=data, method='POST')
    req.add_header('Content-Type', ctype)
    if token_: req.add_header('Authorization', 'Bearer ' + token_)
    try:
        with urllib.request.urlopen(req, context=CTX) as r: return r.status, json.loads(r.read().decode())
    except urllib.error.HTTPError as e:
        t = e.read().decode()
        try: return e.code, json.loads(t)
        except Exception: return e.code, t

s, r = api_upload(A, 'LP05', 'phong.png', png_bytes())
check('Admin tải ảnh PNG lên bộ ảnh hạng LP05 → 201, lưu ở /uploads/room-types/LP05/', s == 201 and r['DuongDan'].startswith('/uploads/room-types/LP05/'), detail(r))
img = r if s == 201 else {}
try:
    with urllib.request.urlopen(API + img.get('DuongDan', '/uploads/x'), context=CTX) as f: served = f.status == 200 and f.read()[:4] == b'\x89PNG'
except Exception: served = False
check('  Ảnh được API phục vụ công khai tại đường dẫn trả về', served)
check('  Ảnh xuất hiện trong bộ ảnh của hạng phòng', any(h['MaHinhAnh'] == img.get('MaHinhAnh') for h in api('GET', '/api/room-types/LP05')[1]['HinhAnhs']))
s, r = api_upload(A, 'LP05', 'gia-anh.png', b'day khong phai anh')
check('File không phải ảnh (đổi đuôi .png) → 400', s == 400 and 'không phải ảnh' in detail(r), detail(r))
s, r = api_upload(A, 'LP05', 'tai-lieu.pdf', png_bytes())
check('Đuôi file không hỗ trợ → 400', s == 400, detail(r))
check('Lễ tân tải ảnh → 403', api_upload(L, 'LP05', 'a.png', png_bytes())[0] == 403)
check('Hạng phòng không tồn tại → 404', api_upload(A, 'XX', 'a.png', png_bytes())[0] == 404)
s, _ = api('DELETE', f"/api/room-types/LP05/images/{img.get('MaHinhAnh')}", A)
check('Xoá ảnh → 204, file bị xoá khỏi máy chủ', s == 204 and all(h['MaHinhAnh'] != img.get('MaHinhAnh') for h in api('GET', '/api/room-types/LP05')[1]['HinhAnhs']))
check('  Xoá lại ảnh đã xoá → 404', api('DELETE', f"/api/room-types/LP05/images/{img.get('MaHinhAnh')}", A)[0] == 404)

# Hạng phòng mới chưa có ảnh: ảnh đầu tiên tải lên trở thành ảnh đại diện, xoá hết thì bỏ ảnh đại diện
s, r = api('POST', '/api/room-types', A, {'TenLoai': 'Hạng Thử Ảnh', 'SoNguoiToiDa': 2, 'GiaThueNgay': 900000, 'GiaThueGio': 100000, 'MoTa': 'x',
                                         'DienTich': 30, 'LoaiGiuong': '1 giường đôi', 'HuongNhin': 'Hướng phố', 'MoTaChiTiet': 'Chi tiết'})
NEW = r['MaLoai'] if s == 201 else None
check('Thêm hạng phòng kèm diện tích / giường / hướng nhìn / mô tả chi tiết', s == 201 and r['DienTich'] == 30 and r['HuongNhin'] == 'Hướng phố', detail(r))
s1, i1 = api_upload(A, NEW, 'a.png', png_bytes()); s2, i2 = api_upload(A, NEW, 'b.png', png_bytes())
t = api('GET', f'/api/room-types/{NEW}')[1]
check('  Ảnh đầu tiên tải lên là ảnh đại diện', s1 == s2 == 201 and t['AnhDaiDien'] == i1['DuongDan'] and [h['MaHinhAnh'] for h in t['HinhAnhs']] == [i1['MaHinhAnh'], i2['MaHinhAnh']])
api('DELETE', f"/api/room-types/{NEW}/images/{i1['MaHinhAnh']}", A)
check('  Xoá ảnh đại diện → ảnh kế tiếp thành ảnh đại diện', api('GET', f'/api/room-types/{NEW}')[1]['AnhDaiDien'] == i2['DuongDan'])
api('DELETE', f"/api/room-types/{NEW}/images/{i2['MaHinhAnh']}", A)
check('  Xoá hết ảnh → không còn ảnh đại diện', api('GET', f'/api/room-types/{NEW}')[1]['AnhDaiDien'] is None)

adw = web_login('admin1')
s, loc, body = web_post(adw, '/Admin/EditRoomType', {'maLoai': NEW, 'tenLoai': 'Hạng Thử Ảnh', 'soNguoiToiDa': 2, 'giaThueNgay': 900000, 'giaThueGio': 100000,
                                                    'moTa': 'Mô tả "có ngoặc"', 'dienTich': 32, 'loaiGiuong': '2 giường đơn', 'huongNhin': 'Hướng vườn', 'moTaChiTiet': 'Dòng 1\nDòng 2'})
t = api('GET', f'/api/room-types/{NEW}')[1]
check('Web: sửa hạng phòng cập nhật các trường mới', alert(body)[0] == 'success' and t['DienTich'] == 32 and t['LoaiGiuong'] == '2 giường đơn' and t['MoTaChiTiet'] == 'Dòng 1\nDòng 2', alert(body))
check('  Trang danh sách hiện ảnh, diện tích, nút bộ ảnh', '32 m²' in body and f'/Admin/RoomTypeImages/{NEW}' in body)
s, _, body = web(adw, 'GET', f'/Admin/RoomTypeImages/{NEW}')
check('Web: trang bộ ảnh có form tải ảnh', s == 200 and 'enctype="multipart/form-data"' in body and 'chưa có ảnh' in body)
data, ctype = multipart({'maLoai': NEW, '__RequestVerificationToken': csrf_token(adw)}, 'web.png', png_bytes())
rq = urllib.request.Request(WEB + '/Admin/UploadRoomTypeImage', data=data, method='POST'); rq.add_header('Content-Type', ctype)
try:
    with adw.open(rq) as r: up_status, up_loc = r.status, r.headers.get('Location')
except urllib.error.HTTPError as e: up_status, up_loc = e.code, e.headers.get('Location')
s, _, body = web(adw, 'GET', f'/Admin/RoomTypeImages/{NEW}')
t = api('GET', f'/api/room-types/{NEW}')[1]
check('Web: tải ảnh qua form quản trị → ảnh hiện trong bộ ảnh (ảnh đại diện)', up_status == 302 and len(t['HinhAnhs']) == 1 and 'Ảnh đại diện' in body and t['HinhAnhs'][0]['DuongDan'] in body, (up_status, up_loc))
s, loc, body = web_post(adw, '/Admin/DeleteRoomTypeImage', {'maLoai': NEW, 'maHinhAnh': t['HinhAnhs'][0]['MaHinhAnh']})
check('Web: xoá ảnh', alert(body)[0] == 'success' and not api('GET', f'/api/room-types/{NEW}')[1]['HinhAnhs'])
api('DELETE', f'/api/room-types/{NEW}', A)

s, loc, body = web_post(adw, '/Admin/AddService', {'tenDichVu': 'Lớp nấu ăn', 'giaDichVu': 300000, 'donViTinh': 'Lần', 'laTraiNghiem': 'true', 'moTa': 'Học nấu món địa phương cùng đầu bếp.'})
sv = next((x for x in api('GET', '/api/services')[1] if x['TenDichVu'] == 'Lớp nấu ăn'), None)
check('Web: thêm dịch vụ đánh dấu "Trải nghiệm" kèm mô tả', alert(body)[0] == 'success' and sv and sv['LaTraiNghiem'] and sv['MoTa'] == 'Học nấu món địa phương cùng đầu bếp.', alert(body))
s, loc, body = web_post(adw, '/Admin/EditService', {'maDichVu': sv['MaDichVu'], 'tenDichVu': 'Lớp nấu ăn', 'giaDichVu': 300000, 'donViTinh': 'Lần', 'moTa': ''})
check('Web: sửa dịch vụ bỏ đánh dấu "Trải nghiệm"', not next(x for x in api('GET', '/api/services')[1] if x['MaDichVu'] == sv['MaDichVu'])['LaTraiNghiem'])
api('DELETE', f"/api/services/{sv['MaDichVu']}", A)

s, r = api('POST', '/api/bookings', K2, {'MaLoai': 'LP01', 'SoNguoi': 1, 'CheckIn': d(50), 'CheckOut': d(51), 'YeuCauDacBiet': 'Phòng yên tĩnh', 'GioDenDuKien': 'Trước 14:00'})
B3 = r.get('MaDatPhong') if s == 201 else None
s, _, body = web(lw, 'GET', '/Reception')
check('Lễ tân: phiếu online hiện yêu cầu riêng, giờ đến và nút đổi phòng', 'Phòng yên tĩnh' in body and 'Trước 14:00' in body and f'/Reception/ChangeRoom/{B3}' in body)
s, _, body = web(lw, 'GET', f'/Reception/ChangeRoom/{B3}')
opts = re.findall(r'name="maPhong" value="([^"]+)"', body)
check('Lễ tân: trang đổi phòng liệt kê phòng trống đủ sức chứa', s == 200 and opts and r['ChiTietDatPhongs'][0]['MaPhong'] not in opts, opts)
s, loc, body = web_post(lw, f'/Reception/ChangeRoom/{B3}', {'id': B3, 'maPhong': opts[0]})
check('  Chọn phòng mới → phiếu chuyển sang phòng đã chọn', alert(body)[0] == 'success' and api('GET', f'/api/bookings/{B3}', L)[1]['ChiTietDatPhongs'][0]['MaPhong'] == opts[0], alert(body))

print(f"\n==> {sum(results)}/{len(results)} kiểm tra đạt")
sys.exit(0 if all(results) else 1)
