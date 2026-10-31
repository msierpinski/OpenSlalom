<?php

declare(strict_types=1);

?>

<section class="vstack gap-5">

    <header>
        <p class="text-muted mb-0">Verwaltung</p>
        <h1 class="text-body-emphasis display-5 fw-bold mb-0"><?= escape($masterTitle) ?></h1>
    </header>

    <div class="d-flex flex-wrap gap-3 align-items-start justify-content-between">
        <a class="btn btn-primary" href="<?= escape(base_url('verwaltung/' . $masterType . '/neu')) ?>"><i class="bi bi-plus me-1" aria-hidden="true"></i><?= escape($masterTitle) ?> anlegen</a>

        <div class="flex-grow-1">
            <?php $listPath = 'verwaltung/' . $masterType;
            $showSearch = true;
            $showPagination = false;
            require_once __DIR__ . '/list-controls.php'; ?>
        </div>
    </div>

    <?php if ($items === []): ?>
        <div class="alert alert-info"><strong>Keine <?= escape($masterTitle) ?> vorhanden.</strong> Lege den ersten Datensatz über den Button oben rechts an.</div>
    <?php else: ?>
        <div class="row row-cols-1 row-cols-md-2 row-cols-xl-3 g-3">
            <?php foreach ($items as $item): ?>
                <div class="col">
                    <article class="card h-100 shadow-sm">
                        <div class="card-body">
                            <?php if ($masterType === 'vereine'): ?>
                                <h2 class="h5"><?= escape($item['vereinsname']) ?></h2>
                                <dl class="small mb-0"><dt class="text-body-secondary">Mitgliedsnummer</dt><dd><?= escape($item['mitglieds_nummer']) ?: '-' ?></dd><dt class="text-body-secondary">Ort</dt><dd><?= escape(trim($item['postleitzahl'] . ' ' . $item['ort'])) ?: '-' ?></dd><dt class="text-body-secondary">Logo</dt><dd><?= $item['has_logo'] ? 'Vorhanden' : '-' ?></dd></dl>
                            <?php elseif ($masterType === 'fahrer'): ?>
                                <h2 class="h5"><?= escape(display_name($item['vorname'], $item['nachname'])) ?></h2>
                                <dl class="small mb-0"><dt class="text-body-secondary">Verein</dt><dd><?= escape($item['vereinsname']) ?></dd><dt class="text-body-secondary">Mitgliedsnummer</dt><dd><?= escape($item['mitglieds_nummer']) ?: '-' ?></dd><dt class="text-body-secondary">Geburtsdatum</dt><dd><?= escape(format_date($item['geburtsdatum'])) ?></dd><dt class="text-body-secondary">Geschlecht</dt><dd><?= escape(['m' => 'Männlich', 'w' => 'Weiblich', 'd' => 'Divers'][$item['geschlecht']] ?? '-') ?></dd></dl>
                            <?php elseif ($masterType === 'disziplinen'): ?>
                                <h2 class="h5"><?= escape($item['name']) ?></h2>
                                <dl class="small mb-0"><dt class="text-body-secondary">TF-Strafe</dt><dd><?= escape(format_penalty((float) $item['tf'])) ?></dd><dt class="text-body-secondary">PF-Strafe</dt><dd><?= escape(format_penalty((float) $item['pf'])) ?></dd></dl>
                            <?php elseif ($masterType === 'karts'): ?>
                                <h2 class="h5"><?= escape($item['name'] ?? '-') ?></h2>
                                <dl class="small mb-0"><dt class="text-body-secondary">Verein</dt><dd><?= escape($item['vereinsname']) ?></dd><dt class="text-body-secondary">Disziplin</dt><dd><?= escape($item['disziplin']) ?></dd><dt class="text-body-secondary">Motor</dt><dd><?= escape($item['motor'] ?? '-') ?></dd><dt class="text-body-secondary">Chassis</dt><dd><?= escape($item['chassis'] ?? '-') ?></dd></dl>
                            <?php else: ?>
                                <h2 class="h5 mb-0"><?= escape($item['name']) ?></h2>
                            <?php endif; ?>
                        </div>
                        <div class="card-footer bg-transparent d-flex gap-2">
                            <a class="btn btn-primary" href="<?= escape(base_url('verwaltung/' . $masterType . '/' . $item['id'] . '/bearbeiten')) ?>"><i class="bi bi-pencil me-1" aria-hidden="true"></i>Bearbeiten</a>
                            <a class="btn btn-outline-danger" href="<?= escape(base_url('verwaltung/' . $masterType . '/' . $item['id'] . '/loeschen')) ?>"><i class="bi bi-trash me-1" aria-hidden="true"></i>Löschen</a>
                        </div>
                    </article>
                </div>
            <?php endforeach; ?>
        </div>
    <?php endif; ?>
    <?php if ($items !== []): ?>
        <?php $listPath = 'verwaltung/' . $masterType;
        $showSearch = false;
        $showPagination = true;
        require_once __DIR__ . '/list-controls.php'; ?>
    <?php endif; ?>
</section>
