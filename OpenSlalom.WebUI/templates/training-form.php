<?php
declare(strict_types=1);

$formAction = $editMode ? 'training/' . $trainingUuid : 'trainings';
?>
<section class="vstack gap-4">
    <header>
        <div>
            <p class="text-muted mb-0">Trainingsverwaltung</p>
            <h1 class="text-body-emphasis display-5 fw-bold"><?= $editMode ? 'Training bearbeiten' : 'Training anlegen' ?></h1>
        </div>
    </header>

    <div class="card card-body border-0 shadow-sm">
        <?php if (isset($formError)): ?><div class="alert alert-danger" role="alert"><?= escape($formError) ?></div><?php endif; ?>
        <form action="<?= escape(base_url($formAction)) ?>" method="post">
            <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">

            <div class="mb-3"><label class="form-label" for="name">Name</label><input class="form-control" id="name" name="name" value="<?= escape($formValues['name'] ?? '') ?>" required maxlength="100"></div>
            <div class="mb-3"><label class="form-label" for="beschreibung">Beschreibung</label><textarea class="form-control" id="beschreibung" name="beschreibung" required maxlength="250" rows="4"><?= escape($formValues['beschreibung'] ?? '') ?></textarea></div>
            <div class="mb-3"><label class="form-label" for="zeitpunkt">Datum</label><input class="form-control" type="date" id="zeitpunkt" name="zeitpunkt" value="<?= escape($formValues['zeitpunkt'] ?? '') ?>" required></div>
            <div class="mb-3"><label class="form-label" for="verein_id">Verein</label>
                <select class="form-select" id="verein_id" name="verein_id" required>
                    <option value="">Bitte auswählen</option>
                    <?php foreach ($lookups['clubs'] as $item): ?><option value="<?= (int) $item['id'] ?>" <?= (int) ($formValues['verein_id'] ?? 0) === (int) $item['id'] ? 'selected' : '' ?>><?= escape($item['name']) ?></option><?php endforeach; ?>
                </select>
            </div>
            <div class="mb-3"><label class="form-label" for="disziplin_id">Disziplin</label>
                <select class="form-select" id="disziplin_id" name="disziplin_id" required>
                    <option value="">Bitte auswählen</option>
                    <?php foreach ($lookups['disciplines'] as $item): ?><option value="<?= (int) $item['id'] ?>" <?= (int) ($formValues['disziplin_id'] ?? 0) === (int) $item['id'] ? 'selected' : '' ?>><?= escape($item['name']) ?></option><?php endforeach; ?>
                </select>
            </div>
            <div class="mb-3"><label class="form-label" for="wetter_id">Wetter</label>
                <select class="form-select" id="wetter_id" name="wetter_id" required>
                    <option value="">Bitte auswählen</option>
                    <?php foreach ($lookups['weather'] as $item): ?><option value="<?= (int) $item['id'] ?>" <?= (int) ($formValues['wetter_id'] ?? 0) === (int) $item['id'] ? 'selected' : '' ?>><?= escape($item['name']) ?></option><?php endforeach; ?>
                </select>
            </div>

            <div class="mb-3">
                <div class="form-check"><input class="form-check-input" type="checkbox" id="training_abgeschlossen" name="training_abgeschlossen" value="1" <?= !empty($formValues['training_abgeschlossen']) ? 'checked' : '' ?>><label class="form-check-label" for="training_abgeschlossen">Training abgeschlossen</label></div>
                <div class="form-check"><input class="form-check-input" type="checkbox" id="ist_veroeffentlicht" name="ist_veroeffentlicht" value="1" <?= !empty($formValues['ist_veroeffentlicht']) ? 'checked' : '' ?>><label class="form-check-label" for="ist_veroeffentlicht">Training öffentlich in der WebUI freigeben</label></div>
            </div>

            <div class="d-flex gap-2">
                <a class="btn btn-secondary" href="<?= escape(base_url($editMode ? 'training/' . $trainingUuid : 'trainings')) ?>">Abbrechen</a>
                <button class="btn btn-primary" type="submit"><?= $editMode ? 'Änderungen speichern' : 'Training anlegen' ?></button>
            </div>
        </form>
    </div>
</section>
