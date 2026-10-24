"""Lấy antiforgery token như trình duyệt thật: mở một trang có form (trang Đăng ký mở được với mọi người dùng)
và đọc giá trị __RequestVerificationToken. Token gắn với người đang đăng nhập nên lấy mới trước mỗi POST."""
import html, re, urllib.error, urllib.request

WEB = 'https://localhost:7234'

def token(op):
    try:
        with op.open(urllib.request.Request(WEB + '/Auth/Register')) as r:
            body = r.read().decode('utf-8', 'replace')
    except urllib.error.HTTPError as e:
        body = e.read().decode('utf-8', 'replace')
    m = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', body)
    return html.unescape(m.group(1)) if m else ''

def with_token(op, method, form):
    if method == 'POST' and form is not None and '__RequestVerificationToken' not in form:
        form = dict(form)
        form['__RequestVerificationToken'] = token(op)
    return form
