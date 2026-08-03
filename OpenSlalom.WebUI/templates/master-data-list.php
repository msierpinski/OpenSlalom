<?php declare(strict_types=1); ?>
<section class="shell listing-page master-data-page">
    <div class="page-heading-row">
        <div><p class="eyebrow"><span></span> Stammdatenverwaltung</p><h1><?= escape($masterTitle) ?></h1></div>
        <a class="button button-primary rounded-action" href="<?= escape(base_url('verwaltung/' . $masterType . '/neu')) ?>">+ <?= escape(rtrim($masterTitle, 'e')) ?> anlegen</a>
    </div>
    <?php $listPath = 'verwaltung/' . $masterType; $showSearch = true; $showPagination = false; require __DIR__ . '/list-controls.php'; ?>
    <?php if ($items === []): ?>
        <div class="empty-state"><strong>Keine <?= escape(strtolower($masterTitle)) ?> vorhanden.</strong><span>Lege den ersten Datensatz über den Button oben rechts an.</span></div>
    <?php else: ?>
        <div class="table-frame master-data-table">
            <table>
                <thead>
                    <?php if ($masterType === 'vereine'): ?><tr><th>Verein</th><th>Mitgliedsnummer</th><th>Ort</th><th>Logo</th><th>Aktionen</th></tr><?php endif; ?>
                    <?php if ($masterType === 'fahrer'): ?><tr><th>Fahrer</th><th>Verein</th><th>Mitgliedsnummer</th><th>Geburtsdatum</th><th>Geschlecht</th><th>Aktionen</th></tr><?php endif; ?>
                    <?php if ($masterType === 'disziplinen'): ?><tr><th>Disziplin</th><th>TF-Strafe</th><th>PF-Strafe</th><th>Aktionen</th></tr><?php endif; ?>
                    <?php if ($masterType === 'karts'): ?><tr><th>Kart</th><th>Verein</th><th>Disziplin</th><th>Motor</th><th>Chassis</th><th>Aktionen</th></tr><?php endif; ?>
                    <?php if ($masterType === 'wetter'): ?><tr><th>Bezeichnung</th><th>Aktionen</th></tr><?php endif; ?>
                </thead>
                <tbody>
                <?php foreach ($items as $item): ?>
                    <tr>
                        <?php if ($masterType === 'vereine'): ?><td data-label="Verein"><strong><?= escape($item['vereinsname']) ?></strong></td><td data-label="Mitgliedsnummer"><?= escape($item['mitglieds_nummer']) ?: '-' ?></td><td data-label="Ort"><?= escape(trim($item['postleitzahl'] . ' ' . $item['ort'])) ?: '-' ?></td><td data-label="Logo"><?= $item['has_logo'] ? 'Vorhanden' : '-' ?></td><?php endif; ?>
                        <?php if ($masterType === 'fahrer'): ?><td data-label="Fahrer"><strong><?= escape(display_name($item['vorname'], $item['nachname'])) ?></strong></td><td data-label="Verein"><?= escape($item['vereinsname']) ?></td><td data-label="Mitgliedsnummer"><?= escape($item['mitglieds_nummer']) ?: '-' ?></td><td data-label="Geburtsdatum"><?= escape(format_date($item['geburtsdatum'])) ?></td><td data-label="Geschlecht"><?= escape(['m' => 'Männlich', 'w' => 'Weiblich', 'd' => 'Divers'][$item['geschlecht']] ?? '-') ?></td><?php endif; ?>
                        <?php if ($masterType === 'disziplinen'): ?><td data-label="Disziplin"><strong><?= escape($item['name']) ?></strong></td><td data-label="TF-Strafe"><?= escape(format_penalty((float) $item['tf'])) ?></td><td data-label="PF-Strafe"><?= escape(format_penalty((float) $item['pf'])) ?></td><?php endif; ?>
                        <?php if ($masterType === 'karts'): ?><td data-label="Kart"><strong><?= escape($item['name'] ?? '-') ?></strong></td><td data-label="Verein"><?= escape($item['vereinsname']) ?></td><td data-label="Disziplin"><?= escape($item['disziplin']) ?></td><td data-label="Motor"><?= escape($item['motor'] ?? '-') ?></td><td data-label="Chassis"><?= escape($item['chassis'] ?? '-') ?></td><?php endif; ?>
                        <?php if ($masterType === 'wetter'): ?><td data-label="Bezeichnung"><strong><?= escape($item['name']) ?></strong></td><?php endif; ?>
                        <td data-label="Aktionen" class="row-actions"><a class="table-action" href="<?= escape(base_url('verwaltung/' . $masterType . '/' . $item['id'] . '/bearbeiten')) ?>">Bearbeiten</a><a class="table-action danger" href="<?= escape(base_url('verwaltung/' . $masterType . '/' . $item['id'] . '/loeschen')) ?>">Löschen</a></td>
                    </tr>
                <?php endforeach; ?>
                </tbody>
            </table>
        </div>
    <?php endif; ?>
    <?php if ($items !== []): ?><?php $listPath = 'verwaltung/' . $masterType; $showSearch = false; $showPagination = true; require __DIR__ . '/list-controls.php'; ?><?php endif; ?>
</section>
