<?php

declare(strict_types=1);

// TODO: The table and overall layout is not really good. But i will do the work for now

?>

<section class="vstack gap-5">
    <header>
        <p class="text-muted mb-0">Administration</p>
        <h1 class="text-body-emphasis display-5 fw-bold mb-0">Benutzer</h1>
    </header>

    <div class="vstack gap-3">
        <a class="btn btn-primary w-fit-content" href="<?= escape(base_url('admin/benutzer/neu')) ?>"><i class="bi bi-person-add"></i> Benutzer anlegen</a>

        <?php if (isset($_SESSION['flash_success'])): ?>
            <div class="alert alert-success" role="alert"><?= escape((string) $_SESSION['flash_success']) ?></div>
            <?php unset($_SESSION['flash_success']); ?>

        <?php endif; ?>

        <?php
        $listPath = 'admin/benutzer';
        $showSearch = true;
        $showPagination = false;
        require_once __DIR__ . '/list-controls.php';
        ?>
    </div>

    <?php if (count($users) === 0): ?>
        <div class="alert alert-warning" role="alert"><i class="bi bi-person-x me-2"></i> Für den Suchbegriff "<?= escape($search) ?>" wurden keine Benutzer gefunden!</div>
    <?php else: ?>
        <div class="table-responsive">
            <table class="table table-striped table-hover">
                <thead>
                    <tr>
                        <th>Benutzername</th>
                        <th>E-Mail-Adresse</th>
                        <th>Rollen</th>
                        <th>Fahrer-ID</th>
                        <th>Status</th>
                        <th>Letzter Login</th>
                        <th>Aktion</th>
                    </tr>
                </thead>
                <tbody>
                    <?php foreach ($users as $user): ?>
                        <tr>
                            <td data-label="Benutzername"><strong><?= escape($user['username']) ?></strong></td>
                            <td data-label="E-Mail-Adresse"><?= escape($user['email'] ?? '-') ?></td>
                            <td data-label="Rollen"><?= escape($user['roles'] ?? '-') ?></td>
                            <td data-label="Fahrer-ID" class="font-monospace"><?= $user['fahrer_id'] === null ? '-' : (int) $user['fahrer_id'] ?></td>
                            <td data-label="Status"><span class="badge <?= (bool) $user['is_active'] ? 'text-bg-primary' : 'text-bg-danger' ?>"><?= (bool) $user['is_active'] ? 'Aktiv' : 'Inaktiv' ?></span></td>
                            <td data-label="Letzter Login"><?= escape(format_date($user['last_login_at_utc'], 'd.m.Y H:i')) ?></td>
                            <td data-label="Aktion"><a class="text-decoration-none" href="<?= escape(base_url('admin/benutzer/' . $user['id'] . '/bearbeiten')) ?>"><i class="bi bi-pencil"></i> Bearbeiten</a></td>
                        </tr>
                    <?php endforeach; ?>
                </tbody>
            </table>
        </div>
    <?php endif; ?>


    <?php

    $listPath = 'admin/benutzer';
    $showSearch = false;
    $showPagination = true;
    require_once __DIR__ . '/list-controls.php';

    ?>
</section>
