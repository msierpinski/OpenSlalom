<?php

declare(strict_types=1);

?>

<section>
    <header>
        <p class="text-muted mb-0">Fehler 404</p>
        <h1 class="text-body-emphasis display-5 fw-bold mb-0">Diese Seite oder Datei ist nicht verfügbar.</h1>
    </header>

    <p class="mt-3">Bitte überprüfen Sie den eingebenen Link. Sollte der Fehler weiterhin bestehen, kontaktieren Sie bitte den Support.</p>

    <a class="btn btn-primary mt-5" href="<?= escape(base_url()) ?>">
        Zur Startseite
        <i class="bi bi-arrow-right" aria-hidden="true"></i>
    </a>
</section>
