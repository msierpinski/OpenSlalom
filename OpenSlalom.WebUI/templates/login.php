<?php

declare(strict_types=1);

?>

<section class="card card-body bg-body border-0 shadow-sm p-3 py-5 text-center mx-auto col-lg-8 vstack gap-5">
    <header>
        <img class="img-fluid mb-3" src="<?= escape(base_url('assets/img/logo.png')) ?>" alt="" width="64" height="64">
        <h1 class="text-body-emphasis display-5 fw-bold"><?= isset($passwordChanged) ? 'Passwort geändert' : 'Willkommen zurück!' ?></h1>
        <p class="text-muted"><?= isset($passwordChanged) ? 'Dein Passwort wurde geändert. Bitte melde dich mit dem neuen Passwort an.' : 'Melde dich an, um auf den internen Bereich und deine Trainings zuzugreifen.' ?></p>
    </header>

    <section>
        <?php if (isset($loginError)): ?>
            <div class="alert alert-danger" role="alert">Fehler! <?= escape($loginError) ?></div>
        <?php endif; ?>
        <form class="text-start mx-auto" action="<?= escape(base_url('login')) ?>" method="post">
            <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">

            <div class="mb-3">
                <label class="form-label" for="login">Benutzername oder E-Mail-Adresse</label>
                <input class="form-control" name="login" id="login" value="<?= escape($login ?? '') ?>" autocomplete="username" required maxlength="254" placeholder="max.mustermann@openslalom.de">
            </div>

            <div class="mb-3">
                <label class="form-label" for="password">Passwort</label>
                <input class="form-control" type="password" name="password" id="password" autocomplete="current-password" required placeholder="Dein Passwort">
            </div>

            <div class="d-flex justify-content-between">
                <a class="btn btn-link text-decoration-none" href="<?= escape(base_url('passwort-vergessen')) ?>">Passwort vergessen?</a>
                <button class="btn btn-primary ms-auto" type="submit">Anmelden <i class="bi bi-arrow-right" aria-hidden="true"></i></button>
            </div>
        </form>
    </section>


    <span>
        Noch kein Konto?
        <a class="text-decoration-none" href="<?= escape(base_url('registrieren')) ?>">Jetzt registrieren</a>
    </span>

    <a class="text-decoration-none small" href=" <?= escape(base_url()) ?>"><i class="bi bi-arrow-left" aria-hidden="true"></i> Zurück zur Startseite</a>

</section>
