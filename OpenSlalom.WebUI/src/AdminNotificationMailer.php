<?php

declare(strict_types=1);

final class AdminNotificationMailer
{
    public function __construct(private readonly array $config)
    {
    }

    public function sendRegistration(array $recipients, string $username, string $email): bool
    {
        $from = (string) ($this->config['from'] ?? 'noreply@localhost.localdomain');
        if (!filter_var($from, FILTER_VALIDATE_EMAIL)) {
            return false;
        }

        $siteName = preg_replace('/[^a-zA-Z0-9 äöüÄÖÜß._-]/u', '', (string) ($this->config['name'] ?? 'openSlalom')) ?: 'openSlalom';
        $subject = $siteName . ': Neue Benutzerregistrierung';
        $message = "In der {$siteName}-WebUI wurde ein neues Konto registriert.\n\n";
        $message .= "Benutzername: {$username}\nE-Mail-Adresse: {$email}\nRolle: Registriert\n\n";
        $message .= "Benutzer prüfen und zuordnen:\n" . absolute_url('admin/benutzer') . "\n";
        $headers = [
            'From: ' . $from,
            'Content-Type: text/plain; charset=UTF-8',
            'X-Mailer: PHP/' . PHP_VERSION,
        ];

        $sent = true;
        foreach ($recipients as $recipient) {
            if (!is_string($recipient) || !filter_var($recipient, FILTER_VALIDATE_EMAIL)) {
                continue;
            }
            $sent = mail($recipient, $subject, $message, implode("\r\n", $headers)) && $sent;
        }

        return $recipients !== [] && $sent;
    }
}
