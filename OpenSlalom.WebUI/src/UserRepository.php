<?php

declare(strict_types=1);

final class UserRepository
{
    private const AllowedRoles = ['Administrator', 'Trainingsleiter', 'Fahrer', 'Registriert'];

    public function __construct(
        private readonly PDO $authConnection,
        private readonly PDO $readerConnection
    ) {
    }

    public function findActiveUserById(int $userId): ?array
    {
        $statement = $this->authConnection->prepare(
            'SELECT id, username, email, password_hash, fahrer_id, session_version FROM web_users WHERE id = :id AND is_active = 1 LIMIT 1'
        );
        $statement->execute(['id' => $userId]);
        $user = $statement->fetch();

        return is_array($user) ? $this->attachRoles($user) : null;
    }

    public function hasAdministrator(): bool
    {
        $statement = $this->authConnection->query(
            "SELECT EXISTS(SELECT 1 FROM web_users u INNER JOIN web_user_roles ur ON ur.user_id = u.id INNER JOIN web_roles r ON r.id = ur.role_id WHERE r.name = 'Administrator')"
        );

        return (bool) $statement->fetchColumn();
    }

    public function findActiveUserByLogin(string $login): ?array
    {
        $statement = $this->authConnection->prepare(
            'SELECT id, username, email, password_hash, fahrer_id, session_version FROM web_users WHERE (username = :login_username OR email = :login_email) AND is_active = 1 LIMIT 1'
        );
        $login = trim($login);
        $statement->execute(['login_username' => $login, 'login_email' => strtolower($login)]);
        $user = $statement->fetch();

        return is_array($user) ? $this->attachRoles($user) : null;
    }

    public function isLoginRateLimited(string $username, string $ipAddress): bool
    {
        $statement = $this->authConnection->prepare(
            "SELECT COUNT(*) FROM web_login_attempts WHERE attempted_at_utc >= UTC_TIMESTAMP() - INTERVAL 15 MINUTE AND (username = :username OR ip_address = :ip_address)"
        );
        $statement->execute(['username' => $username, 'ip_address' => $ipAddress]);

        return (int) $statement->fetchColumn() >= 10;
    }

    public function recordFailedLogin(string $username, string $ipAddress): void
    {
        $this->authConnection->exec(
            'DELETE FROM web_login_attempts WHERE attempted_at_utc < UTC_TIMESTAMP() - INTERVAL 30 DAY'
        );
        $statement = $this->authConnection->prepare(
            'INSERT INTO web_login_attempts (username, ip_address) VALUES (:username, :ip_address)'
        );
        $statement->execute(['username' => $username, 'ip_address' => $ipAddress]);
    }

    public function markSuccessfulLogin(int $userId, ?string $rehash = null): void
    {
        $sql = 'UPDATE web_users SET last_login_at_utc = UTC_TIMESTAMP(), updated_at_utc = UTC_TIMESTAMP()';
        $parameters = ['id' => $userId];
        if ($rehash !== null) {
            $sql .= ', password_hash = :password_hash';
            $parameters['password_hash'] = $rehash;
        }
        $sql .= ' WHERE id = :id';

        $statement = $this->authConnection->prepare($sql);
        $statement->execute($parameters);
    }

    public function listUsers(string $search, int $page, int $perPage): array
    {
        $parameters = ['q' => '%' . $search . '%'];
        $count = $this->authConnection->prepare(
            <<<'SQL'
            SELECT COUNT(*) FROM web_users u
            LEFT JOIN web_user_roles ur ON ur.user_id = u.id
            LEFT JOIN web_roles r ON r.id = ur.role_id
            GROUP BY u.id, u.username, u.email
            HAVING CONCAT_WS(' ', u.username, u.email, GROUP_CONCAT(r.name ORDER BY r.name SEPARATOR ', ')) LIKE :q
            SQL
        );
        $count->execute($parameters);
        $total = count($count->fetchAll());
        $perPage = max(1, min(100, $perPage));
        $pages = max(1, (int) ceil($total / $perPage));
        $page = min(max(1, $page), $pages);
        $offset = ($page - 1) * $perPage;

        $sql = str_replace(['__LIMIT__', '__OFFSET__'], [(string) $perPage, (string) $offset], <<<'SQL'
            SELECT
                u.id,
                u.username,
                u.email,
                u.is_active,
                u.fahrer_id,
                u.created_at_utc,
                u.last_login_at_utc,
                GROUP_CONCAT(r.name ORDER BY r.name SEPARATOR ', ') AS roles
            FROM web_users u
            LEFT JOIN web_user_roles ur ON ur.user_id = u.id
            LEFT JOIN web_roles r ON r.id = ur.role_id
            GROUP BY u.id, u.username, u.email, u.is_active, u.fahrer_id, u.created_at_utc, u.last_login_at_utc
            HAVING CONCAT_WS(' ', u.username, u.email, GROUP_CONCAT(r.name ORDER BY r.name SEPARATOR ', ')) LIKE :q
            ORDER BY u.username
            LIMIT __LIMIT__ OFFSET __OFFSET__
            SQL
        );
        $statement = $this->authConnection->prepare($sql);
        $statement->execute($parameters);

        return ['items' => $statement->fetchAll(), 'pagination' => ['page' => $page, 'pages' => $pages, 'total' => $total]];
    }

