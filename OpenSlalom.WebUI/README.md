# OpenSlalom.WebUI

PHP-Weboberfläche für openSlalom. Öffentliche Ergebnisansichten bleiben schreibgeschützt; Administratoren und Trainingsleiter können zusätzlich Trainings anlegen und bearbeiten. Ein synchronisiertes Training kann über seine UUID unter `/training/{UUID}` aufgerufen werden.

## Anforderungen

- PHP 8.1 oder neuer
- PHP-Erweiterung `pdo_mysql`
- Apache 2.4 mit aktiviertem `mod_rewrite`
- MySQL- oder MariaDB-Zugriff auf die von openSlalom synchronisierte Remote-Datenbank

## Einrichtung

1. `OpenSlalom.WebUI` als DocumentRoot des VirtualHosts einrichten.
2. Für das Verzeichnis `AllowOverride All` erlauben, damit `.htaccess` ausgewertet wird.
3. `config.example.php` nach `config.php` kopieren und die Datenbankwerte eintragen.
4. Die Desktop-App einmal starten oder die Remote-Migration ausführen, damit die WebUI-Tabellen angelegt werden.
5. Den ersten Administrator über die Kommandozeile anlegen.

Beispiel für Apache:

```apache
<VirtualHost *:80>
    ServerName slalom.example.org
    DocumentRoot "C:/Projekte/OpenSlalom/OpenSlalom.WebUI"

    <Directory "C:/Projekte/OpenSlalom/OpenSlalom.WebUI">
        AllowOverride All
        Options -Indexes
        Require all granted
    </Directory>
</VirtualHost>
```

Unter Debian/Ubuntu kann das Rewrite-Modul mit `sudo a2enmod rewrite` aktiviert werden. Danach muss Apache neu geladen werden.

## Datenbankkonfiguration

`config.php` wird durch die Repository-`.gitignore` ausgeschlossen. Echte Zugangsdaten dürfen nicht in `config.example.php` eingetragen werden.

```php
return [
    'database' => [
        'host' => '127.0.0.1',
        'port' => 3306,
        'name' => 'os',
        'user' => 'openslalom_web',
        'password' => 'replace-me',
    ],
    'site' => [
        'name' => 'openSlalom',
        'auto_refresh_seconds' => 15,
    ],
];
```

Der Wert `auto_refresh_seconds` steuert die automatische Aktualisierung der Ergebnisse. Mit `0` wird sie deaktiviert.

## Anmeldung und Rollen

Die WebUI verwendet drei Rollen:

- `Administrator`: Benutzerverwaltung und Zugriff auf alle Trainings.
- `Trainingsleiter`: Zugriff auf alle nicht gelöschten Trainings.
- `Fahrer`: Zugriff auf veröffentlichte Trainings und Trainings, denen der zugeordnete Fahrer in der Starterliste zugeteilt ist.
- `Registriert`: Standardrolle nach Selbstregistrierung; Zugriff ausschließlich auf veröffentlichte Trainings, bis ein Administrator Rolle und Fahrerzuordnung ändert.

Jeder Rolle kann ein Fahrerprofil zugeordnet werden. Für die Rolle `Fahrer` ist diese Zuordnung verpflichtend, für Administratoren und Trainingsleiter optional.

Die Anmeldung akzeptiert wahlweise den Benutzernamen oder die hinterlegte E-Mail-Adresse.

Anonyme Besucher können nur veröffentlichte Trainings über ihre UUID aufrufen. Ein nicht freigegebenes Training liefert für nicht berechtigte Besucher HTTP `404`.

Nach Anwendung der Remote-MySQL-Migration muss einmalig ein Administrator per CLI erzeugt werden:

```bash
php tools/create_admin.php
```

Das Skript fragt Benutzername, E-Mail-Adresse und Passwort interaktiv ab. Es legt standardmäßig keinen weiteren Administrator an, wenn bereits einer existiert. Eine bewusst zusätzliche Anlage ist mit `php tools/create_admin.php --force` möglich.

Die Verzeichnisse `src`, `templates` und `tools` werden durch `.htaccess` für HTTP-Zugriffe gesperrt. Das CLI-Skript muss aus dem WebUI-Projektverzeichnis aufgerufen werden.

## Datenbankrechte

Die Trainings- und Ergebnisabfragen bleiben ausschließlich lesend. Die Benutzerverwaltung benötigt zusätzlich Schreibrechte auf die WebUI-Identitätstabellen.

Für den produktiven Betrieb werden zwei Datenbankbenutzer empfohlen:

