<?php

declare(strict_types=1); ?>
<?php $formValues ??= ['username' => '', 'email' => '']; ?>
<section class="card card-body border-0 shadow-sm p-3 py-5 text-center mx-auto vstack gap-4">
        <header>
            <img class="img-fluid mb-3" src="<?= escape(base_url('assets/img/logo.png')) ?>" alt="" width="64" height="64">
            <p class="text-muted mb-0">Neues Konto</p>
            <h1 class="text-body-emphasis display-5 fw-bold"><?= isset($registrationSuccessful) ? 'Registrierung abgeschlossen' : 'Registrieren' ?></h1>
            <p class="text-muted"><?= isset($registrationSuccessful) ? 'Dein Konto wurde angelegt. Ein Administrator wurde informiert und kann dir später eine Rolle und ein Fahrerprofil zuweisen.' : 'Erstelle ein Konto für den internen openSlalom-Bereich.' ?></p>
        </header>

        <?php if (isset($registrationSuccessful)): ?>
            <a class="btn btn-primary" href="<?= escape(base_url('login')) ?>">Zur Anmeldung <i class="bi bi-arrow-right" aria-hidden="true"></i></a>
        <?php else: ?>
            <?php if (isset($registrationError)): ?><div class="alert alert-danger text-start" role="alert"><?= escape($registrationError) ?></div><?php endif; ?>
            <form class="text-start mx-auto" action="<?= escape(base_url('registrieren')) ?>" method="post">
                <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">
                <div class="visually-hidden"><label for="website">Website</label><input id="website" name="website" tabindex="-1" autocomplete="off"></div>
                <div class="mb-3"><label class="form-label" for="username">Benutzername</label><input class="form-control" id="username" name="username" value="<?= escape($formValues['username']) ?>" autocomplete="username" required minlength="3" maxlength="100" pattern="[A-Za-zÄÖÜäöüß0-9._-]+" placeholder="Dein Benutzername"></div>
                <div class="mb-3"><label class="form-label" for="email">E-Mail-Adresse</label><input class="form-control" type="email" id="email" name="email" value="<?= escape($formValues['email']) ?>" autocomplete="email" required maxlength="254" placeholder="name@beispiel.de"></div>
                <div class="mb-3"><label class="form-label" for="password">Passwort</label><input class="form-control" type="password" id="password" name="password" autocomplete="new-password" required minlength="12" placeholder="Mindestens 12 Zeichen"></div>
                <div class="mb-3"><label class="form-label" for="password_confirmation">Passwort wiederholen</label><input class="form-control" type="password" id="password_confirmation" name="password_confirmation" autocomplete="new-password" required minlength="12"></div>
                <div class="form-check mb-3"><input class="form-check-input" type="checkbox" id="accept_privacy" name="accept_privacy" value="1" required><label class="form-check-label" for="accept_privacy">Ich habe die <a href="<?= escape(base_url('datenschutz')) ?>" target="_blank" rel="noopener">Datenschutzerklärung</a> gelesen.</label></div>
                <button class="btn btn-primary" type="submit">Konto registrieren <i class="bi bi-arrow-right" aria-hidden="true"></i></button>
            </form>
        <?php endif; ?>
        <span>Bereits registriert? <a class="text-decoration-none" href="<?= escape(base_url('login')) ?>">Zur Anmeldung</a></span>
        <a class="text-decoration-none small" href="<?= escape(base_url()) ?>">← Zurück zur Startseite</a>
</section>
