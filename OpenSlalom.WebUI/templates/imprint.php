<?php
declare(strict_types=1);

$operator = trim((string) ($legal['operator_name'] ?? ''));
$isConfigured = $operator !== '' && !str_starts_with($operator, '[');
$email = (string) ($legal['email'] ?? '');
?>
<section class="legal-hero">
    <div class="shell">
        <p class="eyebrow"><span></span> Rechtliche Informationen</p>
        <h1>Impressum</h1>
        <p>Angaben gemäß § 5 Digitale-Dienste-Gesetz (DDG).</p>
    </div>
</section>

<article class="shell legal-content">
    <?php if (!$isConfigured): ?>
        <div class="legal-configuration-warning" role="alert">
            <strong>Betreiberangaben noch nicht konfiguriert</strong>
            <span>Vor einer öffentlichen Bereitstellung müssen die Werte unter <code>legal</code> in <code>config.php</code> vollständig ersetzt und rechtlich geprüft werden.</span>
        </div>
    <?php endif; ?>

    <section>
        <h2>Diensteanbieter</h2>
        <address>
            <strong><?= escape($operator !== '' ? $operator : '[Vollständiger Name oder Firmenname]') ?><?= !empty($legal['legal_form']) ? ' ' . escape($legal['legal_form']) : '' ?></strong><br>
            <?php if (!empty($legal['represented_by'])): ?>Vertreten durch: <?= escape($legal['represented_by']) ?><br><?php endif; ?>
            <?= escape($legal['street'] ?? '[Straße und Hausnummer]') ?><br>
            <?= escape($legal['postal_code'] ?? '[PLZ]') ?> <?= escape($legal['city'] ?? '[Ort]') ?><br>
            <?= escape($legal['country'] ?? 'Deutschland') ?>
        </address>
    </section>

    <section>
        <h2>Kontakt</h2>
        <p>
            Telefon: <?= escape($legal['phone'] ?? '[Telefonnummer]') ?><br>
            E-Mail:
            <?php if (filter_var($email, FILTER_VALIDATE_EMAIL)): ?>
                <a href="mailto:<?= escape($email) ?>"><?= escape($email) ?></a>
            <?php else: ?>
                <?= escape($email !== '' ? $email : '[E-Mail-Adresse]') ?>
            <?php endif; ?>
        </p>
    </section>

    <?php if (!empty($legal['register_name']) || !empty($legal['register_number'])): ?>
        <section>
            <h2>Registereintrag</h2>
            <p>Register: <?= escape($legal['register_name'] ?? '-') ?><br>Registernummer: <?= escape($legal['register_number'] ?? '-') ?></p>
        </section>
    <?php endif; ?>

    <?php if (!empty($legal['vat_id'])): ?>
        <section>
            <h2>Umsatzsteuer-ID</h2>
            <p>Umsatzsteuer-Identifikationsnummer gemäß § 27a Umsatzsteuergesetz: <?= escape($legal['vat_id']) ?></p>
        </section>
    <?php endif; ?>

    <section>
        <h2>Verantwortlich für journalistisch-redaktionelle Inhalte</h2>
        <p>Verantwortlich gemäß § 18 Abs. 2 Medienstaatsvertrag (MStV):<br><?= nl2br(escape($legal['content_responsible'] ?? '[Name und vollständige Anschrift]')) ?></p>
    </section>

    <section>
        <h2>Verbraucherstreitbeilegung</h2>
        <p>Wir sind nicht bereit und nicht verpflichtet, an Streitbeilegungsverfahren vor einer Verbraucherschlichtungsstelle teilzunehmen, sofern keine gesetzliche Verpflichtung zur Teilnahme besteht.</p>
    </section>

    <section>
        <h2>Haftung für Inhalte</h2>
        <p>Als Diensteanbieter sind wir für eigene Inhalte auf diesen Seiten nach den allgemeinen Gesetzen verantwortlich. Eine Verpflichtung zur Überwachung übermittelter oder gespeicherter fremder Informationen besteht nur im Rahmen der gesetzlichen Vorgaben. Verpflichtungen zur Entfernung oder Sperrung der Nutzung von Informationen nach den allgemeinen Gesetzen bleiben unberührt. Eine Haftung ist erst ab Kenntnis einer konkreten Rechtsverletzung möglich. Bei Bekanntwerden entsprechender Rechtsverletzungen werden wir die betroffenen Inhalte unverzüglich entfernen.</p>
        <p>Trainingsergebnisse und Statistiken werden aus den durch die Veranstalter oder Zeitnehmer erfassten Daten berechnet. Trotz sorgfältiger Verarbeitung kann keine Gewähr für Vollständigkeit, Aktualität oder Fehlerfreiheit übernommen werden. Verbindliche sportliche Wertungen und Entscheidungen obliegen ausschließlich dem jeweiligen Veranstalter. Gesetzliche Ansprüche, insbesondere wegen Vorsatz, grober Fahrlässigkeit sowie Verletzung von Leben, Körper oder Gesundheit, bleiben unberührt.</p>
    </section>

    <section>
        <h2>Haftung für externe Links</h2>
        <p>Unser Angebot kann Links zu externen Websites Dritter enthalten, auf deren Inhalte wir keinen Einfluss haben. Für diese fremden Inhalte ist stets der jeweilige Anbieter oder Betreiber verantwortlich. Zum Zeitpunkt der Verlinkung waren keine konkreten Rechtsverletzungen erkennbar. Eine dauerhafte inhaltliche Kontrolle verlinkter Seiten ist ohne konkrete Anhaltspunkte nicht zumutbar. Bei Bekanntwerden von Rechtsverletzungen werden entsprechende Links unverzüglich entfernt.</p>
    </section>

    <section>
        <h2>Urheberrecht</h2>
        <p>Die durch den Seitenbetreiber erstellten Inhalte, Gestaltungen, Grafiken und Softwarebestandteile unterliegen dem deutschen Urheberrecht. Vervielfältigung, Bearbeitung, Verbreitung und jede Art der Verwertung außerhalb der gesetzlichen Schranken bedürfen der vorherigen Zustimmung des jeweiligen Rechteinhabers. Inhalte Dritter werden als solche gekennzeichnet. Gesetzlich zulässige Nutzungen bleiben unberührt.</p>
    </section>

    <p class="legal-updated">Stand: 3. August 2026</p>
</article>
