<?php

declare(strict_types=1);

$bootstrapStylesVersion = (string) filemtime(dirname(__DIR__) . '/assets/css/bootstrap.min.css');
$bootstrapIconsVersion = (string) filemtime(dirname(__DIR__) . '/assets/css/bootstrap-icons.min.css');
$bootstrapJavascriptVersion = (string) filemtime(dirname(__DIR__) . '/assets/js/bootstrap.bundle.min.js');
$customStylesVersion = (string) filemtime(dirname(__DIR__) . '/assets/css/app.css');
$colorSchemeScriptVersion = (string) filemtime(dirname(__DIR__) . '/assets/js/color-scheme.js');

?>

<!doctype html>
<html lang="de">

<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta name="description" content="<?= escape($pageDescription ?? '') ?>">
    <meta name="theme-color" content="#1f84de">
    <title><?= escape($pageTitle ?? 'openSlalom') ?></title>
    <link rel="icon" href="<?= escape(base_url('assets/img/logo.png')) ?>" type="image/png">
    <link rel="stylesheet" href="<?= escape(base_url('assets/css/bootstrap.min.css?v=' . $bootstrapStylesVersion)) ?>">
    <link rel="stylesheet" href="<?= escape(base_url('assets/css/bootstrap.icons.min.css?v=' . $bootstrapIconsVersion)) ?>">
    <link rel="stylesheet" href="<?= escape(base_url('assets/css/app.css?v=' . $customStylesVersion)) ?>">
</head>

