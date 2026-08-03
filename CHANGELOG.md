# Changelog

Alle relevanten Änderungen an OpenSlalom werden in dieser Datei dokumentiert.
Das Format orientiert sich an [Keep a Changelog](https://keepachangelog.com/de/1.1.0/),
die Versionsnummern folgen der [semantischen Versionierung](https://semver.org/lang/de/).

## [Unreleased]

### Hinzugefügt

### Geändert

### Behoben

- Die UUID-Nachrüstung für Trainings und Meisterschaften markiert bestehende Datensätze nicht mehr fälschlich als ausstehende Synchronisierungsänderungen.

## [0.8.8-Beta] - 2026-08-03

### Hinzugefügt

- Die Remote-Datenbankverbindung kann in den allgemeinen Einstellungen konfiguriert und gespeichert werden.
- Für Trainings können globale Defaultwerte für Rundenanzahl, das Überschreiten der Sollrundenzahl und die zweite Zeitnahme festgelegt werden.
- Eine automatische Synchronisierung gespeicherter Stints und nachträglich geänderter Runden zur Remote-Datenbank kann global oder training-spezifisch aktiviert werden.
- Jedes Training erhält eine dauerhaft eindeutige UUID, die in der lokalen und der Remote-Datenbank gespeichert wird.
- Jede Meisterschaft erhält eine dauerhaft eindeutige UUID, die in der lokalen und der Remote-Datenbank gespeichert wird.
- Die neue PHP-WebUI beschreibt openSlalom und stellt synchronisierte Trainingsergebnisse über eine schreibgeschützte UUID-Route bereit.
- Die öffentliche Trainingsansicht der WebUI enthält einen Statistik-Tab mit trainingsbezogenen Kennzahlen und Fahrerstatistiken.
- Trainings können in der Desktop-App für die öffentliche WebUI freigegeben oder privat gehalten werden.
- Die WebUI besitzt eine Anmeldung mit den Rollen Administrator, Trainingsleiter und Fahrer sowie eine Administrator-Benutzerverwaltung.

### Geändert

- Die Buttons "Start", "Runde" und "Stint löschen" sind linksbündig angeordnet, während "Stint speichern" rechtsbündig angezeigt wird.
- Änderungen an der Remote-Datenbankverbindung werden nach dem Neuaufbau der Verbindung ohne Neustart wirksam.
- Automatische und manuelle Synchronisierungen werden geschützt nacheinander ausgeführt.
- Die WebUI-Landingpage bietet einen hervorgehobenen Zugang zur Anmeldung bzw. zum internen Bereich; das Anmeldeformular wurde neu gestaltet.
- Der interne WebUI-Bereich besitzt eine horizontale Navigation für Trainings und Benutzerverwaltung.
- WebUI-Benutzer können mit E-Mail-Adresse, Rolle, Fahrerzuordnung, Aktivstatus und optional neuem Passwort bearbeitet werden.
- Fahrerprofile können in der WebUI jeder Benutzerrolle zugeordnet werden; für die Rolle Fahrer bleibt die Zuordnung verpflichtend.
- Die WebUI unterstützt zeitlich begrenzte Passwort-Reset-Links per E-Mail.
- Die WebUI-Anmeldung akzeptiert Benutzername oder E-Mail-Adresse als Anmeldebezeichner.
- Die WebUI enthält öffentlich verlinkte Seiten für Impressum und Datenschutz mit konfigurierbaren Betreiber-, Hosting- und Aufsichtsangaben.
- Neue WebUI-Benutzer können sich selbst mit der Rolle Registriert anmelden; Administratoren werden per E-Mail informiert und weisen später Rolle sowie Fahrerprofil zu.
- Administratoren und Trainingsleiter können Trainings im internen WebUI-Bereich anlegen und vorhandene Trainings bearbeiten.
- Angemeldete WebUI-Benutzer können ihr Passwort ändern oder nach Sicherheitsbestätigung das eigene WebUI-Konto löschen.
- Die WebUI enthält CRUD-Ansichten für Vereine, Fahrer, Disziplinen mit Altersklassen, Karts und Wetter.
- Trainings-, Benutzer- und Stammdatenlisten der WebUI bieten Suche und serverseitige Pagination.

## [0.8.7-Beta] - 2026-08-01

### Hinzugefügt

- Die Fahrer in der Starterliste eines Trainings können per Drag-and-drop neu angeordnet werden.
- Die gezogene Fahrerzeile wird farblich hervorgehoben und während des Ziehens halbtransparent dargestellt.
- Beim Überfahren einer anderen Fahrerzeile wird die gezogene Zeile bereits als temporäre Vorschau an die neue Position verschoben.
- Eine geänderte Fahrerreihenfolge wird direkt in den Trainingszuordnungen gespeichert und für folgende Fahrerwechsel verwendet.
- In den trainingsbezogenen Einstellungen kann erlaubt werden, die vorgegebene Sollrundenzahl zu überschreiten.
- Beim Erreichen der Sollrundenzahl pulsiert der Hintergrund der Zeitmessung grün.
- Der neue Trainingstab "Gefahrene Stints" stellt die gespeicherten Stints eines auswählbaren Fahrers mit Zeitpunkt, Kart, Altersklasse, Gesamtzeit, Durchschnittszeit und allen Runden dar.
- Pylonenfehler (`PF`) und Torfehler (`TF`) gespeicherter Runden können im Stint-Tab nachträglich geändert werden.
- Gespeicherte Runden können im Stint-Tab nachträglich als ungültig markiert oder wieder aktiviert werden.
- Eine zweite parallele Zeitnahme kann trainingsbezogen in den Einstellungen aktiviert werden.
- Die zweite Zeitnahme besitzt eine eigene Stoppuhr, Rundentabelle, Strafenerfassung und Stint-Steuerung mit den Shortcuts `A`, `S` und `D`.
- Fahrer der ersten Zeitnahme werden in der Starterliste grün, Fahrer der zweiten Zeitnahme orange markiert.

### Geändert

- Nach der Kartauswahl wird der Tastaturfokus auf die Starterliste zurückgesetzt.
- Der Start-Button mit Shortcut `Q` wird während einer laufenden Zeitnahme zu einem Stop-Button und kann den Stint vor Erreichen der Sollrundenzahl beenden.
- Vorzeitig beendete Stints können beim Fahrerwechsel mit den bis dahin abgeschlossenen Runden gespeichert werden.
- Die trainingsbezogene Rundenanzahl und die neue Option zum Überschreiten der Sollrunden werden automatisch gespeichert.
- Bei erlaubtem Überschreiten beendet das Erreichen der Sollrundenzahl den Stint nicht automatisch; er wird ausdrücklich mit `Q` beendet.
- Änderungen an gespeicherten Runden werden unmittelbar lokal gespeichert und aktualisieren Strafzeiten, Stint-Summen sowie die Schnellste-Runde-Auswertung.
- Ein Doppelklick auf eine Fahrerzeile weist den Fahrer automatisch der ersten verfügbaren Zeitnahme zu; Zeitnahme 1 hat dabei Vorrang vor Zeitnahme 2.
- Der Button "Fahrer überspringen" verwendet dieselbe Stationspriorität und kann bei laufender Zeitnahme 1 den nächsten verfügbaren Fahrer für Zeitnahme 2 aktivieren.
- Der Button "Nächster Fahrer" berücksichtigt beendete Stints beider Zeitnahmen, speichert die priorisierte Station und weist ihr den nächsten noch freien Fahrer zu.
- Läuft Zeitnahme 1, wird "Nächster Fahrer" auch bei einer freien Zeitnahme 2 verfügbar und kann diese mit dem nächsten Fahrer belegen.
- Läuft Zeitnahme 1, wird ein verfügbarer Fahrer der zweiten Zeitnahme zugewiesen. Laufen beide Stints, bleibt der Doppelklick ohne Wirkung.
- Beendete Stints beider Zeitnahmen werden vor einer Neubelegung gespeichert; ein Fahrer kann nicht gleichzeitig beide Stationen belegen.
- Beide Stoppuhren laufen unabhängig voneinander weiter, wenn die jeweils andere Zeitnahme gestoppt oder abgeschlossen wird.
- Bei aktivierter zweiter Zeitnahme wird der verfügbare Zeitnahmebereich gleichmäßig im Verhältnis 50:50 auf beide Stationen verteilt.
- Beide Zeitnahmen besitzen einen eigenen Button "Stint speichern", der einen beendeten Stint speichert und die jeweilige Zeitnahmeeinheit anschließend ohne automatische Neuzuweisung freigibt.
- Nach dem expliziten Speichern und Freigeben einer Station wird "Nächster Fahrer" wieder aktiviert und setzt die Fahrerreihenfolge hinter dem zuletzt gespeicherten Fahrer fort.

### Behoben

- Nach Auswahl eines Karts wurden die Shortcuts `Q`, `W` und `E` als Text in die editierbare Kombinationsbox geschrieben, statt die Zeitnahme zu steuern.
- Die Auswahl eines Karts löst keine Exception mehr aus, wenn das angeklickte ComboBox-Element nicht Teil des visuellen WPF-Baums ist.
- Kann eine per Drag-and-drop geänderte Fahrerreihenfolge nicht gespeichert werden, wird wieder die zuvor gespeicherte Reihenfolge geladen.
- Wird Drag-and-drop abgebrochen oder außerhalb einer Fahrerzeile beendet, kehrt die gezogene Zeile an ihre Ausgangsposition zurück.
- Der bisherige Übernehmen-Button für die trainingsbezogene Rundenanzahl wurde entfernt.

## [0.8.6-Beta] - 2026-08-01

### Hinzugefügt

- In der Starterliste der Trainingsansicht kann per Doppelklick direkt zu einem bestimmten Fahrer gewechselt werden.

### Geändert

- Fahrerwechsel sind nur noch möglich, wenn der aktuelle Stint noch nicht begonnen wurde oder vollständig abgeschlossen ist.
- Die Aktionen "Nächster Fahrer", "Fahrer überspringen" und der neue Doppelklick verwenden denselben abgesicherten Wechselvorgang.
- Ein vollständig abgeschlossener Stint wird vor jedem Fahrerwechsel inklusive Runden, Fehlern, Ungültig-Markierungen, Kart und Altersklasse gespeichert.
- Checkboxen und die Kart-Auswahl innerhalb der Starterliste lösen keinen Fahrerwechsel per Doppelklick aus.

### Behoben

- "Fahrer überspringen" konnte zuvor einen abgeschlossenen Stint wechseln, ohne ihn zu speichern.
- Ein angehaltener, aber unvollständiger Stint kann nicht mehr über "Fahrer überspringen" verlassen werden.
- Bei einem Speicherfehler wird der Fahrer nicht gewechselt und der aktuelle Stint bleibt erhalten.

## [0.8.5-Beta] und älter

- Für diese Versionen liegt kein gepflegtes Änderungsprotokoll vor.
