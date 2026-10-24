import os
from csrfhelp import with_token
import html, http.cookiejar, ssl, sys, urllib.parse, urllib.request, urllib.error

WEB = os.environ.get('QLKSAN_WEB', 'https://localhost:7234')
CTX = ssl._create_unverified_context()
results = []

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k): return None

def session():
    jar = http.cookiejar.CookieJar()
    opener = urllib.request.build_opener(urllib.request.HTTPSHandler(context=CTX),
                                         urllib.request.HTTPCookieProcessor(jar), NoRedirect)
    return opener, jar

def req(opener, method, path, form=None):
    form = with_token(opener, method, form)
    data = urllib.parse.urlencode(form).encode() if form else None
    r = urllib.request.Request(WEB + path, data=data, method=method)
    try:
        with opener.open(r) as resp:
            return resp.status, resp.headers.get('Location'), html.unescape(resp.read().decode('utf-8', 'replace'))
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get('Location'), html.unescape(e.read().decode('utf-8', 'replace'))

def check(name, cond, info=''):
    results.append(bool(cond))
    print(f"{'OK  ' if cond else 'FAIL'} {name}{' | ' + info if info else ''}")

def login(opener, user, pwd='123456', return_url=None):
    form = {'username': user, 'password': pwd}
    if return_url: form['returnUrl'] = return_url
    return req(opener, 'POST', '/Auth/Login', form)

print('--- Chưa đăng nhập ---')
anon, _ = session()
s, loc, _ = req(anon, 'GET', '/Reception')
check('Vào /Reception khi chưa đăng nhập → chuyển tới trang đăng nhập', s == 302 and '/Auth/Login' in (loc or ''), f'{s} → {loc}')
s, _, body = req(anon, 'GET', '/')
check('Trang chủ mở được khi chưa đăng nhập', s == 200)
check('Trang chủ hiện hạng phòng thật (không còn phòng mẫu viết cứng, không lộ mã nội bộ)', 'Phòng Tiêu Chuẩn' in body and '(STD)' not in body and 'Imperial President Suite' not in body)
s, _, body = login(anon, 'admin1', 'saimatkhau')
check('Sai mật khẩu → thông báo lỗi từ API', s == 200 and 'Thông tin đăng nhập không chính xác' in body, f'{s}')

print('--- Lễ tân ---')
lt, jar = session()
s, loc, _ = login(lt, 'letan1')
check('letan1 đăng nhập → chuyển tới /Reception', s == 302 and loc.startswith('/Reception'), f'{s} → {loc}')
cookie = next((c for c in jar if c.name == 'QLKSan.Auth'), None)
check('Cookie đăng nhập QLKSan.Auth được tạo (HttpOnly)', cookie is not None and (cookie.has_nonstandard_attr('httponly') or cookie.has_nonstandard_attr('HttpOnly')))
for path in ['/Reception', '/Reception/Customers', '/Reception/BookingHistory', '/Room/Status', '/Room/Details/P101',
             '/Service', '/Housekeeping/Tasks', '/Checkout/Process', '/Reception/WalkIn']:
    s, loc, body = req(lt, 'GET', path)
    check(f'Lễ tân mở {path}', s == 200, f'{s} {loc or ""}')
s, _, body = req(lt, 'GET', '/Reception')
check('Layout hiển thị vai trò "Lễ tân"', 'Lễ tân' in body)
s, loc, _ = req(lt, 'GET', '/Admin')
check('Lễ tân vào /Admin → bị từ chối', s == 302 and 'AccessDenied' in (loc or ''), f'{s} → {loc}')
s, _, body = req(lt, 'GET', '/Auth/AccessDenied')
check('Trang AccessDenied hiển thị thông báo', s == 200 and 'không có quyền' in body)
s, loc, _ = req(lt, 'GET', '/Customer/MyStays')
check('Lễ tân vào trang khách hàng → bị từ chối', s == 302 and 'AccessDenied' in (loc or ''), f'{s} → {loc}')
s, loc, _ = req(lt, 'GET', '/Auth/Login')
check('Đã đăng nhập mà vào /Auth/Login → về trang của vai trò', s == 302 and loc.startswith('/Reception'), f'{loc}')
s, loc, _ = req(lt, 'GET', '/Auth/Logout')
check('Đăng xuất → về trang chủ', s == 302 and loc == '/', f'{loc}')
s, loc, _ = req(lt, 'GET', '/Reception')
check('Sau khi đăng xuất không vào được /Reception', s == 302 and '/Auth/Login' in (loc or ''), f'{loc}')