- Lesekonto: `SELECT` auf den OpenSlalom-Fachtabellen.
- Authentifizierungskonto: begrenzte Rechte auf `web_users`, `web_roles`, `web_user_roles`, `web_login_attempts` und `web_password_reset_tokens`; zusätzlich `SELECT` auf `fahrer` zur Validierung einer Fahrerzuordnung.
- Trainings-Schreibkonto: `SELECT` auf `training`, `vereine`, `disziplin` und `wetter` sowie ausschließlich `INSERT` und `UPDATE` auf `training`.

Beispiel für das Authentifizierungskonto:

```sql
CREATE USER 'openslalom_web_accounts'@'%' IDENTIFIED BY 'ein-langes-zufälliges-passwort';
GRANT SELECT, INSERT, UPDATE ON openslalom.web_users TO 'openslalom_web_accounts'@'%';
GRANT SELECT ON openslalom.web_roles TO 'openslalom_web_accounts'@'%';
GRANT SELECT, INSERT, DELETE ON openslalom.web_user_roles TO 'openslalom_web_accounts'@'%';
GRANT SELECT, INSERT, DELETE ON openslalom.web_login_attempts TO 'openslalom_web_accounts'@'%';
GRANT SELECT, INSERT, DELETE ON openslalom.web_registration_attempts TO 'openslalom_web_accounts'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON openslalom.web_password_reset_tokens TO 'openslalom_web_accounts'@'%';
GRANT SELECT ON openslalom.fahrer TO 'openslalom_web_accounts'@'%';
FLUSH PRIVILEGES;
```

Der erlaubte Host sollte in einer produktiven Umgebung statt `%` auf den Webserver eingeschränkt werden.

`auth_database` in `config.php` kann für dieses getrennte Konto konfiguriert werden. Fehlt der Abschnitt, verwendet die WebUI weiterhin den Hauptdatenbankzugang.

Für die Trainingsverwaltung kann analog `write_database` konfiguriert werden:

```sql
CREATE USER 'openslalom_web_training_writer'@'%' IDENTIFIED BY 'ein-langes-zufälliges-passwort';
GRANT SELECT, INSERT, UPDATE ON openslalom.training TO 'openslalom_web_training_writer'@'%';
GRANT SELECT, INSERT, UPDATE ON openslalom.vereine TO 'openslalom_web_training_writer'@'%';
GRANT SELECT, INSERT, UPDATE ON openslalom.fahrer TO 'openslalom_web_training_writer'@'%';
GRANT SELECT, INSERT, UPDATE ON openslalom.disziplin TO 'openslalom_web_training_writer'@'%';
GRANT SELECT, INSERT, UPDATE ON openslalom.disziplin_altersklassen TO 'openslalom_web_training_writer'@'%';
GRANT SELECT, INSERT, UPDATE ON openslalom.karts TO 'openslalom_web_training_writer'@'%';
GRANT SELECT, INSERT, UPDATE ON openslalom.wetter TO 'openslalom_web_training_writer'@'%';
FLUSH PRIVILEGES;
```

Neue und geänderte Trainings erhalten einen aktuellen Sync-Zeitstempel und werden bei der nächsten bidirektionalen Synchronisierung von der Desktop-App in SQLite übernommen.

## Passwort zurücksetzen

Benutzerkonten besitzen eine eindeutige E-Mail-Adresse. Über `/passwort-vergessen` kann ein zeitlich begrenzter Einmal-Link angefordert werden. Tokens werden ausschließlich gehasht gespeichert, sind 60 Minuten gültig und werden nach Verwendung ungültig. Pro Konto werden höchstens drei Anfragen pro Stunde akzeptiert.

Der Versand verwendet die PHP-Funktion `mail()`. Der Webserver benötigt deshalb einen konfigurierten Mail Transfer Agent oder eine passende PHP-Mailkonfiguration. Der Absender wird in `config.php` festgelegt:

```php
'mail' => [
    'from' => 'noreply@example.org',
    'name' => 'openSlalom',
],
```

In produktiven Installationen sollte eine gültige Absenderdomain mit SPF, DKIM und DMARC verwendet werden.

## Routing

