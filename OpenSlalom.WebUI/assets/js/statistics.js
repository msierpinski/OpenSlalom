(() => {
    const toggle = (row) => {
        const details = document.getElementById(row.getAttribute('aria-controls'));
        if (!details) return;
        const expanded = row.getAttribute('aria-expanded') === 'true';
        row.setAttribute('aria-expanded', expanded ? 'false' : 'true');
        details.hidden = expanded;
    };

    document.querySelectorAll('[data-kart-summary]').forEach((row) => {
        row.addEventListener('click', () => toggle(row));
        row.addEventListener('keydown', (event) => {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                toggle(row);
            }
        });
    });
})();
