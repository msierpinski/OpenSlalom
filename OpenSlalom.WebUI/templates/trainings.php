<?php declare(strict_types=1); ?>
<section class="vstack gap-4">
    <div class="d-flex flex-wrap gap-3 justify-content-between align-items-start">
        <div><p class="text-muted mb-0">Persönliche Übersicht</p><h1 class="text-body-emphasis display-5 fw-bold">Trainings</h1></div>
        <?php if ($canManageTrainings): ?><a class="btn btn-primary" href="<?= escape(base_url('trainings/neu')) ?>">+ Training anlegen</a><?php endif; ?>
    </div>
    <p class="lead">Veröffentlichte Trainings und Trainings, denen du als Fahrer zugeordnet bist.</p>
    <?php $listPath = 'trainings'; $showSearch = true; $showPagination = false; require __DIR__ . '/list-controls.php'; ?>
    <?php if ($trainings === []): ?>
        <div class="alert alert-info"><strong>Keine Trainings verfügbar.</strong> Es wurden noch keine passenden Trainings veröffentlicht oder zugeordnet.</div>
    <?php else: ?>
        <div class="row row-cols-1 row-cols-lg-2 g-3">
            <?php foreach ($trainings as $training): ?>
                <article class="col">
                    <div class="card card-body border-0 shadow-sm h-100">
                    <a class="text-decoration-none" href="<?= escape(base_url('training/' . $training['uuid'])) ?>">
                        <span class="text-muted"><?= escape(format_date($training['zeitpunkt'])) ?></span>
                        <h2 class="h4"><?= escape($training['name']) ?></h2>
                        <p class="text-body"><?= escape($training['beschreibung']) ?></p>
                        <div class="d-flex gap-3 text-muted"><span><?= escape($training['vereinsname']) ?></span><span><?= escape($training['disziplin']) ?></span></div>
                    </a>
                    <?php if ((bool) $training['ist_veroeffentlicht']): ?><span class="badge text-bg-success align-self-start mt-3">Veröffentlicht</span><?php endif; ?>
                    <?php if ($canManageTrainings): ?><a class="btn btn-sm btn-outline-secondary align-self-start mt-3" href="<?= escape(base_url('training/' . $training['uuid'] . '/bearbeiten')) ?>">Bearbeiten</a><?php endif; ?>
                    </div>
                </article>
            <?php endforeach; ?>
        </div>
    <?php endif; ?>
    <?php if ($trainings !== []): ?><?php $listPath = 'trainings'; $showSearch = false; $showPagination = true; require __DIR__ . '/list-controls.php'; ?><?php endif; ?>
</section>
