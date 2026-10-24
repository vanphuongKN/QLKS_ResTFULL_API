"""Chụp nội dung chữ hiển thị của các trang Web để so sánh trước/sau khi chuyển sang gọi API.
Cách dùng: python snapshot_pages.py <thư mục lưu>"""
from csrfhelp import with_token
import html, http.cookiejar, json, os, re, ssl, sys, urllib.parse, urllib.request, urllib.error

WEB = os.environ.get('QLKSAN_WEB', 'https://localhost:7234')
CTX = ssl._create_unverified_context()
OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k): return None

def session(user=None):
    jar = http.cookiejar.CookieJar()
    op = urllib.request.build_opener(urllib.request.HTTPSHandler(context=CTX), urllib.request.HTTPCookieProcessor(jar), NoRedirect)
    if user:
        data = urllib.parse.urlencode(with_token(op, 'POST', {'username': user, 'password': '123456'})).encode()
        try: op.open(urllib.request.Request(WEB + '/Auth/Login', data=data, method='POST'))
        except urllib.error.HTTPError: pass
    return op

def get(op, path):
    try:
        with op.open(urllib.request.Request(WEB + path)) as r:
            return r.status, r.read().decode('utf-8', 'replace')
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode('utf-8', 'replace')

def visible_text(page):
    page = re.sub(r'(?is)<(style|svg)[^>]*>.*?</\1>', ' ', page)
    # giữ lại nội dung <script> (có dữ liệu JSON/biến JS sinh từ server) nhưng bỏ thẻ
    page = re.sub(r'(?s)<!--.*?-->', ' ', page)
    # giữ value="..." của input (dữ liệu điền sẵn)
    page = re.sub(r'(?i)<input[^>]*?value="([^"]*)"[^>]*>', r' [value=\1] ', page)
    page = re.sub(r'(?s)<[^>]+>', ' ', page)
    page = html.unescape(page)
    lines = [re.sub(r'\s+', ' ', l).strip() for l in page.splitlines()]
    return '\n'.join(l for l in lines if l)

PAGES = {
    None: ['/'],
    'letan1': ['/', '/Reception', '/Reception?search=tran', '/Reception/Customers', '/Reception/Customers?search=phuong',
               '/Reception/BookingHistory', '/Reception/BookingHistory?search=101', '/Reception/CustomerDetails/KH01',
               '/Reception/CustomerDetails/KH04', '/Reception/WalkIn', '/Room/Status',
               '/Room/Details/P101', '/Room/Details/P102', '/Room/Details/P201', '/Room/Details/P202', '/Room/Details/P301', '/Room/Details/P302',
               '/Service', '/Service?maPhong=P102', '/Service?roomNumber=102', '/Housekeeping/Tasks',
               '/Checkout/Process', '/Checkout/Process?maPhong=P102', '/Checkout/Process?maPhong=P101',
               '/Room/Timeline', '/Room/Timeline?soNgay=31', '/Room/Guests', '/Invoice/Print/HD01'],
    'admin1': ['/Admin', '/Admin/RoomTypes', '/Admin/Rooms', '/Admin/Services', '/Admin/Staff', '/Admin/Promotions', '/Admin/Reviews', '/Admin/Pricing'],
    'khach1': ['/', '/Customer/MyStays', '/Customer/Profile', '/Customer/Feedback', '/Customer/Feedback?roomId=P101',
               '/Customer/BookRoom', '/Customer/CheckPromotion?code=KM03', '/Customer/CheckPromotion?code=KM01',
               '/Customer/CheckPromotion?code=XX', '/Invoice/Print/HD01'],
    'khach4': ['/Customer/MyStays', '/Customer/Profile', '/Customer/Feedback'],
}

summary = {}
for user, paths in PAGES.items():
    op = session(user)
    for p in paths:
        status, body = get(op, p)
        name = f"{user or 'anon'}__{re.sub(r'[^A-Za-z0-9]+', '_', p).strip('_') or 'home'}.txt"
        text = visible_text(body)
        open(os.path.join(OUT, name), 'w', encoding='utf-8').write(f'HTTP {status}\n{text}\n')
        summary[name] = status
        print(f'{status} {user or "anon":7} {p:45} {len(text):6} ký tự')
json.dump(summary, open(os.path.join(OUT, '_summary.json'), 'w'), indent=1)
