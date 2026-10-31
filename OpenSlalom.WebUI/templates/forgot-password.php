<?php

declare(strict_types=1);

?>

<section class="card card-body bg-body border-0 shadow-sm p-3 py-5 text-center mx-auto col-lg-8 vstack gap-5">
    <header>
        <img class="img-fluid mb-3" src="<?= escape(base_url('assets/img/logo.png')) ?>" alt="" width="64" height="64">
        <h1 class="text-body-emphasis display-5 fw-bold">Passwort vergessen?</h1>
        <p class="text-muted">Wir senden dir einen Link zum Zurücksetzen deines Passworts, wenn ein aktives Konto zu deiner E-Mail-Adresse existiert.</p>
    </header>

    <section>
        <?php if (isset($requestSent)): ?>
            <div class="alert alert-success" role="alert">Wenn ein passendes Konto existiert, wurde eine E-Mail versendet.</div>
        <?php else: ?>
            <form class="text-start mx-auto" action="<?= escape(base_url('passwort-vergessen')) ?>" method="post">
                <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">

                <div class="mb-3">
                    <label class="form-label" for="email">E-Mail-Adresse</label>
                    <input class="form-control" type="email" id="email" name="email" autocomplete="email" required maxlength="254" placeholder="max.mustermann@openslalom.de">
                </div>

                <button class="btn btn-primary" type="submit">Link anfordern <i class="bi bi-arrow-right" aria-hidden="true"></i></button>
            </form>
        <?php endif; ?>
    </section>

    <a class="text-decoration-none" href="<?= escape(base_url('login')) ?>"><i class="bi bi-arrow-left"></i> Zurück zur Anmeldung</a>
</section>
