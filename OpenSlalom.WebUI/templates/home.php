<?php declare(strict_types=1); ?>
<section class="hero home-hero">
    <div class="shell hero-grid">
        <div class="hero-copy">
            <p class="eyebrow"><span></span> Kart-Slalom digital organisiert</p>
            <h1>Training steuern.<br>Leistung sichtbar machen.</h1>
            <p class="hero-lead">openSlalom verbindet Vorbereitung, doppelte Zeitnahme, Fehlererfassung, Auswertung und veröffentlichte Ergebnisse in einem durchgängigen Ablauf.</p>
            <div class="hero-actions">
                <a class="button button-primary rounded-action" href="#funktionen">Funktionsumfang ansehen</a>
                <a class="home-text-link" href="<?= escape(base_url('registrieren')) ?>">Konto registrieren <span>→</span></a>
            </div>
            <div class="hero-facts" aria-label="Kernfunktionen">
                <span><b>2</b> parallele Zeitnahmen</span>
                <span><b>QR</b> direkter Ergebniszugang</span>
                <span><b>Live</b> veröffentlichte Trainings</span>
            </div>
        </div>
        <div class="hero-visual">
            <img src="<?= escape(base_url('assets/img/desktop-timing.svg')) ?>" alt="Illustration der Desktop-Zeitnahme mit zwei parallel laufenden Uhren">
            <div class="hero-visual-note"><span class="status-dot"></span> Lokal erfassen. Sicher synchronisieren.</div>
        </div>
    </div>
</section>

<section class="capability-ribbon" aria-label="Ablauf">
    <div class="shell capability-ribbon-grid">
        <div><span>01</span><strong>Vorbereiten</strong><small>Stammdaten, Fahrer, Karts und Trainings</small></div>
        <div><span>02</span><strong>Fahren</strong><small>Runden, Fehler und Stints erfassen</small></div>
        <div><span>03</span><strong>Auswerten</strong><small>Bestzeiten, Karts und Statistiken</small></div>
        <div><span>04</span><strong>Teilen</strong><small>Web-Ergebnisse per UUID und QR-Code</small></div>
    </div>
</section>

<section id="funktionen" class="home-section shell">
    <div class="section-heading home-heading">
        <p class="eyebrow"><span></span> Der Trainingsalltag in einer Anwendung</p>
        <h2>Von der Starterliste bis zur veröffentlichten Runde.</h2>
        <p>Die Desktop-App bleibt schnell und zuverlässig an der Strecke. Die WebUI macht Ergebnisse, Auswertungen und Verwaltung dort verfügbar, wo sie gebraucht werden.</p>
    </div>

    <div class="home-feature-grid">
        <article class="home-feature feature-timing">
            <span class="home-feature-number">01</span>
            <div class="home-icon timer-icon" aria-hidden="true">00</div>
            <h3>Präzise Zeitnahme</h3>
            <p>Zwei Zeitnahmestationen laufen unabhängig. Start, Runde, Stop und Stint-Speicherung sind für den Ablauf am Platz optimiert.</p>
            <ul><li>Shortcut-gesteuerte Bedienung</li><li>Rundenziele und Überschreiten</li><li>Parallele Fahrerzuweisung</li></ul>
        </article>
        <article class="home-feature feature-errors">
            <span class="home-feature-number">02</span>
            <div class="home-icon cone-icon" aria-hidden="true">PF</div>
            <h3>Fehler transparent erfassen</h3>
            <p>Pylonen- und Torfehler, ungültige Runden und Strafzeiten bleiben vom einzelnen Lauf bis zur Rangliste nachvollziehbar.</p>
            <ul><li>PF und TF pro Runde</li><li>Effektive Zeiten mit Strafen</li><li>Nachträgliche Korrekturen</li></ul>
        </article>
        <article class="home-feature feature-stints">
            <span class="home-feature-number">03</span>
            <div class="home-icon lap-icon" aria-hidden="true">↻</div>
            <h3>Stints im Kontext</h3>
            <p>Jeder gespeicherte Stint verbindet Fahrer, Kart, Altersklasse, Zeitpunkt und alle gefahrenen Runden.</p>
            <ul><li>Gesamt- und Durchschnittszeit</li><li>Historische Kartzuordnung</li><li>Komplette Rundendetails</li></ul>
        </article>
        <article class="home-feature feature-management">
            <span class="home-feature-number">04</span>
            <div class="home-icon grid-icon" aria-hidden="true">+</div>
            <h3>Stammdaten sauber verwalten</h3>
            <p>Vereine, Fahrer, Disziplinen mit Altersklassen, Karts und Wetter bilden eine konsistente Grundlage für jedes Training.</p>
            <ul><li>Altersklassen ohne Überschneidungen</li><li>Vereinslogos und Mitgliedsnummern</li><li>Soft-Delete statt Datenverlust</li></ul>
        </article>
    </div>
