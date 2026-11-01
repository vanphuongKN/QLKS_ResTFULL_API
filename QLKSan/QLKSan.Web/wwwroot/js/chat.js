// Widget trợ lý ảo trên trang khách: gọi /Chat/Conversations và /Chat/Messages của Web (Web chuyển tiếp tới ChatBot qua Gateway)
(function () {
    const root = document.getElementById('lrChat');
    if (!root) return;
    const toggle = document.getElementById('lrChatToggle');
    const panel = document.getElementById('lrChatPanel');
    const body = document.getElementById('lrChatBody');
    const form = document.getElementById('lrChatForm');
    const input = document.getElementById('lrChatInput');
    const token = root.querySelector('input[name="__RequestVerificationToken"]').value;
    const KEY = 'qlksan.chat';
    let maHoiThoai = null;
    let dangGui = false;

    const store = {
        get() { try { return sessionStorage.getItem(KEY); } catch { return null; } },
        set(v) { try { v ? sessionStorage.setItem(KEY, v) : sessionStorage.removeItem(KEY); } catch { } }
    };

    async function post(url, data) {
        const res = await fetch(url, {
            method: 'POST',
            headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams(data)
        });
        const json = await res.json().catch(() => ({}));
        if (!res.ok) { const err = new Error(json.loi || 'Có lỗi xảy ra, vui lòng thử lại.'); err.status = res.status; throw err; }
        return json;
    }

    function el(tag, cls, text) {
        const e = document.createElement(tag);
        if (cls) e.className = cls;
        if (text != null) e.textContent = text;
        return e;
    }

    function render(m) {
        const wrap = el('div', 'lr-chat-msg ' + (m.VaiTro === 'khach' ? 'is-guest' : 'is-bot'));
        const bubble = el('div', 'lr-chat-bubble');
        (m.NoiDung || '').split('\n').forEach((line, i) => { if (i) bubble.appendChild(document.createElement('br')); bubble.appendChild(document.createTextNode(line)); });
        wrap.appendChild(bubble);
        if (m.LienKet && m.LienKet.length) {
            const links = el('div', 'lr-chat-links');
            m.LienKet.forEach(l => {
                const a = el('a', 'lr-chat-link', l.Nhan);
                a.href = l.Url;
                if (!l.Url.startsWith(location.origin)) { a.target = '_blank'; a.rel = 'noopener'; }
                links.appendChild(a);
            });
            wrap.appendChild(links);
        }
        body.appendChild(wrap);
        return wrap;
    }

    function renderGoiY(list) {
        body.querySelectorAll('.lr-chat-chips').forEach(c => c.remove());
        if (!list || !list.length) return;
        const chips = el('div', 'lr-chat-chips');
        list.forEach(g => {
            const b = el('button', 'lr-chat-chip', g);
            b.type = 'button';
            b.addEventListener('click', () => send(g));
            chips.appendChild(b);
        });
        body.appendChild(chips);
    }

    function scroll() { body.scrollTop = body.scrollHeight; }

    async function open() {
        panel.hidden = false;
        toggle.setAttribute('aria-expanded', 'true');
        root.classList.add('is-open');
        input.focus();
        if (maHoiThoai) return;
        try {
            const saved = store.get();
            const hoiThoai = await post(root.dataset.urlConversations, saved ? { maHoiThoai: saved } : {});
            maHoiThoai = hoiThoai.MaHoiThoai;
            store.set(maHoiThoai);
            body.innerHTML = '';
            hoiThoai.TinNhan.forEach(render);
            const cuoi = hoiThoai.TinNhan[hoiThoai.TinNhan.length - 1];
            if (cuoi && cuoi.VaiTro === 'bot') renderGoiY(cuoi.GoiY);
            scroll();
        } catch (e) {
            render({ VaiTro: 'bot', NoiDung: e.message });
        }
    }

    function close() {
        panel.hidden = true;
        toggle.setAttribute('aria-expanded', 'false');
        root.classList.remove('is-open');
        toggle.focus();
    }

    async function send(text) {
        text = (text || '').trim();
        if (!text || dangGui || !maHoiThoai) return;
        dangGui = true;
        input.value = '';
        body.querySelectorAll('.lr-chat-chips').forEach(c => c.remove());
        render({ VaiTro: 'khach', NoiDung: text });
        const typing = el('div', 'lr-chat-msg is-bot lr-chat-typing');
        typing.appendChild(el('div', 'lr-chat-bubble', 'Đang trả lời…'));
        body.appendChild(typing);
        scroll();
        try {
            const m = await post(root.dataset.urlMessages, { maHoiThoai, noiDung: text });
            typing.remove();
            render(m);
            renderGoiY(m.GoiY);
        } catch (e) {
            typing.remove();
            render({ VaiTro: 'bot', NoiDung: e.message });
            if (e.status === 404) { maHoiThoai = null; store.set(null); }
        } finally {
            dangGui = false;
            scroll();
        }
    }

    toggle.addEventListener('click', () => panel.hidden ? open() : close());
    document.getElementById('lrChatClose').addEventListener('click', close);
    panel.addEventListener('keydown', e => { if (e.key === 'Escape') close(); });
    form.addEventListener('submit', e => { e.preventDefault(); send(input.value); });
})();
