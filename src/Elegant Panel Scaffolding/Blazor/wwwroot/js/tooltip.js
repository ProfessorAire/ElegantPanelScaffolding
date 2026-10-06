/**
 * Elegant Panel Scaffolding — Fixed-position tooltip helper
 * Positions a single shared tooltip element at fixed coordinates so it is
 * never clipped by overflow:auto/hidden ancestor containers.
 *
 * Usage: add data-tooltip="<text>" to any element.  Call
 * window.__initTooltips() after Blazor renders new content.
 */
(function () {
    let _tip = null;

    function ensureTip() {
        if (_tip) return _tip;
        _tip = document.createElement('div');
        _tip.id = 'eps-fixed-tooltip';
        _tip.style.cssText = [
            'position:fixed',
            'display:none',
            'z-index:99999',
            'background:var(--color-tooltip-bg)',
            'color:var(--color-tooltip-text)',
            'border:1px solid var(--color-border)',
            'border-radius:4px',
            'padding:.4rem .65rem',
            'font-size:.78rem',
            'max-width:300px',
            'white-space:pre-wrap',
            'box-shadow:var(--shadow-md)',
            'pointer-events:none',
            'line-height:1.4'
        ].join(';');
        document.body.appendChild(_tip);
        return _tip;
    }

    function show(e) {
        const text = e.currentTarget.getAttribute('data-tooltip');
        if (!text) return;
        const tip = ensureTip();
        tip.textContent = text;
        position(e, tip);
        tip.style.display = 'block';
    }

    function move(e) {
        if (_tip && _tip.style.display === 'block') {
            position(e, _tip);
        }
    }

    function hide() {
        if (_tip) _tip.style.display = 'none';
    }

    function position(e, tip) {
        const margin = 12;
        const vw = window.innerWidth;
        const vh = window.innerHeight;
        let x = e.clientX + margin;
        let y = e.clientY + margin;
        // Clamp so tooltip doesn't go off-screen (use rough size estimate before paint).
        if (x + 310 > vw) x = e.clientX - margin - Math.min(tip.offsetWidth || 200, 300);
        if (y + 80 > vh)  y = e.clientY - margin - (tip.offsetHeight || 40);
        tip.style.left = x + 'px';
        tip.style.top  = y + 'px';
    }

    function wire(root) {
        const els = (root || document).querySelectorAll('[data-tooltip]');
        els.forEach(el => {
            if (el.__epsTipBound) return;
            el.__epsTipBound = true;
            el.addEventListener('mouseenter', show);
            el.addEventListener('mousemove',  move);
            el.addEventListener('mouseleave', hide);
            // Suppress native browser tooltip.
            if (el.hasAttribute('title')) el.removeAttribute('title');
        });
    }

    function init() {
        wire(document);
        // Re-wire whenever the DOM changes (Blazor re-renders).
        if (!window.__epsTipObserver) {
            window.__epsTipObserver = new MutationObserver(() => wire(document));
            window.__epsTipObserver.observe(document.body, { childList: true, subtree: true });
        }
    }

    window.__initTooltips = init;
})();