</section>

<section class="results-showcase">
    <div class="shell results-showcase-grid">
        <div class="results-showcase-visual"><img src="<?= escape(base_url('assets/img/web-results.svg')) ?>" alt="Illustration einer veröffentlichten Trainingsauswertung im Web"></div>
        <div class="results-showcase-copy">
            <p class="eyebrow light"><span></span> Ergebnisse im Web</p>
            <h2>Ein Training. Ein Link. Ein klarer Zwischenstand.</h2>
            <p>Veröffentlichte Trainings erhalten eine eindeutige UUID. Per QR-Code oder Trainingslink können Fahrer und Zuschauer Bestzeiten, Stints, Runden und Statistiken direkt aufrufen.</p>
            <div class="results-checks">
                <span>Öffentliche Ergebnisse mit Datenschutz</span>
                <span>Vornamen sichtbar, Nachnamen geschützt</span>
                <span>Vollständige Daten für berechtigte Benutzer</span>
                <span>Aktualisierung während laufender Trainings</span>
            </div>
        </div>
    </div>
</section>

<section class="home-section shell">
    <div class="section-heading home-heading compact-home-heading">
        <p class="eyebrow"><span></span> Mehr als eine Ergebnisliste</p>
        <h2>Auswertungen, die den nächsten Schritt zeigen.</h2>
    </div>
    <div class="insight-layout">
        <article class="insight-card insight-primary">
            <div class="insight-head"><span>STATISTIKEN</span><b>Aktuelles Jahr</b></div>
            <div class="insight-metric"><strong>284</strong><span>zeitgemessene Runden</span></div>
            <div class="bar-chart" aria-hidden="true"><i style="height:40%"></i><i style="height:66%"></i><i style="height:53%"></i><i style="height:82%"></i><i style="height:96%"></i><i style="height:72%"></i><i style="height:88%"></i></div>
            <p>Globale Statistik mit frei wählbarem Zeitraum, Fahrerwerten und Kart-Auswertung.</p>
        </article>
        <article class="insight-card">
            <span class="insight-label">KART-AUSWERTUNG</span>
            <h3>Welches Kart wurde wie gefahren?</h3>
            <p>Fahrzeit, Runden, Stints, Fehler und Fahrer können pro Kart und bis auf einzelne Fahrer aufgeschlüsselt werden.</p>
            <div class="mini-list"><span>Kart 7 <b>12 Stints</b></span><span>Kart 3 <b>9 Stints</b></span><span>Kart 4 <b>7 Stints</b></span></div>
        </article>
        <article class="insight-card">
            <span class="insight-label">BERECHTIGUNGEN</span>
            <h3>Rollen dort, wo sie wirken.</h3>
            <p>Administratoren, Trainingsleiter, Fahrer und registrierte Benutzer erhalten nur die Funktionen und Trainings, die für sie bestimmt sind.</p>
            <div class="role-pills"><span>Admin</span><span>Leitung</span><span>Fahrer</span><span>Registriert</span></div>
        </article>
    </div>
</section>

<section class="home-section shell home-management-section">
    <div class="management-copy">
        <p class="eyebrow"><span></span> Sicher verwalten</p>
        <h2>Vom Konto bis zur Veröffentlichung kontrolliert.</h2>
        <p>Die WebUI bringt Benutzerregistrierung, Passwort-Reset, eigene Kontoverwaltung und rollenbasierte CRUD-Bereiche zusammen. Öffentliche Trainings bleiben bewusst von internen Verwaltungsfunktionen getrennt.</p>
        <a class="button button-primary rounded-action" href="<?= escape(base_url('registrieren')) ?>">Jetzt registrieren</a>
    </div>
    <div class="management-steps">
        <div><b>01</b><span><strong>Konto erstellen</strong><small>Registrierte Benutzer starten ohne Fahrerzuordnung.</small></span></div>
        <div><b>02</b><span><strong>Rolle zuweisen</strong><small>Administratoren ordnen Rechte und Fahrerprofil zu.</small></span></div>
        <div><b>03</b><span><strong>Training freigeben</strong><small>Öffentliche Ergebnisse nur bewusst veröffentlichen.</small></span></div>
    </div>
</section>

<section class="closing home-closing shell">
    <img src="<?= escape(base_url('assets/img/logo.svg')) ?>" alt="" width="86" height="86">
    <div>
        <p class="eyebrow"><span></span> openSlalom</p>
        <h2>Weniger Verwaltung.<br>Mehr Zeit auf der Strecke.</h2>
    </div>
    <a class="button button-primary rounded-action" href="<?= escape(base_url('registrieren')) ?>">Konto erstellen</a>
</section>
