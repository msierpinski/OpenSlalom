<?php

declare(strict_types=1);

$action = $editMode ? 'verwaltung/' . $masterType . '/' . $itemId : 'verwaltung/' . $masterType;

?>

<section class="container-sm vstack gap-4">
    <header>
        <p class="text-body-secondary mb-1">Stammdatenverwaltung</p>
        <h1 class="text-body-emphasis display-5 fw-bold mb-0"><?= escape($masterTitle) ?> <?= $editMode ? 'bearbeiten' : 'anlegen' ?></h1>
    </header>

    <div class="card border-0 shadow-sm">
        <div class="card-body p-4">
            <?php if ($formError !== null): ?><div class="alert alert-danger" role="alert"><?= escape($formError) ?></div><?php endif; ?>
            <form class="vstack gap-3" action="<?= escape(base_url($action)) ?>" method="post" enctype="multipart/form-data">
                <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">
                <?php if ($masterType === 'vereine'): ?>
                    <div><label class="form-label" for="vereinsname">Vereinsname</label><input class="form-control" id="vereinsname" name="vereinsname" value="<?= escape($formValues['vereinsname'] ?? '') ?>" required maxlength="100"></div>
                    <div><label class="form-label" for="mitglieds_nummer">Mitgliedsnummer</label><input class="form-control" id="mitglieds_nummer" name="mitglieds_nummer" value="<?= escape($formValues['mitglieds_nummer'] ?? '') ?>" maxlength="50"></div>
                    <div class="row g-3">
                        <div class="col-md-4"><label class="form-label" for="postleitzahl">Postleitzahl</label><input class="form-control" id="postleitzahl" name="postleitzahl" value="<?= escape($formValues['postleitzahl'] ?? '') ?>" maxlength="20"></div>
                        <div class="col-md-8"><label class="form-label" for="ort">Ort</label><input class="form-control" id="ort" name="ort" value="<?= escape($formValues['ort'] ?? '') ?>" maxlength="100"></div>
                    </div>
                    <div><label class="form-label" for="adresse">Adresse</label><textarea class="form-control" id="adresse" name="adresse" rows="3" maxlength="250"><?= escape($formValues['adresse'] ?? '') ?></textarea></div>
                    <div><label class="form-label" for="logo">Vereinslogo (PNG, JPG oder BMP, max. 2 MB)</label><input class="form-control" id="logo" type="file" name="logo" accept="image/png,image/jpeg,image/bmp"></div>
                    <?php if ($editMode && !empty($formValues['logo'])): ?><div class="form-check"><input class="form-check-input" id="logo_loeschen" type="checkbox" name="logo_loeschen" value="1"><label class="form-check-label" for="logo_loeschen">Bestehendes Logo löschen</label></div><?php endif; ?>
                <?php endif; ?>
                <?php if ($masterType === 'fahrer'): ?>
                    <div class="row g-3"><div class="col-md-6"><label class="form-label" for="vorname">Vorname</label><input class="form-control" id="vorname" name="vorname" value="<?= escape($formValues['vorname'] ?? '') ?>" required maxlength="100"></div><div class="col-md-6"><label class="form-label" for="nachname">Nachname</label><input class="form-control" id="nachname" name="nachname" value="<?= escape($formValues['nachname'] ?? '') ?>" maxlength="100"></div></div>
                    <div><label class="form-label" for="verein_id">Verein</label><select class="form-select" id="verein_id" name="verein_id" required><option value="">Bitte auswählen</option><?php foreach ($lookups['clubs'] as $item): ?><option value="<?= (int) $item['id'] ?>" <?= (int) ($formValues['verein_id'] ?? 0) === (int) $item['id'] ? 'selected' : '' ?>><?= escape($item['name']) ?></option><?php endforeach; ?></select></div>
                    <div><label class="form-label" for="fahrer_mitglieds_nummer">Mitgliedsnummer</label><input class="form-control" id="fahrer_mitglieds_nummer" name="mitglieds_nummer" value="<?= escape($formValues['mitglieds_nummer'] ?? '') ?>" maxlength="50"></div>
                    <div class="row g-3"><div class="col-md-6"><label class="form-label" for="geburtsdatum">Geburtsdatum</label><input class="form-control" id="geburtsdatum" type="date" name="geburtsdatum" value="<?= escape($formValues['geburtsdatum'] ?? '') ?>"></div><div class="col-md-6"><label class="form-label" for="geschlecht">Geschlecht</label><select class="form-select" id="geschlecht" name="geschlecht"><option value="">Keine Angabe</option><option value="m" <?= ($formValues['geschlecht'] ?? '') === 'm' ? 'selected' : '' ?>>Männlich</option><option value="w" <?= ($formValues['geschlecht'] ?? '') === 'w' ? 'selected' : '' ?>>Weiblich</option><option value="d" <?= ($formValues['geschlecht'] ?? '') === 'd' ? 'selected' : '' ?>>Divers</option></select></div></div>
                <?php endif; ?>
                <?php if ($masterType === 'disziplinen'): ?>
                    <div><label class="form-label" for="name">Disziplinname</label><input class="form-control" id="name" name="name" value="<?= escape($formValues['name'] ?? '') ?>" required maxlength="50"></div>
                    <div class="row g-3"><div class="col-md-6"><label class="form-label" for="tf">Torfehler-Strafe in Sekunden</label><input class="form-control" id="tf" name="tf" value="<?= escape((string) ($formValues['tf'] ?? '0')) ?>" required inputmode="decimal"></div><div class="col-md-6"><label class="form-label" for="pf">Pylonenfehler-Strafe in Sekunden</label><input class="form-control" id="pf" name="pf" value="<?= escape((string) ($formValues['pf'] ?? '0')) ?>" required inputmode="decimal"></div></div>
                    <fieldset class="border rounded p-3"><legend class="float-none w-auto px-2 fs-6 mb-0">Altersklassen</legend><div id="age-class-list" class="vstack gap-2"><?php foreach (($formValues['altersklassen'] ?? []) as $class): ?><div class="age-class-row row g-2 align-items-center"><div class="col-md"><input class="form-control" name="age_label[]" value="<?= escape($class['label'] ?? '') ?>" placeholder="Bezeichnung"></div><div class="col-6 col-md-2"><input class="form-control" name="age_from[]" value="<?= escape((string) ($class['age_from'] ?? '')) ?>" type="number" min="0" placeholder="Von"></div><div class="col-6 col-md-2"><input class="form-control" name="age_to[]" value="<?= escape((string) ($class['age_to'] ?? '')) ?>" type="number" min="0" placeholder="Bis"></div><div class="col-auto"><button type="button" class="remove-age-class btn btn-outline-danger" aria-label="Altersklasse entfernen"><i class="bi bi-trash" aria-hidden="true"></i></button></div></div><?php endforeach; ?></div><button type="button" class="btn btn-outline-secondary mt-3" id="add-age-class"><i class="bi bi-plus me-1" aria-hidden="true"></i>Klasse hinzufügen</button></fieldset>
                <?php endif; ?>
                <?php if ($masterType === 'karts'): ?>
                    <div class="row g-3"><div class="col-md-6"><label class="form-label" for="kart_verein_id">Verein</label><select class="form-select" id="kart_verein_id" name="verein_id" required><option value="">Bitte auswählen</option><?php foreach ($lookups['clubs'] as $item): ?><option value="<?= (int) $item['id'] ?>" <?= (int) ($formValues['verein_id'] ?? 0) === (int) $item['id'] ? 'selected' : '' ?>><?= escape($item['name']) ?></option><?php endforeach; ?></select></div><div class="col-md-6"><label class="form-label" for="disziplin_id">Disziplin</label><select class="form-select" id="disziplin_id" name="disziplin_id" required><option value="">Bitte auswählen</option><?php foreach ($lookups['disciplines'] as $item): ?><option value="<?= (int) $item['id'] ?>" <?= (int) ($formValues['disziplin_id'] ?? 0) === (int) $item['id'] ? 'selected' : '' ?>><?= escape($item['name']) ?></option><?php endforeach; ?></select></div></div>
                    <div class="row g-3"><div class="col-md-4"><label class="form-label" for="name">Name</label><input class="form-control" id="name" name="name" value="<?= escape($formValues['name'] ?? '') ?>" maxlength="100"></div><div class="col-md-4"><label class="form-label" for="motor">Motor</label><input class="form-control" id="motor" name="motor" value="<?= escape($formValues['motor'] ?? '') ?>" maxlength="100"></div><div class="col-md-4"><label class="form-label" for="chassis">Chassis</label><input class="form-control" id="chassis" name="chassis" value="<?= escape($formValues['chassis'] ?? '') ?>" maxlength="100"></div></div>
                <?php endif; ?>
                <?php if ($masterType === 'wetter'): ?><div><label class="form-label" for="name">Bezeichnung</label><input class="form-control" id="name" name="name" value="<?= escape($formValues['name'] ?? '') ?>" required maxlength="50"></div><?php endif; ?>
                <div class="d-flex flex-wrap justify-content-end gap-2 pt-2"><a class="btn btn-secondary" href="<?= escape(base_url('verwaltung/' . $masterType)) ?>">Abbrechen</a><button class="btn btn-primary" type="submit"><?= $editMode ? 'Speichern' : 'Anlegen' ?></button></div>
            </form>
        </div>
    </div>
</section>
<?php if ($masterType === 'disziplinen'): ?><script src="<?= escape(base_url('assets/js/master-data.js')) ?>" defer></script><?php endif; ?>
