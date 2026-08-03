<?php

declare(strict_types=1);

final class MasterDataRepository
{
    private const Types = ['vereine', 'fahrer', 'disziplinen', 'karts', 'wetter'];

    public function __construct(private readonly PDO $connection)
    {
    }

    public function list(string $type, string $search, int $page, int $perPage): array
    {
        $search = '%' . $search . '%';
        return match ($this->assertType($type)) {
            'vereine' => $this->paginate("SELECT COUNT(*) FROM vereine WHERE is_deleted = 0 AND CONCAT_WS(' ', vereinsname, mitglieds_nummer, ort) LIKE :q", "SELECT id, vereinsname, mitglieds_nummer, postleitzahl, ort, adresse, logo IS NOT NULL AS has_logo FROM vereine WHERE is_deleted = 0 AND CONCAT_WS(' ', vereinsname, mitglieds_nummer, ort) LIKE :q ORDER BY vereinsname", ['q' => $search], $page, $perPage),
            'fahrer' => $this->paginate("SELECT COUNT(*) FROM fahrer f INNER JOIN vereine v ON v.id = f.fk_id_verein AND v.is_deleted = 0 WHERE f.is_deleted = 0 AND CONCAT_WS(' ', f.vorname, f.nachname, f.mitglieds_nummer, v.vereinsname) LIKE :q", "SELECT f.id, f.vorname, COALESCE(f.nachname, '') AS nachname, f.mitglieds_nummer, f.geburtsdatum, f.geschlecht, v.vereinsname FROM fahrer f INNER JOIN vereine v ON v.id = f.fk_id_verein AND v.is_deleted = 0 WHERE f.is_deleted = 0 AND CONCAT_WS(' ', f.vorname, f.nachname, f.mitglieds_nummer, v.vereinsname) LIKE :q ORDER BY f.vorname, f.nachname", ['q' => $search], $page, $perPage),
            'disziplinen' => $this->paginate('SELECT COUNT(*) FROM disziplin WHERE is_deleted = 0 AND disziplin LIKE :q', 'SELECT id, disziplin AS name, tf, pf FROM disziplin WHERE is_deleted = 0 AND disziplin LIKE :q ORDER BY disziplin', ['q' => $search], $page, $perPage),
            'karts' => $this->paginate("SELECT COUNT(*) FROM karts k INNER JOIN vereine v ON v.id = k.fk_id_verein AND v.is_deleted = 0 INNER JOIN disziplin d ON d.id = k.fk_id_disziplin AND d.is_deleted = 0 WHERE k.is_deleted = 0 AND CONCAT_WS(' ', k.Name, k.Motor, k.Chassis, v.vereinsname, d.disziplin) LIKE :q", "SELECT k.id, k.Name AS name, k.Motor AS motor, k.Chassis AS chassis, v.vereinsname, d.disziplin FROM karts k INNER JOIN vereine v ON v.id = k.fk_id_verein AND v.is_deleted = 0 INNER JOIN disziplin d ON d.id = k.fk_id_disziplin AND d.is_deleted = 0 WHERE k.is_deleted = 0 AND CONCAT_WS(' ', k.Name, k.Motor, k.Chassis, v.vereinsname, d.disziplin) LIKE :q ORDER BY k.Name", ['q' => $search], $page, $perPage),
            'wetter' => $this->paginate('SELECT COUNT(*) FROM wetter WHERE is_deleted = 0 AND wetter LIKE :q', 'SELECT id, wetter AS name FROM wetter WHERE is_deleted = 0 AND wetter LIKE :q ORDER BY wetter', ['q' => $search], $page, $perPage),
        };
    }

