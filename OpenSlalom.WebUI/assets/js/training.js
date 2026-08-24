(() => {
    const root = document.querySelector('[data-auto-refresh]');
    const toggle = document.querySelector('#refresh-toggle');
    const tabs = [...document.querySelectorAll('[data-training-tab]')];
    const panels = [...document.querySelectorAll('[data-training-panel]')];
    const tabStorageKey = `openslalom-tab:${window.location.pathname}`;

    const selectTab = (name) => {
        const selectedTab = tabs.find((tab) => tab.dataset.trainingTab === name) || tabs[0];
        if (!selectedTab) return;

        tabs.forEach((tab) => {
            const selected = tab === selectedTab;
            tab.setAttribute('aria-selected', selected ? 'true' : 'false');
            tab.tabIndex = selected ? 0 : -1;
        });
        panels.forEach((panel) => {
            panel.hidden = panel.dataset.trainingPanel !== selectedTab.dataset.trainingTab;
        });
        window.sessionStorage.setItem(tabStorageKey, selectedTab.dataset.trainingTab);
    };

    tabs.forEach((tab) => {
        tab.addEventListener('click', () => selectTab(tab.dataset.trainingTab));
    });
    if (tabs.length > 0) {
        selectTab(window.sessionStorage.getItem(tabStorageKey) || tabs[0].dataset.trainingTab);
    }

    if (!root || !toggle) return;

    const seconds = Number.parseInt(root.dataset.autoRefresh || '0', 10);
    if (!Number.isFinite(seconds) || seconds <= 0) return;

    const storageKey = `openslalom-refresh:${window.location.pathname}`;
    const detailKey = `openslalom-details:${window.location.pathname}`;
    let enabled = window.localStorage.getItem(storageKey) !== 'off';
    let timerId = null;

    const saveOpenDetails = () => {
        const openIds = [...document.querySelectorAll('details[open][data-detail-id]')]
            .map((detail) => detail.dataset.detailId);
        window.sessionStorage.setItem(detailKey, JSON.stringify(openIds));
    };

    const restoreOpenDetails = () => {
        try {
            const ids = JSON.parse(window.sessionStorage.getItem(detailKey) || '[]');
            ids.forEach((id) => {
                const detail = document.querySelector(`[data-detail-id="${CSS.escape(id)}"]`);
                if (detail) detail.open = true;
            });
        } catch (_) {
            window.sessionStorage.removeItem(detailKey);
        }
    };

    const updateToggle = () => {
        toggle.setAttribute('aria-pressed', enabled ? 'true' : 'false');
        toggle.querySelector('span').textContent = enabled
            ? `Live-Aktualisierung an · ${seconds}s`
            : 'Live-Aktualisierung aus';
        toggle.classList.toggle('is-paused', !enabled);
    };

    const schedule = () => {
        window.clearTimeout(timerId);
        if (!enabled) return;
        timerId = window.setTimeout(() => {
            saveOpenDetails();
            window.location.reload();
        }, seconds * 1000);
    };

    toggle.addEventListener('click', () => {
        enabled = !enabled;
        window.localStorage.setItem(storageKey, enabled ? 'on' : 'off');
        updateToggle();
        schedule();
    });

    document.addEventListener('toggle', saveOpenDetails, true);
    restoreOpenDetails();
    updateToggle();
    schedule();
})();
