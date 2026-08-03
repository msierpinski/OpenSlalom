<?php declare(strict_types=1); ?>
<section class="shell listing-page account-page-content">
    <div class="page-heading-row">
        <div><p class="eyebrow"><span></span> Persönlicher Bereich</p><h1>Mein Konto</h1></div>
    </div>

    <div class="account-grid">
        <section class="result-section account-summary">
            <h2>Kontodaten</h2>
            <dl>
                <div><dt>Benutzername</dt><dd><?= escape($currentUser['username']) ?></dd></div>
                <div><dt>E-Mail-Adresse</dt><dd><?= escape($currentUser['email'] ?? '-') ?></dd></div>
                <div><dt>Rolle<?= count($currentUser['roles']) === 1 ? '' : 'n' ?></dt><dd><?= escape(implode(', ', $currentUser['roles'])) ?></dd></div>
                <div><dt>Fahrerzuordnung</dt><dd><?= escape($driverName ?? '-') ?></dd></div>
            </dl>
        </section>

        <section class="result-section account-password">
            <h2>Passwort ändern</h2>
            <p>Nach der Änderung werden alle bestehenden Sitzungen beendet.</p>
            <?php if (isset($passwordError)): ?><div class="form-message error" role="alert"><?= escape($passwordError) ?></div><?php endif; ?>
            <form class="app-form" action="<?= escape(base_url('konto/passwort')) ?>" method="post">
                <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">
                <label><span>Aktuelles Passwort</span><input type="password" name="current_password" autocomplete="current-password" required></label>
                <label><span>Neues Passwort</span><input type="password" name="new_password" autocomplete="new-password" required minlength="12"></label>
                <label><span>Neues Passwort wiederholen</span><input type="password" name="password_confirmation" autocomplete="new-password" required minlength="12"></label>
                <button class="button button-primary rounded-action" type="submit">Passwort ändern</button>
            </form>
        </section>

        <section class="result-section account-delete">
            <h2>Konto löschen</h2>
            <p>Dein WebUI-Konto, Rollen und Passwort-Reset-Tokens werden unwiderruflich gelöscht. Fahrerprofile, Trainings und Ergebnisse bleiben erhalten. Sicherheitsprotokolle werden entsprechend der Datenschutzerklärung technisch befristet aufbewahrt.</p>
            <?php if (isset($deleteError)): ?><div class="form-message error" role="alert"><?= escape($deleteError) ?></div><?php endif; ?>
            <form class="app-form" action="<?= escape(base_url('konto/loeschen')) ?>" method="post">
                <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">
                <label><span>Aktuelles Passwort</span><input type="password" name="current_password" autocomplete="current-password" required></label>
                <label><span>Zur Bestätigung LÖSCHEN eingeben</span><input name="delete_confirmation" autocomplete="off" required></label>
                <button class="button button-danger rounded-action" type="submit">Konto endgültig löschen</button>
            </form>
        </section>
    </div>
</section>