    public function listActiveDrivers(): array
    {
        $statement = $this->readerConnection->query(
            "SELECT id, vorname, COALESCE(nachname, '') AS nachname FROM fahrer WHERE is_deleted = 0 ORDER BY vorname, nachname"
        );

        return $statement->fetchAll();
    }

    public function registerUser(string $username, string $email, string $password, string $ipAddress): void
    {
        $this->authConnection->exec(
            'DELETE FROM web_registration_attempts WHERE attempted_at_utc < UTC_TIMESTAMP() - INTERVAL 30 DAY'
        );
        $limit = $this->authConnection->prepare(
            'SELECT COUNT(*) FROM web_registration_attempts WHERE ip_address = :ip_address AND attempted_at_utc >= UTC_TIMESTAMP() - INTERVAL 60 MINUTE'
        );
        $limit->execute(['ip_address' => $ipAddress]);
        if ((int) $limit->fetchColumn() >= 5) {
            throw new RuntimeException('Zu viele Registrierungsversuche. Bitte versuche es später erneut.');
        }

        $this->authConnection->prepare(
            'INSERT INTO web_registration_attempts (ip_address) VALUES (:ip_address)'
        )->execute(['ip_address' => $ipAddress]);
        $this->createUser($username, $email, $password, 'Registriert', null);
    }

    public function listAdministratorEmails(): array
    {
        $statement = $this->authConnection->query(
            <<<'SQL'
            SELECT DISTINCT u.email
            FROM web_users u
            INNER JOIN web_user_roles ur ON ur.user_id = u.id
            INNER JOIN web_roles r ON r.id = ur.role_id
            WHERE u.is_active = 1 AND u.email IS NOT NULL AND r.name = 'Administrator'
            SQL
        );

        return array_values(array_filter(
            array_column($statement->fetchAll(), 'email'),
            static fn (mixed $email): bool => is_string($email) && filter_var($email, FILTER_VALIDATE_EMAIL) !== false
        ));
    }

    public function findUserForAdministration(int $userId): ?array
    {
        $statement = $this->authConnection->prepare(
            'SELECT id, username, email, fahrer_id, is_active FROM web_users WHERE id = :id LIMIT 1'
        );
        $statement->execute(['id' => $userId]);
        $user = $statement->fetch();
        if (!is_array($user)) {
            return null;
        }

        $user = $this->attachRoles($user);
        $user['role'] = $user['roles'][0] ?? '';
        $user['is_active'] = (bool) $user['is_active'];

        return $user;
    }

    public function createUser(string $username, string $email, string $password, string $role, ?int $fahrerId): void
    {
        $username = trim($username);
        $email = $this->normalizeEmail($email);
        $this->validateAccount($username, $email, $role, $fahrerId);
        if (strlen($password) < 12) {
            throw new InvalidArgumentException('Das Passwort muss mindestens 12 Zeichen enthalten.');
        }

        $this->authConnection->beginTransaction();
        try {
            $statement = $this->authConnection->prepare(
                'INSERT INTO web_users (username, email, password_hash, fahrer_id) VALUES (:username, :email, :password_hash, :fahrer_id)'
            );
            $statement->execute([
                'username' => $username,
                'email' => $email,
                'password_hash' => password_hash($password, self::passwordAlgorithm()),
                'fahrer_id' => $fahrerId,
            ]);
            $userId = (int) $this->authConnection->lastInsertId();

            $roleId = $this->roleId($role);
            $assignment = $this->authConnection->prepare(
                'INSERT INTO web_user_roles (user_id, role_id) VALUES (:user_id, :role_id)'
            );
            $assignment->execute(['user_id' => $userId, 'role_id' => $roleId]);
            $this->authConnection->commit();
        } catch (Throwable $exception) {
            $this->authConnection->rollBack();
            throw $exception;
        }
    }

