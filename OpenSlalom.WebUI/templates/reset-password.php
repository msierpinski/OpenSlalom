<?php declare(strict_types=1); ?>
<section class="auth-page shell">
    <div class="auth-panel">
        <div class="auth-panel-accent" aria-hidden="true"></div>
        <div class="auth-logo-wrap"><img src="<?= escape(base_url('assets/img/logo.svg')) ?>" alt="" width="64" height="64"></div>
        <div class="auth-heading">
            <p class="eyebrow"><span></span> Zugang sichern</p>
            <h1><?= isset($resetSuccessful) ? 'Passwort geändert' : 'Neues Passwort' ?></h1>
            <p><?= isset($resetSuccessful) ? 'Dein neues Passwort ist aktiv. Du kannst dich jetzt anmelden.' : 'Lege ein neues Passwort mit mindestens 12 Zeichen fest.' ?></p>
        </div>
        <?php if (isset($resetSuccessful)): ?>
            <a class="button button-primary auth-submit" href="<?= escape(base_url('login')) ?>"><span>Zur Anmeldung</span><i aria-hidden="true">→</i></a>
        <?php else: ?>
            <?php if (isset($resetError)): ?><div class="form-message error" role="alert"><?= escape($resetError) ?></div><?php endif; ?>
            <?php if ($token !== ''): ?>
                <form class="app-form" action="<?= escape(base_url('passwort-zuruecksetzen')) ?>" method="post">
                    <input type="hidden" name="csrf_token" value="<?= escape(csrf_token()) ?>">
                    <input type="hidden" name="token" value="<?= escape($token) ?>">
                    <label><span>Neues Passwort</span><input type="password" name="password" autocomplete="new-password" required minlength="12"></label>
                    <label><span>Passwort wiederholen</span><input type="password" name="password_confirmation" autocomplete="new-password" required minlength="12"></label>
                    <button class="button button-primary auth-submit" type="submit"><span>Passwort speichern</span><i aria-hidden="true">→</i></button>
                </form>
            <?php else: ?>
                <a class="button button-primary auth-submit" href="<?= escape(base_url('passwort-vergessen')) ?>"><span>Neuen Link anfordern</span><i aria-hidden="true">→</i></a>
            <?php endif; ?>
        <?php endif; ?>
    </div>
</section>
