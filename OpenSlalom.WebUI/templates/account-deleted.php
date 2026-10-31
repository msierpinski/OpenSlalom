<?php

declare(strict_types=1);

?>

<section>
    <header>
        <p class="text-muted mb-0">Konto gelöscht</p>
        <h1 class="text-body-emphasis display-5 fw-bold mb-0">Schade, dass du gehst!</h1>
    </header>

    <p class="mt-3">Dein WebUI-Konto wurde gelöscht. Fahrerprofile, Trainings und Ergebnisse sind davon nicht betroffen.</p>

    <a class="btn btn-primary mt-5" href="<?= escape(base_url()) ?>">
        Zur Startseite
        <i class="bi bi-arrow-right" aria-hidden="true"></i>
    </a>
</section>
