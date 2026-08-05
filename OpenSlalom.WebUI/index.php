<?php

declare(strict_types=1);

require __DIR__ . '/src/helpers.php';
require __DIR__ . '/src/Database.php';
require __DIR__ . '/src/TrainingRepository.php';
require __DIR__ . '/src/TrainingManagementRepository.php';
require __DIR__ . '/src/MasterDataRepository.php';
require __DIR__ . '/src/GlobalStatisticsRepository.php';
require __DIR__ . '/src/TrainingViewBuilder.php';
require __DIR__ . '/src/UserRepository.php';
require __DIR__ . '/src/Auth.php';
require __DIR__ . '/src/PasswordResetMailer.php';
require __DIR__ . '/src/AdminNotificationMailer.php';

Auth::startSession();

$path = request_path();
$method = $_SERVER['REQUEST_METHOD'] ?? 'GET';
$isReadRequest = $method === 'GET' || $method === 'HEAD';
$currentUser = null;
$publicConfig = [];
$publicConfigPath = __DIR__ . '/config.php';
if (is_file($publicConfigPath)) {
    try {
        $loadedPublicConfig = require $publicConfigPath;
        $publicConfig = is_array($loadedPublicConfig) ? $loadedPublicConfig : [];
    } catch (Throwable $exception) {
        error_log(sprintf('OpenSlalom.WebUI: Öffentliche Konfiguration konnte nicht geladen werden: %s', $exception->getMessage()));
    }
}
$services = static function () use (&$currentUser): array {
    static $instance = null;
    if ($instance !== null) {
        return $instance;
    }

    $configPath = __DIR__ . '/config.php';
    if (!is_file($configPath)) {
        throw new RuntimeException('Die WebUI ist noch nicht mit der Datenbank verbunden.');
    }
    $config = require $configPath;
    if (!is_array($config)) {
        throw new RuntimeException('Die WebUI-Konfiguration ist ungültig.');
    }

    $readerConnection = Database::connect($config['database'] ?? []);
    $authConnection = Database::connect($config['auth_database'] ?? $config['database'] ?? []);
    $writeConnection = Database::connect($config['write_database'] ?? $config['database'] ?? []);
    $users = new UserRepository($authConnection, $readerConnection);
    $auth = new Auth($users);
    $currentUser = $auth->currentUser();

    return $instance = [
        'config' => $config,
        'auth' => $auth,
        'users' => $users,
        'trainings' => new TrainingRepository($readerConnection),
        'trainingManagement' => new TrainingManagementRepository($writeConnection),
        'masterData' => new MasterDataRepository($writeConnection),
        'statistics' => new GlobalStatisticsRepository($readerConnection),
        'mailer' => new PasswordResetMailer($config['mail'] ?? ['name' => $config['site']['name'] ?? 'openSlalom']),
        'adminMailer' => new AdminNotificationMailer($config['mail'] ?? ['name' => $config['site']['name'] ?? 'openSlalom']),
    ];
};

if ($path === '/' && $isReadRequest) {
    try {
        $app = $services();
        $currentUser = $app['auth']->currentUser();
    } catch (Throwable $exception) {
        error_log(sprintf('OpenSlalom.WebUI: Anmeldung auf der Startseite nicht verfügbar: %s', $exception->getMessage()));
        $currentUser = null;
    }

    render('home', [
        'pageTitle' => 'openSlalom | Kart-Slalom digital organisiert',
        'pageDescription' => 'openSlalom verbindet Trainingsorganisation, Zeitnahme, Strafen, Stints und Statistiken in einer Anwendung.',
        'pageClass' => 'home-page',
    ]);
}

if (($path === '/impressum' || $path === '/datenschutz') && $isReadRequest) {
    try {
        $app = $services();
        $currentUser = $app['auth']->currentUser();
    } catch (Throwable $exception) {
        $currentUser = null;
    }

    $isImprint = $path === '/impressum';
    render($isImprint ? 'imprint' : 'privacy', [
        'pageTitle' => ($isImprint ? 'Impressum' : 'Datenschutz') . ' | openSlalom',
        'pageDescription' => $isImprint ? 'Impressum und rechtliche Hinweise für openSlalom.' : 'Datenschutzerklärung für die openSlalom-WebUI.',
        'pageClass' => 'legal-page',
        'legal' => $publicConfig['legal'] ?? [],
    ]);
}