    public function updateUser(
        int $userId,
        string $username,
        string $email,
        string $role,
        ?int $fahrerId,
        bool $isActive,
        ?string $newPassword
    ): void {
        $username = trim($username);
        $email = $this->normalizeEmail($email);
        $this->validateAccount($username, $email, $role, $fahrerId);

        $this->authConnection->beginTransaction();
        try {
            $sql = 'UPDATE web_users SET username = :username, email = :email, fahrer_id = :fahrer_id, is_active = :is_active, updated_at_utc = UTC_TIMESTAMP()';
            $parameters = [
                'id' => $userId,
                'username' => $username,
                'email' => $email,
                'fahrer_id' => $fahrerId,
                'is_active' => $isActive ? 1 : 0,
            ];
            if ($newPassword !== null) {
                $sql .= ', password_hash = :password_hash, session_version = session_version + 1';
                $parameters['password_hash'] = password_hash($newPassword, self::passwordAlgorithm());
            }
            $sql .= ' WHERE id = :id';

            $statement = $this->authConnection->prepare($sql);
            $statement->execute($parameters);
            if ($statement->rowCount() === 0 && $this->findUserForAdministration($userId) === null) {
                throw new InvalidArgumentException('Der Benutzer wurde nicht gefunden.');
            }

            $this->authConnection->prepare('DELETE FROM web_user_roles WHERE user_id = :user_id')
                ->execute(['user_id' => $userId]);
            $this->authConnection->prepare('INSERT INTO web_user_roles (user_id, role_id) VALUES (:user_id, :role_id)')
                ->execute(['user_id' => $userId, 'role_id' => $this->roleId($role)]);
            $this->authConnection->commit();
        } catch (Throwable $exception) {
            $this->authConnection->rollBack();
            throw $exception;
        }
    }

    public function driverDisplayName(?int $fahrerId): ?string
    {
        if ($fahrerId === null) {
            return null;
        }

        $statement = $this->readerConnection->prepare(
            "SELECT vorname, COALESCE(nachname, '') AS nachname FROM fahrer WHERE id = :id AND is_deleted = 0"
        );
        $statement->execute(['id' => $fahrerId]);
        $driver = $statement->fetch();
        if (!is_array($driver)) {
            return null;
        }

        return display_name((string) $driver['vorname'], (string) $driver['nachname']);
    }

    public function changeOwnPassword(int $userId, string $currentPassword, string $newPassword): void
    {
        if (strlen($newPassword) < 12) {
            throw new InvalidArgumentException('Das neue Passwort muss mindestens 12 Zeichen enthalten.');
        }

        $this->authConnection->beginTransaction();
        try {
            $statement = $this->authConnection->prepare(
                'SELECT password_hash FROM web_users WHERE id = :id AND is_active = 1 LIMIT 1 FOR UPDATE'
            );
            $statement->execute(['id' => $userId]);
            $passwordHash = $statement->fetchColumn();
            if (!is_string($passwordHash) || !password_verify($currentPassword, $passwordHash)) {
                throw new InvalidArgumentException('Das aktuelle Passwort ist nicht korrekt.');
            }

            $this->authConnection->prepare(
                'UPDATE web_users SET password_hash = :password_hash, session_version = session_version + 1, updated_at_utc = UTC_TIMESTAMP() WHERE id = :id'
            )->execute([
                'password_hash' => password_hash($newPassword, self::passwordAlgorithm()),
                'id' => $userId,
            ]);
            $this->authConnection->prepare(
                'UPDATE web_password_reset_tokens SET used_at_utc = UTC_TIMESTAMP() WHERE user_id = :user_id AND used_at_utc IS NULL'
            )->execute(['user_id' => $userId]);
            $this->authConnection->commit();
        } catch (Throwable $exception) {
            $this->authConnection->rollBack();
            throw $exception;
        }
    }

