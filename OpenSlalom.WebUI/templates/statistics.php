<?php

declare(strict_types=1);

$summary = $statistics['summary'];
?>

<section class="container vstack gap-4 py-4">
    <header class="d-flex flex-column flex-md-row justify-content-between align-items-md-end gap-3">
        <div>
            <p class="text-body-secondary mb-1">Auswertung</p>
            <h1 class="display-6 fw-bold mb-0">Statistiken</h1>
            <p class="text-body-secondary mt-2 mb-0">
                Trainings, Fahrer und Karts im ausgewählten Zeitraum im Überblick.
            </p>
        </div>
    </header>

    <section class="card shadow-sm">
        <div class="card-body">
            <div class="d-flex flex-column flex-md-row justify-content-between align-items-md-center gap-2 mb-3">
                <div>
                    <h2 class="h5 mb-1">Auswertungszeitraum</h2>
                    <p class="text-body-secondary small mb-0">
                        Es werden Trainings berücksichtigt, deren Trainingsdatum im gewählten Zeitraum liegt.
                    </p>
                </div>
            </div>

            <form class="row row-cols-1 row-cols-md-3 g-3 align-items-end"
                action="<?= escape(base_url('statistiken')) ?>"
                method="get">
                <div class="col">
                    <label class="form-label" for="statistics-from">Auswertung von</label>
                    <input class="form-control" id="statistics-from" type="date" name="from"
                        value="<?= escape($period['from']) ?>" required>
                </div>
                <div class="col">
                    <label class="form-label" for="statistics-to">Auswertung bis</label>
                    <input class="form-control" id="statistics-to" type="date" name="to"
                        value="<?= escape($period['to']) ?>" required>
                </div>
                <div class="col">
                    <button class="btn btn-primary w-100" type="submit">
                        <i class="bi bi-funnel me-1" aria-hidden="true"></i>
                        Auswerten
                    </button>
                </div>
            </form>
        </div>
    </section>

    <?php if (isset($statisticsError)): ?>
        <div class="alert alert-danger mb-0" role="alert">
            <?= escape($statisticsError) ?>
        </div>
    <?php endif; ?>

    <section aria-label="Kennzahlen im ausgewählten Zeitraum">
        <div class="row row-cols-2 row-cols-md-3 row-cols-xl-4 g-3">
            <div class="col">
                <div class="card h-100">
                    <div class="card-body">
                        <div class="d-flex align-items-center gap-2 text-body-secondary mb-2">
                            <i class="bi bi-people" aria-hidden="true"></i>
                            <span class="small">Aktive Fahrer</span>
                        </div>
                        <p class="fs-3 fw-bold mb-0"><?= (int) ($summary['drivers'] ?? 0) ?></p>
                    </div>
                </div>
            </div>
            <div class="col">
                <div class="card h-100">
                    <div class="card-body">
                        <div class="d-flex align-items-center gap-2 text-body-secondary mb-2">
                            <i class="bi bi-speedometer2" aria-hidden="true"></i>
                            <span class="small">Eingesetzte Karts</span>
                        </div>
                        <p class="fs-3 fw-bold mb-0"><?= (int) ($summary['karts'] ?? 0) ?></p>
                    </div>
                </div>
            </div>
            <div class="col">
                <div class="card h-100">
                    <div class="card-body">
                        <div class="d-flex align-items-center gap-2 text-body-secondary mb-2">
                            <i class="bi bi-calendar-event" aria-hidden="true"></i>
                            <span class="small">Trainings</span>
                        </div>
                        <p class="fs-3 fw-bold mb-0"><?= (int) ($summary['trainings'] ?? 0) ?></p>
                    </div>
                </div>
            </div>
            <div class="col">
                <div class="card h-100">
                    <div class="card-body">
                        <div class="d-flex align-items-center gap-2 text-body-secondary mb-2">
                            <i class="bi bi-flag" aria-hidden="true"></i>
                            <span class="small">Zeitgemessene Runden</span>
                        </div>
                        <p class="fs-3 fw-bold mb-0"><?= (int) ($summary['rounds'] ?? 0) ?></p>
                    </div>
                </div>
            </div>
            <div class="col">
                <div class="card h-100">
                    <div class="card-body">
                        <div class="d-flex align-items-center gap-2 text-body-secondary mb-2">
                            <i class="bi bi-stopwatch" aria-hidden="true"></i>
                            <span class="small">Stints</span>
                        </div>
                        <p class="fs-3 fw-bold mb-0"><?= (int) ($summary['stints'] ?? 0) ?></p>
                    </div>
                </div>
            </div>
            <div class="col">
                <div class="card h-100">
                    <div class="card-body">
                        <div class="d-flex align-items-center gap-2 text-body-secondary mb-2">
                            <i class="bi bi-clock-history" aria-hidden="true"></i>
                            <span class="small">Gesamte Fahrzeit</span>
                        </div>
                        <p class="fs-3 fw-bold font-monospace mb-0"><?= escape(format_duration((float) ($summary['seconds'] ?? 0))) ?></p>
                    </div>
                </div>
            </div>
            <div class="col">
                <div class="card h-100">
                    <div class="card-body">
                        <div class="d-flex align-items-center gap-2 text-body-secondary mb-2">
                            <i class="bi bi-cone-striped" aria-hidden="true"></i>
                            <span class="small">Pylonenfehler (PF)</span>
                        </div>
                        <p class="fs-3 fw-bold mb-0"><?= (int) ($summary['pf'] ?? 0) ?></p>
                    </div>
                </div>
            </div>
            <div class="col">
                <div class="card h-100">
                    <div class="card-body">
                        <div class="d-flex align-items-center gap-2 text-body-secondary mb-2">
                            <i class="bi bi-sign-stop" aria-hidden="true"></i>
                            <span class="small">Torfehler (TF)</span>
                        </div>
                        <p class="fs-3 fw-bold mb-0"><?= (int) ($summary['tf'] ?? 0) ?></p>
                    </div>
                </div>
            </div>
            <div class="col">
                <div class="card h-100">
                    <div class="card-body">
                        <div class="d-flex align-items-center gap-2 text-body-secondary mb-2">
                            <i class="bi bi-cone" aria-hidden="true"></i>
                            <span class="small">PF pro Runde</span>
                        </div>
                        <p class="fs-3 fw-bold mb-0"><?= escape(number_format((float) ($summary['average_pf'] ?? 0), 2, '.', '')) ?></p>
                    </div>
                </div>
            </div>
            <div class="col">
                <div class="card h-100">
                    <div class="card-body">
                        <div class="d-flex align-items-center gap-2 text-body-secondary mb-2">
                            <i class="bi bi-sign-turn-right" aria-hidden="true"></i>
                            <span class="small">TF pro Runde</span>
                        </div>
                        <p class="fs-3 fw-bold mb-0"><?= escape(number_format((float) ($summary['average_tf'] ?? 0), 2, '.', '')) ?></p>
                    </div>
                </div>
            </div>
            <div class="col">
                <div class="card h-100">
                    <div class="card-body">
                        <div class="d-flex align-items-center gap-2 text-body-secondary mb-2">
                            <i class="bi bi-check-circle" aria-hidden="true"></i>
                            <span class="small">Fehlerfreie Runden</span>
                        </div>
                        <p class="fs-3 fw-bold mb-0"><?= escape(number_format((float) ($summary['error_free_percent'] ?? 0), 2, '.', '')) ?>%</p>
                    </div>
                </div>
            </div>
        </div>
    </section>

    <section class="card shadow-sm">
        <div class="card-header bg-body d-flex flex-column flex-sm-row justify-content-between align-items-sm-center gap-2 py-3">
            <div>
                <p class="text-body-secondary small mb-1">Fahrerübersicht</p>
                <h2 class="h4 mb-0">Fahrerstatistik</h2>
            </div>
            <span class="badge text-bg-secondary"><?= count($statistics['drivers']) ?> Fahrer</span>
        </div>
        <div class="card-body p-0">
            <?php if ($statistics['drivers'] === []): ?>
                <div class="alert alert-info m-3 mb-0" role="status">
                    <strong>Keine Daten im gewählten Zeitraum.</strong>
                    <span>Wähle einen anderen Zeitraum oder erfasse Trainingsrunden.</span>
                </div>
                <div class="p-3"></div>
            <?php else: ?>
                <div class="row g-3 p-3">
                    <?php foreach ($statistics['drivers'] as $driver): ?>
                        <div class="col-12 col-md-6 col-xl-4">
                            <article class="card h-100">
                                <div class="card-body">
                                    <h3 class="h6 fw-semibold mb-3"><?= escape($driver['name']) ?></h3>
                                    <dl class="row mb-0 small">
                                        <dt class="col-7 fw-normal text-body-secondary">Fahrzeit</dt>
                                        <dd class="col-5 text-end font-monospace text-nowrap"><?= escape(format_duration($driver['seconds'])) ?></dd>
                                        <dt class="col-7 fw-normal text-body-secondary">Trainings</dt>
                                        <dd class="col-5 text-end"><?= (int) $driver['trainings'] ?></dd>
                                        <dt class="col-7 fw-normal text-body-secondary">Runden</dt>
                                        <dd class="col-5 text-end"><?= (int) $driver['rounds'] ?></dd>
                                        <dt class="col-7 fw-normal text-body-secondary">Fehlerfreie Runden</dt>
                                        <dd class="col-5 text-end text-nowrap"><?= (int) $driver['error_free'] ?> <span class="text-body-secondary">(<?= escape(number_format($driver['error_free_percent'], 2, '.', '')) ?>%)</span></dd>
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
        </div>
    </section>

    <section class="card shadow-sm">
        <div class="card-header bg-body d-flex flex-column flex-sm-row justify-content-between align-items-sm-center gap-2 py-3">
            <div>
                <p class="text-body-secondary small mb-1">Materialübersicht</p>
                <h2 class="h4 mb-0">Kart-Auswertung</h2>
            </div>
            <span class="badge text-bg-secondary"><?= count($statistics['karts']) ?> Karts</span>
        </div>
        <div class="card-body p-0">
            <?php if ($statistics['karts'] === []): ?>
                <div class="alert alert-info m-3 mb-0" role="status">
                    <strong>Keine Kartdaten im gewählten Zeitraum.</strong>
                    <span>Kartdaten erscheinen, sobald gespeicherte Stints einem Kart zugeordnet sind.</span>
                </div>
                <div class="p-3"></div>
            <?php else: ?>
                <div class="row g-3 p-3">
                    <?php foreach ($statistics['karts'] as $kartIndex => $kart): ?>
                        <div class="col-12 col-md-6 col-xl-4">
                            <article class="card h-100">
                                <div class="card-body">
                                    <div data-kart-summary tabindex="0" role="button" aria-expanded="false"
                                        aria-controls="kart-driver-details-<?= (int) $kartIndex ?>">
                                        <h3 class="h6 fw-semibold mb-3">
                                            <i class="bi bi-chevron-right me-1" aria-hidden="true"></i><?= escape($kart['name']) ?>
                                        </h3>
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
                                    <div id="kart-driver-details-<?= (int) $kartIndex ?>" data-kart-details class="border-top mt-3 pt-3" hidden>
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
        </div>
    </section>
</section>

<script src="<?= escape(base_url('assets/js/statistics.js')) ?>" defer></script>
