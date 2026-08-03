<?php declare(strict_types=1); ?>
<section class="auth-page shell">
    <div class="auth-panel account-deleted-panel">
        <div class="auth-panel-accent danger" aria-hidden="true"></div>
        <div class="auth-heading">
            <p class="eyebrow"><span></span> Konto entfernt</p>
            <h1>Konto gelöscht</h1>
            <p>Dein WebUI-Konto wurde gelöscht. Fahrerprofile, Trainings und Ergebnisse sind davon nicht betroffen.</p>
        </div>
        <a class="button button-primary auth-submit" href="<?= escape(base_url()) ?>"><span>Zur Startseite</span><i aria-hidden="true">→</i></a>
    </div>
</section>
