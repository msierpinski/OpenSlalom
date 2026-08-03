<?php declare(strict_types=1); ?>
<section class="auth-page shell">
    <div class="auth-panel">
        <div class="auth-panel-accent" aria-hidden="true"></div>
        <div class="auth-logo-wrap"><img src="<?= escape(base_url('assets/img/logo.svg')) ?>" alt="" width="64" height="64"></div>
        <div class="auth-heading">
            <p class="eyebrow"><span></span> Zugang wiederherstellen</p>
            <h1>Passwort vergessen?</h1>
            <p>Gib deine E-Mail-Adresse ein. Wenn ein aktives Konto existiert, erhältst du einen Link zum Zurücksetzen.</p>
        </div>
        <?php if (isset($requestSent)): ?>
            <div class="form-message success">Wenn ein passendes Konto existiert, wurde eine E-Mail versendet.</div>
        <?php else: ?>
            <form class="app-form" action="<?= escape(base_url('passwort-vergessen')) ?>" method="post">
                <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">
                <label><span>E-Mail-Adresse</span><input type="email" name="email" autocomplete="email" required maxlength="254" placeholder="name@beispiel.de"></label>
                <button class="button button-primary auth-submit" type="submit"><span>Link anfordern</span><i aria-hidden="true">→</i></button>
            </form>
        <?php endif; ?>
        <a class="auth-back-link" href="<?= escape(base_url('login')) ?>">← Zurück zur Anmeldung</a>
    </div>
</section>
