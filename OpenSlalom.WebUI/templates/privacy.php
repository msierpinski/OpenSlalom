<?php
declare(strict_types=1);

$operator = trim((string) ($legal['operator_name'] ?? ''));
$isConfigured = $operator !== '' && !str_starts_with($operator, '[');
$privacyEmail = (string) ($legal['privacy_contact_email'] ?? $legal['email'] ?? '');
$retentionDays = max(1, (int) ($legal['log_retention_days'] ?? 7));
$authorityUrl = (string) ($legal['supervisory_authority_url'] ?? '');
?>
<section class="legal-hero">
    <div class="shell">
        <p class="eyebrow"><span></span> Schutz personenbezogener Daten</p>
        <h1>Datenschutzerklärung</h1>
        <p>Informationen gemäß Art. 13 und 14 Datenschutz-Grundverordnung (DSGVO).</p>
    </div>
</section>

<article class="shell legal-content">
    <?php if (!$isConfigured): ?>
        <div class="legal-configuration-warning" role="alert">
            <strong>Verantwortliche Stelle noch nicht konfiguriert</strong>
            <span>Vor einer öffentlichen Bereitstellung müssen die Werte unter <code>legal</code> in <code>config.php</code> vollständig ersetzt und die Rechtsgrundlagen mit dem Betreiber abgestimmt werden.</span>
        </div>
    <?php endif; ?>

    <section>
        <h2>1. Verantwortlicher</h2>
        <address>
            <strong><?= escape($operator !== '' ? $operator : '[Vollständiger Name oder Firmenname]') ?></strong><br>
            <?= escape($legal['street'] ?? '[Straße und Hausnummer]') ?><br>
            <?= escape($legal['postal_code'] ?? '[PLZ]') ?> <?= escape($legal['city'] ?? '[Ort]') ?><br>
            <?= escape($legal['country'] ?? 'Deutschland') ?><br>
            E-Mail: <?= escape($privacyEmail !== '' ? $privacyEmail : '[Datenschutz-Kontaktadresse]') ?>
        </address>
    </section>

    <section>
        <h2>2. Hosting und Serverprotokolle</h2>
        <p>Beim Aufruf der WebUI verarbeitet der Webserver technisch erforderliche Zugriffsdaten. Dazu können IP-Adresse, Datum und Uhrzeit, aufgerufene Adresse, Referrer, Browsertyp, Betriebssystem, übertragene Datenmenge und HTTP-Status gehören. Die Verarbeitung dient der sicheren und störungsfreien Bereitstellung, der Fehleranalyse und der Abwehr von Missbrauch.</p>
        <p>Rechtsgrundlage ist Art. 6 Abs. 1 lit. f DSGVO. Das berechtigte Interesse liegt in Betrieb, Stabilität und IT-Sicherheit des Angebots. Serverprotokolle werden grundsätzlich nach <?= $retentionDays ?> Tagen gelöscht, sofern kein sicherheitsrelevantes Ereignis eine längere Aufbewahrung zur Beweissicherung erforderlich macht.</p>
        <p>Hosting-Anbieter: <?= escape($legal['hosting_provider'] ?? '[Hosting-Anbieter und Anschrift]') ?><br>Verarbeitungsort: <?= escape($legal['hosting_country'] ?? 'Deutschland') ?></p>
    </section>

    <section>
        <h2>3. Öffentliche Trainingsergebnisse</h2>
        <p>Bei ausdrücklich veröffentlichten Trainings können Namen von Fahrerinnen und Fahrern, Vereinszugehörigkeit, Altersklasse, Kart, Stints, Rundenzeiten, Fehler, Strafzeiten, Ranglisten und Statistiken öffentlich angezeigt werden. Die Veröffentlichung erfolgt durch den Trainingsverantwortlichen in der Desktop-App.</p>
        <p>Der jeweilige Betreiber muss vor der Veröffentlichung eine tragfähige Rechtsgrundlage sicherstellen. Je nach Organisation und Teilnehmerkreis kommt insbesondere eine Einwilligung nach Art. 6 Abs. 1 lit. a DSGVO oder ein berechtigtes Interesse nach Art. 6 Abs. 1 lit. f DSGVO nach dokumentierter Interessenabwägung in Betracht. Bei minderjährigen Teilnehmenden sind die besonderen Anforderungen an Einwilligung und Information der Sorgeberechtigten zu beachten.</p>
        <p>Eine Veröffentlichung kann durch Deaktivieren der Trainingsfreigabe beendet werden. Bereits erfolgte Abrufe, Screenshots oder zulässige Weiterverarbeitungen durch Dritte können technisch nicht vollständig zurückgerufen werden.</p>
    </section>

    <section>
        <h2>4. Benutzerkonten und interner Bereich</h2>
        <p>Für den internen Bereich und bei einer Selbstregistrierung verarbeiten wir Benutzername, E-Mail-Adresse, Passwort-Hash, Rollen, optionale Fahrerzuordnung, Aktivstatus, Zeitpunkt der Kontoerstellung und des letzten Logins sowie eine Sitzungsrevision. Passwörter werden ausschließlich als nicht rückrechenbarer Hash gespeichert. Neue Selbstregistrierungen erhalten zunächst die Rolle „Registriert“; Administratoren werden per E-Mail über die Kontoanlage informiert und können später Rolle und Fahrerzuordnung ändern.</p>
        <p>Die Daten werden zur Bereitstellung und Absicherung des Benutzerkontos, zur rollenbasierten Zugriffskontrolle und zur Anzeige zugeordneter Trainings verarbeitet. Rechtsgrundlage ist Art. 6 Abs. 1 lit. b DSGVO, soweit die Verarbeitung zur Erfüllung einer Nutzungsvereinbarung erforderlich ist, sowie Art. 6 Abs. 1 lit. f DSGVO für Zugriffsschutz und Missbrauchsprävention.</p>
        <p>Kontodaten werden für die Dauer des aktiven Benutzerkontos gespeichert. Benutzer können ihr WebUI-Konto nach erneuter Passwortbestätigung selbst löschen. Dabei werden das Konto, Rollen und Passwort-Reset-Tokens entfernt; verknüpfte Fahrerprofile, Trainings und Ergebnisse bleiben erhalten. Technische Sicherheitsprotokolle werden entsprechend den genannten Fristen weitergeführt und anschließend gelöscht. Abweichende gesetzliche Aufbewahrungspflichten, Sicherheitsinteressen oder offene Ansprüche bleiben unberührt.</p>
    </section>

    <section>
        <h2>5. Anmeldung und Schutz vor Missbrauch</h2>
        <p>Fehlgeschlagene Anmeldeversuche werden mit eingegebenem Anmeldebezeichner, IP-Adresse und Zeitpunkt protokolliert. Registrierungsversuche werden mit IP-Adresse und Zeitpunkt erfasst. Diese Daten dienen der Begrenzung automatisierter Angriffe, unbefugter Zugriffsversuche und massenhafter Kontoanlagen. Sie werden nur für Sicherheitszwecke verwendet und nach spätestens 30 Tagen gelöscht. Rechtsgrundlage ist Art. 6 Abs. 1 lit. f DSGVO.</p>
    </section>

    <section>
        <h2>6. Passwort zurücksetzen und E-Mailversand</h2>
        <p>Bei Anforderung eines Passwort-Resets verarbeiten wir die eingegebene E-Mail-Adresse. Für bestehende aktive Konten wird ein zufälliger Einmal-Token erzeugt; in der Datenbank wird nur dessen Hash gespeichert. Der Link ist 60 Minuten gültig und nach Verwendung nicht erneut nutzbar. Verbrauchte und abgelaufene Token-Datensätze werden spätestens nach weiteren sieben Tagen gelöscht. Zur Vermeidung von Kontoermittlung erhält jeder Antrag dieselbe Bestätigung.</p>
        <p>Für den Versand kann ein E-Mail-Dienst oder Mailserver eingesetzt werden, der Empfängeradresse, Absender, Zeitstempel und technische Versanddaten verarbeitet. Rechtsgrundlage ist Art. 6 Abs. 1 lit. b DSGVO sowie Art. 6 Abs. 1 lit. f DSGVO zur sicheren Wiederherstellung des Kontozugangs.</p>
    </section>

    <section>
        <h2>7. Cookies und lokale Browserspeicherung</h2>
        <p>Die WebUI setzt ausschließlich ein technisch notwendiges Session-Cookie namens <code>openslalom_web</code>. Es enthält eine zufällige Sitzungskennung, ist für JavaScript nicht lesbar (<code>HttpOnly</code>), verwendet <code>SameSite=Lax</code> und wird beim Schließen des Browsers beziehungsweise bei der Abmeldung ungültig. Unter HTTPS wird es nur verschlüsselt übertragen (<code>Secure</code>).</p>
        <p>Zusätzlich speichert die Ergebnisansicht lokale Darstellungspräferenzen wie aktiven Tab, Live-Aktualisierung und geöffnete Detailbereiche in <code>sessionStorage</code> oder <code>localStorage</code>. Diese Informationen verbleiben im Browser und dienen ausschließlich der gewünschten Funktion. Rechtsgrundlage für den Zugriff auf diese technisch erforderlichen Informationen ist § 25 Abs. 2 Nr. 2 TDDDG; die anschließende Verarbeitung erfolgt gemäß Art. 6 Abs. 1 lit. f DSGVO.</p>
        <p>Es werden keine Analyse-, Marketing- oder Tracking-Cookies eingesetzt. Daher ist für den derzeitigen Funktionsumfang kein Einwilligungsbanner erforderlich.</p>
    </section>

    <section>
        <h2>8. Empfänger und Auftragsverarbeitung</h2>
        <p>Personenbezogene Daten erhalten nur Personen und Dienstleister, die sie für Betrieb, Hosting, Administration oder E-Mailversand benötigen. Soweit Dienstleister Daten in unserem Auftrag verarbeiten, werden erforderliche Verträge nach Art. 28 DSGVO geschlossen. Eine Übermittlung in Staaten außerhalb der EU oder des EWR findet nur statt, wenn hierfür die Voraussetzungen der Art. 44 ff. DSGVO erfüllt sind.</p>
    </section>

    <section>
        <h2>9. Datensicherheit</h2>
        <p>Wir treffen angemessene technische und organisatorische Maßnahmen nach Art. 32 DSGVO. Dazu gehören rollenbasierte Zugriffe, gehashte Passwörter, zeitlich begrenzte Reset-Tokens, Schutz gegen Session-Fixierung und Anmeldebegrenzung. Bei öffentlichem Betrieb ist die Website ausschließlich über HTTPS bereitzustellen. Ein absoluter Schutz bei Datenübertragungen im Internet kann dennoch nicht garantiert werden.</p>
    </section>

    <section>
        <h2>10. Rechte betroffener Personen</h2>
        <p>Betroffene Personen haben im Rahmen der gesetzlichen Voraussetzungen das Recht auf Auskunft (Art. 15 DSGVO), Berichtigung (Art. 16), Löschung (Art. 17), Einschränkung der Verarbeitung (Art. 18), Datenübertragbarkeit (Art. 20) und Widerspruch gegen Verarbeitungen auf Grundlage von Art. 6 Abs. 1 lit. e oder f DSGVO (Art. 21). Eine erteilte Einwilligung kann jederzeit mit Wirkung für die Zukunft widerrufen werden.</p>
        <p>Anfragen können an <?= escape($privacyEmail !== '' ? $privacyEmail : '[Datenschutz-Kontaktadresse]') ?> gerichtet werden. Zur Vermeidung unbefugter Auskünfte kann ein Identitätsnachweis erforderlich sein.</p>
    </section>

    <section>
        <h2>11. Beschwerderecht</h2>
        <p>Nach Art. 77 DSGVO besteht das Recht, sich bei einer Datenschutzaufsichtsbehörde zu beschweren. Zuständig ist insbesondere die Behörde am gewöhnlichen Aufenthaltsort, Arbeitsplatz oder Ort des mutmaßlichen Verstoßes.</p>
        <p>
            <?= escape($legal['supervisory_authority_name'] ?? '[Zuständige Datenschutzaufsichtsbehörde]') ?>
            <?php if (filter_var($authorityUrl, FILTER_VALIDATE_URL)): ?><br><a href="<?= escape($authorityUrl) ?>" rel="noopener noreferrer">Kontaktdaten der Aufsichtsbehörde</a><?php endif; ?>
        </p>
    </section>

    <section>
        <h2>12. Automatisierte Entscheidungen</h2>
        <p>Eine ausschließlich automatisierte Entscheidungsfindung mit rechtlicher oder ähnlich erheblicher Wirkung einschließlich Profiling im Sinne von Art. 22 DSGVO findet nicht statt. Sportliche Ranglisten werden rechnerisch aus erfassten Zeiten und Fehlern gebildet; verbindliche Entscheidungen trifft der jeweilige Veranstalter.</p>
    </section>

    <section>
        <h2>13. Änderungen dieser Erklärung</h2>
        <p>Diese Datenschutzerklärung wird angepasst, wenn sich Funktionen, Rechtsgrundlagen, eingesetzte Dienstleister oder gesetzliche Anforderungen ändern. Es gilt die auf dieser Seite veröffentlichte Fassung.</p>
    </section>

    <p class="legal-updated">Stand: 3. August 2026</p>
</article>
