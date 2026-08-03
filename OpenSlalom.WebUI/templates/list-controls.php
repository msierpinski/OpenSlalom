<?php declare(strict_types=1); ?>
<?php $showSearch ??= true; $showPagination ??= true; ?>
<?php if ($showSearch): ?>
    <div class="list-controls">
        <form class="list-search" action="<?= escape(base_url($listPath)) ?>" method="get">
            <label><span class="visually-hidden">Liste durchsuchen</span><input name="q" value="<?= escape($search) ?>" maxlength="100" placeholder="Suchen ..."></label>
            <button class="button button-primary rounded-action" type="submit">Suchen</button>
            <?php if ($search !== ''): ?><a class="button button-secondary rounded-action" href="<?= escape(base_url($listPath)) ?>">Zurücksetzen</a><?php endif; ?>
        </form>
        <span class="list-count"><?= (int) $pagination['total'] ?> Eintrag<?= (int) $pagination['total'] === 1 ? '' : 'e' ?></span>
    </div>
<?php endif; ?>
<?php if ($showPagination && $pagination['pages'] > 1): ?>
    <nav class="pagination" aria-label="Seitennavigation">
        <?php if ($pagination['page'] > 1): ?><a href="<?= escape(list_page_url($listPath, $pagination['page'] - 1, $search)) ?>">← Zurück</a><?php endif; ?>
        <?php for ($page = max(1, $pagination['page'] - 2); $page <= min($pagination['pages'], $pagination['page'] + 2); $page++): ?>
            <a class="<?= $page === $pagination['page'] ? 'active' : '' ?>" href="<?= escape(list_page_url($listPath, $page, $search)) ?>"><?= $page ?></a>
        <?php endfor; ?>
        <?php if ($pagination['page'] < $pagination['pages']): ?><a href="<?= escape(list_page_url($listPath, $pagination['page'] + 1, $search)) ?>">Weiter →</a><?php endif; ?>
    </nav>
<?php endif; ?>
