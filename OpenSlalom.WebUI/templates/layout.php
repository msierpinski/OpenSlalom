<?php
declare(strict_types=1);

$stylesheetVersion = (string) filemtime(dirname(__DIR__) . '/assets/css/app.css');
?>
<!doctype html>
<html lang="de">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta name="description" content="<?= escape($pageDescription ?? '') ?>">
    <meta name="theme-color" content="#1f84de">
    <title><?= escape($pageTitle ?? 'openSlalom') ?></title>
    <link rel="icon" href="<?= escape(base_url('assets/img/logo.svg')) ?>" type="image/svg+xml">
    <link rel="stylesheet" href="<?= escape(base_url('assets/css/app.css?v=' . $stylesheetVersion)) ?>">
</head>
<body class="<?= escape($pageClass ?? '') ?>">
    <a class="skip-link" href="#content">Zum Inhalt springen</a>
    <header class="site-header">
        <div class="shell header-inner">
            <a class="brand" href="<?= escape(base_url()) ?>" aria-label="openSlalom Startseite">
                <img src="<?= escape(base_url('assets/img/logo.svg')) ?>" alt="" width="38" height="38">
                <span>openSlalom</span>
            </a>
            <?php if (($pageClass ?? '') === 'home-page'): ?>
                <a class="header-cta" href="<?= escape(base_url($currentUser === null ? 'login' : 'trainings')) ?>">
                    <span><?= $currentUser === null ? 'Anmelden' : 'Interner Bereich' ?></span>
                    <i aria-hidden="true">→</i>
                </a>
            <?php else: ?>
                <nav class="header-nav" aria-label="Hauptnavigation">
                    <?php if ($currentUser === null): ?>
                        <a class="header-cta compact" href="<?= escape(base_url('login')) ?>"><span>Anmelden</span><i aria-hidden="true">→</i></a>
                    <?php else: ?>
                        <a class="account-name" href="<?= escape(base_url('konto')) ?>"><?= escape($currentUser['username']) ?></a>
                        <form action="<?= escape(base_url('logout')) ?>" method="post">
                            <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">
                            <button type="submit">Abmelden</button>
                        </form>
                    <?php endif; ?>
                </nav>
            <?php endif; ?>
        </div>
    </header>

    <?php if ($currentUser !== null && ($pageClass ?? '') !== 'home-page'): ?>
        <?php $currentPath = request_path(); ?>
        <nav class="internal-menu" aria-label="Interner Bereich">
            <div class="shell internal-menu-inner">
                <a class="<?= str_starts_with($currentPath, '/training') ? 'active' : '' ?>" href="<?= escape(base_url('trainings')) ?>">Trainings</a>
                <?php if (Auth::canManageMasterData($currentUser)): ?>
                    <a class="<?= str_starts_with($currentPath, '/verwaltung/vereine') ? 'active' : '' ?>" href="<?= escape(base_url('verwaltung/vereine')) ?>">Vereine</a>
                    <a class="<?= str_starts_with($currentPath, '/verwaltung/fahrer') ? 'active' : '' ?>" href="<?= escape(base_url('verwaltung/fahrer')) ?>">Fahrer</a>
                    <a class="<?= str_starts_with($currentPath, '/verwaltung/disziplinen') ? 'active' : '' ?>" href="<?= escape(base_url('verwaltung/disziplinen')) ?>">Disziplinen</a>
                    <a class="<?= str_starts_with($currentPath, '/verwaltung/karts') ? 'active' : '' ?>" href="<?= escape(base_url('verwaltung/karts')) ?>">Karts</a>
                    <a class="<?= str_starts_with($currentPath, '/verwaltung/wetter') ? 'active' : '' ?>" href="<?= escape(base_url('verwaltung/wetter')) ?>">Wetter</a>
                <?php endif; ?>
                <?php if (Auth::hasRole($currentUser, 'Administrator')): ?>
                    <a class="<?= str_starts_with($currentPath, '/admin/benutzer') ? 'active' : '' ?>" href="<?= escape(base_url('admin/benutzer')) ?>">Benutzer</a>
                <?php endif; ?>
                <a class="<?= str_starts_with($currentPath, '/konto') ? 'active' : '' ?>" href="<?= escape(base_url('konto')) ?>">Eigenes Konto</a>
            </div>
        </nav>
    <?php endif; ?>

    <main id="content">
        <?php require $contentTemplate; ?>
    </main>

    <footer class="site-footer">
        <div class="shell footer-inner">
            <div>
                <strong>openSlalom</strong>
                <span>Digitale Trainingsorganisation für den Kart-Slalom.</span>
            </div>
            <nav class="footer-links" aria-label="Rechtliche Informationen">
                <a href="<?= escape(base_url('impressum')) ?>">Impressum</a>
                <a href="<?= escape(base_url('datenschutz')) ?>">Datenschutz</a>
            </nav>
        </div>
    </footer>
</body>
</html>