try {
    $app = $services();
    $currentUser = $app['auth']->currentUser();

    if ($path === '/login' && $isReadRequest) {
        if ($currentUser !== null) {
            redirect('trainings');
        }

        render('login', [
            'pageTitle' => 'Anmelden | openSlalom',
            'pageDescription' => 'Anmeldung für die geschützte OpenSlalom-WebUI.',
            'pageClass' => 'login-page',
        ]);
    }

    if ($path === '/login' && $method === 'POST') {
        require_valid_csrf();
        $login = is_string($_POST['login'] ?? null) ? $_POST['login'] : '';
        $password = is_string($_POST['password'] ?? null) ? $_POST['password'] : '';
        if ($app['auth']->attempt($login, $password, client_ip())) {
            redirect('trainings');
        }

        render('login', [
            'pageTitle' => 'Anmelden | openSlalom',
            'pageDescription' => 'Anmeldung für die geschützte OpenSlalom-WebUI.',
            'pageClass' => 'login-page',
            'loginError' => 'Benutzername oder Passwort ungültig. Bitte versuche es später erneut.',
            'login' => trim($login),
        ], 422);
    }

    if ($path === '/registrieren' && $isReadRequest) {
        if ($currentUser !== null) {
            redirect('trainings');
        }
        render('register', [
            'pageTitle' => 'Registrieren | openSlalom',
            'pageDescription' => 'Neues Benutzerkonto für die OpenSlalom-WebUI registrieren.',
            'pageClass' => 'login-page',
        ]);
    }

    if ($path === '/registrieren' && $method === 'POST') {
        require_valid_csrf();
        $username = is_string($_POST['username'] ?? null) ? trim($_POST['username']) : '';
        $email = is_string($_POST['email'] ?? null) ? strtolower(trim($_POST['email'])) : '';
        $password = is_string($_POST['password'] ?? null) ? $_POST['password'] : '';
        $confirmation = is_string($_POST['password_confirmation'] ?? null) ? $_POST['password_confirmation'] : '';
        $acceptedPrivacy = isset($_POST['accept_privacy']);
        $honeypot = is_string($_POST['website'] ?? null) ? trim($_POST['website']) : '';
        $formValues = ['username' => $username, 'email' => $email];

        if ($honeypot !== '') {
            render('register', [
                'pageTitle' => 'Registrierung abgeschlossen | openSlalom',
                'pageDescription' => 'Registrierung für die OpenSlalom-WebUI.',
                'pageClass' => 'login-page',
                'registrationSuccessful' => true,
            ]);
        }

        $registrationError = null;
        if (!$acceptedPrivacy) {
            $registrationError = 'Bitte bestätige, dass du die Datenschutzerklärung gelesen hast.';
        } elseif (strlen($password) < 12 || !hash_equals($password, $confirmation)) {
            $registrationError = 'Das Passwort muss mindestens 12 Zeichen enthalten und mit der Wiederholung übereinstimmen.';
        }

        try {
            if ($registrationError !== null) {
                throw new InvalidArgumentException($registrationError);
            }
            $app['users']->registerUser($username, $email, $password, client_ip());
            $administratorEmails = $app['users']->listAdministratorEmails();
            if (!$app['adminMailer']->sendRegistration($administratorEmails, $username, $email)) {
                error_log('OpenSlalom.WebUI: Administratoren konnten nicht über die neue Registrierung informiert werden.');
            }

            render('register', [
                'pageTitle' => 'Registrierung abgeschlossen | openSlalom',
                'pageDescription' => 'Registrierung für die OpenSlalom-WebUI.',
                'pageClass' => 'login-page',
                'registrationSuccessful' => true,
            ]);
        } catch (Throwable $exception) {
            $message = $exception instanceof PDOException
                ? 'Benutzername oder E-Mail-Adresse ist bereits vergeben.'
                : ($exception instanceof InvalidArgumentException || $exception instanceof RuntimeException
                    ? $exception->getMessage()
                    : 'Die Registrierung konnte nicht abgeschlossen werden.');
            render('register', [
                'pageTitle' => 'Registrieren | openSlalom',
                'pageDescription' => 'Neues Benutzerkonto für die OpenSlalom-WebUI registrieren.',
                'pageClass' => 'login-page',
                'registrationError' => $message,
                'formValues' => $formValues,
            ], 422);
        }
    }

    if ($path === '/passwort-vergessen' && $isReadRequest) {
        render('forgot-password', [
            'pageTitle' => 'Passwort vergessen | openSlalom',
            'pageDescription' => 'Passwort für die OpenSlalom-WebUI zurücksetzen.',
            'pageClass' => 'login-page',
        ]);
    }

    if ($path === '/passwort-vergessen' && $method === 'POST') {
        require_valid_csrf();
        $email = is_string($_POST['email'] ?? null) ? strtolower(trim($_POST['email'])) : '';
        if (filter_var($email, FILTER_VALIDATE_EMAIL)) {
            $token = $app['users']->createPasswordResetToken($email);
            if ($token !== null && !$app['mailer']->send($email, $token)) {
                error_log('OpenSlalom.WebUI: Passwort-Reset-E-Mail konnte nicht versendet werden.');
            }
        }

        render('forgot-password', [
            'pageTitle' => 'Passwort vergessen | openSlalom',
            'pageDescription' => 'Passwort für die OpenSlalom-WebUI zurücksetzen.',
            'pageClass' => 'login-page',
            'requestSent' => true,
        ]);
    }

    if ($path === '/passwort-zuruecksetzen' && $isReadRequest) {
        render('reset-password', [
            'pageTitle' => 'Neues Passwort | openSlalom',
            'pageDescription' => 'Neues Passwort für die OpenSlalom-WebUI festlegen.',
            'pageClass' => 'login-page',
            'token' => is_string($_GET['token'] ?? null) ? $_GET['token'] : '',
        ]);
    }

    if ($path === '/passwort-zuruecksetzen' && $method === 'POST') {
        require_valid_csrf();
        $token = is_string($_POST['token'] ?? null) ? $_POST['token'] : '';
        $password = is_string($_POST['password'] ?? null) ? $_POST['password'] : '';
        $confirmation = is_string($_POST['password_confirmation'] ?? null) ? $_POST['password_confirmation'] : '';
        if (strlen($password) < 12 || !hash_equals($password, $confirmation)) {
            render('reset-password', [
                'pageTitle' => 'Neues Passwort | openSlalom',
                'pageDescription' => 'Neues Passwort für die OpenSlalom-WebUI festlegen.',
                'pageClass' => 'login-page',
                'token' => $token,
                'resetError' => 'Das Passwort muss mindestens 12 Zeichen enthalten und mit der Wiederholung übereinstimmen.',
            ], 422);
        }

        if (!$app['users']->resetPassword($token, $password)) {
            render('reset-password', [
                'pageTitle' => 'Neues Passwort | openSlalom',
                'pageDescription' => 'Neues Passwort für die OpenSlalom-WebUI festlegen.',
                'pageClass' => 'login-page',
                'token' => '',
                'resetError' => 'Der Link ist ungültig oder abgelaufen. Bitte fordere einen neuen Link an.',
            ], 422);
        }

        render('reset-password', [
            'pageTitle' => 'Passwort geändert | openSlalom',
            'pageDescription' => 'Das Passwort wurde erfolgreich geändert.',
            'pageClass' => 'login-page',
            'resetSuccessful' => true,
            'token' => '',
        ]);
    }

    if ($path === '/logout' && $method === 'POST') {
        require_valid_csrf();
        $app['auth']->logout();
        redirect('');
    }

    if ($path === '/konto' && $isReadRequest) {
        requireAuthenticated($currentUser);
        render('account', [
            'pageTitle' => 'Mein Konto | openSlalom',
            'pageDescription' => 'Konto und Sicherheitseinstellungen verwalten.',
            'pageClass' => 'account-page',
            'driverName' => $app['users']->driverDisplayName($currentUser['fahrer_id']),
        ]);
    }

    if ($path === '/konto/passwort' && $method === 'POST') {
        requireAuthenticated($currentUser);
        require_valid_csrf();
        $currentPassword = is_string($_POST['current_password'] ?? null) ? $_POST['current_password'] : '';
        $newPassword = is_string($_POST['new_password'] ?? null) ? $_POST['new_password'] : '';
        $confirmation = is_string($_POST['password_confirmation'] ?? null) ? $_POST['password_confirmation'] : '';

        try {
            if (!hash_equals($newPassword, $confirmation)) {
                throw new InvalidArgumentException('Das neue Passwort stimmt nicht mit der Wiederholung überein.');
            }
            $app['users']->changeOwnPassword((int) $currentUser['id'], $currentPassword, $newPassword);
            $app['auth']->logout();
            render('login', [
                'pageTitle' => 'Passwort geändert | openSlalom',
                'pageDescription' => 'Das Passwort wurde geändert.',
                'pageClass' => 'login-page',
                'passwordChanged' => true,
            ]);
        } catch (InvalidArgumentException $exception) {
            render('account', [
                'pageTitle' => 'Mein Konto | openSlalom',
                'pageDescription' => 'Konto und Sicherheitseinstellungen verwalten.',
                'pageClass' => 'account-page',
                'driverName' => $app['users']->driverDisplayName($currentUser['fahrer_id']),
                'passwordError' => $exception->getMessage(),
            ], 422);
        }
    }

    if ($path === '/konto/loeschen' && $method === 'POST') {
        requireAuthenticated($currentUser);
        require_valid_csrf();
        $currentPassword = is_string($_POST['current_password'] ?? null) ? $_POST['current_password'] : '';
        $confirmation = is_string($_POST['delete_confirmation'] ?? null) ? trim($_POST['delete_confirmation']) : '';

        try {
            if ($confirmation !== 'LÖSCHEN') {
                throw new InvalidArgumentException('Bitte bestätige die Löschung durch die Eingabe von LÖSCHEN.');
            }
            $app['users']->deleteOwnAccount((int) $currentUser['id'], $currentPassword);
            $app['auth']->logout();
            render('account-deleted', [
                'pageTitle' => 'Konto gelöscht | openSlalom',
                'pageDescription' => 'Das WebUI-Konto wurde gelöscht.',
                'pageClass' => 'login-page',
            ]);
        } catch (InvalidArgumentException $exception) {
            render('account', [
                'pageTitle' => 'Mein Konto | openSlalom',
                'pageDescription' => 'Konto und Sicherheitseinstellungen verwalten.',
                'pageClass' => 'account-page',
                'driverName' => $app['users']->driverDisplayName($currentUser['fahrer_id']),
                'deleteError' => $exception->getMessage(),
            ], 422);
        }
    }

    if ($path === '/trainings' && $isReadRequest) {
        requireAuthenticated($currentUser);
        $options = list_options();
        $result = $app['trainings']->findVisibleTrainings($currentUser, $options['search'], $options['page'], $options['per_page']);
        render('trainings', [
            'pageTitle' => 'Trainings | openSlalom',
            'pageDescription' => 'Zugängliche Trainings in openSlalom.',
            'pageClass' => 'trainings-page',
            'trainings' => $result['items'],
            'pagination' => $result['pagination'],
            'search' => $options['search'],
            'canManageTrainings' => Auth::canManageTrainings($currentUser),
        ]);
    }

    if ($path === '/statistiken' && $isReadRequest) {
        requireMasterDataManager($currentUser);
        try {
            $period = statistics_period();
            $statistics = $app['statistics']->build($period['from'], $period['to']);
            render('statistics', [
                'pageTitle' => 'Statistiken | openSlalom',
                'pageDescription' => 'Globale Trainingsstatistik für einen auswählbaren Zeitraum.',
                'pageClass' => 'statistics-page',
                'period' => $period,
                'statistics' => $statistics,
            ]);
        } catch (InvalidArgumentException $exception) {
            render('statistics', [
                'pageTitle' => 'Statistiken | openSlalom',
                'pageDescription' => 'Globale Trainingsstatistik für einen auswählbaren Zeitraum.',
                'pageClass' => 'statistics-page',
                'period' => ['from' => sprintf('%04d-01-01', (int) date('Y')), 'to' => sprintf('%04d-12-31', (int) date('Y'))],
                'statistics' => ['summary' => [], 'drivers' => [], 'karts' => []],
                'statisticsError' => $exception->getMessage(),
            ], 422);
        }
    }

    if (preg_match('~^/verwaltung/(vereine|fahrer|disziplinen|karts|wetter)$~', $path, $matches) === 1 && $isReadRequest) {
        requireMasterDataManager($currentUser);
        $type = $matches[1];
        $options = list_options();
        $result = $app['masterData']->list($type, $options['search'], $options['page'], $options['per_page']);
        render('master-data-list', [
            'pageTitle' => masterDataTitle($type) . ' | openSlalom',
            'pageDescription' => masterDataTitle($type) . ' verwalten.',
            'pageClass' => 'admin-page',
            'masterType' => $type,
            'masterTitle' => masterDataTitle($type),
            'items' => $result['items'],
            'pagination' => $result['pagination'],
            'search' => $options['search'],
        ]);
    }

    if (preg_match('~^/verwaltung/(vereine|fahrer|disziplinen|karts|wetter)/neu$~', $path, $matches) === 1 && $isReadRequest) {
        requireMasterDataManager($currentUser);
        $type = $matches[1];
        renderMasterDataForm($app, $type, false, null, masterDataDefaultValues($type));
    }

    if (preg_match('~^/verwaltung/(vereine|fahrer|disziplinen|karts|wetter)$~', $path, $matches) === 1 && $method === 'POST') {
        requireMasterDataManager($currentUser);
        require_valid_csrf();
        $type = $matches[1];
        $values = masterDataValuesFromPost($type);
        try {
            $id = $app['masterData']->create($type, $values, $_FILES);
            redirect('verwaltung/' . $type . '/' . $id . '/bearbeiten');
        } catch (InvalidArgumentException|PDOException $exception) {
            renderMasterDataForm($app, $type, false, null, $values, $exception instanceof InvalidArgumentException ? $exception->getMessage() : 'Der Datensatz konnte nicht gespeichert werden.');
        }
    }

    if (preg_match('~^/verwaltung/(vereine|fahrer|disziplinen|karts|wetter)/(\d+)/bearbeiten$~', $path, $matches) === 1 && $isReadRequest) {
        requireMasterDataManager($currentUser);
        $type = $matches[1];
        $id = (int) $matches[2];
        $item = $app['masterData']->find($type, $id);
        if ($item === null) renderNotFound();
        renderMasterDataForm($app, $type, true, $id, $item);
    }

    if (preg_match('~^/verwaltung/(vereine|fahrer|disziplinen|karts|wetter)/(\d+)$~', $path, $matches) === 1 && $method === 'POST') {
        requireMasterDataManager($currentUser);
        require_valid_csrf();
        $type = $matches[1];
        $id = (int) $matches[2];
        $values = masterDataValuesFromPost($type);
        try {
            if (!$app['masterData']->update($type, $id, $values, $_FILES)) renderNotFound();
            redirect('verwaltung/' . $type);
        } catch (InvalidArgumentException|PDOException $exception) {
            renderMasterDataForm($app, $type, true, $id, $values, $exception instanceof InvalidArgumentException ? $exception->getMessage() : 'Der Datensatz konnte nicht gespeichert werden.');
        }
    }

    if (preg_match('~^/verwaltung/(vereine|fahrer|disziplinen|karts|wetter)/(\d+)/loeschen$~', $path, $matches) === 1 && $isReadRequest) {
        requireMasterDataManager($currentUser);
        $type = $matches[1];
        $id = (int) $matches[2];
        $item = $app['masterData']->find($type, $id);
        if ($item === null) renderNotFound();
        render('master-data-delete', ['pageTitle' => masterDataTitle($type) . ' löschen | openSlalom', 'pageDescription' => 'Datensatz löschen.', 'pageClass' => 'admin-page', 'masterType' => $type, 'masterTitle' => masterDataTitle($type), 'item' => $item]);
    }

    if (preg_match('~^/verwaltung/(vereine|fahrer|disziplinen|karts|wetter)/(\d+)/loeschen$~', $path, $matches) === 1 && $method === 'POST') {
        requireMasterDataManager($currentUser);
        require_valid_csrf();
        $type = $matches[1];
        if (!$app['masterData']->delete($type, (int) $matches[2])) renderNotFound();
        redirect('verwaltung/' . $type);
    }

    if ($path === '/trainings/neu' && $isReadRequest) {
        requireTrainingManager($currentUser);
        render('training-form', [
            'pageTitle' => 'Training anlegen | openSlalom',
            'pageDescription' => 'Neues Training in openSlalom anlegen.',
            'pageClass' => 'trainings-page',
            'editMode' => false,
            'lookups' => $app['trainingManagement']->lookups(),
            'formValues' => ['name' => '', 'beschreibung' => '', 'zeitpunkt' => date('Y-m-d'), 'verein_id' => null, 'disziplin_id' => null, 'wetter_id' => null, 'training_abgeschlossen' => false, 'ist_veroeffentlicht' => false],
        ]);
    }

    if ($path === '/trainings' && $method === 'POST') {
        requireTrainingManager($currentUser);
        require_valid_csrf();
        $formValues = trainingFormValuesFromPost();
        try {
            $uuid = $app['trainingManagement']->create($formValues);
            redirect('training/' . $uuid);
        } catch (InvalidArgumentException|PDOException $exception) {
            render('training-form', [
                'pageTitle' => 'Training anlegen | openSlalom',
                'pageDescription' => 'Neues Training in openSlalom anlegen.',
                'pageClass' => 'trainings-page',
                'editMode' => false,
                'lookups' => $app['trainingManagement']->lookups(),
                'formValues' => $formValues,
                'formError' => $exception instanceof InvalidArgumentException ? $exception->getMessage() : 'Das Training konnte nicht gespeichert werden.',
            ], 422);
        }
    }

    if (preg_match('~^/training/([0-9a-f-]+)/bearbeiten$~i', $path, $matches) === 1 && $isReadRequest) {
        requireTrainingManager($currentUser);
        $uuid = strtolower($matches[1]);
        $training = $app['trainingManagement']->findByUuid($uuid);
        if ($training === null) {
            renderNotFound();
        }
        render('training-form', [
            'pageTitle' => 'Training bearbeiten | openSlalom',
            'pageDescription' => 'Training in openSlalom bearbeiten.',
            'pageClass' => 'trainings-page',
            'editMode' => true,
            'trainingUuid' => $uuid,
            'lookups' => $app['trainingManagement']->lookups(),
            'formValues' => $training,
        ]);
    }

    if (preg_match('~^/training/([0-9a-f-]+)$~i', $path, $matches) === 1 && $method === 'POST') {
        requireTrainingManager($currentUser);
        require_valid_csrf();
        $uuid = strtolower($matches[1]);
        $formValues = trainingFormValuesFromPost();
        try {
            if (!$app['trainingManagement']->update($uuid, $formValues)) {
                renderNotFound();
            }
            redirect('training/' . $uuid);
        } catch (InvalidArgumentException|PDOException $exception) {
            render('training-form', [
                'pageTitle' => 'Training bearbeiten | openSlalom',
                'pageDescription' => 'Training in openSlalom bearbeiten.',
                'pageClass' => 'trainings-page',
                'editMode' => true,
                'trainingUuid' => $uuid,
                'lookups' => $app['trainingManagement']->lookups(),
                'formValues' => $formValues,
                'formError' => $exception instanceof InvalidArgumentException ? $exception->getMessage() : 'Das Training konnte nicht gespeichert werden.',
            ], 422);
        }
    }

    if ($path === '/admin/benutzer' && $isReadRequest) {
        requireRole($currentUser, 'Administrator');
        $options = list_options();
        $result = $app['users']->listUsers($options['search'], $options['page'], $options['per_page']);
        render('admin-users', [
            'pageTitle' => 'Benutzerverwaltung | openSlalom',
            'pageDescription' => 'WebUI-Benutzer verwalten.',
            'pageClass' => 'admin-page',
            'users' => $result['items'],
            'pagination' => $result['pagination'],
            'search' => $options['search'],
        ]);
    }

    if ($path === '/admin/benutzer/neu' && $isReadRequest) {
        requireRole($currentUser, 'Administrator');
        render('admin-user-form', [
            'pageTitle' => 'Benutzer anlegen | openSlalom',
            'pageDescription' => 'Neuen WebUI-Benutzer anlegen.',
            'pageClass' => 'admin-page',
            'drivers' => $app['users']->listActiveDrivers(),
            'editMode' => false,
        ]);
    }

    if (preg_match('~^/admin/benutzer/(\d+)/bearbeiten$~', $path, $matches) === 1 && $isReadRequest) {
        requireRole($currentUser, 'Administrator');
        $editedUser = $app['users']->findUserForAdministration((int) $matches[1]);
        if ($editedUser === null) {
            renderNotFound();
        }
        render('admin-user-form', [
            'pageTitle' => 'Benutzer bearbeiten | openSlalom',
            'pageDescription' => 'WebUI-Benutzer bearbeiten.',
            'pageClass' => 'admin-page',
            'drivers' => $app['users']->listActiveDrivers(),
            'editMode' => true,
            'editedUserId' => (int) $editedUser['id'],
            'formValues' => $editedUser,
        ]);
    }

    if ($path === '/admin/benutzer' && $method === 'POST') {
        requireRole($currentUser, 'Administrator');
        require_valid_csrf();
        $username = is_string($_POST['username'] ?? null) ? $_POST['username'] : '';
        $email = is_string($_POST['email'] ?? null) ? $_POST['email'] : '';
        $password = is_string($_POST['password'] ?? null) ? $_POST['password'] : '';
        $passwordConfirmation = is_string($_POST['password_confirmation'] ?? null) ? $_POST['password_confirmation'] : '';
        $role = is_string($_POST['role'] ?? null) ? $_POST['role'] : '';
        $fahrerId = filter_input(INPUT_POST, 'fahrer_id', FILTER_VALIDATE_INT, ['options' => ['min_range' => 1]]) ?: null;

        if (mb_strlen($password) < 12 || !hash_equals($password, $passwordConfirmation)) {
            render('admin-user-form', [
                'pageTitle' => 'Benutzer anlegen | openSlalom',
                'pageDescription' => 'Neuen WebUI-Benutzer anlegen.',
                'pageClass' => 'admin-page',
                'drivers' => $app['users']->listActiveDrivers(),
                'editMode' => false,
                'formError' => 'Das Passwort muss mindestens 12 Zeichen enthalten und mit der Wiederholung übereinstimmen.',
                'formValues' => ['username' => trim($username), 'email' => trim($email), 'role' => $role, 'fahrer_id' => $fahrerId],
            ], 422);
        }

        try {
            $app['users']->createUser($username, $email, $password, $role, $fahrerId);
            $_SESSION['flash_success'] = 'Benutzer wurde angelegt.';
            redirect('admin/benutzer');
        } catch (InvalidArgumentException $exception) {
            render('admin-user-form', [
                'pageTitle' => 'Benutzer anlegen | openSlalom',
                'pageDescription' => 'Neuen WebUI-Benutzer anlegen.',
                'pageClass' => 'admin-page',
                'drivers' => $app['users']->listActiveDrivers(),
                'editMode' => false,
                'formError' => $exception->getMessage(),
                'formValues' => ['username' => trim($username), 'email' => trim($email), 'role' => $role, 'fahrer_id' => $fahrerId],
            ], 422);
        } catch (PDOException) {
            render('admin-user-form', [
                'pageTitle' => 'Benutzer anlegen | openSlalom',
                'pageDescription' => 'Neuen WebUI-Benutzer anlegen.',
                'pageClass' => 'admin-page',
                'drivers' => $app['users']->listActiveDrivers(),
                'editMode' => false,
                'formError' => 'Benutzername, E-Mail-Adresse oder Fahrerzuordnung ist bereits vergeben.',
                'formValues' => ['username' => trim($username), 'email' => trim($email), 'role' => $role, 'fahrer_id' => $fahrerId],
            ], 422);
        }
    }

    if (preg_match('~^/admin/benutzer/(\d+)$~', $path, $matches) === 1 && $method === 'POST') {
        requireRole($currentUser, 'Administrator');
        require_valid_csrf();
        $editedUserId = (int) $matches[1];
        $username = is_string($_POST['username'] ?? null) ? $_POST['username'] : '';
        $email = is_string($_POST['email'] ?? null) ? $_POST['email'] : '';
        $role = is_string($_POST['role'] ?? null) ? $_POST['role'] : '';
        $fahrerId = filter_input(INPUT_POST, 'fahrer_id', FILTER_VALIDATE_INT, ['options' => ['min_range' => 1]]) ?: null;
        $isActive = isset($_POST['is_active']);
        $newPassword = is_string($_POST['password'] ?? null) && $_POST['password'] !== '' ? $_POST['password'] : null;
        $confirmation = is_string($_POST['password_confirmation'] ?? null) ? $_POST['password_confirmation'] : '';
        $formValues = ['id' => $editedUserId, 'username' => trim($username), 'email' => trim($email), 'role' => $role, 'fahrer_id' => $fahrerId, 'is_active' => $isActive];

        try {
            if ((int) $currentUser['id'] === $editedUserId && (!$isActive || $role !== 'Administrator')) {
                throw new InvalidArgumentException('Du kannst dein eigenes Administratorkonto nicht deaktivieren oder herabstufen.');
            }
            if ($newPassword !== null && (strlen($newPassword) < 12 || !hash_equals($newPassword, $confirmation))) {
                throw new InvalidArgumentException('Das neue Passwort muss mindestens 12 Zeichen enthalten und mit der Wiederholung übereinstimmen.');
            }

            $app['users']->updateUser($editedUserId, $username, $email, $role, $fahrerId, $isActive, $newPassword);
            $_SESSION['flash_success'] = 'Benutzerdetails wurden gespeichert.';
            redirect('admin/benutzer');
        } catch (InvalidArgumentException|PDOException $exception) {
            render('admin-user-form', [
                'pageTitle' => 'Benutzer bearbeiten | openSlalom',
                'pageDescription' => 'WebUI-Benutzer bearbeiten.',
                'pageClass' => 'admin-page',
                'drivers' => $app['users']->listActiveDrivers(),
                'editMode' => true,
                'editedUserId' => $editedUserId,
                'formValues' => $formValues,
                'formError' => $exception instanceof InvalidArgumentException ? $exception->getMessage() : 'Benutzername, E-Mail-Adresse oder Fahrerzuordnung ist bereits vergeben.',
            ], 422);
        }
    }

    if (preg_match('~^/training/([^/]+)$~', $path, $matches) === 1 && $isReadRequest) {
        $uuid = strtolower($matches[1]);
        if (preg_match('/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/', $uuid) !== 1) {
            renderNotFound();
        }

        $trainingData = $app['trainings']->findByUuid($uuid, $currentUser);
        if ($trainingData === null) {
            renderNotFound();
        }

        $view = TrainingViewBuilder::build($trainingData, $currentUser !== null);
        $refreshSeconds = $view['training']['is_finished']
            ? 0
            : max(0, (int) ($app['config']['site']['auto_refresh_seconds'] ?? 15));

        render('training', [
            'pageTitle' => $view['training']['name'] . ' | openSlalom',
            'pageDescription' => 'Live-Ergebnisse und Rundenzeiten für ' . $view['training']['name'],
            'pageClass' => 'training-page',
            'view' => $view,
            'refreshSeconds' => $refreshSeconds,
            'canManageTrainings' => Auth::canManageTrainings($currentUser),
        ]);
    }

    renderNotFound();
} catch (Throwable $exception) {
    error_log(sprintf('OpenSlalom.WebUI: %s', $exception->getMessage()));
    render('error', [
        'pageTitle' => 'Ergebnisse nicht verfügbar | openSlalom',
        'pageDescription' => 'Die angeforderten Daten sind vorübergehend nicht verfügbar.',
        'pageClass' => 'error-page',
    ], 503);
}