    public function find(string $type, int $id): ?array
    {
        $type = $this->assertType($type);
        $queries = [
            'vereine' => 'SELECT id, vereinsname, mitglieds_nummer, postleitzahl, ort, adresse, logo FROM vereine WHERE id = :id AND is_deleted = 0',
            'fahrer' => 'SELECT id, fk_id_verein AS verein_id, vorname, COALESCE(nachname, \'\') AS nachname, mitglieds_nummer, geburtsdatum, geschlecht FROM fahrer WHERE id = :id AND is_deleted = 0',
            'disziplinen' => 'SELECT id, disziplin AS name, tf, pf FROM disziplin WHERE id = :id AND is_deleted = 0',
            'karts' => 'SELECT id, fk_id_verein AS verein_id, fk_id_disziplin AS disziplin_id, COALESCE(Name, \'\') AS name, COALESCE(Motor, \'\') AS motor, COALESCE(Chassis, \'\') AS chassis FROM karts WHERE id = :id AND is_deleted = 0',
            'wetter' => 'SELECT id, wetter AS name FROM wetter WHERE id = :id AND is_deleted = 0',
        ];
        $statement = $this->connection->prepare($queries[$type]);
        $statement->execute(['id' => $id]);
        $item = $statement->fetch();
        if (!is_array($item)) {
            return null;
        }
        if ($type === 'disziplinen') {
            $item['altersklassen'] = $this->ageClasses($id);
        }

        return $item;
    }

    public function lookups(string $type): array
    {
        $type = $this->assertType($type);
        return match ($type) {
            'fahrer' => ['clubs' => $this->activeClubs()],
            'karts' => ['clubs' => $this->activeClubs(), 'disciplines' => $this->activeDisciplines()],
            default => [],
        };
    }

    public function create(string $type, array $values, array $files): int
    {
        $type = $this->assertType($type);
        return match ($type) {
            'vereine' => $this->saveClub(null, $values, $files),
            'fahrer' => $this->saveDriver(null, $values),
            'disziplinen' => $this->saveDiscipline(null, $values),
            'karts' => $this->saveKart(null, $values),
            'wetter' => $this->saveWeather(null, $values),
        };
    }

    public function update(string $type, int $id, array $values, array $files): bool
    {
        if ($this->find($type, $id) === null) {
            return false;
        }
        $type = $this->assertType($type);
        match ($type) {
            'vereine' => $this->saveClub($id, $values, $files),
            'fahrer' => $this->saveDriver($id, $values),
            'disziplinen' => $this->saveDiscipline($id, $values),
            'karts' => $this->saveKart($id, $values),
            'wetter' => $this->saveWeather($id, $values),
        };

        return true;
    }

    public function delete(string $type, int $id): bool
    {
        $table = match ($this->assertType($type)) {
            'vereine' => 'vereine', 'fahrer' => 'fahrer', 'disziplinen' => 'disziplin', 'karts' => 'karts', 'wetter' => 'wetter',
        };
        $statement = $this->connection->prepare("UPDATE {$table} SET is_deleted = 1, deleted_at_utc = UTC_TIMESTAMP(), updated_at_utc = UTC_TIMESTAMP() WHERE id = :id AND is_deleted = 0");
        $statement->execute(['id' => $id]);
        return $statement->rowCount() === 1;
    }

    private function saveClub(?int $id, array $values, array $files): int
    {
        $name = $this->required($values['vereinsname'] ?? '', 'Der Vereinsname ist erforderlich.', 100);
        $member = $this->text($values['mitglieds_nummer'] ?? '', 50);
        $postal = $this->text($values['postleitzahl'] ?? '', 20);
        $city = $this->text($values['ort'] ?? '', 100);
        $address = $this->text($values['adresse'] ?? '', 250);
        $logo = $this->logoFromUpload($files['logo'] ?? null);
        $removeLogo = !empty($values['logo_loeschen']);
        if ($removeLogo) {
            $logo = null;
        }

        if ($id === null) {
            $statement = $this->connection->prepare('INSERT INTO vereine (vereinsname, mitglieds_nummer, postleitzahl, ort, adresse, logo, updated_at_utc, is_deleted) VALUES (:name, :member, :postal, :city, :address, :logo, UTC_TIMESTAMP(), 0)');
            $statement->bindValue('name', $name);
            $statement->bindValue('member', $member);
            $statement->bindValue('postal', $postal);
            $statement->bindValue('city', $city);
            $statement->bindValue('address', $address);
            $statement->bindValue('logo', $logo, $logo === null ? PDO::PARAM_NULL : PDO::PARAM_LOB);
            $statement->execute();
            return (int) $this->connection->lastInsertId();
        }

        $sql = 'UPDATE vereine SET vereinsname = :name, mitglieds_nummer = :member, postleitzahl = :postal, ort = :city, adresse = :address, updated_at_utc = UTC_TIMESTAMP()';
        if ($logo !== null) $sql .= ', logo = :logo';
        elseif ($removeLogo) $sql .= ', logo = NULL';
        $sql .= ' WHERE id = :id AND is_deleted = 0';
        $statement = $this->connection->prepare($sql);
        $statement->bindValue('name', $name);
        $statement->bindValue('member', $member);
        $statement->bindValue('postal', $postal);
        $statement->bindValue('city', $city);
        $statement->bindValue('address', $address);
        $statement->bindValue('id', $id, PDO::PARAM_INT);
        if ($logo !== null) $statement->bindValue('logo', $logo, PDO::PARAM_LOB);
        $statement->execute();
        return $id;
    }

