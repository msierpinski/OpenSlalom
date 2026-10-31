(() => {
    const list = document.querySelector('#age-class-list');
    const add = document.querySelector('#add-age-class');
    if (!list || !add) return;
    const addRow = () => {
        const row = document.createElement('div');
        row.className = 'age-class-row row g-2 align-items-center';
        row.innerHTML = '<div class="col-md"><input class="form-control" name="age_label[]" placeholder="Bezeichnung"></div><div class="col-6 col-md-2"><input class="form-control" name="age_from[]" type="number" min="0" placeholder="Von"></div><div class="col-6 col-md-2"><input class="form-control" name="age_to[]" type="number" min="0" placeholder="Bis"></div><div class="col-auto"><button type="button" class="remove-age-class btn btn-outline-danger" aria-label="Altersklasse entfernen"><i class="bi bi-trash" aria-hidden="true"></i></button></div>';
        list.appendChild(row);
    };
    add.addEventListener('click', addRow);
    list.addEventListener('click', (event) => {
        if (event.target instanceof Element && event.target.classList.contains('remove-age-class')) event.target.closest('.age-class-row')?.remove();
    });
})();