function requireAuthenticated(?array $currentUser): void
{
    if ($currentUser === null) {
        redirect('login');
    }
}

function requireRole(?array $currentUser, string $role): void
{
    if (!Auth::hasRole($currentUser, $role)) {
        renderNotFound();
    }
}

function requireTrainingManager(?array $currentUser): void
{
    if (!Auth::canManageTrainings($currentUser)) {
        renderNotFound();
    }
}

function requireMasterDataManager(?array $currentUser): void
{
    if (!Auth::canManageMasterData($currentUser)) renderNotFound();
}

function masterDataTitle(string $type): string
{
    return ['vereine' => 'Vereine', 'fahrer' => 'Fahrer', 'disziplinen' => 'Disziplinen', 'karts' => 'Karts', 'wetter' => 'Wetter'][$type] ?? 'Verwaltung';
}

function masterDataDefaultValues(string $type): array
{
    return match ($type) {
        'disziplinen' => ['name' => '', 'tf' => '0', 'pf' => '0', 'altersklassen' => []],
        'fahrer' => ['verein_id' => null, 'vorname' => '', 'nachname' => '', 'mitglieds_nummer' => '', 'geburtsdatum' => '', 'geschlecht' => ''],
        'karts' => ['verein_id' => null, 'disziplin_id' => null, 'name' => '', 'motor' => '', 'chassis' => ''],
        default => ['name' => ''],
    };
}

