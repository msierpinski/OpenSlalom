<?php

declare(strict_types=1);

$editMode ??= false;
$formValues ??= ['username' => '', 'email' => '', 'role' => 'Fahrer', 'fahrer_id' => null, 'is_active' => true];
$formAction = $editMode ? 'admin/benutzer/' . (int) $editedUserId : 'admin/benutzer';

?>

<section class="vstack gap-5">
    <header>
        <p class="text-muted mb-0">Administration</p>
        <h1 class="text-body-emphasis display-5 fw-bold mb-0"><?= $editMode ? 'Benutzer bearbeiten' : 'Benutzer anlegen' ?></h1>
    </header>

    <div class="card card-body border-0 shadow-sm">
        <div class="alert alert-info" role="alert">
            <i class="bi bi-info-circle-fill me-2"></i>
            Jeder Rolle kann optional ein Fahrer zugeordnet werden. Für die Rolle Fahrer ist die Zuordnung verpflichtend.
        </div>

        <?php if (isset($formError)): ?><div class="alert alert-danger" role="alert">Fehler! <?= escape($formError) ?></div><?php endif; ?>

        <form action="<?= escape(base_url($formAction)) ?>" method="post">
            <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">

            <div class="mb-3">
                <label class="form-label" for="username">Benutzername</label>
                <input class="form-control" id="username" name="username" value="<?= escape($formValues['username']) ?>" autocomplete="username" required maxlength="100">
            </div>

            <div class="mb-3">
                <label class="form-label" for="email">E-Mail-Adresse</label>
                <input class="form-control" type="email" id="email" name="email" value="<?= escape($formValues['email'] ?? '') ?>" autocomplete="email" required maxlength="254">
            </div>

            <div class="mb-3">
                <label class="form-label" for="role-select">Rolle</label>
                <select class="form-select" name="role" id="role-select" required>
                    <?php foreach (['Administrator', 'Trainingsleiter', 'Fahrer', 'Registriert'] as $role): ?>
                        <option value="<?= escape($role) ?>" <?= $formValues['role'] === $role ? 'selected' : '' ?>><?= escape($role) ?></option>
                    <?php endforeach; ?>
                </select>
            </div>

            <div class="mb-3" id="fahrer-select-wrapper">
                <label class="form-label" for="fahrer-select">Fahrerzuordnung</label>
                <select class="form-select" name="fahrer_id" id="fahrer-select">
                    <option value="">Bitte auswählen</option>
                    <?php foreach ($drivers as $driver): ?>
                        <option value="<?= (int) $driver['id'] ?>" <?= (int) ($formValues['fahrer_id'] ?? 0) === (int) $driver['id'] ? 'selected' : '' ?>>
                            <?= escape(display_name($driver['vorname'], $driver['nachname'])) ?>
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>

            <?php if ($editMode): ?>
                <div class="form-check mb-3">
                    <label class="form-check-label" for="is-active">Benutzerkonto aktiv</label>
                    <input class="form-check-input" type="checkbox" id="is-active" name="is_active" value="1" <?= ($formValues['is_active'] ?? false) ? 'checked' : '' ?>>
                </div>
            <?php endif; ?>

            <div class="mb-3">
                <label class="form-label" for="password"><?= $editMode ? 'Neues Passwort (optional)' : 'Passwort' ?></label>
                <input class="form-control" id="password" type="password" name="password" autocomplete="new-password" <?= $editMode ? '' : 'required' ?> minlength="12">
            </div>
            <div class="mb-3">
                <label class="form-label" for="password_confirmation">Passwort wiederholen</label>
                <input class="form-control" id="password_confirmation" type="password" name="password_confirmation" autocomplete="new-password" <?= $editMode ? '' : 'required' ?> minlength="12">
            </div>

            <div class="d-flex gap-2">
                <a class="btn btn-secondary" href="<?= escape(base_url('admin/benutzer')) ?>">Abbrechen</a>
                <button class="btn btn-primary" type="submit"><?= $editMode ? 'Speichern' : 'Anlegen' ?></button>
            </div>
        </form>
    </div>
</section>

<script>
    (() => {
        const role = document.querySelector('#role-select');
        const wrapper = document.querySelector('#fahrer-select-wrapper');
        const driver = document.querySelector('#fahrer-select');
        const update = () => {
            const required = role.value === 'Fahrer';
            driver.required = required;
            wrapper.querySelector('select').setAttribute('aria-required', required ? 'true' : 'false');
        };
        role.addEventListener('change', update);
        update();
    })();
</script>