    private function saveDriver(?int $id, array $values): int
    {
        $clubId = $this->lookupId($values['verein_id'] ?? null, 'vereine');
        $first = $this->required($values['vorname'] ?? '', 'Der Vorname ist erforderlich.', 100);
        $last = $this->nullableText($values['nachname'] ?? '', 100);
        $member = $this->text($values['mitglieds_nummer'] ?? '', 50);
        $birth = $this->nullableDate($values['geburtsdatum'] ?? '');
        $gender = (string) ($values['geschlecht'] ?? '');
        if (!in_array($gender, ['', 'm', 'w', 'd'], true)) throw new InvalidArgumentException('Ungültiges Geschlecht.');
        $parameters = ['club' => $clubId, 'first' => $first, 'last' => $last, 'member' => $member, 'birth' => $birth, 'gender' => $gender];
        if ($id === null) {
            $statement = $this->connection->prepare('INSERT INTO fahrer (fk_id_verein, vorname, nachname, mitglieds_nummer, geburtsdatum, geschlecht, updated_at_utc, is_deleted) VALUES (:club, :first, :last, :member, :birth, :gender, UTC_TIMESTAMP(), 0)');
            $statement->execute($parameters);
            return (int) $this->connection->lastInsertId();
        }
        $statement = $this->connection->prepare('UPDATE fahrer SET fk_id_verein = :club, vorname = :first, nachname = :last, mitglieds_nummer = :member, geburtsdatum = :birth, geschlecht = :gender, updated_at_utc = UTC_TIMESTAMP() WHERE id = :id AND is_deleted = 0');
        $statement->execute(['id' => $id, ...$parameters]);
        return $id;
    }

    private function saveDiscipline(?int $id, array $values): int
    {
        $name = $this->required($values['name'] ?? '', 'Der Disziplinname ist erforderlich.', 50);
        $tf = $this->penalty($values['tf'] ?? '', 'Torfehler');
        $pf = $this->penalty($values['pf'] ?? '', 'Pylonenfehler');
        $classes = $this->validateAgeClasses($values);
        $this->connection->beginTransaction();
        try {
            if ($id === null) {
                $statement = $this->connection->prepare('INSERT INTO disziplin (disziplin, tf, pf, updated_at_utc, is_deleted) VALUES (:name, :tf, :pf, UTC_TIMESTAMP(), 0)');
                $statement->execute(['name' => $name, 'tf' => $tf, 'pf' => $pf]);
                $id = (int) $this->connection->lastInsertId();
            } else {
                $this->connection->prepare('UPDATE disziplin SET disziplin = :name, tf = :tf, pf = :pf, updated_at_utc = UTC_TIMESTAMP() WHERE id = :id AND is_deleted = 0')
                    ->execute(['id' => $id, 'name' => $name, 'tf' => $tf, 'pf' => $pf]);
                $this->connection->prepare('UPDATE disziplin_altersklassen SET is_deleted = 1, deleted_at_utc = UTC_TIMESTAMP(), updated_at_utc = UTC_TIMESTAMP() WHERE fk_id_disziplin = :id AND is_deleted = 0')
                    ->execute(['id' => $id]);
            }
            $statement = $this->connection->prepare('INSERT INTO disziplin_altersklassen (fk_id_disziplin, bezeichnung, alter_von, alter_bis, updated_at_utc, is_deleted) VALUES (:discipline, :label, :from, :to, UTC_TIMESTAMP(), 0)');
            foreach ($classes as $class) $statement->execute(['discipline' => $id, 'label' => $class['label'], 'from' => $class['from'], 'to' => $class['to']]);
            $this->connection->commit();
            return $id;
        } catch (Throwable $exception) {
            $this->connection->rollBack();
            throw $exception;
        }
    }

