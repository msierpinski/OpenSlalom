<?php

declare(strict_types=1);

final class TrainingViewBuilder
{
    public static function build(array $data): array
    {
        $training = self::normalizeTraining($data['training']);
        $classes = $data['classes'];
        $starters = self::buildStarters($data['starters'], $classes, $training['date_raw']);
        $stints = self::attachLaps($data['stints'], $data['laps']);
        $penaltyTf = (float) $data['training']['torfehler_sekunden'];
        $penaltyPf = (float) $data['training']['pylonen_sekunden'];

        [$leaderboard, $drivers] = self::buildResults($stints, $penaltyTf, $penaltyPf);
        $timestamps = array_column($stints, 'datum');
        $statistics = self::buildStatistics($stints, $starters);

        return [
            'training' => $training,
            'starters' => $starters,
            'leaderboard' => $leaderboard,
            'drivers' => $drivers,
            'statistics' => $statistics,
            'summary' => [
                'registered' => count($starters),
                'participants' => count(array_unique(array_map(
                    static fn (array $stint): int => $stint['fahrer_id'],
                    $stints
                ))),
                'stints' => count($stints),
                'laps' => array_sum(array_map(static fn (array $stint): int => count($stint['laps']), $stints)),
                'started_at' => $timestamps === [] ? null : min($timestamps),
                'finished_at' => $timestamps === [] ? null : max($timestamps),
            ],
            'penalties' => [
                'tf' => $penaltyTf,
                'pf' => $penaltyPf,
            ],
        ];
    }

    private static function normalizeTraining(array $training): array
    {
        return [
            'uuid' => (string) $training['uuid'],
            'name' => (string) $training['name'],
            'description' => (string) $training['beschreibung'],
            'date_raw' => (string) $training['zeitpunkt'],
            'date' => format_date((string) $training['zeitpunkt']),
            'is_finished' => (bool) $training['training_abgeschlossen'],
            'club' => (string) $training['vereinsname'],
            'discipline' => (string) $training['disziplin'],
            'weather' => (string) $training['wetter'],
        ];
    }

    private static function buildStarters(array $starters, array $classes, string $trainingDate): array
    {
        return array_map(
            static function (array $starter, int $index) use ($classes, $trainingDate): array {
                return [
                    'id' => (int) $starter['fahrer_id'],
                    'position' => $index + 1,
                    'name' => display_name((string) $starter['vorname'], (string) $starter['nachname']),
                    'club' => (string) $starter['vereinsname'],
                    'class' => self::resolveClass($starter['geburtsdatum'], $trainingDate, $classes),
                ];
            },
            $starters,
            array_keys($starters)
        );
    }

    private static function resolveClass(mixed $birthDate, string $trainingDate, array $classes): string
    {
        if (!is_string($birthDate) || $birthDate === '' || $trainingDate === '' || $classes === []) {
            return '-';
        }

        try {
            $birth = new DateTimeImmutable($birthDate);
            $date = new DateTimeImmutable($trainingDate);
        } catch (Throwable) {
            return '-';
        }

        if ($date < $birth) {
            return '-';
        }

        $age = $birth->diff($date)->y;
        foreach ($classes as $class) {
            $minimum = (int) $class['alter_von'];
            $maximum = $class['alter_bis'] === null ? null : (int) $class['alter_bis'];
            if ($age >= $minimum && ($maximum === null || $age <= $maximum)) {
                $name = trim((string) $class['bezeichnung']);
                return $name === '' ? '-' : $name;
            }
        }

        return '-';
    }

