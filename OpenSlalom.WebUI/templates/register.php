<?php declare(strict_types=1); ?>
<?php $formValues ??= ['username' => '', 'email' => '']; ?>
<section class="auth-page shell">
    <div class="auth-panel registration-panel">
        <div class="auth-panel-accent" aria-hidden="true"></div>
        <div class="auth-logo-wrap"><img src="<?= escape(base_url('assets/img/logo.svg')) ?>" alt="" width="64" height="64"></div>
        <div class="auth-heading">
            <p class="eyebrow"><span></span> Neues Konto</p>
            <h1><?= isset($registrationSuccessful) ? 'Registrierung abgeschlossen' : 'Registrieren' ?></h1>
            <p><?= isset($registrationSuccessful) ? 'Dein Konto wurde angelegt. Ein Administrator wurde informiert und kann dir später eine Rolle und ein Fahrerprofil zuweisen.' : 'Erstelle ein Konto für den internen openSlalom-Bereich.' ?></p>
        </div>

        <?php if (isset($registrationSuccessful)): ?>
            <a class="button button-primary auth-submit" href="<?= escape(base_url('login')) ?>"><span>Zur Anmeldung</span><i aria-hidden="true">→</i></a>
        <?php else: ?>
            <?php if (isset($registrationError)): ?><div class="form-message error" role="alert"><?= escape($registrationError) ?></div><?php endif; ?>
            <form class="app-form" action="<?= escape(base_url('registrieren')) ?>" method="post">
                <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">
                <label class="honeypot-field" aria-hidden="true">Website<input name="website" tabindex="-1" autocomplete="off"></label>
                <label><span>Benutzername</span><input name="username" value="<?= escape($formValues['username']) ?>" autocomplete="username" required minlength="3" maxlength="100" pattern="[A-Za-zÄÖÜäöüß0-9._-]+" placeholder="Dein Benutzername"></label>
                <label><span>E-Mail-Adresse</span><input type="email" name="email" value="<?= escape($formValues['email']) ?>" autocomplete="email" required maxlength="254" placeholder="name@beispiel.de"></label>
                <label><span>Passwort</span><input type="password" name="password" autocomplete="new-password" required minlength="12" placeholder="Mindestens 12 Zeichen"></label>
                <label><span>Passwort wiederholen</span><input type="password" name="password_confirmation" autocomplete="new-password" required minlength="12"></label>
                <label class="checkbox-label privacy-confirmation"><input type="checkbox" name="accept_privacy" value="1" required><span>Ich habe die <a href="<?= escape(base_url('datenschutz')) ?>" target="_blank" rel="noopener">Datenschutzerklärung</a> gelesen.</span></label>
                <button class="button button-primary auth-submit" type="submit"><span>Konto registrieren</span><i aria-hidden="true">→</i></button>
            </form>
        <?php endif; ?>
        <div class="auth-alternative">Bereits registriert? <a href="<?= escape(base_url('login')) ?>">Zur Anmeldung</a></div>
        <a class="auth-back-link" href="<?= escape(base_url()) ?>">← Zurück zur Startseite</a>
    </div>
</section>
