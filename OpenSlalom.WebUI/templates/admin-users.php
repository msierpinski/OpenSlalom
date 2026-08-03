<?php declare(strict_types=1); ?>
<section class="shell listing-page admin-users-page">
    <div class="page-heading-row">
        <div><p class="eyebrow"><span></span> Administration</p><h1>Benutzer</h1></div>
        <a class="button button-primary" href="<?= escape(base_url('admin/benutzer/neu')) ?>">Benutzer anlegen</a>
    </div>
    <?php if (isset($_SESSION['flash_success'])): ?>
        <div class="form-message success"><?= escape((string) $_SESSION['flash_success']) ?></div>
        <?php unset($_SESSION['flash_success']); ?>
    <?php endif; ?>
    <?php $listPath = 'admin/benutzer'; $showSearch = true; $showPagination = false; require __DIR__ . '/list-controls.php'; ?>
    <div class="table-frame admin-users-table">
        <table>
            <thead><tr><th>Benutzername</th><th>E-Mail-Adresse</th><th>Rollen</th><th>Fahrer-ID</th><th>Status</th><th>Letzter Login</th><th>Aktion</th></tr></thead>
            <tbody>
            <?php foreach ($users as $user): ?>
                <tr>
                    <td data-label="Benutzername"><strong><?= escape($user['username']) ?></strong></td>
                    <td data-label="E-Mail-Adresse"><?= escape($user['email'] ?? '-') ?></td>
                    <td data-label="Rollen"><?= escape($user['roles'] ?? '-') ?></td>
                    <td data-label="Fahrer-ID"><?= $user['fahrer_id'] === null ? '-' : (int) $user['fahrer_id'] ?></td>
                    <td data-label="Status"><span class="account-state <?= (bool) $user['is_active'] ? '' : 'inactive' ?>"><?= (bool) $user['is_active'] ? 'Aktiv' : 'Inaktiv' ?></span></td>
                    <td data-label="Letzter Login"><?= escape(format_date($user['last_login_at_utc'], 'd.m.Y H:i')) ?></td>
                    <td data-label="Aktion"><a class="table-action" href="<?= escape(base_url('admin/benutzer/' . $user['id'] . '/bearbeiten')) ?>">Bearbeiten</a></td>
                </tr>
            <?php endforeach; ?>
            </tbody>
        </table>
    </div>
    <?php $listPath = 'admin/benutzer'; $showSearch = false; $showPagination = true; require __DIR__ . '/list-controls.php'; ?>
</section>
