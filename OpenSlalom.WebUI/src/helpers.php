<?php

declare(strict_types=1);

function escape(mixed $value): string
{
    return htmlspecialchars((string) $value, ENT_QUOTES | ENT_SUBSTITUTE, 'UTF-8');
}

function request_path(): string
{
    $uri = parse_url($_SERVER['REQUEST_URI'] ?? '/', PHP_URL_PATH);
    $path = is_string($uri) ? rawurldecode($uri) : '/';
    $scriptDirectory = str_replace('\\', '/', dirname($_SERVER['SCRIPT_NAME'] ?? '/'));

    if ($scriptDirectory !== '/' && $scriptDirectory !== '.' && str_starts_with($path, $scriptDirectory)) {
        $path = substr($path, strlen($scriptDirectory));
    }

    $normalized = '/' . trim($path, '/');
    return $normalized === '//' ? '/' : $normalized;
}

function base_url(string $path = ''): string
{
    $scriptDirectory = str_replace('\\', '/', dirname($_SERVER['SCRIPT_NAME'] ?? '/'));
    $base = ($scriptDirectory === '/' || $scriptDirectory === '.') ? '' : rtrim($scriptDirectory, '/');

    return $base . '/' . ltrim($path, '/');
}

function absolute_url(string $path = ''): string
{
    $https = ($_SERVER['HTTPS'] ?? '') === 'on' || (int) ($_SERVER['SERVER_PORT'] ?? 80) === 443;
    $host = $_SERVER['HTTP_HOST'] ?? 'localhost';
    if (!preg_match('/^[a-z0-9.:-]+$/i', $host)) {
        $host = 'localhost';
    }

    return ($https ? 'https://' : 'http://') . $host . base_url($path);
}

function format_training_time(?float $seconds): string
{
    if ($seconds === null || $seconds <= 0) {
        return '-';
    }

    $wholeSeconds = (int) floor($seconds);
    $milliseconds = (int) floor((($seconds - $wholeSeconds) * 1000) + 0.000001);

    return sprintf('%02d.%03d', $wholeSeconds, $milliseconds);
}

function format_penalty(float $seconds): string
{
    if ($seconds <= 0) {
        return '-';
    }

    return rtrim(rtrim(number_format($seconds, 3, '.', ''), '0'), '.') . ' s';
}

function format_duration(float $seconds): string
{
    $seconds = (int) floor(max(0, $seconds));
    $hours = (int) floor($seconds / 3600);
    $minutes = (int) floor(($seconds % 3600) / 60);
    $remainingSeconds = (int) floor($seconds % 60);

    return sprintf('%02d:%02d:%02d', $hours, $minutes, $remainingSeconds);
}

function format_date(?string $date, string $format = 'd.m.Y'): string
{
    if ($date === null || $date === '') {
        return '-';
    }

    try {
        return (new DateTimeImmutable($date))->format($format);
    } catch (Throwable) {
        return '-';
    }
}

function display_name(string $firstName, ?string $lastName): string
{
    $lastName = trim((string) $lastName);
    return $lastName === '' ? trim($firstName) : trim($firstName . ' ' . $lastName);
}

function display_initial(string $name): string
{
    return preg_match('/^./u', trim($name), $matches) === 1 ? $matches[0] : '?';
}

function render(string $template, array $data = [], int $status = 200): never
{
    global $currentUser;

    http_response_code($status);
    $data['currentUser'] ??= $currentUser ?? null;
    extract($data, EXTR_SKIP);
    $contentTemplate = dirname(__DIR__) . '/templates/' . $template . '.php';

    require dirname(__DIR__) . '/templates/layout.php';
    exit;
}

function redirect(string $path): never
{
    header('Location: ' . base_url($path), true, 303);
    exit;
}

function csrf_token(): string
{
    if (!isset($_SESSION['csrf_token']) || !is_string($_SESSION['csrf_token'])) {
        $_SESSION['csrf_token'] = bin2hex(random_bytes(32));
    }

    return $_SESSION['csrf_token'];
}

function require_valid_csrf(): void
{
    $token = $_POST['csrf_token'] ?? '';
    if (!is_string($token) || !hash_equals(csrf_token(), $token)) {
        http_response_code(400);
        exit('Ungültige Anfrage.');
    }
}

function client_ip(): string
{
    $ip = $_SERVER['REMOTE_ADDR'] ?? '0.0.0.0';
    return filter_var($ip, FILTER_VALIDATE_IP) !== false ? $ip : '0.0.0.0';
}

function list_options(): array
{
    $search = is_string($_GET['q'] ?? null) ? trim($_GET['q']) : '';
    $page = filter_input(INPUT_GET, 'page', FILTER_VALIDATE_INT, ['options' => ['min_range' => 1]]) ?: 1;

    return ['search' => substr($search, 0, 100), 'page' => $page, 'per_page' => 20];
}

function list_page_url(string $path, int $page, string $search): string
{
    $parameters = ['page' => max(1, $page)];
    if ($search !== '') {
        $parameters['q'] = $search;
    }

    return base_url($path) . '?' . http_build_query($parameters, '', '&', PHP_QUERY_RFC3986);
}
