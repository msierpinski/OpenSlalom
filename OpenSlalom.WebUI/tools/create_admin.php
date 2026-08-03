<?php

declare(strict_types=1);

if (PHP_SAPI !== 'cli') {
    http_response_code(403);
    exit('Dieses Werkzeug ist nur über die Kommandozeile verfügbar.' . PHP_EOL);
}

require dirname(__DIR__) . '/src/Database.php';
require dirname(__DIR__) . '/src/UserRepository.php';

$configPath = dirname(__DIR__) . '/config.php';
if (!is_file($configPath)) {
    exit("config.php wurde nicht gefunden.\n");
}
$config = require $configPath;
if (!is_array($config)) {
    exit("config.php ist ungültig.\n");
}

$readerConnection = Database::connect($config['database'] ?? []);
$authConnection = Database::connect($config['auth_database'] ?? $config['database'] ?? []);
$users = new UserRepository($authConnection, $readerConnection);
$force = in_array('--force', $argv, true);

if ($users->hasAdministrator() && !$force) {
    exit("Ein Administrator existiert bereits. Verwende --force nur für eine bewusst zusätzliche Administratoranlage.\n");
}

$username = trim((string) readline('Benutzername: '));
if ($username === '') {
    exit("Benutzername darf nicht leer sein.\n");
}

$email = trim((string) readline('E-Mail-Adresse: '));
if (!filter_var($email, FILTER_VALIDATE_EMAIL)) {
    exit("Bitte eine gültige E-Mail-Adresse eingeben.\n");
}

function readPassword(string $prompt): string
{
    if (DIRECTORY_SEPARATOR === '/' && function_exists('shell_exec')) {
        shell_exec('stty -echo');
        $password = (string) readline($prompt);
        shell_exec('stty echo');
        fwrite(STDOUT, PHP_EOL);
        return $password;
    }

    return (string) readline($prompt);
}

$password = readPassword('Passwort (mindestens 12 Zeichen): ');
$confirmation = readPassword('Passwort wiederholen: ');
if (strlen($password) < 12 || !hash_equals($password, $confirmation)) {
    exit("Passwort ist zu kurz oder stimmt nicht überein.\n");
}

try {
    $users->createUser($username, $email, $password, 'Administrator', null);
    fwrite(STDOUT, "Administrator '$username' wurde angelegt.\n");
} catch (Throwable $exception) {
    fwrite(STDERR, "Administrator konnte nicht angelegt werden: {$exception->getMessage()}\n");
    exit(1);
}
