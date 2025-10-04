// IMPORTANT: small helper for unique ids (UI only).
function getUniqIdValue(prefix = 'uid') {
    // nota bene: not cryptographically secure; sufficient for DOM ids
    return `${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
}

(function () {
    const selectAll = document.getElementById('selectAll');
    const rowChks = () => Array.from(document.querySelectorAll('.row-chk'));
    const btns = [
        document.getElementById('btn-block'),
        document.getElementById('btn-unblock'),
        document.getElementById('btn-delete')
    ];

    function refreshToolbar() {
        const any = rowChks().some(ch => ch.checked);
        btns.forEach(b => b.disabled = !any);
    }

    if (selectAll) {
        selectAll.addEventListener('change', function () {
            rowChks().forEach(ch => ch.checked = selectAll.checked);
            refreshToolbar();
        });
    }

    document.addEventListener('change', function (e) {
        if (e.target.classList.contains('row-chk')) {
            refreshToolbar();
            const all = rowChks();
            if (!e.target.checked) selectAll.checked = false;
            else selectAll.checked = all.every(ch => ch.checked);
        }
    });

    // enable Bootstrap tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(el => new bootstrap.Tooltip(el));

    refreshToolbar();
})();
