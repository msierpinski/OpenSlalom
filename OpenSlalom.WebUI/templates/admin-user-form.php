<?php declare(strict_types=1); ?>
<?php
$editMode ??= false;
$formValues ??= ['username' => '', 'email' => '', 'role' => 'Fahrer', 'fahrer_id' => null, 'is_active' => true];
$formAction = $editMode ? 'admin/benutzer/' . (int) $editedUserId : 'admin/benutzer';
?>
<section class="auth-page shell">
    <div class="auth-panel admin-form-panel">
        <p class="eyebrow"><span></span> Administration</p>
        <h1><?= $editMode ? 'Benutzer bearbeiten' : 'Benutzer anlegen' ?></h1>
        <p>Jeder Rolle kann optional ein Fahrer zugeordnet werden. Für die Rolle Fahrer ist die Zuordnung verpflichtend.</p>
        <?php if (isset($formError)): ?><div class="form-message error" role="alert"><?= escape($formError) ?></div><?php endif; ?>
        <form class="app-form" action="<?= escape(base_url($formAction)) ?>" method="post">
            <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">
            <label>Benutzername<input name="username" value="<?= escape($formValues['username']) ?>" autocomplete="username" required maxlength="100"></label>
            <label>E-Mail-Adresse<input type="email" name="email" value="<?= escape($formValues['email'] ?? '') ?>" autocomplete="email" required maxlength="254"></label>
            <label>Rolle
                <select name="role" id="role-select" required>
                    <?php foreach (['Administrator', 'Trainingsleiter', 'Fahrer', 'Registriert'] as $role): ?>
                        <option value="<?= escape($role) ?>" <?= $formValues['role'] === $role ? 'selected' : '' ?>><?= escape($role) ?></option>
                    <?php endforeach; ?>
                </select>
            </label>
            <label id="fahrer-select-wrapper">Fahrerzuordnung
                <select name="fahrer_id" id="fahrer-select">
                    <option value="">Bitte auswählen</option>
                    <?php foreach ($drivers as $driver): ?>
                        <option value="<?= (int) $driver['id'] ?>" <?= (int) ($formValues['fahrer_id'] ?? 0) === (int) $driver['id'] ? 'selected' : '' ?>><?= escape(display_name($driver['vorname'], $driver['nachname'])) ?></option>
                    <?php endforeach; ?>
                </select>
            </label>
            <?php if ($editMode): ?>
                <label class="checkbox-label"><input type="checkbox" name="is_active" value="1" <?= ($formValues['is_active'] ?? false) ? 'checked' : '' ?>><span>Benutzerkonto aktiv</span></label>
            <?php endif; ?>
            <label><?= $editMode ? 'Neues Passwort (optional)' : 'Passwort' ?><input type="password" name="password" autocomplete="new-password" <?= $editMode ? '' : 'required' ?> minlength="12"></label>
            <label>Passwort wiederholen<input type="password" name="password_confirmation" autocomplete="new-password" <?= $editMode ? '' : 'required' ?> minlength="12"></label>
            <div class="form-actions"><a class="button button-secondary" href="<?= escape(base_url('admin/benutzer')) ?>">Abbrechen</a><button class="button button-primary" type="submit"><?= $editMode ? 'Speichern' : 'Anlegen' ?></button></div>
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
