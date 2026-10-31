<?php

declare(strict_types=1);

// TODO: This page is misleading. The `$masterType`/`$masterTitle` is not singular and that can confuse the user.

?>
<section class="container-sm vstack gap-4">
    <header>
        <p class="text-body-secondary mb-1">Verwaltung</p>
        <h1 class="text-body-emphasis display-5 fw-bold mb-2"><?= escape($masterTitle) ?> löschen</h1>
        <p class="text-body-secondary mb-0">Der Datensatz wird aus der aktiven Ansicht entfernt. Bereits verknüpfte Trainings- und Ergebnisdaten bleiben erhalten.</p>
    </header>

    <div class="card border-0 shadow-sm">
        <div class="card-body">
            <div class="alert alert-warning" role="alert">
                <i class="bi bi-exclamation-triangle me-2" aria-hidden="true"></i>
                Möchtest du diesen Datensatz wirklich löschen?
            </div>
            <form class="d-flex flex-wrap gap-2" action="<?= escape(base_url('verwaltung/' . $masterType . '/' . $item['id'] . '/loeschen')) ?>" method="post">
                <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">
                <button class="btn btn-danger" type="submit">
                    <i class="bi bi-trash me-1" aria-hidden="true"></i><?= escape($masterTitle) ?> löschen
                </button>
                <button class="btn btn-secondary" type="submit" formaction="<?= escape(base_url('verwaltung/' . $masterType)) ?>">
                    Abbrechen
                </button>
            </form>
        </div>
    </div>
</section>
