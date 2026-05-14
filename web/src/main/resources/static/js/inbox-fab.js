// ��������������� ���������� FAB-������� ��� ���� ������� SPA
// ������������� ����� ��� ������� �����

(function () {
  const DEFAULT_API = '/api/inbox';
  const apiUrl = window.INBOX_API_URL || DEFAULT_API;

  const stateMap = new Map();

  function queryAllFabsForTarget(targetSelector) {
    return Array.from(document.querySelectorAll(`.fab-add-inbox[data-target="${targetSelector}"]`));
  }

  function iconEl(btn) {
    return btn.querySelector('.fab-add-inbox-icon') || btn.querySelector('i');
  }

  function setIcon(btn, iconClass) {
    const ico = iconEl(btn);
    if (!ico) return;
    ico.className = 'fas ' + iconClass + ' fab-add-inbox-icon';
  }

  async function defaultCreate(title) {
    const res = await fetch(apiUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title })
    });
    if (!res.ok) throw new Error('Inbox create failed');
    return res;
  }

  function findInputBar(targetSelector) {
    if (!targetSelector) return null;
    try {
      return document.querySelector(targetSelector);
    } catch (e) {
      return null;
    }
  }

  function autoGrowTextarea(el) {
    if (!el || el.tagName !== 'TEXTAREA') return;
    el.style.height = 'auto';
    el.style.height = Math.min(el.scrollHeight, window.innerHeight * 0.5) + 'px';
  }

  function openInput(targetSelector, btn) {
    const st = stateMap.get(targetSelector);
    const bar = findInputBar(targetSelector);
    if (!bar || !st) return;
    bar.classList.remove('hidden');
    queryAllFabsForTarget(targetSelector).forEach(b => setIcon(b, b === btn ? 'fa-check' : 'fa-plus'));
    const input = bar.querySelector('input, textarea');
    if (input) { input.focus(); autoGrowTextarea(input); }
    st.open = true;
    st.activeBtn = btn;
  }

  function closeInput(targetSelector) {
    const st = stateMap.get(targetSelector);
    const bar = findInputBar(targetSelector);
    if (!bar) return;
    bar.classList.add('hidden');
    const input = bar.querySelector('input, textarea');
    if (input) { input.value = ''; input.style.height = ''; }
    queryAllFabsForTarget(targetSelector).forEach(b => setIcon(b, 'fa-plus'));
    if (st) { st.open = false; st.activeBtn = null; }
  }

  window.closeInboxInput = function (targetSelector = '#inboxInputBar') {
    if (stateMap.has(targetSelector)) closeInput(targetSelector);
    else {
      const bar = findInputBar(targetSelector);
      if (bar) bar.classList.add('hidden');
    }
  };

  async function createAndRefresh(title, targetSelector) {
    const st = stateMap.get(targetSelector);
    if (!st || st.creating) return;
    st.creating = true;
    try {
      if (typeof window.inboxCreate === 'function') {
        await window.inboxCreate(title);
      } else {
        await defaultCreate(title);
      }
      if (typeof window.inboxLoad === 'function') {
        try { await window.inboxLoad(); } catch { /* ignore */ }
      }
    } catch (e) {
      console.warn('Inbox create error', e);
    } finally {
      st.creating = false;
    }
  }

  function initFabForTarget(targetSelector) {
    if (!stateMap.has(targetSelector)) {
      stateMap.set(targetSelector, { open: false, creating: false, activeBtn: null });
    }
    const st = stateMap.get(targetSelector);
    const bar = findInputBar(targetSelector);
    if (!bar) return;

    if (bar.__inbox_fab_initialized) return;
    bar.__inbox_fab_initialized = true;

    bar.addEventListener('keydown', (e) => {
      if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        const inputEl = bar.querySelector('input, textarea');
        const val = (inputEl && inputEl.value.trim()) || '';
        if (!val) return;
        if (st.creating) return;
        createAndRefresh(val, targetSelector).then(() => closeInput(targetSelector));
      }
      if (e.key === 'Escape') {
        closeInput(targetSelector);
      }
    });

    bar.addEventListener('input', (e) => {
      if (e.target && e.target.matches('textarea')) autoGrowTextarea(e.target);
    });

    document.addEventListener('click', (e) => {
      const isInsideBar = !!e.target.closest(targetSelector);
      const isInsideFab = !!e.target.closest(`.fab-add-inbox[data-target="${targetSelector}"]`);
      if (!isInsideBar && !isInsideFab && st.open) {
        closeInput(targetSelector);
      }
    });
  }

  function initInboxFabs() {
    const fabs = Array.from(document.querySelectorAll('.fab-add-inbox'));
    const groups = {};
    fabs.forEach(btn => {
      const target = btn.getAttribute('data-target') || '#inboxInputBar';
      btn.setAttribute('data-target', target);
      if (!groups[target]) groups[target] = [];
      groups[target].push(btn);
    });

    Object.keys(groups).forEach(targetSelector => {
      initFabForTarget(targetSelector);
      const btns = groups[targetSelector];
      btns.forEach(btn => {
        setIcon(btn, 'fa-plus');
        if (btn.__inbox_fab_bound) return;
        btn.__inbox_fab_bound = true;
        btn.addEventListener('click', async (e) => {
          e.preventDefault();
          const st = stateMap.get(targetSelector);
          if (!st) return;
          const bar = findInputBar(targetSelector);
          if (!bar) return;
          if (!st.open) {
            openInput(targetSelector, btn);
            if (typeof window.spawnFabRipple === 'function') {
              try { window.spawnFabRipple(btn); } catch { }
            }
          } else {
            if (st.activeBtn === btn) {
              const input = bar.querySelector('input, textarea');
              const val = (input && input.value.trim()) || '';
              if (!val) {
                if (input) { input.focus(); autoGrowTextarea(input); }
                return;
              }
              if (st.creating) return;
              if (typeof window.spawnFabRipple === 'function') {
                try { window.spawnFabRipple(btn); } catch { }
              }
              await createAndRefresh(val, targetSelector);
              closeInput(targetSelector);
            } else {
              queryAllFabsForTarget(targetSelector).forEach(b => setIcon(b, b === btn ? 'fa-check' : 'fa-plus'));
              st.activeBtn = btn;
              const input = bar.querySelector('input, textarea');
              if (input) { input.focus(); autoGrowTextarea(input); }
            }
          }
        });
      });
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initInboxFabs);
  } else {
    initInboxFabs();
  }

  window.initInboxFabs = initInboxFabs;
})();