    private function saveKart(?int $id, array $values): int
    {
        $clubId = $this->lookupId($values['verein_id'] ?? null, 'vereine');
        $disciplineId = $this->lookupId($values['disziplin_id'] ?? null, 'disziplin');
        $parameters = ['club' => $clubId, 'discipline' => $disciplineId, 'name' => $this->nullableText($values['name'] ?? '', 100), 'motor' => $this->nullableText($values['motor'] ?? '', 100), 'chassis' => $this->nullableText($values['chassis'] ?? '', 100)];
        if ($id === null) {
            $statement = $this->connection->prepare('INSERT INTO karts (fk_id_verein, fk_id_disziplin, Name, Motor, Chassis, updated_at_utc, is_deleted) VALUES (:club, :discipline, :name, :motor, :chassis, UTC_TIMESTAMP(), 0)');
            $statement->execute($parameters);
            return (int) $this->connection->lastInsertId();
        }
        $statement = $this->connection->prepare('UPDATE karts SET fk_id_verein = :club, fk_id_disziplin = :discipline, Name = :name, Motor = :motor, Chassis = :chassis, updated_at_utc = UTC_TIMESTAMP() WHERE id = :id AND is_deleted = 0');
        $statement->execute(['id' => $id, ...$parameters]);
        return $id;
    }

    private function saveWeather(?int $id, array $values): int
    {
        $name = $this->required($values['name'] ?? '', 'Die Wetterbezeichnung ist erforderlich.', 50);
        if ($id === null) {
            $this->connection->prepare('INSERT INTO wetter (wetter, updated_at_utc, is_deleted) VALUES (:name, UTC_TIMESTAMP(), 0)')->execute(['name' => $name]);
            return (int) $this->connection->lastInsertId();
        }
        $this->connection->prepare('UPDATE wetter SET wetter = :name, updated_at_utc = UTC_TIMESTAMP() WHERE id = :id AND is_deleted = 0')->execute(['id' => $id, 'name' => $name]);
        return $id;
    }

    private function validateAgeClasses(array $values): array
    {
        $labels = $values['age_label'] ?? [];
        $froms = $values['age_from'] ?? [];
        $tos = $values['age_to'] ?? [];
        if (!is_array($labels) || !is_array($froms) || !is_array($tos)) throw new InvalidArgumentException('Ungültige Altersklassen.');
        $classes = [];
        foreach ($labels as $index => $label) {
            $label = trim((string) $label);
            if ($label === '') continue;
            $from = filter_var($froms[$index] ?? null, FILTER_VALIDATE_INT, ['options' => ['min_range' => 0]]);
            $toText = trim((string) ($tos[$index] ?? ''));
            $to = $toText === '' ? null : filter_var($toText, FILTER_VALIDATE_INT, ['options' => ['min_range' => 0]]);
            if ($from === false || ($toText !== '' && $to === false) || ($to !== null && $to < $from)) throw new InvalidArgumentException('Ungültiger Altersbereich.');
            $classes[] = ['label' => $this->required($label, 'Die Klassenbezeichnung ist erforderlich.', 100), 'from' => (int) $from, 'to' => $to === null ? null : (int) $to];
        }
        usort($classes, static fn(array $a, array $b): int => ($a['from'] <=> $b['from']) ?: (($a['to'] ?? PHP_INT_MAX) <=> ($b['to'] ?? PHP_INT_MAX)));
        foreach ($classes as $index => $class) if ($index > 0 && $class['from'] <= ($classes[$index - 1]['to'] ?? PHP_INT_MAX)) throw new InvalidArgumentException('Altersklassen dürfen sich nicht überschneiden.');
        return $classes;
    }

