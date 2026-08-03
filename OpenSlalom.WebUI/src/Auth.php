<?php

declare(strict_types=1);

final class Auth
{
    public function __construct(private readonly UserRepository $users)
    {
    }

    public static function startSession(): void
    {
        if (session_status() === PHP_SESSION_ACTIVE) {
            return;
        }

        session_name('openslalom_web');
        session_set_cookie_params([
            'lifetime' => 0,
            'path' => '/',
            'secure' => self::isHttps(),
            'httponly' => true,
            'samesite' => 'Lax',
        ]);
        session_start();
    }

    public function currentUser(): ?array
    {
        $userId = $_SESSION['user_id'] ?? null;
        $sessionVersion = $_SESSION['session_version'] ?? null;
        if (!is_int($userId) || !is_int($sessionVersion)) {
            return null;
        }

        $user = $this->users->findActiveUserById($userId);
        if ($user === null || $user['session_version'] !== $sessionVersion) {
            $this->logout();
            return null;
        }

        unset($user['password_hash']);
        return $user;
    }

    public function attempt(string $login, string $password, string $ipAddress): bool
    {
        $login = trim($login);
        if ($login === '' || $password === '' || $this->users->isLoginRateLimited($login, $ipAddress)) {
            $this->users->recordFailedLogin($login, $ipAddress);
            return false;
        }

        $user = $this->users->findActiveUserByLogin($login);
        if ($user === null || !password_verify($password, (string) $user['password_hash'])) {
            $this->users->recordFailedLogin($login, $ipAddress);
            return false;
        }

        $rehash = password_needs_rehash((string) $user['password_hash'], UserRepository::passwordAlgorithm())
            ? password_hash($password, UserRepository::passwordAlgorithm())
            : null;
        $this->users->markSuccessfulLogin((int) $user['id'], $rehash);
        session_regenerate_id(true);
        $_SESSION['user_id'] = (int) $user['id'];
        $_SESSION['session_version'] = (int) $user['session_version'];

        return true;
    }

    public function logout(): void
    {
        $_SESSION = [];
        if (session_status() === PHP_SESSION_ACTIVE) {
            $parameters = session_get_cookie_params();
            setcookie(session_name(), '', [
                'expires' => time() - 3600,
                'path' => $parameters['path'] ?? '/',
                'secure' => (bool) ($parameters['secure'] ?? false),
                'httponly' => (bool) ($parameters['httponly'] ?? true),
                'samesite' => $parameters['samesite'] ?? 'Lax',
            ]);
            session_destroy();
        }
    }

    public static function hasRole(?array $user, string $role): bool
    {
        return $user !== null && in_array($role, $user['roles'] ?? [], true);
    }

    public static function canManageTrainings(?array $user): bool
    {
        return self::hasRole($user, 'Administrator') || self::hasRole($user, 'Trainingsleiter');
    }

    public static function canManageMasterData(?array $user): bool
    {
        return self::canManageTrainings($user);
    }

    private static function isHttps(): bool
    {
        return ($_SERVER['HTTPS'] ?? '') === 'on' || (int) ($_SERVER['SERVER_PORT'] ?? 80) === 443;
    }
}
