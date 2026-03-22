# Fingerprint

Unlock hyprlock with the fingerprint scanner.

## Enroll

```bash
sudo fprintd-enroll $USER
```

Scan your finger when prompted (8–10 taps). Verify with:

```bash
fprintd-verify $USER
```

## How It Works

Hyprlock 0.9+ has native fingerprint support via
`enable_fingerprint = true` in the `general` section.
The theme engine adds this automatically. Fingerprint and
password run in parallel — either one unlocks the screen.