    public function deleteOwnAccount(int $userId, string $currentPassword): void
    {
        $this->authConnection->beginTransaction();
        try {
            $statement = $this->authConnection->prepare(
                'SELECT password_hash FROM web_users WHERE id = :id AND is_active = 1 LIMIT 1 FOR UPDATE'
            );
            $statement->execute(['id' => $userId]);
            $passwordHash = $statement->fetchColumn();
            if (!is_string($passwordHash) || !password_verify($currentPassword, $passwordHash)) {
                throw new InvalidArgumentException('Das aktuelle Passwort ist nicht korrekt.');
            }

            $administratorCheck = $this->authConnection->prepare(
                <<<'SQL'
                SELECT EXISTS(
                    SELECT 1
                    FROM web_user_roles ur
                    INNER JOIN web_roles r ON r.id = ur.role_id
                    WHERE ur.user_id = :user_id AND r.name = 'Administrator'
                )
                SQL
            );
            $administratorCheck->execute(['user_id' => $userId]);
            if ((bool) $administratorCheck->fetchColumn() && !$this->hasOtherActiveAdministrator($userId)) {
                throw new InvalidArgumentException('Das letzte aktive Administratorkonto kann nicht gelöscht werden.');
            }

            $this->authConnection->prepare('DELETE FROM web_users WHERE id = :id')->execute(['id' => $userId]);
            $this->authConnection->commit();
        } catch (Throwable $exception) {
            $this->authConnection->rollBack();
            throw $exception;
        }
    }

    public function createPasswordResetToken(string $email): ?string
    {
        $this->authConnection->exec(
            'DELETE FROM web_password_reset_tokens WHERE expires_at_utc < UTC_TIMESTAMP() - INTERVAL 7 DAY OR used_at_utc < UTC_TIMESTAMP() - INTERVAL 7 DAY'
        );
        $email = strtolower(trim($email));
        $statement = $this->authConnection->prepare(
            'SELECT id FROM web_users WHERE email = :email AND is_active = 1 LIMIT 1'
        );
        $statement->execute(['email' => $email]);
        $userId = $statement->fetchColumn();
        if ($userId === false) {
            return null;
        }

        $limitStatement = $this->authConnection->prepare(
            'SELECT COUNT(*) FROM web_password_reset_tokens WHERE user_id = :user_id AND created_at_utc >= UTC_TIMESTAMP() - INTERVAL 60 MINUTE'
        );
        $limitStatement->execute(['user_id' => (int) $userId]);
        if ((int) $limitStatement->fetchColumn() >= 3) {
            return null;
        }

        $token = bin2hex(random_bytes(32));
        $tokenHash = hash('sha256', $token);
        $this->authConnection->beginTransaction();
        try {
            $this->authConnection->prepare(
                'UPDATE web_password_reset_tokens SET used_at_utc = UTC_TIMESTAMP() WHERE user_id = :user_id AND used_at_utc IS NULL'
            )->execute(['user_id' => (int) $userId]);
            $this->authConnection->prepare(
                'INSERT INTO web_password_reset_tokens (user_id, token_hash, expires_at_utc) VALUES (:user_id, :token_hash, UTC_TIMESTAMP() + INTERVAL 60 MINUTE)'
            )->execute(['user_id' => (int) $userId, 'token_hash' => $tokenHash]);
            $this->authConnection->commit();
        } catch (Throwable $exception) {
            $this->authConnection->rollBack();
            throw $exception;
        }

        return $token;
    }

