<?php

declare(strict_types=1);

?>

<section class="card card-body bg-body border-0 shadow-sm p-3 py-5 text-center mx-auto vstack gap-5">
    <header>
        <img class="img-fluid mb-3" src="<?= escape(base_url('assets/img/logo.png')) ?>" alt="" width="64" height="64">
        <p class="text-muted mb-0">Zugang sichern</p>
        <h1 class="text-body-emphasis display-5 fw-bold"><?= isset($resetSuccessful) ? 'Passwort geändert' : 'Neues Passwort' ?></h1>
        <p class="text-muted"><?= isset($resetSuccessful) ? 'Dein neues Passwort ist aktiv. Du kannst dich jetzt anmelden.' : 'Lege ein neues Passwort mit mindestens 12 Zeichen fest.' ?></p>
    </header>
    <?php if (isset($resetSuccessful)): ?>
        <a class="btn btn-primary" href="<?= escape(base_url('login')) ?>">Zur Anmeldung <i class="bi bi-arrow-right" aria-hidden="true"></i></a>
    <?php else: ?>
        <?php if (isset($resetError)): ?><div class="alert alert-danger" role="alert"><?= escape($resetError) ?></div><?php endif; ?>
        <?php if ($token !== ''): ?>
            <form class="text-start mx-auto" action="<?= escape(base_url('passwort-zuruecksetzen')) ?>" method="post">
                <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">
                <input type="hidden" name="token" value="<?= escape($token) ?>">
                <div class="mb-3"><label class="form-label" for="password">Neues Passwort</label><input class="form-control" type="password" id="password" name="password" autocomplete="new-password" required minlength="12"></div>
                <div class="mb-3"><label class="form-label" for="password_confirmation">Passwort wiederholen</label><input class="form-control" type="password" id="password_confirmation" name="password_confirmation" autocomplete="new-password" required minlength="12"></div>
                <button class="btn btn-primary" type="submit">Passwort speichern <i class="bi bi-arrow-right" aria-hidden="true"></i></button>
            </form>
        <?php else: ?>
            <a class="btn btn-primary" href="<?= escape(base_url('passwort-vergessen')) ?>">Neuen Link anfordern <i class="bi bi-arrow-right" aria-hidden="true"></i></a>
        <?php endif; ?>
    <?php endif; ?>
</section>
