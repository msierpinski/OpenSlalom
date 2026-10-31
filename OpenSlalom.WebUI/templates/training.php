<?php

declare(strict_types=1);

$training = $view['training'];
$summary = $view['summary'];
$leaderboard = $view['leaderboard'];
$statistics = $view['statistics'];
$status = $view['status'];
$trainingScriptVersion = (string) filemtime(dirname(__DIR__) . '/assets/js/training.js');
$trainingBreadcrumbTarget = $currentUser === null ? '' : 'trainings';

$badgeFinishedColor = $training['is_finished'] ? 'success' : 'primary';

?>

<div class="container-fluid py-4" data-auto-refresh="<?= (int) $refreshSeconds ?>">
    <header class="card shadow-sm mb-4">
        <div class="card-body p-4">
            <div class="d-flex flex-column flex-xl-row justify-content-between align-items-start gap-3 mb-4">
                <div class="flex-grow-1">
                    <div class="d-flex flex-wrap align-items-center gap-2 mb-2">
                        <span class="badge text-bg-<?= $badgeFinishedColor ?>">
                            <?= $training['is_finished'] ? 'Training abgeschlossen' : 'Training läuft' ?>
                        </span>
                        <?php if (!$training['is_finished'] && $refreshSeconds > 0): ?>
                            <span class="badge text-bg-success">
                                <span class="spinner-grow spinner-grow-sm me-1" aria-hidden="true"></span>
                                Live
                            </span>
                        <?php endif; ?>
                    </div>
                    <h1 class="h2 fw-bold text-body-emphasis mb-2"><?= escape($training['name']) ?></h1>
                    <?php if ($training['description'] !== ''): ?>
                        <p class="text-body-secondary mb-0"><?= escape($training['description']) ?></p>
                    <?php endif; ?>
                </div>

                <div class="d-flex flex-column flex-sm-row w-100 w-xl-auto align-items-stretch align-items-sm-center gap-2">
                    <?php if ($canManageTrainings): ?>
                        <a class="btn btn-outline-primary flex-grow-1 flex-sm-grow-0" href="<?= escape(base_url('trainings/' . (int) $training['id'] . '/edit')) ?>">
                            <i class="bi bi-pencil me-1" aria-hidden="true"></i>
                            Training bearbeiten
                        </a>
                    <?php endif; ?>
                    <?php if ($refreshSeconds > 0): ?>
                        <button id="refresh-toggle" class="btn btn-outline-secondary flex-grow-1 flex-sm-grow-0" type="button" aria-pressed="true">
                            <span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>
                            <span data-refresh-label>Live-Aktualisierung an</span>
                        </button>
                    <?php endif; ?>
                </div>
            </div>

            <div class="row row-cols-2 row-cols-md-4 g-3">
                <div class="col">
                    <div class="border rounded p-3 h-100">
                        <div class="small text-body-secondary mb-1">Datum</div>
                        <div class="fw-semibold"><?= escape($training['date']) ?></div>
                    </div>
                </div>
                <div class="col">
                    <div class="border rounded p-3 h-100">
                        <div class="small text-body-secondary mb-1">Verein</div>
                        <div class="fw-semibold"><?= escape($training['club']) ?></div>
                    </div>
                </div>
                <div class="col">
                    <div class="border rounded p-3 h-100">
                        <div class="small text-body-secondary mb-1">Disziplin</div>
                        <div class="fw-semibold"><?= escape($training['discipline']) ?></div>
                    </div>
                </div>
                <div class="col">
                    <div class="border rounded p-3 h-100">
                        <div class="small text-body-secondary mb-1">Wetter</div>
                        <div class="fw-semibold"><?= escape($training['weather'] ?: 'Keine Angabe') ?></div>
                    </div>
                </div>
            </div>

            <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mt-3 pt-3 border-top">
                <span class="small text-body-secondary">Stand <strong id="refresh-time" class="text-body"><?= escape((new DateTimeImmutable())->format('H:i:s')) ?></strong></span>
                <?php if ($refreshSeconds > 0): ?>
                    <span class="small text-body-secondary">Automatische Aktualisierung aktiviert</span>
                <?php endif; ?>
            </div>
        </div>
    </header>

    <section>
        <ul class="nav nav-tabs mb-4" role="tablist" aria-label="Trainingsansicht">
            <?php if (!$training['is_finished']): ?>
                <li class="nav-item" role="presentation">
                    <button id="status-tab" class="nav-link active" type="button" role="tab" aria-selected="true" aria-controls="status-panel" data-training-tab="status">
                        <i class="bi bi-activity me-1" aria-hidden="true"></i> Status
                    </button>
                </li>
            <?php endif; ?>
            <li class="nav-item" role="presentation">
                <button id="results-tab" class="nav-link <?= $training['is_finished'] ? 'active' : '' ?>" type="button" role="tab" aria-selected="<?= $training['is_finished'] ? 'true' : 'false' ?>" aria-controls="results-panel" data-training-tab="results">
                    <i class="bi bi-trophy me-1" aria-hidden="true"></i> Ergebnisse
                </button>
            </li>
            <li class="nav-item" role="presentation">
                <button id="statistics-tab" class="nav-link" type="button" role="tab" aria-selected="false" aria-controls="statistics-panel" data-training-tab="statistics">
                    <i class="bi bi-bar-chart-line me-1" aria-hidden="true"></i> Statistik
                </button>
            </li>
        </ul>

        <?php if ($status !== null): ?>
            <section id="status-panel" class="vstack gap-4" role="tabpanel" aria-labelledby="status-tab" data-training-panel="status" <?= $training['is_finished'] ? 'hidden' : '' ?>>
                <div class="d-flex flex-column flex-md-row justify-content-between align-items-md-end gap-2">
                    <div>
                        <div class="small text-body-secondary text-uppercase fw-semibold mb-1">Live-Status</div>
                        <h2 class="h4 mb-0">Fahrerstatus</h2>
                    </div>
                    <p class="small text-body-secondary mb-0">Anzeige des zuletzt gespeicherten Status der Zeitnahme.</p>
                </div>

                <div class="row row-cols-1 row-cols-md-2 g-3">
                    <?php foreach ($status['stations'] as $stationIndex => $station): ?>
                        <?php if ($stationIndex === 0 || $status['has_second_station']): ?>
                            <div class="col">
                                <section class="card h-100 shadow-sm">
                                    <div class="card-header bg-body-tertiary fw-semibold"><?= escape($station['name']) ?></div>
                                    <div class="card-body vstack gap-3">
                                        <div>
                                            <div class="small text-body-secondary mb-1">Fährt gerade</div>
                                            <div class="fs-5 fw-semibold"><?= escape($station['current']['name'] ?? 'Kein Fahrer aktiv') ?></div>
                                        </div>
                                        <div class="border-top pt-3">
                                            <div class="small text-body-secondary mb-1">Als Nächstes</div>
                                            <div class="fw-semibold"><?= escape($station['next']['name'] ?? 'Kein nächster Fahrer') ?></div>
                                        </div>
                                    </div>
                                </section>
                            </div>
                        <?php endif; ?>
                    <?php endforeach; ?>
                </div>

                <div class="row g-4">
                    <div class="col-12 col-xl-6">
                        <section class="card h-100 shadow-sm">
                            <div class="card-header bg-body d-flex justify-content-between align-items-center gap-2">
                                <div>
                                    <div class="small text-body-secondary">Startreihenfolge</div>
                                    <h2 class="h5 mb-0">Fahrerliste</h2>
                                </div>
                                <span class="badge text-bg-secondary"><?= count($status['drivers']) ?></span>
                            </div>
                            <?php if ($status['drivers'] === []): ?>
                                <div class="card-body">
                                    <div class="alert alert-info mb-0">Noch keine Fahrer zugeordnet.</div>
                                </div>
                            <?php else: ?>
                                <ol class="list-group list-group-flush">
                                    <?php foreach ($status['drivers'] as $driver): ?>
                                        <?php
                                        $driverStatusLabel = match ($driver['status']) {
                                            'active-first' => 'FÄHRT ZN 1',
                                            'active-second' => 'FÄHRT ZN 2',
                                            'next-first' => 'NÄCHSTE ZN 1',
                                            'next-second' => 'NÄCHSTE ZN 2',
                                            'inactive' => 'PAUSIERT',
                                            default => $driver['class'],
                                        };
                                        $driverStatusColor = str_starts_with($driver['status'], 'active-') ? 'success' : ($driver['status'] === 'inactive' ? 'secondary' : 'primary');
                                        ?>
                                        <li class="list-group-item d-flex align-items-center gap-3">
                                            <span class="badge text-bg-secondary rounded-pill"><?= (int) $driver['position'] ?></span>
                                            <div class="flex-grow-1 min-w-0">
                                                <div class="fw-semibold"><?= escape($driver['name']) ?></div>
                                                <div class="small text-body-secondary"><?= escape($driver['club']) ?></div>
                                            </div>
                                            <span class="badge text-bg-<?= $driverStatusColor ?> text-wrap"><?= escape($driverStatusLabel) ?></span>
                                        </li>
                                    <?php endforeach; ?>
                                </ol>
                            <?php endif; ?>
                        </section>
                    </div>

                    <div class="col-12 col-xl-6">
                        <section class="card h-100 shadow-sm">
                            <div class="card-header bg-body">
                                <div class="small text-body-secondary">Zuletzt gespeichert</div>
                                <h2 class="h5 mb-0">Letzte zehn Stints</h2>
                            </div>
                            <?php if ($status['recent_stints'] === []): ?>
                                <div class="card-body">
                                    <div class="alert alert-info mb-0">Noch keine Stints gespeichert.</div>
                                </div>
                            <?php else: ?>
                                <div class="list-group list-group-flush">
                                    <?php foreach ($status['recent_stints'] as $stint): ?>
                                        <details class="list-group-item" data-detail-id="status-stint-<?= (int) $stint['id'] ?>">
                                            <summary class="d-flex flex-column flex-lg-row justify-content-between gap-3">
                                                <span>
                                                    <strong class="d-block"><?= escape($stint['driver']) ?></strong>
                                                    <span class="small text-body-secondary">Stint vom <?= escape(format_date($stint['datum'], 'd.m.Y · H:i:s')) ?> · <?= escape($stint['kart']) ?> · <?= escape($stint['class']) ?></span>
                                                </span>
                                                <span class="d-flex flex-wrap gap-3">
                                                    <span><span class="small text-body-secondary d-block">Bestzeit</span><strong class="font-monospace"><?= escape(format_training_time($stint['best_lap'])) ?></strong></span>
                                                    <span><span class="small text-body-secondary d-block">Δ Bestzeit</span><strong class="font-monospace"><?= $stint['best_lap_difference'] === null ? '–' : '+' . escape(format_training_time($stint['best_lap_difference'])) ?></strong></span>
                                                    <span><span class="small text-body-secondary d-block">PF / TF</span><strong><?= (int) $stint['pf'] ?> / <?= (int) $stint['tf'] ?></strong></span>
                                                    <span><span class="small text-body-secondary d-block">Gesamt</span><strong class="font-monospace"><?= escape(format_training_time($stint['total'])) ?></strong></span>
                                                </span>
                                            </summary>
                                            <div class="d-flex flex-wrap gap-3 my-3 small">
                                                <span>Gültige Runden: <strong><?= (int) $stint['valid_laps'] ?></strong></span>
                                                <span>Durchschnitt: <strong class="font-monospace"><?= escape(format_training_time($stint['average'])) ?></strong></span>
                                                <span>Δ Gesamt: <strong class="font-monospace"><?= $stint['total_difference'] === null ? '–' : '+' . escape(format_training_time($stint['total_difference'])) ?></strong></span>
                                            </div>
                                            <div class="table-responsive">
                                                <table class="table table-sm table-striped table-hover align-middle mb-0">
                                                    <thead class="table-light">
                                                        <tr>
                                                            <th scope="col">Runde</th>
                                                            <th scope="col">Zeit</th>
                                                            <th scope="col">Strafe</th>
                                                            <th scope="col">PF</th>
                                                            <th scope="col">TF</th>
                                                            <th scope="col">Status</th>
                                                        </tr>
                                                    </thead>
                                                    <tbody>
                                                        <?php foreach ($stint['laps'] as $lap): ?>
                                                            <tr class="<?= $lap['invalid'] ? 'table-danger' : '' ?>">
                                                                <td><?= (int) $lap['number'] ?></td>
                                                                <td class="font-monospace fw-semibold text-nowrap"><?= escape(format_training_time($lap['time'])) ?></td>
                                                                <td><?= escape(format_penalty($lap['penalty'])) ?></td>
                                                                <td><?= (int) $lap['pf'] ?></td>
                                                                <td><?= (int) $lap['tf'] ?></td>
                                                                <td><span class="badge text-bg-<?= $lap['invalid'] ? 'danger' : 'success' ?>"><?= $lap['invalid'] ? 'Ungültig' : 'Gewertet' ?></span></td>
                                                            </tr>
                                                        <?php endforeach; ?>
                                                    </tbody>
                                                </table>
                                            </div>
                                        </details>
                                    <?php endforeach; ?>
                                </div>
                            <?php endif; ?>
                        </section>
                    </div>
                </div>
            </section>
        <?php endif; ?>

        <div id="results-panel" role="tabpanel" aria-labelledby="results-tab" data-training-panel="results" <?= $training['is_finished'] ? '' : 'hidden' ?>>
            <div class="row row-cols-2 row-cols-md-4 g-3 mb-4" aria-label="Trainingsübersicht">
                <div class="col">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="small text-body-secondary mb-1">Fahrer mit Stint</div>
                            <div class="fs-2 fw-bold"><?= (int) $summary['participants'] ?></div>
                        </div>
                    </div>
                </div>
                <div class="col">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="small text-body-secondary mb-1">Gespeicherte Stints</div>
                            <div class="fs-2 fw-bold"><?= (int) $summary['stints'] ?></div>
                        </div>
                    </div>
                </div>
                <div class="col">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="small text-body-secondary mb-1">Erfasste Runden</div>
                            <div class="fs-2 fw-bold"><?= (int) $summary['laps'] ?></div>
                        </div>
                    </div>
                </div>
                <div class="col">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="small text-body-secondary mb-1">Erster Start</div>
                            <div class="fs-2 fw-bold font-monospace"><?= escape(format_date($summary['started_at'], 'H:i:s')) ?></div>
                        </div>
                    </div>
                </div>
            </div>

            <section class="card shadow-sm mb-4">
                <div class="card-header bg-body d-flex flex-column flex-md-row justify-content-between align-items-md-center gap-2 py-3">
                    <div>
                        <div class="small text-body-secondary">Zwischenstand</div>
                        <h2 class="h5 mb-0">Schnellste Runden</h2>
                    </div>
                    <div class="d-flex flex-wrap gap-3 small"><span>PF <strong><?= escape(format_penalty($view['penalties']['pf'])) ?></strong></span><span>TF <strong><?= escape(format_penalty($view['penalties']['tf'])) ?></strong></span></div>
                </div>
                <?php if ($leaderboard === []): ?>
                    <div class="card-body">
                        <div class="alert alert-info mb-0"><strong class="d-block">Noch keine gewertete Runde.</strong>Die Rangliste erscheint, sobald eine gültige Rundenzeit gespeichert wurde.</div>
                    </div>
                <?php else: ?>
                    <div class="table-responsive">
                        <table class="table table-hover table-striped align-middle mb-0">
                            <thead class="table-light">
                                <tr>
                                    <th scope="col">Pos.</th>
                                    <th scope="col">Fahrer</th>
                                    <th scope="col" class="d-none d-md-table-cell">Klasse</th>
                                    <th scope="col" class="d-none d-md-table-cell">Kart</th>
                                    <th scope="col" class="text-end">Bestzeit</th>
                                    <th scope="col" class="text-end">Abstand</th>
                                    <th scope="col" class="text-end d-none d-md-table-cell">Ø-Zeit</th>
                                    <th scope="col" class="text-end d-none d-md-table-cell">Runden</th>
                                    <th scope="col" class="text-end d-none d-md-table-cell">Zuletzt</th>
                                </tr>
                            </thead>
                            <tbody>
                                <?php foreach ($leaderboard as $row): ?>
                                    <tr>
                                        <td><span class="badge text-bg-<?= (int) $row['position'] === 1 ? 'warning' : 'secondary' ?>"><?= (int) $row['position'] ?></span></td>
                                        <td class="fw-semibold"><?= escape($row['driver']) ?></td>
                                        <td class="d-none d-md-table-cell"><span class="badge text-bg-light border"><?= escape($row['class']) ?></span></td>
                                        <td class="d-none d-md-table-cell"><?= escape($row['kart']) ?></td>
                                        <td class="text-end font-monospace fw-bold text-nowrap"><?= escape(format_training_time($row['best'])) ?></td>
                                        <td class="text-end font-monospace text-nowrap"><?= (int) $row['position'] === 1 ? '–' : '+' . escape(format_training_time($row['difference'])) ?></td>
                                        <td class="text-end font-monospace text-nowrap d-none d-md-table-cell"><?= escape(format_training_time($row['average'])) ?></td>
                                        <td class="text-end d-none d-md-table-cell"><?= (int) $row['laps'] ?></td>
                                        <td class="text-end font-monospace d-none d-md-table-cell"><?= escape(format_date($row['last_drive'], 'H:i:s')) ?></td>
                                    </tr>
                                <?php endforeach; ?>
                            </tbody>
                        </table>
                    </div>
                <?php endif; ?>
            </section>

            <div class="row g-4">
                <div class="col-12 col-xl-5">
                    <section class="card h-100 shadow-sm">
                        <div class="card-header bg-body d-flex justify-content-between align-items-center">
                            <div>
                                <div class="small text-body-secondary">Teilnehmer</div>
                                <h2 class="h5 mb-0">Starterliste</h2>
                            </div><span class="badge text-bg-secondary"><?= (int) $summary['registered'] ?></span>
                        </div>
                        <?php if ($view['starters'] === []): ?>
                            <div class="card-body">
                                <div class="alert alert-info mb-0">Noch keine Fahrer zugeordnet.</div>
                            </div>
                        <?php else: ?>
                            <ol class="list-group list-group-flush list-group-numbered">
                                <?php foreach ($view['starters'] as $starter): ?>
                                    <li class="list-group-item d-flex align-items-center gap-3">
                                        <div class="flex-grow-1">
                                            <div class="fw-semibold"><?= escape($starter['name']) ?></div>
                                            <div class="small text-body-secondary"><?= escape($starter['club']) ?></div>
                                        </div>
                                        <span class="badge text-bg-light border"><?= escape($starter['class']) ?></span>
                                    </li>
                                <?php endforeach; ?>
                            </ol>
                        <?php endif; ?>
                    </section>
                </div>

                <div class="col-12 col-xl-7">
                    <section class="card h-100 shadow-sm">
                        <div class="card-header bg-body">
                            <div class="small text-body-secondary">Verlauf</div>
                            <h2 class="h5 mb-0">Stints und Runden</h2>
                        </div>
                        <?php if ($view['drivers'] === []): ?>
                            <div class="card-body">
                                <div class="alert alert-info mb-0">Noch keine Stints gespeichert.</div>
                            </div>
                        <?php else: ?>
                            <div class="list-group list-group-flush">
                                <?php foreach ($view['drivers'] as $driver): ?>
                                    <details class="list-group-item" data-detail-id="driver-<?= (int) $driver['id'] ?>">
                                        <summary class="d-flex align-items-center gap-3">
                                            <span class="badge text-bg-primary rounded-pill"><?= escape(display_initial($driver['name'])) ?></span>
                                            <span class="flex-grow-1"><strong class="d-block"><?= escape($driver['name']) ?></strong><span class="small text-body-secondary"><?= count($driver['stints']) ?> Stint<?= count($driver['stints']) === 1 ? '' : 's' ?></span></span>
                                        </summary>
                                        <div class="vstack gap-3 mt-3">
                                            <?php foreach ($driver['stints'] as $stint): ?>
                                                <details class="card" data-detail-id="stint-<?= (int) $stint['id'] ?>">
                                                    <summary class="card-header d-flex flex-column flex-sm-row justify-content-between gap-2">
                                                        <span><strong class="d-block">Stint vom <?= escape(format_date($stint['datum'], 'd.m.Y · H:i:s')) ?></strong><span class="small text-body-secondary">Kart <?= escape($stint['kart'] ?? "Unbekanntes Kart") ?> · Klasse <?= escape($stint['class']) ?></span></span>
                                                        <span><span class="small text-body-secondary d-block">Gesamt-Fahrzeit</span><strong class="font-monospace"><?= escape(format_training_time($stint['total'])) ?></strong></span>
                                                    </summary>
                                                    <div class="card-body">
                                                        <div class="d-flex flex-wrap gap-3 small mb-3"><span>Gültige Runden <strong><?= (int) $stint['valid_laps'] ?></strong></span><span>Durchschnitt <strong class="font-monospace"><?= escape(format_training_time($stint['average'])) ?></strong></span></div>
                                                        <div class="table-responsive">
                                                            <table class="table table-sm table-striped align-middle mb-0">
                                                                <thead class="table-light">
                                                                    <tr>
                                                                        <th scope="col">Runde</th>
                                                                        <th scope="col">Zeit</th>
                                                                        <th scope="col">Strafe</th>
                                                                        <th scope="col">PF</th>
                                                                        <th scope="col">TF</th>
                                                                        <th scope="col">Status</th>
                                                                    </tr>
                                                                </thead>
                                                                <tbody>
                                                                    <?php foreach ($stint['laps'] as $lap): ?>
                                                                        <tr class="<?= $lap['invalid'] ? 'table-danger' : '' ?>">
                                                                            <td><?= (int) $lap['number'] ?></td>
                                                                            <td class="font-monospace fw-semibold text-nowrap"><?= escape(format_training_time($lap['time'])) ?></td>
                                                                            <td><?= escape(format_penalty($lap['penalty'])) ?></td>
                                                                            <td><?= (int) $lap['pf'] ?></td>
                                                                            <td><?= (int) $lap['tf'] ?></td>
                                                                            <td><span class="badge text-bg-<?= $lap['invalid'] ? 'danger' : 'success' ?>"><?= $lap['invalid'] ? 'Ungültig' : 'Gewertet' ?></span></td>
                                                                        </tr>
                                                                    <?php endforeach; ?>
                                                                </tbody>
                                                            </table>
                                                        </div>
                                                    </div>
                                                </details>
                                            <?php endforeach; ?>
                                        </div>
                                    </details>
                                <?php endforeach; ?>
                            </div>
                        <?php endif; ?>
                    </section>
                </div>
            </div>
        </div>

        <section id="statistics-panel" class="vstack gap-4" role="tabpanel" aria-labelledby="statistics-tab" data-training-panel="statistics" hidden>
            <div class="d-flex flex-column flex-md-row justify-content-between align-items-md-end gap-2">
                <div>
                    <div class="small text-body-secondary text-uppercase fw-semibold mb-1">Trainingsauswertung</div>
                    <h2 class="h4 mb-0">Statistik</h2>
                </div>
                <p class="small text-body-secondary mb-0">Nur Runden und Stints dieses Trainings. Die Fahrzeit enthält keine Strafsekunden.</p>
            </div>

            <div class="row row-cols-2 row-cols-md-3 row-cols-xl-5 g-3" aria-label="Trainingskennzahlen">
                <div class="col">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="small text-body-secondary">Registrierte Fahrer</div>
                            <div class="fs-3 fw-bold"><?= (int) $summary['registered'] ?></div>
                        </div>
                    </div>
                </div>
                <div class="col">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="small text-body-secondary">Fahrer mit Stint</div>
                            <div class="fs-3 fw-bold"><?= (int) $summary['participants'] ?></div>
                        </div>
                    </div>
                </div>
                <div class="col">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="small text-body-secondary">Stints</div>
                            <div class="fs-3 fw-bold"><?= (int) $summary['stints'] ?></div>
                        </div>
                    </div>
                </div>
                <div class="col">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="small text-body-secondary">Zeitgemessene Runden</div>
                            <div class="fs-3 fw-bold"><?= (int) $statistics['total_rounds'] ?></div>
                        </div>
                    </div>
                </div>
                <div class="col">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="small text-body-secondary">Gesamte Fahrzeit</div>
                            <div class="fs-3 fw-bold font-monospace"><?= escape(format_duration($statistics['total_seconds'])) ?></div>
                        </div>
                    </div>
                </div>
                <div class="col">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="small text-body-secondary">Pylonenfehler (PF)</div>
                            <div class="fs-3 fw-bold"><?= (int) $statistics['total_pf'] ?></div>
                        </div>
                    </div>
                </div>
                <div class="col">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="small text-body-secondary">Torfehler (TF)</div>
                            <div class="fs-3 fw-bold"><?= (int) $statistics['total_tf'] ?></div>
                        </div>
                    </div>
                </div>
                <div class="col">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="small text-body-secondary">PF pro Runde</div>
                            <div class="fs-3 fw-bold"><?= escape(number_format($statistics['average_pf'], 2, '.', '')) ?></div>
                        </div>
                    </div>
                </div>
                <div class="col">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="small text-body-secondary">TF pro Runde</div>
                            <div class="fs-3 fw-bold"><?= escape(number_format($statistics['average_tf'], 2, '.', '')) ?></div>
                        </div>
                    </div>
                </div>
                <div class="col">
                    <div class="card h-100">
                        <div class="card-body">
                            <div class="small text-body-secondary">Fehlerfreie Runden</div>
                            <div class="fs-3 fw-bold"><?= escape(number_format($statistics['error_free_percent'], 2, '.', '')) ?>%</div>
                        </div>
                    </div>
                </div>
            </div>

            <section class="card shadow-sm">
                <div class="card-header bg-body d-flex justify-content-between align-items-center">
                    <div>
                        <div class="small text-body-secondary">Fahrerübersicht</div>
                        <h2 class="h5 mb-0">Fahrerdaten</h2>
                    </div><span class="badge text-bg-secondary"><?= count($statistics['drivers']) ?></span>
                </div>
                <?php if ($statistics['drivers'] === []): ?>
                    <div class="card-body">
                        <div class="alert alert-info mb-0"><strong class="d-block">Noch keine Trainingsdaten.</strong>Die Statistik erscheint, sobald Fahrer einem Training zugeordnet oder Stints gespeichert wurden.</div>
                    </div>
                <?php else: ?>
                    <div class="row g-3 p-3">
                        <?php foreach ($statistics['drivers'] as $driver): ?>
                            <div class="col-12 col-md-6 col-xl-4">
                                <article class="card h-100">
                                    <div class="card-body">
                                        <h3 class="h6 fw-semibold mb-3"><?= escape($driver['name']) ?></h3>
                                        <dl class="row mb-0 small">
                                            <dt class="col-7 fw-normal text-body-secondary">Fahrzeit</dt>
                                            <dd class="col-5 text-end font-monospace text-nowrap"><?= escape(format_duration($driver['raw_seconds'])) ?></dd>
                                            <dt class="col-7 fw-normal text-body-secondary">Runden</dt>
                                            <dd class="col-5 text-end"><?= (int) $driver['rounds'] ?></dd>
                                            <dt class="col-7 fw-normal text-body-secondary">Fehlerfreie Runden</dt>
                                            <dd class="col-5 text-end text-nowrap"><?= (int) $driver['error_free_rounds'] ?> <span class="text-body-secondary">(<?= escape(number_format($driver['error_free_percent'], 2, '.', '')) ?>%)</span></dd>
                                            <dt class="col-7 fw-normal text-body-secondary">Stints</dt>
                                            <dd class="col-5 text-end"><?= (int) $driver['stints'] ?></dd>
                                            <dt class="col-7 fw-normal text-body-secondary">PF</dt>
                                            <dd class="col-5 text-end"><?= (int) $driver['pf'] ?></dd>
                                            <dt class="col-7 fw-normal text-body-secondary">TF</dt>
                                            <dd class="col-5 text-end"><?= (int) $driver['tf'] ?></dd>
                                            <dt class="col-7 fw-normal text-body-secondary">PF / Runde</dt>
                                            <dd class="col-5 text-end"><?= escape(number_format($driver['average_pf'], 2, '.', '')) ?></dd>
                                            <dt class="col-7 fw-normal text-body-secondary">TF / Runde</dt>
                                            <dd class="col-5 text-end"><?= escape(number_format($driver['average_tf'], 2, '.', '')) ?></dd>
                                        </dl>
                                    </div>
                                </article>
                            </div>
                        <?php endforeach; ?>
                    </div>
                <?php endif; ?>
            </section>

            <section class="card shadow-sm">
                <div class="card-header bg-body d-flex justify-content-between align-items-center">
                    <div>
                        <div class="small text-body-secondary">Kart-Auswertung</div>
                        <h2 class="h5 mb-0">Karts</h2>
                    </div><span class="badge text-bg-secondary"><?= count($view['karts']) ?></span>
                </div>
                <?php if ($view['karts'] === []): ?>
                    <div class="card-body">
                        <div class="alert alert-info mb-0"><strong class="d-block">Keine Kartdaten für dieses Training.</strong>Kartdaten erscheinen, sobald gespeicherte Stints einem Kart zugeordnet sind.</div>
                    </div>
                <?php else: ?>
                    <div class="row g-3 p-3">
                        <?php foreach ($view['karts'] as $kartIndex => $kart): ?>
                            <div class="col-12 col-md-6 col-xl-4">
                                <article class="card h-100">
                                    <div class="card-body">
                                        <div data-kart-summary tabindex="0" role="button" aria-expanded="false" aria-controls="training-kart-driver-details-<?= (int) $kartIndex ?>">
                                            <h3 class="h6 fw-semibold mb-3"><?= escape($kart['name']) ?></h3>
                                            <dl class="row mb-0 small">
                                                <dt class="col-7 fw-normal text-body-secondary">Fahrzeit</dt>
                                                <dd class="col-5 text-end font-monospace text-nowrap"><?= escape(format_duration($kart['seconds'])) ?></dd>
                                                <dt class="col-7 fw-normal text-body-secondary">Runden</dt>
                                                <dd class="col-5 text-end"><?= (int) $kart['rounds'] ?></dd>
                                                <dt class="col-7 fw-normal text-body-secondary">Stints</dt>
                                                <dd class="col-5 text-end"><?= (int) $kart['stints'] ?></dd>
                                                <dt class="col-7 fw-normal text-body-secondary">Fahrer</dt>
                                                <dd class="col-5 text-end"><?= count($kart['drivers']) ?></dd>
                                                <dt class="col-7 fw-normal text-body-secondary">PF</dt>
                                                <dd class="col-5 text-end"><?= (int) $kart['pf'] ?></dd>
                                                <dt class="col-7 fw-normal text-body-secondary">TF</dt>
                                                <dd class="col-5 text-end"><?= (int) $kart['tf'] ?></dd>
                                                <dt class="col-7 fw-normal text-body-secondary">PF / Runde</dt>
                                                <dd class="col-5 text-end"><?= escape(number_format($kart['average_pf'], 2, '.', '')) ?></dd>
                                                <dt class="col-7 fw-normal text-body-secondary">TF / Runde</dt>
                                                <dd class="col-5 text-end"><?= escape(number_format($kart['average_tf'], 2, '.', '')) ?></dd>
                                            </dl>
                                        </div>
                                        <div id="training-kart-driver-details-<?= (int) $kartIndex ?>" data-kart-details class="border-top mt-3 pt-3" hidden>
                                            <div class="fw-semibold mb-2">Fahrerbezogene Auswertung</div>
                                            <div class="vstack gap-3">
                                                <?php foreach ($kart['drivers'] as $driver): ?>
                                                    <div class="border rounded p-3">
                                                        <div class="fw-semibold mb-2"><?= escape($driver['name']) ?></div>
                                                        <dl class="row mb-0 small">
                                                            <dt class="col-7 fw-normal text-body-secondary">Fahrzeit</dt>
                                                            <dd class="col-5 text-end font-monospace text-nowrap"><?= escape(format_duration($driver['seconds'])) ?></dd>
                                                            <dt class="col-7 fw-normal text-body-secondary">Runden</dt>
                                                            <dd class="col-5 text-end"><?= (int) $driver['rounds'] ?></dd>
                                                            <dt class="col-7 fw-normal text-body-secondary">Stints</dt>
                                                            <dd class="col-5 text-end"><?= (int) $driver['stints'] ?></dd>
                                                            <dt class="col-7 fw-normal text-body-secondary">PF</dt>
                                                            <dd class="col-5 text-end"><?= (int) $driver['pf'] ?></dd>
                                                            <dt class="col-7 fw-normal text-body-secondary">TF</dt>
                                                            <dd class="col-5 text-end"><?= (int) $driver['tf'] ?></dd>
                                                            <dt class="col-7 fw-normal text-body-secondary">PF / Runde</dt>
                                                            <dd class="col-5 text-end"><?= escape(number_format($driver['average_pf'], 2, '.', '')) ?></dd>
                                                            <dt class="col-7 fw-normal text-body-secondary">TF / Runde</dt>
                                                            <dd class="col-5 text-end"><?= escape(number_format($driver['average_tf'], 2, '.', '')) ?></dd>
                                                        </dl>
                                                    </div>
                                                <?php endforeach; ?>
                                            </div>
                                        </div>
                                    </div>
                                </article>
                            </div>
                        <?php endforeach; ?>
                    </div>
                <?php endif; ?>
            </section>
        </section>
    </section>
</div>

<script src="<?= escape(base_url('assets/js/training.js?v=' . $trainingScriptVersion)) ?>" defer></script>
<script src="<?= escape(base_url('assets/js/statistics.js')) ?>" defer></script>