function masterDataValuesFromPost(string $type): array
{
    $values = $_POST;
    if ($type === 'disziplinen') {
        $labels = is_array($_POST['age_label'] ?? null) ? $_POST['age_label'] : [];
        $froms = is_array($_POST['age_from'] ?? null) ? $_POST['age_from'] : [];
        $tos = is_array($_POST['age_to'] ?? null) ? $_POST['age_to'] : [];
        $values['altersklassen'] = [];
        foreach ($labels as $index => $label) {
            $values['altersklassen'][] = ['label' => $label, 'age_from' => $froms[$index] ?? '', 'age_to' => $tos[$index] ?? ''];
        }
    }
    if ($type === 'vereine') {
        $values['vereinsname'] = is_string($_POST['vereinsname'] ?? null) ? $_POST['vereinsname'] : '';
        $values['mitglieds_nummer'] = is_string($_POST['mitglieds_nummer'] ?? null) ? $_POST['mitglieds_nummer'] : '';
        $values['postleitzahl'] = is_string($_POST['postleitzahl'] ?? null) ? $_POST['postleitzahl'] : '';
        $values['ort'] = is_string($_POST['ort'] ?? null) ? $_POST['ort'] : '';
        $values['adresse'] = is_string($_POST['adresse'] ?? null) ? $_POST['adresse'] : '';
    }
    return $values;
}

