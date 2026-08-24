<?php

declare(strict_types=1);

final class TrainingRepository
{
    public function __construct(private readonly PDO $connection)
    {
    }

    public function findByUuid(string $uuid, ?array $currentUser): ?array
    {
        [$visibilitySql, $visibilityParameters] = $this->visibilityConstraint($currentUser);
        $training = $this->fetchOne(
            <<<SQL
            SELECT
                t.id,
                t.uuid,
                t.name,
                t.beschreibung,
                t.zeitpunkt,
                t.training_abgeschlossen,
                t.aktiver_fahrer_zeitnahme_1_id,
                t.aktiver_fahrer_zeitnahme_2_id,
                t.naechster_fahrer_zeitnahme_1_id,
                t.naechster_fahrer_zeitnahme_2_id,
                t.fk_id_disziplin AS disziplin_id,
                v.vereinsname,
                d.disziplin,
                d.tf AS torfehler_sekunden,
                d.pf AS pylonen_sekunden,
                w.wetter
            FROM training t
            INNER JOIN vereine v ON v.id = t.fk_id_verein AND v.is_deleted = 0
            INNER JOIN disziplin d ON d.id = t.fk_id_disziplin AND d.is_deleted = 0
            INNER JOIN wetter w ON w.id = t.fk_id_wetter AND w.is_deleted = 0
            WHERE t.uuid = :uuid AND t.is_deleted = 0 AND {$visibilitySql}
            LIMIT 1
            SQL,
            ['uuid' => $uuid, ...$visibilityParameters]
        );

        if ($training === null) {
            return null;
        }

        $trainingId = (int) $training['id'];
        $disciplineId = (int) $training['disziplin_id'];

        return [
            'training' => $training,
            'starters' => $this->fetchAll(
                <<<'SQL'
                SELECT
                    fit.fk_id_fahrer AS fahrer_id,
                    fit.reihenfolge,
                    fit.fahrer_faehrt,
                    f.vorname,
                    COALESCE(f.nachname, '') AS nachname,
                    f.geburtsdatum,
                    fv.vereinsname
                FROM fahrer_im_training fit
                INNER JOIN fahrer f ON f.id = fit.fk_id_fahrer AND f.is_deleted = 0
                INNER JOIN vereine fv ON fv.id = f.fk_id_verein AND fv.is_deleted = 0
                WHERE fit.fk_id_training = :training_id AND fit.is_deleted = 0
                ORDER BY fit.reihenfolge, f.vorname, f.nachname
                SQL,
                ['training_id' => $trainingId]
            ),
            'classes' => $this->fetchAll(
                <<<'SQL'
                SELECT bezeichnung, alter_von, alter_bis
                FROM disziplin_altersklassen
                WHERE fk_id_disziplin = :disziplin_id AND is_deleted = 0
                ORDER BY alter_von, COALESCE(alter_bis, 2147483647)
                SQL,
                ['disziplin_id' => $disciplineId]
            ),
            'stints' => $this->fetchAll(
                <<<'SQL'
                SELECT
                    s.id AS stint_id,
                    s.fk_id_fahrer AS fahrer_id,
                    s.fk_id_kart AS kart_id,
                    s.altersklasse_snapshot,
                    s.datum,
                    f.vorname,
                    COALESCE(f.nachname, '') AS nachname,
                    k.Name AS kart_name
                FROM tstints s
                INNER JOIN fahrer f ON f.id = s.fk_id_fahrer AND f.is_deleted = 0
                LEFT JOIN karts k ON k.id = s.fk_id_kart AND k.is_deleted = 0
                WHERE s.fk_id_training = :training_id AND s.is_deleted = 0
                ORDER BY s.datum, s.id
                SQL,
                ['training_id' => $trainingId]
            ),
            'laps' => $this->fetchAll(
                <<<'SQL'
                SELECT
                    r.id AS runden_id,
                    r.fk_id_tstint AS stint_id,
                    r.runde,
                    r.rundenzeit,
                    COALESCE(r.pf, 0) AS pf,
                    COALESCE(r.tf, 0) AS tf,
                    r.ungueltig
                FROM trunden r
                INNER JOIN tstints s ON s.id = r.fk_id_tstint AND s.is_deleted = 0
                WHERE s.fk_id_training = :training_id AND r.is_deleted = 0
                ORDER BY s.datum, s.id, COALESCE(r.runde, 2147483647), r.id
                SQL,
                ['training_id' => $trainingId]
            ),
        ];
    }

    public function findVisibleTrainings(?array $currentUser, string $search, int $page, int $perPage): array
    {
        [$visibilitySql, $visibilityParameters] = $this->visibilityConstraint($currentUser);
        $parameters = ['q' => '%' . $search . '%', ...$visibilityParameters];
        $where = "t.is_deleted = 0 AND {$visibilitySql} AND CONCAT_WS(' ', t.name, t.beschreibung, v.vereinsname, d.disziplin) LIKE :q";
        $count = $this->connection->prepare(
            <<<SQL
            SELECT COUNT(*)
            FROM training t
            INNER JOIN vereine v ON v.id = t.fk_id_verein AND v.is_deleted = 0
            INNER JOIN disziplin d ON d.id = t.fk_id_disziplin AND d.is_deleted = 0
            WHERE {$where}
            SQL
        );
        $count->execute($parameters);
        $total = (int) $count->fetchColumn();
        $perPage = max(1, min(100, $perPage));
        $pages = max(1, (int) ceil($total / $perPage));
        $page = min(max(1, $page), $pages);
        $offset = ($page - 1) * $perPage;
        $items = $this->fetchAll(
            <<<SQL
            SELECT t.uuid, t.name, t.beschreibung, t.zeitpunkt, t.ist_veroeffentlicht,
                   v.vereinsname, d.disziplin
            FROM training t
            INNER JOIN vereine v ON v.id = t.fk_id_verein AND v.is_deleted = 0
            INNER JOIN disziplin d ON d.id = t.fk_id_disziplin AND d.is_deleted = 0
            WHERE {$where}
            ORDER BY t.zeitpunkt DESC, t.name
            LIMIT {$perPage} OFFSET {$offset}
            SQL,
            $parameters
        );

        return ['items' => $items, 'pagination' => ['page' => $page, 'pages' => $pages, 'total' => $total]];
    }

    private function fetchOne(string $sql, array $parameters): ?array
    {
        $statement = $this->connection->prepare($sql);
        $statement->execute($parameters);
        $result = $statement->fetch();

        return is_array($result) ? $result : null;
    }

    private function fetchAll(string $sql, array $parameters): array
    {
        $statement = $this->connection->prepare($sql);
        $statement->execute($parameters);

        return $statement->fetchAll();
    }

    private function visibilityConstraint(?array $currentUser): array
    {
        if ($currentUser === null) {
            return ['t.ist_veroeffentlicht = 1', []];
        }

        $roles = $currentUser['roles'] ?? [];
        if (in_array('Administrator', $roles, true) || in_array('Trainingsleiter', $roles, true)) {
            return ['1 = 1', []];
        }

        $fahrerId = $currentUser['fahrer_id'] ?? null;
        if (in_array('Fahrer', $roles, true) && is_int($fahrerId)) {
            return [
                '(
                    t.ist_veroeffentlicht = 1
                    OR EXISTS (
                        SELECT 1 FROM fahrer_im_training fit
                        WHERE fit.fk_id_training = t.id
                          AND fit.fk_id_fahrer = :fahrer_id
                          AND fit.is_deleted = 0
                    )
                )',
                ['fahrer_id' => $fahrerId],
            ];
        }

        return ['t.ist_veroeffentlicht = 1', []];
    }
}
