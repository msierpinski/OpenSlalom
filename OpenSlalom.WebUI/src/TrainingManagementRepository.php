<?php

declare(strict_types=1);

final class TrainingManagementRepository
{
    public function __construct(private readonly PDO $connection)
    {
    }

    public function lookups(): array
    {
        return [
            'clubs' => $this->connection->query('SELECT id, vereinsname AS name FROM vereine WHERE is_deleted = 0 ORDER BY vereinsname')->fetchAll(),
            'disciplines' => $this->connection->query('SELECT id, disziplin AS name FROM disziplin WHERE is_deleted = 0 ORDER BY disziplin')->fetchAll(),
            'weather' => $this->connection->query('SELECT id, wetter AS name FROM wetter WHERE is_deleted = 0 ORDER BY wetter')->fetchAll(),
        ];
    }

    public function findByUuid(string $uuid): ?array
    {
        $statement = $this->connection->prepare(
            <<<'SQL'
            SELECT id, uuid, name, beschreibung, zeitpunkt, fk_id_verein AS verein_id,
                   fk_id_disziplin AS disziplin_id, fk_id_wetter AS wetter_id,
                   training_abgeschlossen, ist_veroeffentlicht
            FROM training
            WHERE uuid = :uuid AND is_deleted = 0
            LIMIT 1
            SQL
        );
        $statement->execute(['uuid' => $uuid]);
        $training = $statement->fetch();

        return is_array($training) ? $training : null;
    }

    public function create(array $values): string
    {
        $values = $this->validate($values);
        $uuid = self::newUuid();
        $statement = $this->connection->prepare(
            <<<'SQL'
            INSERT INTO training (
                uuid, fk_id_verein, fk_id_disziplin, fk_id_wetter, name, beschreibung,
                zeitpunkt, training_abgeschlossen, ist_veroeffentlicht,
                updated_at_utc, is_deleted, deleted_at_utc
            ) VALUES (
                :uuid, :verein_id, :disziplin_id, :wetter_id, :name, :beschreibung,
                :zeitpunkt, :training_abgeschlossen, :ist_veroeffentlicht,
                UTC_TIMESTAMP(), 0, NULL
            )
            SQL
        );
        $statement->execute(['uuid' => $uuid, ...$values]);

        return $uuid;
    }

    public function update(string $uuid, array $values): bool
    {
        if ($this->findByUuid($uuid) === null) {
            return false;
        }

        $values = $this->validate($values);
        $statement = $this->connection->prepare(
            <<<'SQL'
            UPDATE training
            SET fk_id_verein = :verein_id,
                fk_id_disziplin = :disziplin_id,
                fk_id_wetter = :wetter_id,
                name = :name,
                beschreibung = :beschreibung,
                zeitpunkt = :zeitpunkt,
                training_abgeschlossen = :training_abgeschlossen,
                ist_veroeffentlicht = :ist_veroeffentlicht,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE uuid = :uuid AND is_deleted = 0
            SQL
        );
        $statement->execute(['uuid' => $uuid, ...$values]);

        return true;
    }

    private function validate(array $values): array
    {
        $name = trim((string) ($values['name'] ?? ''));
        $description = trim((string) ($values['beschreibung'] ?? ''));
        $date = (string) ($values['zeitpunkt'] ?? '');
        $clubId = filter_var($values['verein_id'] ?? null, FILTER_VALIDATE_INT, ['options' => ['min_range' => 1]]);
        $disciplineId = filter_var($values['disziplin_id'] ?? null, FILTER_VALIDATE_INT, ['options' => ['min_range' => 1]]);
        $weatherId = filter_var($values['wetter_id'] ?? null, FILTER_VALIDATE_INT, ['options' => ['min_range' => 1]]);
        $parsedDate = DateTimeImmutable::createFromFormat('!Y-m-d', $date);

        if ($name === '' || strlen($name) > 100) {
            throw new InvalidArgumentException('Der Trainingsname muss zwischen 1 und 100 Zeichen enthalten.');
        }
        if ($description === '' || strlen($description) > 250) {
            throw new InvalidArgumentException('Die Beschreibung muss zwischen 1 und 250 Zeichen enthalten.');
        }
        if ($parsedDate === false || $parsedDate->format('Y-m-d') !== $date) {
            throw new InvalidArgumentException('Bitte ein gültiges Trainingsdatum eingeben.');
        }
        if ($clubId === false || $disciplineId === false || $weatherId === false) {
            throw new InvalidArgumentException('Bitte Verein, Disziplin und Wetter vollständig auswählen.');
        }

        $this->assertLookupExists('vereine', (int) $clubId);
        $this->assertLookupExists('disziplin', (int) $disciplineId);
        $this->assertLookupExists('wetter', (int) $weatherId);

        return [
            'verein_id' => (int) $clubId,
            'disziplin_id' => (int) $disciplineId,
            'wetter_id' => (int) $weatherId,
            'name' => $name,
            'beschreibung' => $description,
            'zeitpunkt' => $date,
            'training_abgeschlossen' => !empty($values['training_abgeschlossen']) ? 1 : 0,
            'ist_veroeffentlicht' => !empty($values['ist_veroeffentlicht']) ? 1 : 0,
        ];
    }

    private function assertLookupExists(string $table, int $id): void
    {
        $allowedTables = ['vereine', 'disziplin', 'wetter'];
        if (!in_array($table, $allowedTables, true)) {
            throw new LogicException('Ungültige Nachschlagetabelle.');
        }

        $statement = $this->connection->prepare("SELECT 1 FROM {$table} WHERE id = :id AND is_deleted = 0");
        $statement->execute(['id' => $id]);
        if ($statement->fetchColumn() === false) {
            throw new InvalidArgumentException('Eine ausgewählte Stammdaten-Zuordnung ist nicht mehr verfügbar.');
        }
    }

    private static function newUuid(): string
    {
        $bytes = random_bytes(16);
        $bytes[6] = chr((ord($bytes[6]) & 0x0f) | 0x40);
        $bytes[8] = chr((ord($bytes[8]) & 0x3f) | 0x80);
        $hex = bin2hex($bytes);

        return sprintf('%s-%s-%s-%s-%s', substr($hex, 0, 8), substr($hex, 8, 4), substr($hex, 12, 4), substr($hex, 16, 4), substr($hex, 20));
    }
}
