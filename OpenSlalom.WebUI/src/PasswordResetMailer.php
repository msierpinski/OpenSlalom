<?php

declare(strict_types=1);

final class PasswordResetMailer
{
    public function __construct(private readonly array $config)
    {
    }

    public function send(string $recipient, string $token): bool
    {
        $from = (string) ($this->config['from'] ?? 'noreply@localhost.localdomain');
        if (!filter_var($recipient, FILTER_VALIDATE_EMAIL) || !filter_var($from, FILTER_VALIDATE_EMAIL)) {
            return false;
        }

        $siteName = preg_replace('/[^a-zA-Z0-9 äöüÄÖÜß._-]/u', '', (string) ($this->config['name'] ?? 'openSlalom')) ?: 'openSlalom';
        $resetUrl = absolute_url('passwort-zuruecksetzen?token=' . rawurlencode($token));
        $subject = $siteName . ': Passwort zuruecksetzen';
        $message = "Für dein {$siteName}-Konto wurde ein neues Passwort angefordert.\n\n";
        $message .= "Passwort innerhalb von 60 Minuten zurücksetzen:\n{$resetUrl}\n\n";
        $message .= "Falls du diese Anfrage nicht gestellt hast, kannst du diese E-Mail ignorieren.\n";
        $headers = [
            'From: ' . $from,
            'Content-Type: text/plain; charset=UTF-8',
            'X-Mailer: PHP/' . PHP_VERSION,
        ];

        return mail($recipient, $subject, $message, implode("\r\n", $headers));
    }
}
