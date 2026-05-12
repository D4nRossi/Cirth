// Cirth — frontend glue (HTMX helpers)

// Auto-remove toasts after the CSS animation
document.addEventListener('htmx:afterSwap', (e) => {
    if (e.detail.target.id === 'toast-container') {
        const toasts = e.detail.target.querySelectorAll('.toast');
        toasts.forEach(t => setTimeout(() => t.remove(), 5200));
    }
});

// HTMX response error → toast (without throwing)
document.addEventListener('htmx:responseError', (e) => {
    const msg = e.detail.xhr.status === 0
        ? 'Servidor não respondeu. Verifique sua conexão.'
        : `Erro ${e.detail.xhr.status} no servidor.`;
    pushToast(msg, 'error');
});

document.addEventListener('htmx:sendError', () => {
    pushToast('Falha de rede. Tente novamente.', 'error');
});

// Server can include `HX-Trigger: {"toast":{"message":"...","level":"success"}}`
// in any response — we attach a global listener below.
document.body.addEventListener('toast', (e) => {
    pushToast(e.detail.message ?? '', e.detail.level ?? 'info');
});

function pushToast(message, level) {
    if (!message) return;
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        document.body.appendChild(container);
    }
    const div = document.createElement('div');
    div.className = `toast ${level || ''}`;
    div.textContent = message;
    container.appendChild(div);
    setTimeout(() => div.remove(), 5200);
}

// Scroll chat to bottom on new content
document.addEventListener('htmx:afterSwap', (e) => {
    const chat = e.detail.target.closest('.chat-messages')
              ?? document.querySelector('.chat-messages');
    if (chat) chat.scrollTop = chat.scrollHeight;
});

// On SSE close (e.g. chat stream done), remove streaming animation
document.body.addEventListener('htmx:sseClose', (e) => {
    e.detail.elt?.classList.remove('streaming');
});

// Auto-grow textareas
document.addEventListener('input', (e) => {
    if (e.target.matches('textarea[data-autosize]')) {
        e.target.style.height = 'auto';
        e.target.style.height = e.target.scrollHeight + 'px';
    }
});

// Ctrl+Enter to submit forms
document.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
        const form = e.target.closest('form');
        if (form) {
            e.preventDefault();
            form.requestSubmit();
        }
    }
});

// Tab system (vanilla, controlled by data-tab attributes)
document.addEventListener('click', (e) => {
    const tab = e.target.closest('[data-tab]');
    if (!tab) return;
    const group = tab.closest('[data-tabs]');
    if (!group) return;
    group.querySelectorAll('[data-tab]').forEach(t => t.classList.toggle('active', t === tab));
    const targetId = tab.dataset.tab;
    group.parentElement.querySelectorAll('[data-tab-panel]').forEach(p => {
        p.hidden = p.dataset.tabPanel !== targetId;
    });
    // Fire HTMX request if tab has data-load-url and not already loaded.
    // Note: don't use `data-hx-get` here — HTMX 2.x auto-processes `data-hx-*`
    // attributes and would fire the request a second time, swapping into the
    // wrong target.
    if (tab.dataset.loadUrl && !tab.dataset.loaded) {
        htmx.ajax('GET', tab.dataset.loadUrl, { target: `[data-tab-panel="${targetId}"]`, swap: 'innerHTML' });
        tab.dataset.loaded = '1';
    }
});
