import { useEffect, useRef, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { chatApi, type ChatLink, type ChatMessage } from '../api/chat';
import { ApiError } from '../api/client';

const KEY = 'qlksan.chat';

// Liên kết của trợ lý trỏ tới trang Web MVC: đổi sang trang tương ứng của SPA để khách ở lại trong ứng dụng
function spaPath(url: string): string | null {
  try {
    const u = new URL(url);
    if (u.pathname === '/Customer/BookRoom') return `/dat-phong${u.search}`;
    const m = u.pathname.match(/^\/Home\/RoomType\/([^/]+)$/);
    if (m) return `/hang-phong/${m[1]}`;
  } catch {
    /* không phải URL tuyệt đối */
  }
  return null;
}

export function ChatWidget() {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [id, setId] = useState<string | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [text, setText] = useState('');
  const [busy, setBusy] = useState(false);
  const bodyRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    bodyRef.current?.scrollTo({ top: bodyRef.current.scrollHeight });
  }, [messages, busy]);

  async function openPanel() {
    setOpen(true);
    setTimeout(() => inputRef.current?.focus(), 0);
    if (id) return;
    const saved = sessionStorage.getItem(KEY);
    try {
      const conv = await (saved ? chatApi.get(saved).catch(() => chatApi.create()) : chatApi.create());
      setId(conv.MaHoiThoai);
      sessionStorage.setItem(KEY, conv.MaHoiThoai);
      setMessages(conv.TinNhan);
    } catch (e) {
      setMessages([botMessage((e as Error).message)]);
    }
  }

  async function send(q: string) {
    q = q.trim();
    if (!q || busy || !id) return;
    setText('');
    setBusy(true);
    setMessages((m) => [...m, { VaiTro: 'khach', NoiDung: q, ThoiGian: new Date().toISOString(), LienKet: [], GoiY: [] }]);
    try {
      const reply = await chatApi.send(id, q);
      setMessages((m) => [...m, reply]);
    } catch (e) {
      if (e instanceof ApiError && e.status === 404) {
        sessionStorage.removeItem(KEY);
        setId(null);
      }
      setMessages((m) => [...m, botMessage(e instanceof ApiError && e.status === 404 ? 'Hội thoại đã kết thúc, vui lòng đóng và mở lại trợ lý.' : (e as Error).message)]);
    } finally {
      setBusy(false);
    }
  }

  function openLink(e: React.MouseEvent, l: ChatLink) {
    const path = spaPath(l.Url);
    if (path) {
      e.preventDefault();
      navigate(path);
    }
  }

  const last = messages[messages.length - 1];
  return (
    <div className={`chat${open ? ' is-open' : ''}`}>
      {!open && (
        <button type="button" className="chat-toggle" onClick={openPanel} aria-label="Mở trợ lý ảo" aria-expanded={false}>💬</button>
      )}
      {open && (
        <section className="chat-panel" role="dialog" aria-label="Trợ lý ảo" onKeyDown={(e) => e.key === 'Escape' && setOpen(false)}>
          <header className="chat-head">
            <div><strong>Trợ lý ảo</strong><small>Tìm phòng · báo giá · chính sách</small></div>
            <button type="button" onClick={() => setOpen(false)} aria-label="Đóng trợ lý ảo">✕</button>
          </header>
          <div className="chat-body" ref={bodyRef} aria-live="polite">
            {messages.map((m, i) => (
              <div key={i} className={`chat-msg ${m.VaiTro === 'khach' ? 'is-guest' : 'is-bot'}`}>
                <div className="chat-bubble">{m.NoiDung}</div>
                {m.LienKet.length > 0 && (
                  <div className="chat-links">
                    {m.LienKet.map((l) => {
                      const noiBo = spaPath(l.Url) !== null;
                      return <a key={l.Url} className="chat-link" href={l.Url} onClick={(e) => openLink(e, l)} {...(noiBo ? {} : { target: '_blank', rel: 'noopener' })}>{l.Nhan}</a>;
                    })}
                  </div>
                )}
              </div>
            ))}
            {busy && <div className="chat-msg is-bot"><div className="chat-bubble chat-typing">Đang trả lời…</div></div>}
            {!busy && last?.VaiTro === 'bot' && last.GoiY.length > 0 && (
              <div className="chat-chips">
                {last.GoiY.map((g) => <button key={g} type="button" className="chat-chip" onClick={() => send(g)}>{g}</button>)}
              </div>
            )}
          </div>
          <form className="chat-form" onSubmit={(e: FormEvent) => { e.preventDefault(); send(text); }}>
            <label htmlFor="chatInput" className="sr-only">Câu hỏi</label>
            <input id="chatInput" ref={inputRef} value={text} maxLength={500} onChange={(e) => setText(e.target.value)} placeholder="Ví dụ: còn phòng 20/11 – 22/11 cho 2 người?" />
            <button type="submit" aria-label="Gửi" disabled={busy}>➤</button>
          </form>
        </section>
      )}
    </div>
  );
}

function botMessage(noiDung: string): ChatMessage {
  return { VaiTro: 'bot', NoiDung: noiDung, ThoiGian: new Date().toISOString(), LienKet: [], GoiY: [] };
}
