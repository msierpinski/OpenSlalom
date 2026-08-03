<?php declare(strict_types=1); ?>
<section class="auth-page shell">
    <div class="auth-panel">
        <div class="auth-panel-accent" aria-hidden="true"></div>
        <div class="auth-logo-wrap">
            <img src="<?= escape(base_url('assets/img/logo.svg')) ?>" alt="" width="64" height="64">
        </div>
        <div class="auth-heading">
            <p class="eyebrow"><span></span> Geschützter Bereich</p>
            <h1><?= isset($passwordChanged) ? 'Passwort geändert' : 'Willkommen zurück' ?></h1>
            <p><?= isset($passwordChanged) ? 'Dein Passwort wurde geändert. Bitte melde dich mit dem neuen Passwort an.' : 'Melde dich an, um interne Trainings und deine persönlichen Zuordnungen aufzurufen.' ?></p>
        </div>
        <?php if (isset($loginError)): ?>
            <div class="form-message error" role="alert"><?= escape($loginError) ?></div>
        <?php endif; ?>
        <form class="app-form" action="<?= escape(base_url('login')) ?>" method="post">
            <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">
            <label><span>Benutzername oder E-Mail-Adresse</span><input name="login" value="<?= escape($login ?? '') ?>" autocomplete="username" required maxlength="254" placeholder="Benutzername oder E-Mail-Adresse"></label>
            <label><span>Passwort</span><input type="password" name="password" autocomplete="current-password" required placeholder="Dein Passwort"></label>
            <a class="forgot-password-link" href="<?= escape(base_url('passwort-vergessen')) ?>">Passwort vergessen?</a>
            <button class="button button-primary auth-submit" type="submit"><span>Anmelden</span><i aria-hidden="true">→</i></button>
        </form>
        <div class="auth-alternative">Noch kein Konto? <a href="<?= escape(base_url('registrieren')) ?>">Jetzt registrieren</a></div>
        <a class="auth-back-link" href="<?= escape(base_url()) ?>">← Zurück zur Startseite</a>
    </div>
</section>
