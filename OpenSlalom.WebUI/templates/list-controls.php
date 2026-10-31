<?php

declare(strict_types=1);

$showSearch ??= true;
$showPagination ??= true;

?>

<?php if ($showSearch): ?>
    <form class="row g-2" action="<?= escape(base_url($listPath)) ?>" method="get">
        <label for="search-input" class="visually-hidden">
            Liste durchsuchen
        </label>
        <div class="col-12 col-sm">
            <input class="form-control" id="search-input" name="q" value="<?= escape($search) ?>" maxlength="100" placeholder="Suchen ...">
        </div>
        <div class="col-12 col-sm-auto">
            <button class="btn btn-primary w-100" type="submit">
            <i class="bi bi-search"></i> Suchen
            </button>
        </div>

        <?php if ($search !== ''): ?><div class="col-12 col-sm-auto"><a class="btn btn-secondary w-100" href="<?= escape(base_url($listPath)) ?>">Zurücksetzen</a></div><?php endif; ?>
    </form>
<?php endif; ?>

<?php if ($showPagination && $pagination['pages'] > 1): ?>
    <nav aria-label="Seitennavigation">
        <ul class="pagination flex-wrap mb-0">
            <?php if ($pagination['page'] > 1): ?><li class="page-item"><a class="page-link" href="<?= escape(list_page_url($listPath, $pagination['page'] - 1, $search)) ?>">← Zurück</a></li><?php endif; ?>
            <?php for ($page = max(1, $pagination['page'] - 2); $page <= min($pagination['pages'], $pagination['page'] + 2); $page++): ?>
                <li class="page-item <?= $page === $pagination['page'] ? 'active' : '' ?>"><a class="page-link" href="<?= escape(list_page_url($listPath, $page, $search)) ?>"><?= $page ?></a></li>
            <?php endfor; ?>
            <?php if ($pagination['page'] < $pagination['pages']): ?><li class="page-item"><a class="page-link" href="<?= escape(list_page_url($listPath, $pagination['page'] + 1, $search)) ?>">Weiter →</a></li><?php endif; ?>
        </ul>
    </nav>
<?php endif; ?>