print('--- Admin ---')
ad, _ = session()
s, loc, _ = login(ad, 'admin1')
check('admin1 đăng nhập → chuyển tới /Admin', s == 302 and loc.startswith('/Admin'), f'{loc}')
for path in ['/Admin', '/Admin/RoomTypes', '/Admin/Rooms', '/Admin/Services', '/Admin/Staff', '/Admin/Promotions',
             '/Admin/Reviews', '/Reception', '/Room/Status']:
    s, loc, _ = req(ad, 'GET', path)
    check(f'Admin mở {path}', s == 200, f'{s} {loc or ""}')
s, _, body = req(ad, 'GET', '/Admin')
check('Sidebar hiện mục "Bảng điều khiển" cho Admin', 'Bảng điều khiển' in body)

print('--- Khách hàng ---')
kh, _ = session()
s, loc, _ = login(kh, 'khach1')
check('khach1 đăng nhập → chuyển tới /Customer/MyStays', s == 302 and loc.startswith('/Customer/MyStays'), f'{loc}')
for path in ['/Customer/MyStays', '/Customer/Profile', '/Customer/Feedback', '/Customer/BookRoom', '/Customer/CheckPromotion?code=KM03']:
    s, loc, _ = req(kh, 'GET', path)
    check(f'Khách mở {path}', s == 200, f'{s} {loc or ""}')
s, _, body = req(kh, 'GET', '/')
check('Trang chủ hiện trạng thái đã đăng nhập (link Đăng xuất, không còn nút Đăng nhập)', '/Auth/Logout' in body and 'href="/Auth/Login"' not in body)
s, loc, _ = req(kh, 'GET', '/Reception')
check('Khách vào /Reception → bị từ chối', s == 302 and 'AccessDenied' in (loc or ''), f'{loc}')

print('--- returnUrl ---')
ru, _ = session()
s, loc, _ = req(ru, 'GET', '/Room/Status')
s, loc, _ = login(ru, 'letan1', return_url='/Room/Status')
check('Đăng nhập với returnUrl → quay lại /Room/Status', s == 302 and loc == '/Room/Status', f'{loc}')
s, loc, _ = login(session()[0], 'letan1', return_url='https://evil.example.com')
check('returnUrl ra ngoài trang bị bỏ qua (chống open redirect)', s == 302 and loc.startswith('/Reception'), f'{loc}')

print('--- Đăng ký (chỉ kiểm tra lỗi, không tạo tài khoản) ---')
rg, _ = session()
base = {'ho': 'Test', 'ten': 'User', 'sdt': '0911111119', 'cccd': '999999999001', 'ngaySinh': '2000-01-01',
        'email': '', 'diaChi': 'Đà Nẵng', 'username': 'khach1', 'password': '123456', 'confirmPassword': '123456'}
s, _, body = req(rg, 'POST', '/Auth/Register', base)
check('Trùng tên đăng nhập → thông báo từ API (409)', s == 200 and 'Tên đăng nhập đã tồn tại' in body)
s, _, body = req(rg, 'POST', '/Auth/Register', dict(base, username='newuser_zz', confirmPassword='654321'))
check('Mật khẩu xác nhận sai → thông báo validation (400)', s == 200 and 'Mật khẩu xác nhận không khớp' in body)
check('Dữ liệu đã nhập được giữ lại trên form', 'value="Đà Nẵng"' in body)

print('--- Chống CSRF ---')
cs, _ = session()
login(cs, 'letan1')
raw = urllib.request.Request(WEB + '/Reception/ApproveBooking', data=urllib.parse.urlencode({'id': 'XXX'}).encode(), method='POST')
try:
    with cs.open(raw) as r: st = r.status
except urllib.error.HTTPError as e: st = e.code
check('POST không kèm antiforgery token bị chặn (400)', st == 400, str(st))
s, loc, _ = req(cs, 'POST', '/Reception/ApproveBooking', {'id': 'XXX'})
check('POST kèm token hợp lệ được xử lý', s == 302, f'{s} → {loc}')

print(f"\n==> {sum(results)}/{len(results)} kiểm tra đạt")
sys.exit(0 if all(results) else 1)
