<?php
declare(strict_types=1);

$training = $view['training'];
$summary = $view['summary'];
$leaderboard = $view['leaderboard'];
$statistics = $view['statistics'];
$trainingScriptVersion = (string) filemtime(dirname(__DIR__) . '/assets/js/training.js');
$trainingBreadcrumbTarget = $currentUser === null ? '' : 'trainings';
?>
<div class="training-live" data-auto-refresh="<?= (int) $refreshSeconds ?>">
    <section class="training-hero">
        <div class="shell">
            <div class="training-crumb"><a href="<?= escape(base_url($trainingBreadcrumbTarget)) ?>">openSlalom</a><span>/</span>Trainingsergebnisse</div>
            <div class="training-title-row">
                <div>
                    <div class="live-state <?= $training['is_finished'] ? 'is-finished' : '' ?>">
                        <span></span><?= $training['is_finished'] ? 'Training abgeschlossen' : 'Training läuft' ?>
                    </div>
                    <h1><?= escape($training['name']) ?></h1>
                    <p><?= escape($training['description']) ?></p>
                </div>
                <div class="refresh-panel">
                    <span>Stand <strong id="refresh-time"><?= escape((new DateTimeImmutable())->format('H:i:s')) ?></strong></span>
                    <?php if ($canManageTrainings): ?><a class="training-edit-link" href="<?= escape(base_url('training/' . $training['uuid'] . '/bearbeiten')) ?>">Training bearbeiten</a><?php endif; ?>
                    <?php if ($refreshSeconds > 0): ?>
                        <button id="refresh-toggle" type="button" aria-pressed="true">
                            <i></i><span>Live-Aktualisierung an</span>
                        </button>
                    <?php endif; ?>
                </div>
            </div>

            <div class="training-meta">
                <div><span>DATUM</span><strong><?= escape($training['date']) ?></strong></div>
                <div><span>VEREIN</span><strong><?= escape($training['club']) ?></strong></div>
                <div><span>DISZIPLIN</span><strong><?= escape($training['discipline']) ?></strong></div>
                <div><span>WETTER</span><strong><?= escape($training['weather']) ?></strong></div>
            </div>
        </div>
    </section>

    <section class="shell result-content">
        <div class="training-tabs" role="tablist" aria-label="Trainingsansicht">
            <button id="results-tab" type="button" role="tab" aria-selected="true" aria-controls="results-panel" data-training-tab="results">Ergebnisse</button>
            <button id="statistics-tab" type="button" role="tab" aria-selected="false" aria-controls="statistics-panel" data-training-tab="statistics">Statistik</button>
        </div>

        <div id="results-panel" role="tabpanel" aria-labelledby="results-tab" data-training-panel="results">
        <div class="summary-rail" aria-label="Trainingsübersicht">
            <div><strong><?= (int) $summary['participants'] ?></strong><span>Fahrer mit Stint</span></div>
            <div><strong><?= (int) $summary['stints'] ?></strong><span>Gespeicherte Stints</span></div>
            <div><strong><?= (int) $summary['laps'] ?></strong><span>Erfasste Runden</span></div>
            <div><strong><?= escape(format_date($summary['started_at'], 'H:i:s')) ?></strong><span>Erster Start</span></div>
        </div>

        <section class="result-section leaderboard-section">
            <div class="result-heading">
                <div>
                    <p class="eyebrow"><span></span> Zwischenstand</p>
                    <h2>Schnellste Runden</h2>
                </div>
                <div class="penalty-legend">
                    <span>PF <b><?= escape(format_penalty($view['penalties']['pf'])) ?></b></span>
                    <span>TF <b><?= escape(format_penalty($view['penalties']['tf'])) ?></b></span>
                </div>
            </div>

            <?php if ($leaderboard === []): ?>
                <div class="empty-state"><strong>Noch keine gewertete Runde.</strong><span>Die Rangliste erscheint, sobald eine gültige Rundenzeit gespeichert wurde.</span></div>
            <?php else: ?>
                <div class="table-frame leaderboard-table">
                    <table>
                        <thead><tr><th>Pos.</th><th>Fahrer</th><th>Klasse</th><th>Kart</th><th>Bestzeit</th><th>Abstand</th><th>Ø-Zeit</th><th>Runden</th><th>Zuletzt</th></tr></thead>
                        <tbody>
                        <?php foreach ($leaderboard as $row): ?>
                            <tr class="position-<?= (int) $row['position'] ?>">
                                <td data-label="Position"><span class="rank-number"><?= (int) $row['position'] ?></span></td>
                                <td data-label="Fahrer"><strong><?= escape($row['driver']) ?></strong></td>
                                <td data-label="Klasse"><?= escape($row['class']) ?></td>
                                <td data-label="Kart"><?= escape($row['kart']) ?></td>
                                <td data-label="Bestzeit"><strong class="time-value"><?= escape(format_training_time($row['best'])) ?></strong></td>
                                <td data-label="Abstand"><?= (int) $row['position'] === 1 ? '-' : '+' . escape(format_training_time($row['difference'])) ?></td>
                                <td data-label="Durchschnitt"><?= escape(format_training_time($row['average'])) ?></td>
                                <td data-label="Runden"><?= (int) $row['laps'] ?></td>
                                <td data-label="Zuletzt"><?= escape(format_date($row['last_drive'], 'H:i:s')) ?></td>
                            </tr>
                        <?php endforeach; ?>
                        </tbody>
                    </table>
                </div>
            <?php endif; ?>
        </section>

        <div class="result-columns">
            <section class="result-section starters-section">
                <div class="result-heading compact-heading">
                    <div><p class="eyebrow"><span></span> Teilnehmer</p><h2>Starterliste</h2></div>
                    <span class="count-badge"><?= (int) $summary['registered'] ?></span>
                </div>
                <?php if ($view['starters'] === []): ?>
                    <div class="empty-state small"><span>Noch keine Fahrer zugeordnet.</span></div>
                <?php else: ?>
                    <ol class="starter-list">
                        <?php foreach ($view['starters'] as $starter): ?>
                            <li><span><?= (int) $starter['position'] ?></span><div><strong><?= escape($starter['name']) ?></strong><small><?= escape($starter['club']) ?></small></div><b><?= escape($starter['class']) ?></b></li>
                        <?php endforeach; ?>
                    </ol>
                <?php endif; ?>
            </section>

            <section class="result-section stint-section">
                <div class="result-heading compact-heading">
                    <div><p class="eyebrow"><span></span> Verlauf</p><h2>Stints und Runden</h2></div>
                </div>
                <?php if ($view['drivers'] === []): ?>
                    <div class="empty-state small"><span>Noch keine Stints gespeichert.</span></div>
                <?php else: ?>
                    <div class="driver-results">
                        <?php foreach ($view['drivers'] as $driver): ?>
                            <details class="driver-detail" data-detail-id="driver-<?= (int) $driver['id'] ?>">
                                <summary>
                                    <span class="driver-initial"><?= escape(display_initial($driver['name'])) ?></span>
                                    <span><strong><?= escape($driver['name']) ?></strong><small><?= count($driver['stints']) ?> Stint<?= count($driver['stints']) === 1 ? '' : 's' ?></small></span>
                                    <i></i>
                                </summary>
                                <div class="driver-stints">
                                    <?php foreach ($driver['stints'] as $stint): ?>
                                        <details class="stint-detail" data-detail-id="stint-<?= (int) $stint['id'] ?>">
                                            <summary>
                                                <span><strong>Stint vom <?= escape(format_date($stint['datum'], 'd.m.Y · H:i:s')) ?></strong><small><?= escape($stint['kart']) ?> · <?= escape($stint['class']) ?></small></span>
                                                <span class="stint-total"><small>Gesamt</small><strong><?= escape(format_training_time($stint['total'])) ?></strong></span>
                                            </summary>
                                            <div class="stint-metrics">
                                                <span>Gültige Runden <strong><?= (int) $stint['valid_laps'] ?></strong></span>
                                                <span>Durchschnitt <strong><?= escape(format_training_time($stint['average'])) ?></strong></span>
                                            </div>
                                            <div class="table-frame lap-table">
                                                <table>
                                                    <thead><tr><th>Runde</th><th>Zeit</th><th>Strafe</th><th>PF</th><th>TF</th><th>Status</th></tr></thead>
                                                    <tbody>
                                                    <?php foreach ($stint['laps'] as $lap): ?>
                                                        <tr class="<?= $lap['invalid'] ? 'invalid-lap' : '' ?>">
                                                            <td data-label="Runde"><?= (int) $lap['number'] ?></td>
                                                            <td data-label="Zeit"><strong><?= escape(format_training_time($lap['time'])) ?></strong></td>
                                                            <td data-label="Strafe"><?= escape(format_penalty($lap['penalty'])) ?></td>
                                                            <td data-label="PF"><?= (int) $lap['pf'] ?></td>
                                                            <td data-label="TF"><?= (int) $lap['tf'] ?></td>
                                                            <td data-label="Status"><span class="lap-state <?= $lap['invalid'] ? 'invalid' : '' ?>"><?= $lap['invalid'] ? 'Ungültig' : 'Gewertet' ?></span></td>
                                                        </tr>
                                                    <?php endforeach; ?>
                                                    </tbody>
                                                </table>
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

        <section id="statistics-panel" class="statistics-tab-panel" role="tabpanel" aria-labelledby="statistics-tab" data-training-panel="statistics" hidden>
            <div class="result-heading statistics-heading">
                <div>
                    <p class="eyebrow"><span></span> Trainingsauswertung</p>
                    <h2>Statistik</h2>
                </div>
                <p>Nur Runden und Stints dieses Trainings. Die Fahrzeit enthält keine Strafsekunden.</p>
            </div>

            <div class="statistics-cards" aria-label="Trainingskennzahlen">
                <div><strong><?= (int) $summary['registered'] ?></strong><span>Registrierte Fahrer</span></div>
                <div><strong><?= (int) $summary['participants'] ?></strong><span>Fahrer mit Stint</span></div>
                <div><strong><?= (int) $summary['stints'] ?></strong><span>Stints</span></div>
                <div><strong><?= (int) $statistics['total_rounds'] ?></strong><span>Zeitgemessene Runden</span></div>
                <div><strong><?= escape(format_duration($statistics['total_seconds'])) ?></strong><span>Gesamte Fahrzeit</span></div>
                <div><strong><?= (int) $statistics['total_pf'] ?></strong><span>Pylonenfehler</span></div>
                <div><strong><?= (int) $statistics['total_tf'] ?></strong><span>Torfehler</span></div>
                <div><strong><?= escape(number_format($statistics['average_pf'], 2, '.', '')) ?></strong><span>PF pro Runde</span></div>
                <div><strong><?= escape(number_format($statistics['average_tf'], 2, '.', '')) ?></strong><span>TF pro Runde</span></div>
                <div><strong><?= escape(number_format($statistics['error_free_percent'], 2, '.', '')) ?>%</strong><span>Fehlerfreie Runden</span></div>
            </div>

            <section class="result-section driver-statistics-section">
                <div class="result-heading compact-heading">
                    <div><p class="eyebrow"><span></span> Fahrerübersicht</p><h2>Fahrerdaten</h2></div>
                    <span class="count-badge"><?= count($statistics['drivers']) ?></span>
                </div>
                <?php if ($statistics['drivers'] === []): ?>
                    <div class="empty-state"><strong>Noch keine Trainingsdaten.</strong><span>Die Statistik erscheint, sobald Fahrer einem Training zugeordnet oder Stints gespeichert wurden.</span></div>
                <?php else: ?>
                    <div class="table-frame driver-statistics-table">
                        <table>
                            <thead><tr><th>Fahrer</th><th>Fahrzeit</th><th>Runden</th><th>Fehlerfreie Runden</th><th>Stints</th><th>PF</th><th>TF</th><th>PF / Runde</th><th>TF / Runde</th></tr></thead>
                            <tbody>
                            <?php foreach ($statistics['drivers'] as $driver): ?>
                                <tr>
                                    <td data-label="Fahrer"><strong><?= escape($driver['name']) ?></strong></td>
                                    <td data-label="Fahrzeit"><strong class="time-value duration-value"><?= escape(format_duration($driver['raw_seconds'])) ?></strong></td>
                                    <td data-label="Runden"><?= (int) $driver['rounds'] ?></td>
                                    <td data-label="Fehlerfreie Runden"><?= (int) $driver['error_free_rounds'] ?> <span class="percentage">(<?= escape(number_format($driver['error_free_percent'], 2, '.', '')) ?>%)</span></td>
                                    <td data-label="Stints"><?= (int) $driver['stints'] ?></td>
                                    <td data-label="PF"><?= (int) $driver['pf'] ?></td>
                                    <td data-label="TF"><?= (int) $driver['tf'] ?></td>
                                    <td data-label="PF / Runde"><?= escape(number_format($driver['average_pf'], 2, '.', '')) ?></td>
                                    <td data-label="TF / Runde"><?= escape(number_format($driver['average_tf'], 2, '.', '')) ?></td>
                                </tr>
                            <?php endforeach; ?>
                            </tbody>
                        </table>
                    </div>
                <?php endif; ?>
            </section>

            <section class="result-section training-kart-statistics">
                <div class="result-heading compact-heading"><div><p class="eyebrow"><span></span> Karts</p><h2>Kart-Auswertung</h2></div><span class="count-badge"><?= count($view['karts']) ?></span></div>
                <?php if ($view['karts'] === []): ?>
                    <div class="empty-state"><strong>Keine Kartdaten für dieses Training.</strong><span>Kartdaten erscheinen, sobald gespeicherte Stints einem Kart zugeordnet sind.</span></div>
                <?php else: ?>
                    <div class="table-frame global-kart-statistics-table">
                        <table>
                            <thead><tr><th>Kart</th><th>Fahrzeit</th><th>Runden</th><th>Stints</th><th>Fahrer</th><th>PF</th><th>TF</th><th>PF / Runde</th><th>TF / Runde</th></tr></thead>
                            <tbody>
                            <?php foreach ($view['karts'] as $kartIndex => $kart): ?>
                                <tr class="kart-summary-row" data-kart-summary tabindex="0" role="button" aria-expanded="false" aria-controls="training-kart-driver-details-<?= $kartIndex ?>">
                                    <td data-label="Kart"><strong><?= escape($kart['name']) ?></strong></td>
                                    <td data-label="Fahrzeit"><strong class="time-value duration-value"><?= escape(format_duration($kart['seconds'])) ?></strong></td>
                                    <td data-label="Runden"><?= (int) $kart['rounds'] ?></td>
                                    <td data-label="Stints"><?= (int) $kart['stints'] ?></td>
                                    <td data-label="Fahrer"><span class="kart-driver-list"><?= count($kart['drivers']) ?> Fahrer</span></td>
                                    <td data-label="PF"><?= (int) $kart['pf'] ?></td>
                                    <td data-label="TF"><?= (int) $kart['tf'] ?></td>
                                    <td data-label="PF / Runde"><?= escape(number_format($kart['average_pf'], 2, '.', '')) ?></td>
                                    <td data-label="TF / Runde"><?= escape(number_format($kart['average_tf'], 2, '.', '')) ?></td>
                                </tr>
                                <tr id="training-kart-driver-details-<?= $kartIndex ?>" class="kart-driver-expansion" data-kart-details hidden>
                                    <td colspan="9"><div class="kart-driver-expansion-content"><span class="kart-driver-expansion-label">Fahrerbezogene Auswertung</span><div class="table-frame kart-driver-table"><table><thead><tr><th>Fahrer</th><th>Fahrzeit</th><th>Runden</th><th>Stints</th><th>PF</th><th>TF</th><th>PF / Runde</th><th>TF / Runde</th></tr></thead><tbody>
                                    <?php foreach ($kart['drivers'] as $driver): ?>
                                        <tr><td data-label="Fahrer"><strong><?= escape($driver['name']) ?></strong></td><td data-label="Fahrzeit"><strong class="time-value duration-value"><?= escape(format_duration($driver['seconds'])) ?></strong></td><td data-label="Runden"><?= (int) $driver['rounds'] ?></td><td data-label="Stints"><?= (int) $driver['stints'] ?></td><td data-label="PF"><?= (int) $driver['pf'] ?></td><td data-label="TF"><?= (int) $driver['tf'] ?></td><td data-label="PF / Runde"><?= escape(number_format($driver['average_pf'], 2, '.', '')) ?></td><td data-label="TF / Runde"><?= escape(number_format($driver['average_tf'], 2, '.', '')) ?></td></tr>
                                    <?php endforeach; ?>
                                    </tbody></table></div></div></td>
                                </tr>
                            <?php endforeach; ?>
                            </tbody>
                        </table>
                    </div>
                <?php endif; ?>
            </section>
        </section>
    </section>
</div>
<script src="<?= escape(base_url('assets/js/training.js?v=' . $trainingScriptVersion)) ?>" defer></script>
<script src="<?= escape(base_url('assets/js/statistics.js')) ?>" defer></script>
