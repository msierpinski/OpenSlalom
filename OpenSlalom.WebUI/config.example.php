<?php

declare(strict_types=1);

return [
    'database' => [
        'host' => '127.0.0.1',
        'port' => 3306,
        'name' => 'os',
        'user' => 'openslalom_web',
        'password' => 'replace-me',
    ],
    // Optional: separates, narrowly scoped account for web_users/web_roles/web_user_roles.
    // Falls omitted, the database account above is also used for authentication.
    'auth_database' => [
        'host' => '127.0.0.1',
        'port' => 3306,
        'name' => 'os',
        'user' => 'openslalom_web_accounts',
        'password' => 'replace-me',
    ],
    // Optional: narrowly scoped writer for training creation and editing.
    // Falls omitted, the database account above is used.
    'write_database' => [
        'host' => '127.0.0.1',
        'port' => 3306,
        'name' => 'os',
        'user' => 'openslalom_web_training_writer',
        'password' => 'replace-me',
    ],
    'site' => [
        'name' => 'openSlalom',
        'auto_refresh_seconds' => 15,
    ],
    'mail' => [
        'from' => 'noreply@example.org',
        'name' => 'openSlalom',
    ],
    'legal' => [
        'operator_name' => '[Vollständiger Name oder Firmenname]',
        'legal_form' => '',
        'represented_by' => '[Vertretungsberechtigte Person]',
        'street' => '[Straße und Hausnummer]',
        'postal_code' => '[PLZ]',
        'city' => '[Ort]',
        'country' => 'Deutschland',
        'email' => 'kontakt@example.org',
        'phone' => '[Telefonnummer]',
        'content_responsible' => '[Verantwortliche Person nach § 18 Abs. 2 MStV, Anschrift wie oben]',
        'register_name' => '',
        'register_number' => '',
        'vat_id' => '',
        'privacy_contact_email' => 'datenschutz@example.org',
        'hosting_provider' => '[Hosting-Anbieter und Anschrift]',
        'hosting_country' => 'Deutschland',
        'log_retention_days' => 7,
        'supervisory_authority_name' => '[Zuständige Datenschutzaufsichtsbehörde]',
        'supervisory_authority_url' => 'https://www.bfdi.bund.de/DE/Service/Anschriften/anschriften_table.html',
    ],
];
