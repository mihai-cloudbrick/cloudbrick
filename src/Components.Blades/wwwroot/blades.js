(function () {
  function init() {
    const host = document.getElementById('cbb-host');
    if (!host) return;
    let dragging = null;

    host.addEventListener('mousedown', function (e) {
      const s = e.target.closest('.cbb-splitter');
      if (!s) return;
      e.preventDefault();
      dragging = { splitter: s };
      document.body.style.userSelect = 'none';
    });

    window.addEventListener('mousemove', function (e) {
      if (!dragging) return;
      const s = dragging.splitter;
      const idx = parseInt(s.dataset.index);
      const items = host.querySelectorAll('.cbb-host-item');
      if (idx >= items.length) return;
      const left = items[idx];
      const blade = left.querySelector('.cbb-blade');
      const rect = host.getBoundingClientRect();
      const min = 280, max = 1200;
      let newW = e.clientX - left.getBoundingClientRect().left;
      newW = Math.max(min, Math.min(max, newW));
      blade.style.width = newW + 'px';
    });

    window.addEventListener('mouseup', function () {
      if (!dragging) return;
      dragging = null;
      document.body.style.userSelect = '';
    });
  }

  if (document.readyState === 'loading')
    document.addEventListener('DOMContentLoaded', init);
  else
    init();
})();

// Persist & restore widths per blade key
(function () {
  function restore() {
    const host = document.getElementById('cbb-host');
    if (!host) return;
    for (const item of host.querySelectorAll('.cbb-host-item')) {
      const key = item.getAttribute('data-key') || '';
      const w = localStorage.getItem('cbb:width:' + key);
      if (!w) continue;
      const blade = item.querySelector('.cbb-blade');
      if (blade) blade.style.width = w + 'px';
    }
  }

  function persist() {
    const host = document.getElementById('cbb-host');
    if (!host) return;
    for (const item of host.querySelectorAll('.cbb-host-item')) {
      const key = item.getAttribute('data-key') || '';
      const blade = item.querySelector('.cbb-blade');
      if (!blade) continue;
      const w = parseInt(getComputedStyle(blade).width);
      if (!isNaN(w)) localStorage.setItem('cbb:width:' + key, String(w));
    }
  }

  window.addEventListener('DOMContentLoaded', restore);
  window.addEventListener('mouseup', persist);
})();
