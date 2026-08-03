<?php declare(strict_types=1); ?>
<section class="hero">
    <div class="shell hero-grid">
        <div class="hero-copy">
            <p class="eyebrow"><span></span> Software für Kart-Slalom-Training</p>
            <h1>Vom ersten Start bis zur letzten Runde.</h1>
            <p class="hero-lead">openSlalom bringt Organisation, Zeitnahme und Auswertung an einen Ort. Entwickelt für den Ablauf an der Strecke, nicht für den Schreibtisch.</p>
            <div class="hero-actions">
                <a class="button button-primary" href="#funktionen">Funktionen entdecken</a>
                <span class="hero-note">Lokal schnell. Remote synchron. Live einsehbar.</span>
            </div>
        </div>

        <div class="timing-stage" aria-label="Illustration einer openSlalom-Zeitnahme">
            <div class="stage-topline">
                <span>ZEITNAHME 1</span>
                <span class="live-pill">LIVE</span>
            </div>
            <div class="stage-time">38.427</div>
            <div class="stage-progress"><span style="width: 72%"></span></div>
            <div class="stage-driver">
                <div><small>FAHRER</small><strong>Leistung im Blick</strong></div>
                <div><small>RUNDE</small><strong>07 / 10</strong></div>
            </div>
            <div class="slalom-line" aria-hidden="true">
                <i></i><i></i><i></i><i></i><i></i>
            </div>
        </div>
    </div>
</section>

<section class="signal-strip" aria-label="Kernfunktionen">
    <div class="shell signal-grid">
        <span><strong>2</strong> parallele Zeitnahmen</span>
        <span><strong>∞</strong> Stints und Runden</span>
        <span><strong>1</strong> klarer Datenstand</span>
        <span><strong>Live</strong> über Trainings-UUID</span>
    </div>
</section>

<section id="funktionen" class="feature-section shell">
    <div class="section-heading">
        <p class="eyebrow"><span></span> Was openSlalom leistet</p>
        <h2>Alles, was an einem Trainingstag zählt.</h2>
        <p>Vom Fahrerfeld bis zur Bestzeit bleibt der Ablauf nachvollziehbar, schnell bedienbar und auswertbar.</p>
    </div>

    <div class="feature-board">
        <article class="feature feature-wide feature-blue">
            <div class="feature-index">01</div>
            <div>
                <p class="feature-kicker">Zeitnahme</p>
                <h3>Zwei Fahrer. Zwei Uhren. Ein konzentrierter Ablauf.</h3>
                <p>Parallele Zeitnahmen, klare Tastatursteuerung, automatische Rundenfortschritte und direkte Stint-Speicherung halten das Training in Bewegung.</p>
            </div>
            <div class="mini-timers" aria-hidden="true">
                <span>32.118</span><span>34.602</span>
            </div>
        </article>

        <article class="feature">
            <div class="feature-index">02</div>
            <p class="feature-kicker">Organisation</p>
            <h3>Starterlisten, die mit dem Training arbeiten.</h3>
            <p>Fahrer sortieren, Karts zuordnen, Altersklassen bestimmen und den nächsten Starter ohne Umwege übernehmen.</p>
        </article>

        <article class="feature feature-dark">
            <div class="feature-index">03</div>
            <p class="feature-kicker">Präzision</p>
            <h3>PF, TF und ungültige Runden bleiben transparent.</h3>
            <p>Fehler und Strafzeiten fließen nachvollziehbar in effektive Zeiten, Stint-Summen und Ranglisten ein.</p>
        </article>

        <article class="feature feature-tall">
            <div class="feature-index">04</div>
            <p class="feature-kicker">Auswertung</p>
            <h3>Leistung wird sichtbar, nicht nur gespeichert.</h3>
            <p>Bestzeiten, Abstände, Durchschnittszeiten und komplette Stint-Verläufe zeigen Fortschritte Runde für Runde.</p>
            <div class="rank-preview" aria-hidden="true">
                <div><b>1</b><span>Bestzeit</span><strong>31.842</strong></div>
                <div><b>2</b><span>Verfolger</span><strong>+0.391</strong></div>
                <div><b>3</b><span>Verfolger</span><strong>+0.728</strong></div>
            </div>
        </article>

        <article class="feature feature-wide">
            <div class="feature-index">05</div>
            <div>
                <p class="feature-kicker">Daten</p>
                <h3>Offline erfassen. Remote spiegeln. Überall lesen.</h3>
                <p>SQLite hält den Betrieb an der Strecke stabil. Die Remote-Datenbank übernimmt den gemeinsamen Stand und stellt Ergebnisse schreibgeschützt im Web bereit.</p>
            </div>
            <div class="sync-graphic" aria-hidden="true"><span>LOCAL</span><i></i><span>REMOTE</span></div>
        </article>
    </div>
</section>

<section class="workflow-section">
    <div class="shell workflow-grid">
        <div class="section-heading compact">
            <p class="eyebrow light"><span></span> Ein System, drei Perspektiven</p>
            <h2>Für Organisation, Strecke und Fahrer.</h2>
        </div>
        <ol class="workflow-list">
            <li><span>01</span><div><strong>Vorbereiten</strong><p>Vereine, Fahrer, Disziplinen, Karts und Trainings zentral pflegen.</p></div></li>
            <li><span>02</span><div><strong>Fahren</strong><p>Runden stoppen, Fehler erfassen und Stints sicher lokal speichern.</p></div></li>
            <li><span>03</span><div><strong>Verstehen</strong><p>Ergebnisse synchronisieren und über einen eindeutigen Trainingslink verfolgen.</p></div></li>
        </ol>
    </div>
</section>

<section class="closing shell">
    <img src="<?= escape(base_url('assets/img/logo.svg')) ?>" alt="" width="86" height="86">
    <div>
        <p class="eyebrow"><span></span> openSlalom</p>
        <h2>Weniger Verwaltung. Mehr Zeit auf der Strecke.</h2>
    </div>
</section>