<body class="<?= escape($pageClass ?? '') ?>">
    <a class="visually-hidden-focusable" href="#page-content">Zum Inhalt springen</a>


    <nav class="navbar navbar-expand-lg bg-body bg-opacity-75 border-bottom sticky-top shadow-sm blur" aria-label="Hauptnavigation">
        <div class="container-xxl">
            <a class="navbar-brand" href="<?= escape(base_url()) ?>" aria-label="openSlalom Startseite">
                <img src="<?= escape(base_url('assets/img/logo.png')) ?>" alt="" width="38" height="38">
                <span class="ms-3">openSlalom</span>
            </a>

            <button
                class="navbar-toggler"
                type="button"
                data-bs-toggle="collapse"
                data-bs-target="#mainNavbar"
                aria-controls="mainNavbar"
                aria-expanded="false"
                aria-label="Navigation umschalten">
                <span class="navbar-toggler-icon"></span>
            </button>

            <div class="collapse navbar-collapse" id="mainNavbar">
                <ul class="navbar-nav me-auto mb-2 mb-lg-0">
                    <?php if ($currentUser !== null && ($pageClass ?? '') !== 'home-page'): ?>
                        <?php $currentPath = request_path(); ?>

                        <li class="nav-item">
                            <a
                                class="nav-link <?= str_starts_with($currentPath, '/training') ? 'active' : '' ?>"
                                href="<?= escape(base_url('trainings')) ?>"
                                <?= str_starts_with($currentPath, '/training') ? 'aria-current="page"' : '' ?>>
                                Trainings
                            </a>
                        </li>

                        <?php if (Auth::canManageMasterData($currentUser)): ?>
                            <li class="nav-item">
                                <a
                                    class="nav-link <?= str_starts_with($currentPath, '/statistiken') ? 'active' : '' ?>"
                                    href="<?= escape(base_url('statistiken')) ?>"
                                    <?= str_starts_with($currentPath, '/statistiken') ? 'aria-current="page"' : '' ?>>
                                    Statistiken
                                </a>
                            </li>

                            <li class="nav-item dropdown">
                                <a
                                    class="nav-link dropdown-toggle"
                                    href="#"
                                    role="button"
                                    data-bs-toggle="dropdown"
                                    aria-expanded="false">
                                    Verwaltung
                                </a>

                                <ul class="dropdown-menu">
                                    <li>
                                        <a class="dropdown-item <?= str_starts_with($currentPath, '/verwaltung/vereine') ? 'active' : '' ?>"
                                            href="<?= escape(base_url('verwaltung/vereine')) ?>">
                                            Vereine
                                        </a>
                                    </li>
                                    <li>
                                        <a class="dropdown-item <?= str_starts_with($currentPath, '/verwaltung/fahrer') ? 'active' : '' ?>"
                                            href="<?= escape(base_url('verwaltung/fahrer')) ?>">
                                            Fahrer
                                        </a>
                                    </li>
                                    <li>
                                        <a class="dropdown-item <?= str_starts_with($currentPath, '/verwaltung/disziplinen') ? 'active' : '' ?>"
                                            href="<?= escape(base_url('verwaltung/disziplinen')) ?>">
                                            Disziplinen
                                        </a>
                                    </li>
                                    <li>
                                        <a class="dropdown-item <?= str_starts_with($currentPath, '/verwaltung/karts') ? 'active' : '' ?>"
                                            href="<?= escape(base_url('verwaltung/karts')) ?>">
                                            Karts
                                        </a>
                                    </li>
                                    <li>
                                        <a class="dropdown-item <?= str_starts_with($currentPath, '/verwaltung/wetter') ? 'active' : '' ?>"
                                            href="<?= escape(base_url('verwaltung/wetter')) ?>">
                                            Wetter
                                        </a>
                                    </li>

                                    <?php if (Auth::hasRole($currentUser, 'Administrator')): ?>
                                        <li>
                                            <a class="dropdown-item <?= str_starts_with($currentPath, '/admin/benutzer') ? 'active' : '' ?>"
                                                href="<?= escape(base_url('admin/benutzer')) ?>">
                                                Benutzer
                                            </a>
                                        </li>
                                    <?php endif; ?>
                                </ul>
                            </li>
                        <?php endif; ?>
                    <?php endif; ?>
                </ul>

                <?php if (($pageClass ?? '') === 'home-page'): ?>
                    <a class="btn btn-primary" href="<?= escape(base_url($currentUser === null ? 'login' : 'trainings')) ?>">
                        <i class="bi bi-arrow-right" aria-hidden="true"></i>
                        <span><?= $currentUser === null ? 'Anmelden' : 'Interner Bereich' ?></span>
                    </a>
                <?php else: ?>
                    <ul class="navbar-nav mb-2 mb-lg-0" aria-label="Benutzerkonto">
                        <?php if ($currentUser === null): ?>
                            <li class="nav-item">
                                <a class="nav-link" href="<?= escape(base_url('login')) ?>">
                                    <i class="bi bi-arrow-right" aria-hidden="true"></i>
                                    <span>Anmelden</span>
                                </a>
                            </li>
                        <?php else: ?>
                            <li class="nav-item">
                                <a class="nav-link d-flex align-items-center gap-2"
                                    href="<?= escape(base_url('konto')) ?>">
                                    <i class="bi bi-person-circle fs-2" aria-hidden="true"></i>
                                    <span><?= escape($currentUser['username']) ?></span>
                                </a>
                            </li>

                            <li class="nav-item d-flex align-items-center">
                                <form action="<?= escape(base_url('logout')) ?>" method="post">
                                    <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">
                                    <button class="btn btn-sm btn-outline-danger ms-lg-3" type="submit">
                                        Abmelden
                                    </button>
                                </form>
                            </li>
                        <?php endif; ?>

                    </ul>
                <?php endif; ?>
                <ul class="navbar-nav ms-lg-3 mb-2 mb-lg-0">
                    <li class="nav-item">
                        <button
                            class="btn btn-sm btn-link"
                            id="theme-toggle"
                            type="button"
                            aria-label="Farbschema wechseln"
                            title="Farbschema wechseln">
                            <i class="bi bi-moon-stars fs-5" id="theme-toggle-icon" aria-hidden="true"></i>
                        </button>
                    </li>
                </ul>
            </div>
        </div>
    </nav>

    <main class="bg-body-tertiary" id="page-content">
        <div class="container-xxl py-4 py-lg-5">
            <?php require_once $contentTemplate; ?>
        </div>
    </main>

    <footer class="border-top px-3 px-lg-4 py-4 py-lg-5 text-center text-lg-start vstack gap-4 flex-lg-row align-items-center justify-content-lg-between">
        <div class="d-flex flex-column">
            <strong class="fs-3 text-body-emphasis">openSlalom</strong>
            <span>Digitale Trainingsorganisation für den Kart-Slalom.</span>
        </div>

        <nav class="nav justify-content-center justify-content-lg-start" aria-label="Rechtliche Informationen">
            <a class="nav-link" href="<?= escape(base_url('datenschutz')) ?>">Datenschutz</a>
            <a class="nav-link" href="<?= escape(base_url('impressum')) ?>">Impressum</a>
        </nav>
    </footer>



    <script src="<?= escape(base_url('assets/js/color-scheme.js?v=' . $colorSchemeScriptVersion)) ?>" defer></script>
    <script src="<?= escape(base_url('assets/js/bootstrap.bundle.min.js?v=' . $bootstrapJavascriptVersion)) ?>" defer></script>
</body>

</html>
