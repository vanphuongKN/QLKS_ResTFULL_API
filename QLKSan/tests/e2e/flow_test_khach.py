"""Kiểm tra luồng khách hàng (kế hoạch nâng cấp khách hàng / giao diện hạng sang).
GĐ1: liên kết tài khoản cho khách vãng lai, đổi mật khẩu, khách tự sửa liên hệ, nhãn ô đăng nhập.
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

print(f"\n==> {sum(results)}/{len(results)} kiểm tra đạt")
sys.exit(0 if all(results) else 1)
