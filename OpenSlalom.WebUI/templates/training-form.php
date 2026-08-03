<?php
declare(strict_types=1);

$formAction = $editMode ? 'training/' . $trainingUuid : 'trainings';
?>
<section class="shell listing-page training-form-page">
    <div class="page-heading-row">
        <div>
            <p class="eyebrow"><span></span> Trainingsverwaltung</p>
            <h1><?= $editMode ? 'Training bearbeiten' : 'Training anlegen' ?></h1>
        </div>
    </div>

    <div class="result-section training-form-card">
        <?php if (isset($formError)): ?><div class="form-message error" role="alert"><?= escape($formError) ?></div><?php endif; ?>
        <form class="app-form training-management-form" action="<?= escape(base_url($formAction)) ?>" method="post">
            <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">

            <label class="full-width">Name<input name="name" value="<?= escape($formValues['name'] ?? '') ?>" required maxlength="100"></label>
            <label class="full-width">Beschreibung<textarea name="beschreibung" required maxlength="250" rows="4"><?= escape($formValues['beschreibung'] ?? '') ?></textarea></label>
            <label>Datum<input type="date" name="zeitpunkt" value="<?= escape($formValues['zeitpunkt'] ?? '') ?>" required></label>
            <label>Verein
                <select name="verein_id" required>
                    <option value="">Bitte auswählen</option>
                    <?php foreach ($lookups['clubs'] as $item): ?><option value="<?= (int) $item['id'] ?>" <?= (int) ($formValues['verein_id'] ?? 0) === (int) $item['id'] ? 'selected' : '' ?>><?= escape($item['name']) ?></option><?php endforeach; ?>
                </select>
            </label>
            <label>Disziplin
                <select name="disziplin_id" required>
                    <option value="">Bitte auswählen</option>
                    <?php foreach ($lookups['disciplines'] as $item): ?><option value="<?= (int) $item['id'] ?>" <?= (int) ($formValues['disziplin_id'] ?? 0) === (int) $item['id'] ? 'selected' : '' ?>><?= escape($item['name']) ?></option><?php endforeach; ?>
                </select>
            </label>
            <label>Wetter
                <select name="wetter_id" required>
                    <option value="">Bitte auswählen</option>
                    <?php foreach ($lookups['weather'] as $item): ?><option value="<?= (int) $item['id'] ?>" <?= (int) ($formValues['wetter_id'] ?? 0) === (int) $item['id'] ? 'selected' : '' ?>><?= escape($item['name']) ?></option><?php endforeach; ?>
                </select>
            </label>

            <div class="full-width training-form-options">
                <label class="checkbox-label"><input type="checkbox" name="training_abgeschlossen" value="1" <?= !empty($formValues['training_abgeschlossen']) ? 'checked' : '' ?>><span>Training abgeschlossen</span></label>
                <label class="checkbox-label"><input type="checkbox" name="ist_veroeffentlicht" value="1" <?= !empty($formValues['ist_veroeffentlicht']) ? 'checked' : '' ?>><span>Training öffentlich in der WebUI freigeben</span></label>
            </div>

            <div class="form-actions full-width">
                <a class="button button-secondary rounded-action" href="<?= escape(base_url($editMode ? 'training/' . $trainingUuid : 'trainings')) ?>">Abbrechen</a>
                <button class="button button-primary rounded-action" type="submit"><?= $editMode ? 'Änderungen speichern' : 'Training anlegen' ?></button>
            </div>
        </form>
    </div>
</section>