    private static function attachLaps(array $stints, array $laps): array
    {
        $lapsByStint = [];
        foreach ($laps as $lap) {
            $lapsByStint[(int) $lap['stint_id']][] = [
                'id' => (int) $lap['runden_id'],
                'number' => $lap['runde'] === null ? 0 : (int) $lap['runde'],
                'time' => $lap['rundenzeit'] === null ? null : (float) $lap['rundenzeit'],
                'pf' => (int) $lap['pf'],
                'tf' => (int) $lap['tf'],
                'invalid' => (bool) $lap['ungueltig'],
            ];
        }

        return array_map(
            static fn (array $stint): array => [
                'id' => (int) $stint['stint_id'],
                'fahrer_id' => (int) $stint['fahrer_id'],
                'driver' => display_name((string) $stint['vorname'], (string) $stint['nachname']),
                'class' => trim((string) $stint['altersklasse_snapshot']) ?: '-',
                'kart' => trim((string) ($stint['kart_name'] ?? '')) ?: '-',
                'datum' => (string) $stint['datum'],
                'laps' => $lapsByStint[(int) $stint['stint_id']] ?? [],
            ],
            $stints
        );
    }

    private static function buildResults(array $stints, float $penaltyTf, float $penaltyPf): array
    {
        $byDriver = [];
        foreach ($stints as $stint) {
            $byDriver[$stint['fahrer_id']]['name'] = $stint['driver'];
            $byDriver[$stint['fahrer_id']]['stints'][] = self::calculateStint($stint, $penaltyTf, $penaltyPf);
        }

        $leaderboard = [];
        $drivers = [];
        foreach ($byDriver as $driverId => $driver) {
            $validLaps = [];
            foreach ($driver['stints'] as $stint) {
                foreach ($stint['laps'] as $lap) {
                    if (!$lap['invalid'] && $lap['time'] !== null && $lap['time'] > 0) {
                        $validLaps[] = [
                            'effective' => $lap['time'] + $lap['ranking_penalty'],
                            'date' => $stint['datum'],
                            'class' => $stint['class'],
                            'kart' => $stint['kart'],
                        ];
                    }
                }
            }

            usort($validLaps, static fn (array $a, array $b): int =>
                ($a['effective'] <=> $b['effective']) ?: strcmp($a['date'], $b['date'])
            );
            usort($driver['stints'], static fn (array $a, array $b): int =>
                strcmp($b['datum'], $a['datum']) ?: ($b['id'] <=> $a['id'])
            );

            $drivers[] = [
                'id' => $driverId,
                'name' => $driver['name'],
                'stints' => $driver['stints'],
                'has_valid_lap' => $validLaps !== [],
            ];

            if ($validLaps !== []) {
                $best = $validLaps[0];
                $leaderboard[] = [
                    'driver_id' => $driverId,
                    'driver' => $driver['name'],
                    'class' => $best['class'],
                    'kart' => $best['kart'],
                    'best' => $best['effective'],
                    'average' => array_sum(array_column($validLaps, 'effective')) / count($validLaps),
                    'laps' => count($validLaps),
                    'last_drive' => max(array_column($driver['stints'], 'datum')),
                ];
            }
        }

        usort($leaderboard, static fn (array $a, array $b): int =>
            ($a['best'] <=> $b['best']) ?: strcasecmp($a['driver'], $b['driver'])
        );
        $bestOverall = $leaderboard[0]['best'] ?? null;
        foreach ($leaderboard as $index => &$row) {
            $row['position'] = $index + 1;
            $row['difference'] = $bestOverall === null ? null : $row['best'] - $bestOverall;
        }
        unset($row);

        $rank = array_column($leaderboard, 'position', 'driver_id');
        usort($drivers, static fn (array $a, array $b): int =>
            (($rank[$a['id']] ?? PHP_INT_MAX) <=> ($rank[$b['id']] ?? PHP_INT_MAX))
            ?: strcasecmp($a['name'], $b['name'])
        );

        return [$leaderboard, $drivers];
    }

