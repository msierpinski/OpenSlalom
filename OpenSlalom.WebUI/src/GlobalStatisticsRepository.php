<?php

declare(strict_types=1);

final class GlobalStatisticsRepository
{
    public function __construct(private readonly PDO $connection)
    {
    }

    public function build(string $fromDate, string $toDate): array
    {
        $parameters = ['from' => $fromDate, 'to' => $toDate];
        $stints = $this->fetchAll(
            <<<'SQL'
            SELECT s.id, s.fk_id_fahrer AS fahrer_id, s.fk_id_kart AS kart_id,
                   s.fk_id_training AS training_id, f.vorname, COALESCE(f.nachname, '') AS nachname,
                   COALESCE(k.Name, '') AS kart_name
            FROM tstints s
            INNER JOIN training t ON t.id = s.fk_id_training AND t.is_deleted = 0
            INNER JOIN fahrer f ON f.id = s.fk_id_fahrer AND f.is_deleted = 0
            LEFT JOIN karts k ON k.id = s.fk_id_kart AND k.is_deleted = 0
            WHERE s.is_deleted = 0 AND t.zeitpunkt BETWEEN :from AND :to
            SQL,
            $parameters
        );
        $rounds = $this->fetchAll(
            <<<'SQL'
            SELECT s.id AS stint_id, s.fk_id_fahrer AS fahrer_id, r.rundenzeit, COALESCE(r.pf, 0) AS pf,
                   COALESCE(r.tf, 0) AS tf, r.ungueltig
            FROM trunden r
            INNER JOIN tstints s ON s.id = r.fk_id_tstint AND s.is_deleted = 0
            INNER JOIN training t ON t.id = s.fk_id_training AND t.is_deleted = 0
            WHERE r.is_deleted = 0 AND r.rundenzeit IS NOT NULL AND r.rundenzeit > 0
              AND t.zeitpunkt BETWEEN :from AND :to
            SQL,
            $parameters
        );

        $byDriver = [];
        $byKart = [];
        $stintToKart = [];
        foreach ($stints as $stint) {
            $driverId = (int) $stint['fahrer_id'];
            $kartKey = $stint['kart_id'] === null ? 'none' : (string) $stint['kart_id'];
            $driverName = display_name((string) $stint['vorname'], (string) $stint['nachname']);
            $stintToKart[(int) $stint['id']] = $kartKey;
            $byDriver[$driverId] ??= [
                'name' => $driverName,
                'stints' => 0,
                'training_ids' => [],
                'rounds' => 0,
                'seconds' => 0.0,
                'error_free' => 0,
                'pf' => 0,
                'tf' => 0,
            ];
            $byDriver[$driverId]['stints']++;
            $byDriver[$driverId]['training_ids'][(int) $stint['training_id']] = true;
            $byKart[$kartKey] ??= [
                'name' => trim((string) $stint['kart_name']) ?: 'Ohne Kartzuordnung',
                'stints' => 0,
                'rounds' => 0,
                'seconds' => 0.0,
                'pf' => 0,
                'tf' => 0,
                'drivers' => [],
            ];
            $byKart[$kartKey]['stints']++;
            $byKart[$kartKey]['drivers'][$driverId] ??= [
                'name' => $driverName,
                'stints' => 0,
                'rounds' => 0,
                'seconds' => 0.0,
                'pf' => 0,
                'tf' => 0,
            ];
            $byKart[$kartKey]['drivers'][$driverId]['stints']++;
        }

        $totalSeconds = 0.0;
        $totalPf = 0;
        $totalTf = 0;
        $errorFree = 0;
        foreach ($rounds as $round) {
            $driverId = (int) $round['fahrer_id'];
            if (!isset($byDriver[$driverId])) {
                continue;
            }
            $seconds = (float) $round['rundenzeit'];
            $pf = (int) $round['pf'];
            $tf = (int) $round['tf'];
            $invalid = (bool) $round['ungueltig'];
            $totalSeconds += $seconds;
            $totalPf += $pf;
            $totalTf += $tf;
            $byDriver[$driverId]['rounds']++;
            $byDriver[$driverId]['seconds'] += $seconds;
            $byDriver[$driverId]['pf'] += $pf;
            $byDriver[$driverId]['tf'] += $tf;
            $kartKey = $stintToKart[(int) $round['stint_id']] ?? 'none';
            if (isset($byKart[$kartKey])) {
                $byKart[$kartKey]['rounds']++;
                $byKart[$kartKey]['seconds'] += $seconds;
                $byKart[$kartKey]['pf'] += $pf;
                $byKart[$kartKey]['tf'] += $tf;
                $byKart[$kartKey]['drivers'][$driverId]['rounds']++;
                $byKart[$kartKey]['drivers'][$driverId]['seconds'] += $seconds;
                $byKart[$kartKey]['drivers'][$driverId]['pf'] += $pf;
                $byKart[$kartKey]['drivers'][$driverId]['tf'] += $tf;
            }
            if (!$invalid && $pf === 0 && $tf === 0) {
                $errorFree++;
                $byDriver[$driverId]['error_free']++;
            }
        }

        $driverRows = array_values($byDriver);
        foreach ($driverRows as &$driver) {
            $driver['trainings'] = count($driver['training_ids']);
            $driver['error_free_percent'] = $driver['rounds'] > 0 ? $driver['error_free'] / $driver['rounds'] * 100 : 0.0;
            $driver['average_pf'] = $driver['rounds'] > 0 ? $driver['pf'] / $driver['rounds'] : 0.0;
            $driver['average_tf'] = $driver['rounds'] > 0 ? $driver['tf'] / $driver['rounds'] : 0.0;
            unset($driver['training_ids']);
        }
        unset($driver);
        usort($driverRows, static fn (array $a, array $b): int => strcasecmp($a['name'], $b['name']));

        $kartRows = array_values($byKart);
        foreach ($kartRows as &$kart) {
            $kart['drivers'] = array_values($kart['drivers']);
            foreach ($kart['drivers'] as &$driver) {
                $driver['average_pf'] = $driver['rounds'] > 0 ? $driver['pf'] / $driver['rounds'] : 0.0;
                $driver['average_tf'] = $driver['rounds'] > 0 ? $driver['tf'] / $driver['rounds'] : 0.0;
            }
            unset($driver);
            usort($kart['drivers'], static fn (array $a, array $b): int => strcasecmp($a['name'], $b['name']));
            $kart['average_pf'] = $kart['rounds'] > 0 ? $kart['pf'] / $kart['rounds'] : 0.0;
            $kart['average_tf'] = $kart['rounds'] > 0 ? $kart['tf'] / $kart['rounds'] : 0.0;
        }
        unset($kart);
        usort($kartRows, static fn (array $a, array $b): int => strcasecmp($a['name'], $b['name']));

        $trainingCount = $this->count('SELECT COUNT(*) FROM training WHERE is_deleted = 0 AND zeitpunkt BETWEEN :from AND :to', $parameters);
        $kartCount = count(array_unique(array_filter(array_column($stints, 'kart_id'), static fn (mixed $id): bool => $id !== null)));
        $roundCount = count($rounds);

        return [
            'summary' => [
                'drivers' => count($byDriver),
                'karts' => $kartCount,
                'trainings' => $trainingCount,
                'stints' => count($stints),
                'rounds' => $roundCount,
                'seconds' => $totalSeconds,
                'pf' => $totalPf,
                'tf' => $totalTf,
                'average_pf' => $roundCount > 0 ? $totalPf / $roundCount : 0.0,
                'average_tf' => $roundCount > 0 ? $totalTf / $roundCount : 0.0,
                'error_free_percent' => $roundCount > 0 ? $errorFree / $roundCount * 100 : 0.0,
            ],
            'drivers' => $driverRows,
            'karts' => $kartRows,
        ];
    }

    private function fetchAll(string $sql, array $parameters): array
    {
        $statement = $this->connection->prepare($sql);
        $statement->execute($parameters);
        return $statement->fetchAll();
    }

    private function count(string $sql, array $parameters): int
    {
        $statement = $this->connection->prepare($sql);
        $statement->execute($parameters);
        return (int) $statement->fetchColumn();
    }
}
