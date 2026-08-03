<?php

declare(strict_types=1);

final class Database
{
    public static function connect(array $config): PDO
    {
        $host = (string) ($config['host'] ?? '');
        $port = (int) ($config['port'] ?? 3306);
        $database = (string) ($config['name'] ?? '');
        $user = (string) ($config['user'] ?? '');
        $password = (string) ($config['password'] ?? '');

        if ($host === '' || $database === '' || $user === '' || $port < 1 || $port > 65535) {
            throw new RuntimeException('Die Datenbankkonfiguration ist unvollständig.');
        }

        $dsn = sprintf(
            'mysql:host=%s;port=%d;dbname=%s;charset=utf8mb4',
            $host,
            $port,
            $database
        );

        return new PDO($dsn, $user, $password, [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES => false,
        ]);
    }
}