    public function resetPassword(string $token, string $newPassword): bool
    {
        if (!preg_match('/^[a-f0-9]{64}$/', $token)) {
            return false;
        }

        $this->authConnection->beginTransaction();
        try {
            $statement = $this->authConnection->prepare(
                'SELECT id, user_id FROM web_password_reset_tokens WHERE token_hash = :token_hash AND used_at_utc IS NULL AND expires_at_utc > UTC_TIMESTAMP() LIMIT 1 FOR UPDATE'
            );
            $statement->execute(['token_hash' => hash('sha256', $token)]);
            $reset = $statement->fetch();
            if (!is_array($reset)) {
                $this->authConnection->rollBack();
                return false;
            }

            $userUpdate = $this->authConnection->prepare(
                'UPDATE web_users SET password_hash = :password_hash, session_version = session_version + 1, updated_at_utc = UTC_TIMESTAMP() WHERE id = :id AND is_active = 1'
            );
            $userUpdate->execute([
                'password_hash' => password_hash($newPassword, self::passwordAlgorithm()),
                'id' => (int) $reset['user_id'],
            ]);
            if ($userUpdate->rowCount() !== 1) {
                $this->authConnection->rollBack();
                return false;
            }
            $this->authConnection->prepare(
                'UPDATE web_password_reset_tokens SET used_at_utc = UTC_TIMESTAMP() WHERE user_id = :user_id AND used_at_utc IS NULL'
            )->execute(['user_id' => (int) $reset['user_id']]);
            $this->authConnection->commit();
            return true;
        } catch (Throwable $exception) {
            $this->authConnection->rollBack();
            throw $exception;
        }
    }

    public static function passwordAlgorithm(): string|int
    {
        return defined('PASSWORD_ARGON2ID') ? PASSWORD_ARGON2ID : PASSWORD_DEFAULT;
    }

    private function attachRoles(array $user): array
    {
        $statement = $this->authConnection->prepare(
            'SELECT r.name FROM web_roles r INNER JOIN web_user_roles ur ON ur.role_id = r.id WHERE ur.user_id = :user_id ORDER BY r.name'
        );
        $statement->execute(['user_id' => (int) $user['id']]);
        $user['roles'] = array_column($statement->fetchAll(), 'name');
        $user['fahrer_id'] = $user['fahrer_id'] === null ? null : (int) $user['fahrer_id'];
        $user['session_version'] = (int) $user['session_version'];

        return $user;
    }

    private function isActiveDriver(int $fahrerId): bool
    {
        $statement = $this->readerConnection->prepare('SELECT 1 FROM fahrer WHERE id = :id AND is_deleted = 0');
        $statement->execute(['id' => $fahrerId]);

        return $statement->fetchColumn() !== false;
    }

    private function hasOtherActiveAdministrator(int $excludedUserId): bool
    {
        $statement = $this->authConnection->prepare(
            <<<'SQL'
            SELECT EXISTS(
                SELECT 1
                FROM web_users u
                INNER JOIN web_user_roles ur ON ur.user_id = u.id
                INNER JOIN web_roles r ON r.id = ur.role_id
                WHERE u.id <> :user_id AND u.is_active = 1 AND r.name = 'Administrator'
            )
            SQL
        );
        $statement->execute(['user_id' => $excludedUserId]);

        return (bool) $statement->fetchColumn();
    }

    private function validateAccount(string $username, string $email, string $role, ?int $fahrerId): void
    {
        if (strlen($username) < 3 || strlen($username) > 100 || preg_match('/^[\p{L}\p{N}._-]+$/u', $username) !== 1 || !in_array($role, self::AllowedRoles, true)) {
            throw new InvalidArgumentException('Der Benutzername muss 3 bis 100 Zeichen lang sein und darf nur Buchstaben, Zahlen, Punkt, Unterstrich und Bindestrich enthalten.');
        }
        if (!filter_var($email, FILTER_VALIDATE_EMAIL) || strlen($email) > 254) {
            throw new InvalidArgumentException('Bitte eine gültige E-Mail-Adresse eingeben.');
        }
        if ($role === 'Fahrer' && $fahrerId === null) {
            throw new InvalidArgumentException('Für die Rolle Fahrer muss ein Fahrer zugeordnet werden.');
        }
        if ($fahrerId !== null && !$this->isActiveDriver($fahrerId)) {
            throw new InvalidArgumentException('Der ausgewählte Fahrer ist nicht verfügbar.');
        }
    }

    private function normalizeEmail(string $email): string
    {
        return strtolower(trim($email));
    }

    private function roleId(string $role): int
    {
        $statement = $this->authConnection->prepare('SELECT id FROM web_roles WHERE name = :name LIMIT 1');
        $statement->execute(['name' => $role]);
        $roleId = $statement->fetchColumn();
        if ($roleId === false) {
            throw new RuntimeException('Die ausgewählte Rolle ist nicht vorhanden.');
        }

        return (int) $roleId;
    }
}
