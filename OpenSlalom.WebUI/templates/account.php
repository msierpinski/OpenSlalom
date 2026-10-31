<?php

declare(strict_types=1);

?>

<div class="vstack gap-5">
    <header>
        <p class="text-muted mb-0">Persönlicher Bereich</p>
        <h1 class="text-body-emphasis display-5 fw-bold mb-0">Mein Konto</h1>
    </header>

    <section class="card card-body border-0 shadow-sm pb-0">
        <dl class="row row-cols-1 row-cols-sm-2">
            <dt>Benutzername</dt>
            <dd><?= escape($currentUser['username'] ?? []) ?></dd>
            <dt>E-Mail-Adresse</dt>
            <dd class="font-monospace"><?= escape($currentUser['email'] ?? '-') ?></dd>
            <dt>Rolle<?= count($currentUser['roles'] ?? []) === 1 ? '' : 'n' ?></dt>
            <dd><span class="badge text-bg-primary"><?= escape(implode(', ', $currentUser['roles'] ?? ['Keine Rolle'])) ?></span></dd>
            <dt>Fahrerzuordnung</dt>
            <dd><?= escape($driverName ?? 'Keine Zuordnung') ?></dd>
        </dl>
    </section>

    <section class="rounded bg-body shadow-sm p-3">
        <h2 class="fw-bold h3">Passwort ändern</h2>
        <p class="text-muted">Nach der Änderung werden alle bestehenden Sitzungen beendet.</p>

        <?php if (isset($passwordError)): ?>
            <div class="alert alert-danger" role="alert">
                Fehler! <?= escape($passwordError) ?>
            </div>
        <?php endif; ?>
        <form action="<?= escape(base_url('konto/passwort')) ?>" method="post">
            <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">

            <div class="mb-3">
                <label class="form-label" for="current_password">Aktuelles Passwort</label>
                <input class="form-control" type="password" name="current_password" id="current_password" autocomplete="current-password" required>
            </div>
            <div class="mb-3">
                <label class="form-label" for="new_password">Neues Passwort</label>
                <input class="form-control" type="password" name="new_password" id="new_password" autocomplete="new-password" required minlength="12" aria-describedby="new_password_help">
                <div id="new_password_help" class="form-text">Das Passwort muss mindestens 12 Zeichen lang sein.</div>
            </div>
            <div class="mb-3">
                <label class="form-label" for="password_confirmation">Neues Passwort wiederholen</label>
                <input class="form-control" type="password" name="password_confirmation" id="password_confirmation" autocomplete="new-password" required minlength="12">
            </div>
            <button class="btn btn-primary" type="submit">Passwort ändern</button>
        </form>
    </section>

    <section class="border border-danger rounded bg-body shadow-sm p-3">
        <h2 class="fw-bold h3">Konto löschen</h2>

        <p class="fw-bold text-danger"> <i class="bi bi-exclamation-triangle-fill text-danger me-2"></i> Achtung: Diese Aktion kann nicht rückgängig gemacht werden!</p>
        <p class="text-muted">Dein Konto wird unwiderruflich gelöscht. Das Fahrerprofil, Trainings und Ergebnisse bleiben erhalten. Einige Daten werden nach der Löschung entsprechend der Datenschutzerklärung technisch befristet aufbewahrt.</p>
        <?php if (isset($deleteError)): ?>
            <div class="alert alert-danger" role="alert">
                Fehler! <?= escape($deleteError) ?>
            </div>
        <?php endif; ?>
        <form action="<?= escape(base_url('konto/loeschen')) ?>" method="post">
            <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">

            <div class="mb-3">
                <label class="form-label" for="current_password_deleteForm">Aktuelles Passwort</label>
                <input class="form-control" type="password" name="current_password" id="current_password_deleteForm" autocomplete="current-password" required>
            </div>

            <div class="mb-3">
                <label class="form-label" for="delete_confirmation">Zur Bestätigung LÖSCHEN eingeben</label>
                <input class="form-control" name="delete_confirmation" id="delete_confirmation" autocomplete="off" required>
            </div>

            <button class="btn btn-danger" type="submit">Konto endgültig löschen</button>
        </form>
    </section>

</div>
