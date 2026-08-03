(() => {
    const list = document.querySelector('#age-class-list');
    const add = document.querySelector('#add-age-class');
    if (!list || !add) return;
    const addRow = () => {
        const row = document.createElement('div');
        row.className = 'age-class-row';
        row.innerHTML = '<input name="age_label[]" placeholder="Bezeichnung"><input name="age_from[]" type="number" min="0" placeholder="Von"><input name="age_to[]" type="number" min="0" placeholder="Bis (offen)"><button type="button" class="remove-age-class">×</button>';
        list.appendChild(row);
    };
    add.addEventListener('click', addRow);
    list.addEventListener('click', (event) => {
        if (event.target instanceof Element && event.target.classList.contains('remove-age-class')) event.target.closest('.age-class-row')?.remove();
    });
})();