function renderMasterDataForm(array $app, string $type, bool $editMode, ?int $id, array $values, ?string $error = null): never
{
    render('master-data-form', [
        'pageTitle' => ($editMode ? masterDataTitle($type) . ' bearbeiten' : masterDataTitle($type) . ' anlegen') . ' | openSlalom',
        'pageDescription' => masterDataTitle($type) . ' verwalten.',
        'pageClass' => 'admin-page',
        'masterType' => $type,
        'masterTitle' => masterDataTitle($type),
        'editMode' => $editMode,
        'itemId' => $id,
        'formValues' => $values,
        'lookups' => $app['masterData']->lookups($type),
        'formError' => $error,
    ]);
}

function trainingFormValuesFromPost(): array
{
    return [
        'name' => is_string($_POST['name'] ?? null) ? $_POST['name'] : '',
        'beschreibung' => is_string($_POST['beschreibung'] ?? null) ? $_POST['beschreibung'] : '',
        'zeitpunkt' => is_string($_POST['zeitpunkt'] ?? null) ? $_POST['zeitpunkt'] : '',
        'verein_id' => $_POST['verein_id'] ?? null,
        'disziplin_id' => $_POST['disziplin_id'] ?? null,
        'wetter_id' => $_POST['wetter_id'] ?? null,
        'training_abgeschlossen' => isset($_POST['training_abgeschlossen']),
        'ist_veroeffentlicht' => isset($_POST['ist_veroeffentlicht']),
    ];
}

function renderNotFound(): never
{
    render('not-found', [
        'pageTitle' => 'Seite nicht gefunden | openSlalom',
        'pageDescription' => 'Die angeforderte Seite wurde nicht gefunden.',
        'pageClass' => 'error-page',
    ], 404);
}