- `/` zeigt die Informationsseite.
- `/login` stellt die Anmeldung bereit.
- `/registrieren` ermöglicht eine öffentliche Kontoanlage mit der Rolle `Registriert`.
- `/passwort-vergessen` fordert einen Passwort-Reset an.
- `/passwort-zuruecksetzen?token=...` setzt ein Passwort über einen Einmal-Token neu.
- `/trainings` zeigt angemeldeten Benutzern nur die jeweils zugänglichen Trainings.
- `/trainings/neu` erlaubt Administratoren und Trainingsleitern das Anlegen eines Trainings.
- `/training/{UUID}/bearbeiten` erlaubt Administratoren und Trainingsleitern das Bearbeiten eines Trainings.
- `/verwaltung/vereine`, `/verwaltung/fahrer`, `/verwaltung/disziplinen`, `/verwaltung/karts` und `/verwaltung/wetter` bieten Administratoren und Trainingsleitern die Stammdaten-CRUD-Verwaltung.
- `/training/550e8400-e29b-41d4-a716-446655440000` zeigt das zugehörige Training, sofern es veröffentlicht ist oder eine Berechtigung besteht.
- `/admin/benutzer` und die Benutzer-Bearbeitung sind ausschließlich für Administratoren verfügbar.
- `/konto` erlaubt angemeldeten Benutzern Passwortänderung und Selbstlöschung des WebUI-Kontos.
- `/impressum` und `/datenschutz` sind öffentlich erreichbar und im Footer verlinkt.
- Ungültige und unbekannte UUIDs liefern HTTP `404`.
- Datenbank- oder Konfigurationsfehler liefern HTTP `503`, ohne Zugangsdaten oder interne Fehlermeldungen offenzulegen.

Die UUID ist keine Benutzeranmeldung. Bei veröffentlichten Trainings kann jeder mit dem vollständigen Link die angezeigten Ergebnisse und vollständigen Fahrernamen lesen.

## Selbstverwaltung von Konten

Angemeldete Benutzer können unter `/konto` das eigene Passwort ändern. Nach einer Änderung werden alle Sitzungen über die Sitzungsrevision ungültig.

Die eigene Kontolöschung verlangt das aktuelle Passwort sowie die explizite Eingabe von `LÖSCHEN`. Sie entfernt ausschließlich den WebUI-Benutzer samt Rollen und Passwort-Reset-Tokens. Das zugeordnete Fahrerprofil sowie Trainings- und Ergebnisdaten bleiben erhalten. Das letzte aktive Administratorkonto kann nicht gelöscht werden.

## Angezeigte Trainingsdaten

- Trainingsstatus, Datum, Verein, Disziplin und Wetter
- Starterliste mit Verein und Altersklasse
- Bestzeiten, Abstände, Durchschnittszeiten und gültige Rundenzahl
- Gespeicherte Stints mit Kart und Altersklassen-Snapshot
- Einzelrunden, Strafen, PF, TF und Ungültig-Markierung
- Automatische Aktualisierung während eines laufenden Trainings

Soft-gelöschte Datensätze werden in allen Abfragen ausgeschlossen. Die Zeit- und Strafberechnungen entsprechen der Desktop-App.

## Rechtliche Angaben konfigurieren

`config.example.php` enthält unter `legal` alle Platzhalter für Impressum und Datenschutzerklärung. Vor einer öffentlichen Bereitstellung müssen mindestens Betreiber, ladungsfähige Anschrift, Kontakt, inhaltlich Verantwortlicher, Hosting-Anbieter und zuständige Datenschutzaufsichtsbehörde in der lokalen `config.php` eingetragen werden.

Die enthaltenen Texte bilden die aktuellen Funktionen der WebUI ab, ersetzen aber keine individuelle Rechtsberatung. Insbesondere die Rechtsgrundlage für die Veröffentlichung vollständiger Fahrernamen und Ergebnisse, Anforderungen bei Minderjährigen, Aufbewahrungsfristen, eingesetzte Hosting-/Mail-Dienstleister und die Pflicht zur Benennung eines Datenschutzbeauftragten müssen vom tatsächlichen Betreiber geprüft werden.

Da die WebUI derzeit nur technisch notwendige Session- und Darstellungsdaten verwendet und keine Analyse- oder Marketingdienste einsetzt, ist kein Cookie-Einwilligungsbanner vorgesehen. Sobald externe Schriftarten, Karten, Videos, Analyse-, Marketing- oder andere Drittanbieterdienste ergänzt werden, muss diese Bewertung aktualisiert werden.

## Prüfung

Sind PHP und die Erweiterungen installiert, können alle Dateien geprüft werden:

```powershell
Get-ChildItem -Recurse -Filter *.php | ForEach-Object { php -l $_.FullName }
```

Für einen schnellen Test ohne Apache kann die Startseite über den eingebauten PHP-Server geladen werden. Pfadrouting unter `/training/{UUID}` sollte abschließend dennoch mit Apache und `.htaccess` geprüft werden.
