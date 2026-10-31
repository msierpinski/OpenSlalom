<?php

declare(strict_types=1);

?>

<div class="vstack gap-5 gap-lg-5">
    <section class="row align-items-center g-4 g-lg-5">
        <div class="col-12 col-lg-6 order-1 order-lg-1">
            <p class="text-muted mb-0">Kart-Slalom digital organisiert</p>
            <h1 class="display-5 fw-bold text-body-emphasis">Training steuern.<br>Leistung sichtbar machen.</h1>
            <p class="lead mb-4">openSlalom verbindet Vorbereitung, doppelte Zeitnahme, Fehlererfassung, Auswertung und veröffentlichte Ergebnisse in einem durchgängigen Ablauf.</p>
            <div class="d-flex flex-column flex-sm-row align-items-stretch align-items-sm-center gap-2">
                <a class="btn btn-primary" href="#funktionen">Funktionsumfang ansehen</a>
                <a class="btn btn-link text-decoration-none" href="<?= escape(base_url('registrieren')) ?>"><i class="bi bi-arrow-right me-1" aria-hidden="true"></i>Konto registrieren</a>
            </div>
        </div>
        <div class="col-12 col-lg-6 order-0 order-lg-2 text-center">
            <img class="img-fluid" src="<?= escape(base_url('assets/img/desktop-timing.svg')) ?>" alt="Illustration der Desktop-Zeitnahme mit zwei parallel laufenden Uhren">
            <div class="small text-body-secondary mt-2"><span class="spinner-grow spinner-grow-sm text-primary me-1" aria-hidden="true"></span>Lokal erfassen. Sicher synchronisieren.</div>
        </div>
    </section>

    <section class="row row-cols-1 row-cols-sm-3 g-3 text-center" aria-label="Kernfunktionen">
        <div class="col">
            <div class="card card-body border-0 shadow-sm h-100"><strong class="text-primary fs-4">2</strong><span class="small text-body-secondary">parallele Zeitnahmen</span></div>
        </div>
        <div class="col">
            <div class="card card-body border-0 shadow-sm h-100"><strong class="text-primary fs-4">QR</strong><span class="small text-body-secondary">direkter Zugang zu Ergebnis- und Starterlisten</span></div>
        </div>
        <div class="col">
            <div class="card card-body border-0 shadow-sm h-100"><strong class="text-primary fs-4">Live</strong><span class="small text-body-secondary">Trainingsdaten während des Trainings</span></div>
        </div>
    </section>

    <section class="row row-cols-1 row-cols-lg-2 g-3" aria-label="So funktioniert OpenSlalom">
        <div class="col">
            <div class="card card-body bg-body shadow-sm border-0 h-100">
                <span class="fs-5 text-primary">01</span>
                <h2 class="fw-bold text-body-emphasis">Vorbereiten</h2>
                <p class="lead mb-0">
                    Fahrer, Karts und Trainings organisieren – alles übersichtlich an einem Ort.
                </p>
            </div>
        </div>

        <div class="col">
            <div class="card card-body bg-body shadow-sm border-0 h-100">
                <span class="fs-5 text-primary">02</span>
                <h2 class="fw-bold text-body-emphasis">Auf die Strecke</h2>
                <p class="lead mb-0">
                    Rundenzeiten stoppen, Stints erfassen und Fahrfehler direkt dokumentieren.
                </p>
            </div>
        </div>

        <div class="col">
            <div class="card card-body bg-body shadow-sm border-0 h-100">
                <span class="fs-5 text-primary">03</span>
                <h2 class="fw-bold text-body-emphasis">Leistung auswerten</h2>
                <p class="lead mb-0">
                    Bestzeiten vergleichen, Kartleistungen analysieren und Fortschritte verfolgen.
                </p>
            </div>
        </div>

        <div class="col">
            <div class="card card-body bg-body shadow-sm border-0 h-100">
                <span class="fs-5 text-primary">04</span>
                <h2 class="fw-bold text-body-emphasis">Ergebnisse teilen</h2>
                <p class="lead mb-0">
                    Trainingsergebnisse einfach per Link oder QR-Code veröffentlichen.
                </p>
            </div>
        </div>
    </section>

    <section id="funktionen">
        <p class="text-muted mb-0">Der Trainingsalltag in einer Anwendung</p>
        <h2 class="display-5 fw-bold text-body-emphasis">Von der Starterliste bis zur veröffentlichten Runde.</h2>
        <p class="lead mb-4 mb-lg-5">Die Desktop-App bleibt schnell und zuverlässig an der Strecke. Die WebUI macht Ergebnisse, Auswertungen und Verwaltung dort verfügbar, wo sie gebraucht werden.</p>

        <div class="row">
            <div class="col-lg-2 display-4 d-flex flex-column align-items-lg-center font-monospace text-primary">
                01
                <div class="vr align-self-center flex-grow-1 d-none d-lg-flex"></div>
            </div>
            <div class="col-lg-10">
                <h3 class=" fw-bold text-body-emphasis">Präzise Zeitnahme</h3>
                <p class="lead">Zwei Zeitnahmestationen laufen unabhängig. Start, Runde, Stop und Stint-Speicherung sind für den Ablauf am Platz optimiert.</p>
                <ul class="list-group list-group-flush mb-5">
                    <li class="list-group-item bg-transparent">Shortcut-gesteuerte Bedienung</li>
                    <li class="list-group-item bg-transparent">Rundenziele und Überschreiten</li>
                    <li class="list-group-item bg-transparent">Parallele Fahrerzuweisung</li>
                </ul>
            </div>


        </div>
        <div class="row">
            <div class="col-lg-2 display-4 d-flex flex-column align-items-lg-center font-monospace text-primary">
                02
                <div class="vr align-self-center flex-grow-1 d-none d-lg-flex"></div>
            </div>
            <div class="col-lg-10">
                <h3 class=" fw-bold text-body-emphasis">Fehler transparent erfassen</h3>
                <p class="lead">Pylonen- und Torfehler, ungültige Runden und Strafzeiten bleiben vom einzelnen Lauf bis zur Rangliste nachvollziehbar.</p>
                <ul class="list-group list-group-flush mb-5">
                    <li class="list-group-item bg-transparent">PF und TF pro Runde</li>
                    <li class="list-group-item bg-transparent">Effektive Zeiten mit Strafen</li>
                    <li class="list-group-item bg-transparent">Nachträgliche Korrekturen</li>
                </ul>
            </div>
        </div>
        <div class="row">
            <div class="col-lg-2 display-4 d-flex flex-column align-items-lg-center font-monospace text-primary">
                03
                <div class="vr align-self-center flex-grow-1 d-none d-lg-flex"></div>
            </div>
            <div class="col-lg-10">
                <h3 class=" fw-bold text-body-emphasis">Stints im Kontext</h3>
                <p class="lead">Jeder gespeicherte Stint verbindet Fahrer, Kart, Altersklasse, Zeitpunkt und alle gefahrenen Runden.</p>
                <ul class="list-group list-group-flush mb-5">
                    <li class="list-group-item bg-transparent">Gesamt- und Durchschnittszeit</li>
                    <li class="list-group-item bg-transparent">Historische Kartzuordnung</li>
                    <li class="list-group-item bg-transparent">Komplette Rundendetails</li>
                </ul>
            </div>

        </div>
        <div class="row">
            <div class="col-lg-2 display-4 d-flex flex-column align-items-lg-center font-monospace text-primary">
                04
                <div class="vr align-self-center flex-grow-1 d-none d-lg-flex"></div>
            </div>
            <div class="col-lg-10">
                <h3 class="fw-bold text-body-emphasis">Stammdaten sauber verwalten</h3>
                <p class="lead">Vereine, Fahrer, Disziplinen mit Altersklassen, Karts und Wetter bilden eine konsistente Grundlage für jedes Training.</p>
                <ul class="list-group list-group-flush mb-0 ">
                    <li class="list-group-item bg-transparent">Altersklassen ohne Überschneidungen</li>
                    <li class="list-group-item bg-transparent">Vereinslogos und Mitgliedsnummern</li>
                    <li class="list-group-item bg-transparent">Soft-Delete statt Datenverlust</li>
                </ul>
            </div>

        </div>
    </section>

    <section class="card bg-primary text-white border-0 shadow-sm overflow-hidden">
        <div class="card-body p-4 p-lg-5">
            <div class="row align-items-center g-4 g-lg-5">
                <div class="col-lg-5 text-center">
                    <img class="img-fluid" src="<?= escape(base_url('assets/img/web-results.svg')) ?>" alt="Illustration einer veröffentlichten Trainingsauswertung im Web">
                </div>
                <div class="col-lg-7">
                    <p class="text-uppercase text-opacity-75 fw-semibold small mb-2">Ergebnisse im Web</p>
                    <h2 class="display-6 fw-bold">Ein Training. Ein Link. Ein klarer Zwischenstand.</h2>
                    <p class="lead text-opacity-75 mb-4">Veröffentlichte Trainings erhalten eine eindeutige UUID. Per QR-Code oder Trainingslink können Fahrer und Zuschauer Bestzeiten, Stints, Runden und Statistiken direkt aufrufen.</p>
                    <ul class="list-unstyled vstack gap-2 mb-0">
                        <li><i class="bi bi-check2-circle me-2" aria-hidden="true"></i>Öffentliche Ergebnisse mit Datenschutz</li>
                        <li><i class="bi bi-check2-circle me-2" aria-hidden="true"></i>Vornamen sichtbar, Nachnamen geschützt</li>
                        <li><i class="bi bi-check2-circle me-2" aria-hidden="true"></i>Vollständige Daten für berechtigte Benutzer</li>
                        <li><i class="bi bi-check2-circle me-2" aria-hidden="true"></i>Aktualisierung während laufender Trainings</li>
                    </ul>
                </div>
            </div>
        </div>
    </section>

    <section>
        <header class="mb-4">
            <p class="text-body-secondary text-uppercase fw-semibold small mb-2">Mehr als eine Ergebnisliste</p>
            <h2 class="display-6 fw-bold text-body-emphasis mb-0">Auswertungen, die den nächsten Schritt zeigen.</h2>
        </header>
        <div class="row row-cols-1 row-cols-lg-3 g-4">
            <article class="col">
                <div class="card border-0 shadow-sm h-100">
                    <div class="card-body p-4">
                        <div class="d-flex align-items-center gap-3 mb-4">
                            <span class="rounded bg-primary-subtle text-primary p-2"><i class="bi bi-bar-chart-line fs-5" aria-hidden="true"></i></span>
                            <div>
                                <p class="text-body-secondary text-uppercase fw-semibold small mb-0">Statistiken</p><span class="small text-body-secondary">Aktuelles Jahr</span>
                            </div>
                        </div>
                        <div class="display-5 fw-bold text-body-emphasis">284</div>
                        <p class="text-body-secondary mb-3">zeitgemessene Runden</p>
                        <p class="mb-0">Globale Statistik mit frei wählbarem Zeitraum, Fahrerwerten und Kart-Auswertung.</p>
                    </div>
                </div>
            </article>
            <article class="col">
                <div class="card border-0 shadow-sm h-100">
                    <div class="card-body p-4">
                        <div class="d-flex align-items-center gap-3 mb-3">
                            <span class="rounded bg-primary-subtle text-primary p-2"><i class="bi bi-speedometer2 fs-5" aria-hidden="true"></i></span>
                            <p class="text-body-secondary text-uppercase fw-semibold small mb-0">Kart-Auswertung</p>
                        </div>
                        <h3 class="h4">Welches Kart wurde wie gefahren?</h3>
                        <p>Fahrzeit, Runden, Stints, Fehler und Fahrer können pro Kart bis ins Detail aufgeschlüsselt werden.</p>
                        <ul class="list-group list-group-flush small">
                            <li class="list-group-item px-0 d-flex justify-content-between">Kart 7 <strong>12 Stints</strong></li>
                            <li class="list-group-item px-0 d-flex justify-content-between">Kart 3 <strong>9 Stints</strong></li>
                            <li class="list-group-item px-0 d-flex justify-content-between">Kart 4 <strong>7 Stints</strong></li>
                        </ul>
                    </div>
                </div>
            </article>
            <article class="col">
                <div class="card border-0 shadow-sm h-100">
                    <div class="card-body p-4">
                        <div class="d-flex align-items-center gap-3 mb-3">
                            <span class="rounded bg-primary-subtle text-primary p-2"><i class="bi bi-shield-check fs-5" aria-hidden="true"></i></span>
                            <p class="text-body-secondary text-uppercase fw-semibold small mb-0">Berechtigungen</p>
                        </div>
                        <h3 class="h4">Rollen dort, wo sie wirken.</h3>
                        <p>Jede Rolle erhält nur die Funktionen und Trainings, die für sie bestimmt sind.</p>
                        <div class="d-flex flex-wrap gap-2">
                            <span class="badge text-bg-primary">Admin</span><span class="badge text-bg-secondary">Leitung</span><span class="badge text-bg-info">Fahrer</span><span class="badge text-bg-light border">Registriert</span>
                        </div>
                    </div>
                </div>
            </article>
        </div>
    </section>

    <section class="row g-4 align-items-stretch">
        <div class="col-lg-7">
            <div class="h-100 d-flex flex-column justify-content-center">
                <p class="text-body-secondary text-uppercase fw-semibold small mb-2">Sicher verwalten</p>
                <h2 class="display-6 fw-bold text-body-emphasis">Vom Konto bis zur Veröffentlichung kontrolliert.</h2>
                <p class="lead">Die WebUI verbindet Benutzerregistrierung, Passwort-Reset, Kontoverwaltung und rollenbasierte Bereiche. Öffentliche Trainings bleiben bewusst von internen Verwaltungsfunktionen getrennt.</p>
                <div><a class="btn btn-primary" href="<?= escape(base_url('registrieren')) ?>">Jetzt registrieren</a></div>
            </div>
        </div>
        <div class="col-lg-5">
            <div class="card border-0 shadow-sm h-100">
                <div class="list-group list-group-flush">
                    <div class="list-group-item p-3"><span class="badge text-bg-primary me-2">01</span><strong>Konto erstellen</strong><small class="d-block text-body-secondary ms-5">Registrierte Benutzer starten ohne Fahrerzuordnung.</small></div>
                    <div class="list-group-item p-3"><span class="badge text-bg-primary me-2">02</span><strong>Rolle zuweisen</strong><small class="d-block text-body-secondary ms-5">Administratoren ordnen Rechte und Fahrerprofil zu.</small></div>
                    <div class="list-group-item p-3"><span class="badge text-bg-primary me-2">03</span><strong>Training freigeben</strong><small class="d-block text-body-secondary ms-5">Öffentliche Ergebnisse nur bewusst veröffentlichen.</small></div>
                </div>
            </div>
        </div>
    </section>

    <section class="card border-0 shadow-sm text-center">
        <div class="card-body p-4 p-lg-5">
            <img class="mb-3" src="<?= escape(base_url('assets/img/logo.png')) ?>" alt="" width="72" height="72">
            <p class="text-body-secondary text-uppercase fw-semibold small mb-2">openSlalom</p>
            <h2 class="display-6 fw-bold text-body-emphasis">Weniger Verwaltung.<br>Mehr Zeit auf der Strecke.</h2>
            <a class="btn btn-primary mt-2" href="<?= escape(base_url('registrieren')) ?>">Konto erstellen</a>
        </div>
    </section>
</div>