    private static function buildStatistics(array $stints, array $starters): array
    {
        $driverStatistics = [];
        foreach ($starters as $starter) {
            $driverStatistics[$starter['id']] = [
                'name' => $starter['name'],
                'stints' => 0,
                'rounds' => 0,
                'raw_seconds' => 0.0,
                'error_free_rounds' => 0,
                'pf' => 0,
                'tf' => 0,
            ];
        }

        $totalRounds = 0;
        $totalSeconds = 0.0;
        $totalPf = 0;
        $totalTf = 0;
        $errorFreeRounds = 0;

        foreach ($stints as $stint) {
            $driverId = $stint['fahrer_id'];
            $driverStatistics[$driverId] ??= [
                'name' => $stint['driver'],
                'stints' => 0,
                'rounds' => 0,
                'raw_seconds' => 0.0,
                'error_free_rounds' => 0,
                'pf' => 0,
                'tf' => 0,
            ];
            $driverStatistics[$driverId]['stints']++;

            foreach ($stint['laps'] as $lap) {
                if ($lap['time'] === null || $lap['time'] <= 0) {
                    continue;
                }

                $totalRounds++;
                $totalSeconds += $lap['time'];
                $totalPf += $lap['pf'];
                $totalTf += $lap['tf'];

                $driverStatistics[$driverId]['rounds']++;
                $driverStatistics[$driverId]['raw_seconds'] += $lap['time'];
                $driverStatistics[$driverId]['pf'] += $lap['pf'];
                $driverStatistics[$driverId]['tf'] += $lap['tf'];

                if (!$lap['invalid'] && $lap['pf'] === 0 && $lap['tf'] === 0) {
                    $errorFreeRounds++;
                    $driverStatistics[$driverId]['error_free_rounds']++;
                }
            }
        }

        $driverStatistics = array_values($driverStatistics);
        foreach ($driverStatistics as &$driver) {
            $driver['error_free_percent'] = $driver['rounds'] > 0
                ? ($driver['error_free_rounds'] / $driver['rounds']) * 100
                : 0.0;
            $driver['average_pf'] = $driver['rounds'] > 0 ? $driver['pf'] / $driver['rounds'] : 0.0;
            $driver['average_tf'] = $driver['rounds'] > 0 ? $driver['tf'] / $driver['rounds'] : 0.0;
        }
        unset($driver);

        usort($driverStatistics, static fn (array $a, array $b): int => strcasecmp($a['name'], $b['name']));

        return [
            'total_rounds' => $totalRounds,
            'total_seconds' => $totalSeconds,
            'total_pf' => $totalPf,
            'total_tf' => $totalTf,
            'average_pf' => $totalRounds > 0 ? $totalPf / $totalRounds : 0.0,
            'average_tf' => $totalRounds > 0 ? $totalTf / $totalRounds : 0.0,
            'error_free_rounds' => $errorFreeRounds,
            'error_free_percent' => $totalRounds > 0 ? ($errorFreeRounds / $totalRounds) * 100 : 0.0,
            'drivers' => $driverStatistics,
        ];
    }

    private static function calculateStint(array $stint, float $penaltyTf, float $penaltyPf): array
    {
        $validTotal = 0.0;
        $validCount = 0;
        foreach ($stint['laps'] as &$lap) {
            $lap['ranking_penalty'] = max(0.0, ($lap['tf'] * $penaltyTf) + ($lap['pf'] * $penaltyPf));
            $lap['penalty'] = round(
                max(0.0, (max(0, $lap['tf']) * $penaltyTf) + (max(0, $lap['pf']) * $penaltyPf)),
                3,
                PHP_ROUND_HALF_UP
            );
            if (!$lap['invalid'] && $lap['time'] !== null && $lap['time'] > 0) {
                $validTotal += $lap['time'] + $lap['penalty'];
                $validCount++;
            }
        }
        unset($lap);

        $stint['total'] = $validCount > 0 ? $validTotal : null;
        $stint['average'] = $validCount > 0 ? $validTotal / $validCount : null;
        $stint['valid_laps'] = $validCount;

        return $stint;
    }
}