    private function ageClasses(int $disciplineId): array { $s = $this->connection->prepare('SELECT bezeichnung AS label, alter_von AS age_from, alter_bis AS age_to FROM disziplin_altersklassen WHERE fk_id_disziplin = :id AND is_deleted = 0 ORDER BY alter_von, COALESCE(alter_bis, 2147483647)'); $s->execute(['id' => $disciplineId]); return $s->fetchAll(); }
    private function activeClubs(): array { return $this->connection->query('SELECT id, vereinsname AS name FROM vereine WHERE is_deleted = 0 ORDER BY vereinsname')->fetchAll(); }
    private function activeDisciplines(): array { return $this->connection->query('SELECT id, disziplin AS name FROM disziplin WHERE is_deleted = 0 ORDER BY disziplin')->fetchAll(); }
    private function lookupId(mixed $id, string $table): int { $id = filter_var($id, FILTER_VALIDATE_INT, ['options' => ['min_range' => 1]]); if ($id === false) throw new InvalidArgumentException('Bitte eine gültige Zuordnung auswählen.'); $s = $this->connection->prepare("SELECT 1 FROM {$table} WHERE id = :id AND is_deleted = 0"); $s->execute(['id' => $id]); if ($s->fetchColumn() === false) throw new InvalidArgumentException('Eine ausgewählte Zuordnung ist nicht verfügbar.'); return (int) $id; }
    private function required(mixed $value, string $message, int $max): string { $value = trim((string) $value); if ($value === '' || strlen($value) > $max) throw new InvalidArgumentException($message); return $value; }
    private function text(mixed $value, int $max): string { $value = trim((string) $value); if (strlen($value) > $max) throw new InvalidArgumentException('Ein Textfeld ist zu lang.'); return $value; }
    private function nullableText(mixed $value, int $max): ?string { $value = $this->text($value, $max); return $value === '' ? null : $value; }
    private function nullableDate(mixed $value): ?string { $value = trim((string) $value); if ($value === '') return null; $d = DateTimeImmutable::createFromFormat('!Y-m-d', $value); if ($d === false || $d->format('Y-m-d') !== $value) throw new InvalidArgumentException('Ungültiges Geburtsdatum.'); return $value; }
    private function penalty(mixed $value, string $label): float { $value = str_replace(',', '.', trim((string) $value)); if (!is_numeric($value) || !is_finite((float) $value) || (float) $value < 0) throw new InvalidArgumentException("Die Zeitstrafe für {$label} muss eine nicht-negative Zahl sein."); return (float) $value; }
    private function logoFromUpload(mixed $file): ?string { if (!is_array($file) || ($file['error'] ?? UPLOAD_ERR_NO_FILE) === UPLOAD_ERR_NO_FILE) return null; if (($file['error'] ?? UPLOAD_ERR_OK) !== UPLOAD_ERR_OK || !is_uploaded_file($file['tmp_name'] ?? '') || ($file['size'] ?? 0) > 2 * 1024 * 1024) throw new InvalidArgumentException('Das Vereinslogo konnte nicht verarbeitet werden.'); $image = @getimagesize($file['tmp_name']); if ($image === false || !in_array($image[2], [IMAGETYPE_JPEG, IMAGETYPE_PNG, IMAGETYPE_BMP], true)) throw new InvalidArgumentException('Das Vereinslogo muss eine PNG-, JPG- oder BMP-Datei sein.'); $data = file_get_contents($file['tmp_name']); if ($data === false) throw new InvalidArgumentException('Das Vereinslogo konnte nicht gelesen werden.'); return $data; }
    private function assertType(string $type): string { if (!in_array($type, self::Types, true)) throw new InvalidArgumentException('Unbekannter Verwaltungsbereich.'); return $type; }

    private function paginate(string $countSql, string $itemsSql, array $parameters, int $page, int $perPage): array
    {
        $count = $this->connection->prepare($countSql);
        $count->execute($parameters);
        $total = (int) $count->fetchColumn();
        $perPage = max(1, min(100, $perPage));
        $pages = max(1, (int) ceil($total / $perPage));
        $page = min(max(1, $page), $pages);
        $offset = ($page - 1) * $perPage;
        $items = $this->connection->prepare($itemsSql . " LIMIT {$perPage} OFFSET {$offset}");
        $items->execute($parameters);

        return ['items' => $items->fetchAll(), 'pagination' => ['page' => $page, 'pages' => $pages, 'total' => $total]];
    }
}
