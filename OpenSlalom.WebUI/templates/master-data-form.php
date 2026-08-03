<?php declare(strict_types=1); ?>
<?php $action = $editMode ? 'verwaltung/' . $masterType . '/' . $itemId : 'verwaltung/' . $masterType; ?>
<section class="shell listing-page master-data-page">
    <div class="page-heading-row"><div><p class="eyebrow"><span></span> Stammdatenverwaltung</p><h1><?= escape($masterTitle) ?> <?= $editMode ? 'bearbeiten' : 'anlegen' ?></h1></div></div>
    <div class="result-section master-data-form-card">
        <?php if ($formError !== null): ?><div class="form-message error" role="alert"><?= escape($formError) ?></div><?php endif; ?>
        <form class="app-form master-data-form" action="<?= escape(base_url($action)) ?>" method="post" enctype="multipart/form-data">
            <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">
            <?php if ($masterType === 'vereine'): ?>
                <label class="full-width"><span>Vereinsname</span><input name="vereinsname" value="<?= escape($formValues['vereinsname'] ?? '') ?>" required maxlength="100"></label>
                <label><span>Mitgliedsnummer</span><input name="mitglieds_nummer" value="<?= escape($formValues['mitglieds_nummer'] ?? '') ?>" maxlength="50"></label>
                <label><span>Postleitzahl</span><input name="postleitzahl" value="<?= escape($formValues['postleitzahl'] ?? '') ?>" maxlength="20"></label>
                <label><span>Ort</span><input name="ort" value="<?= escape($formValues['ort'] ?? '') ?>" maxlength="100"></label>
                <label class="full-width"><span>Adresse</span><textarea name="adresse" rows="3" maxlength="250"><?= escape($formValues['adresse'] ?? '') ?></textarea></label>
                <label class="full-width"><span>Vereinslogo (PNG, JPG oder BMP, max. 2 MB)</span><input type="file" name="logo" accept="image/png,image/jpeg,image/bmp"></label>
                <?php if ($editMode && !empty($formValues['logo'])): ?><label class="checkbox-label"><input type="checkbox" name="logo_loeschen" value="1"><span>Bestehendes Logo löschen</span></label><?php endif; ?>
            <?php endif; ?>
            <?php if ($masterType === 'fahrer'): ?>
                <label><span>Vorname</span><input name="vorname" value="<?= escape($formValues['vorname'] ?? '') ?>" required maxlength="100"></label>
                <label><span>Nachname</span><input name="nachname" value="<?= escape($formValues['nachname'] ?? '') ?>" maxlength="100"></label>
                <label><span>Verein</span><select name="verein_id" required><option value="">Bitte auswählen</option><?php foreach ($lookups['clubs'] as $item): ?><option value="<?= (int) $item['id'] ?>" <?= (int) ($formValues['verein_id'] ?? 0) === (int) $item['id'] ? 'selected' : '' ?>><?= escape($item['name']) ?></option><?php endforeach; ?></select></label>
                <label><span>Mitgliedsnummer</span><input name="mitglieds_nummer" value="<?= escape($formValues['mitglieds_nummer'] ?? '') ?>" maxlength="50"></label>
                <label><span>Geburtsdatum</span><input type="date" name="geburtsdatum" value="<?= escape($formValues['geburtsdatum'] ?? '') ?>"></label>
                <label><span>Geschlecht</span><select name="geschlecht"><option value="">Keine Angabe</option><option value="m" <?= ($formValues['geschlecht'] ?? '') === 'm' ? 'selected' : '' ?>>Männlich</option><option value="w" <?= ($formValues['geschlecht'] ?? '') === 'w' ? 'selected' : '' ?>>Weiblich</option><option value="d" <?= ($formValues['geschlecht'] ?? '') === 'd' ? 'selected' : '' ?>>Divers</option></select></label>
            <?php endif; ?>
            <?php if ($masterType === 'disziplinen'): ?>
                <label class="full-width"><span>Disziplinname</span><input name="name" value="<?= escape($formValues['name'] ?? '') ?>" required maxlength="50"></label>
                <label><span>Torfehler-Strafe in Sekunden</span><input name="tf" value="<?= escape((string) ($formValues['tf'] ?? '0')) ?>" required inputmode="decimal"></label>
                <label><span>Pylonenfehler-Strafe in Sekunden</span><input name="pf" value="<?= escape((string) ($formValues['pf'] ?? '0')) ?>" required inputmode="decimal"></label>
                <fieldset class="full-width age-classes"><legend>Altersklassen</legend><div id="age-class-list"><?php foreach (($formValues['altersklassen'] ?? []) as $class): ?><div class="age-class-row"><input name="age_label[]" value="<?= escape($class['label'] ?? '') ?>" placeholder="Bezeichnung"><input name="age_from[]" value="<?= escape((string) ($class['age_from'] ?? '')) ?>" type="number" min="0" placeholder="Von"><input name="age_to[]" value="<?= escape((string) ($class['age_to'] ?? '')) ?>" type="number" min="0" placeholder="Bis (offen)"><button type="button" class="remove-age-class">×</button></div><?php endforeach; ?></div><button type="button" class="button button-secondary" id="add-age-class">+ Klasse hinzufügen</button></fieldset>
            <?php endif; ?>
            <?php if ($masterType === 'karts'): ?>
                <label><span>Verein</span><select name="verein_id" required><option value="">Bitte auswählen</option><?php foreach ($lookups['clubs'] as $item): ?><option value="<?= (int) $item['id'] ?>" <?= (int) ($formValues['verein_id'] ?? 0) === (int) $item['id'] ? 'selected' : '' ?>><?= escape($item['name']) ?></option><?php endforeach; ?></select></label>
                <label><span>Disziplin</span><select name="disziplin_id" required><option value="">Bitte auswählen</option><?php foreach ($lookups['disciplines'] as $item): ?><option value="<?= (int) $item['id'] ?>" <?= (int) ($formValues['disziplin_id'] ?? 0) === (int) $item['id'] ? 'selected' : '' ?>><?= escape($item['name']) ?></option><?php endforeach; ?></select></label>
                <label><span>Name</span><input name="name" value="<?= escape($formValues['name'] ?? '') ?>" maxlength="100"></label><label><span>Motor</span><input name="motor" value="<?= escape($formValues['motor'] ?? '') ?>" maxlength="100"></label><label class="full-width"><span>Chassis</span><input name="chassis" value="<?= escape($formValues['chassis'] ?? '') ?>" maxlength="100"></label>
            <?php endif; ?>
            <?php if ($masterType === 'wetter'): ?><label class="full-width"><span>Bezeichnung</span><input name="name" value="<?= escape($formValues['name'] ?? '') ?>" required maxlength="50"></label><?php endif; ?>
            <div class="form-actions full-width"><a class="button button-secondary rounded-action" href="<?= escape(base_url('verwaltung/' . $masterType)) ?>">Abbrechen</a><button class="button button-primary rounded-action" type="submit"><?= $editMode ? 'Speichern' : 'Anlegen' ?></button></div>
        </form>
    </div>
</section>
<?php if ($masterType === 'disziplinen'): ?><script src="<?= escape(base_url('assets/js/master-data.js')) ?>" defer></script><?php endif; ?>
