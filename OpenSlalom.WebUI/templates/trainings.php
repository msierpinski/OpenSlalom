<?php declare(strict_types=1); ?>
<section class="shell listing-page">
    <div class="page-heading-row">
        <div><p class="eyebrow"><span></span> Persönliche Übersicht</p><h1>Trainings</h1></div>
        <?php if ($canManageTrainings): ?><a class="button button-primary rounded-action" href="<?= escape(base_url('trainings/neu')) ?>">+ Training anlegen</a><?php endif; ?>
    </div>
    <p class="listing-lead">Veröffentlichte Trainings und Trainings, denen du als Fahrer zugeordnet bist.</p>
    <?php $listPath = 'trainings'; $showSearch = true; $showPagination = false; require __DIR__ . '/list-controls.php'; ?>
    <?php if ($trainings === []): ?>
        <div class="empty-state"><strong>Keine Trainings verfügbar.</strong><span>Es wurden noch keine passenden Trainings veröffentlicht oder zugeordnet.</span></div>
    <?php else: ?>
        <div class="training-listing">
            <?php foreach ($trainings as $training): ?>
                <article class="training-card">
                    <a class="training-card-main" href="<?= escape(base_url('training/' . $training['uuid'])) ?>">
                        <span class="training-card-date"><?= escape(format_date($training['zeitpunkt'])) ?></span>
                        <h2><?= escape($training['name']) ?></h2>
                        <p><?= escape($training['beschreibung']) ?></p>
                        <div><span><?= escape($training['vereinsname']) ?></span><span><?= escape($training['disziplin']) ?></span></div>
                    </a>
                    <?php if ((bool) $training['ist_veroeffentlicht']): ?><b>Veröffentlicht</b><?php endif; ?>
                    <?php if ($canManageTrainings): ?><a class="training-card-edit" href="<?= escape(base_url('training/' . $training['uuid'] . '/bearbeiten')) ?>">Bearbeiten</a><?php endif; ?>
                </article>
            <?php endforeach; ?>
        </div>
    <?php endif; ?>
    <?php if ($trainings !== []): ?><?php $listPath = 'trainings'; $showSearch = false; $showPagination = true; require __DIR__ . '/list-controls.php'; ?><?php endif; ?>
</section>
