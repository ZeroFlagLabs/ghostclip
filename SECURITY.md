# Security and privacy

GhostClip deliberately handles screenshots and the Windows shared clipboard.
Security reports involving unexpected screenshot persistence, clipboard
history, cloud sync, clipboard clearing or unauthorised data access are
especially important.

## Reporting

Please use GitHub's **Private Vulnerability Reporting** feature for
[ZeroFlagLabs/ghostclip](https://github.com/ZeroFlagLabs/ghostclip/security)
when available. If it is not enabled, contact the repository owner privately
through an existing trusted channel before disclosing sensitive details.

Please **do not post secrets, real screenshots, clipboard contents or exploit
details in public issues**. Reproduce with synthetic sample data where possible.

## Privacy boundaries

GhostClip does not intentionally write screenshot image files, and sets
Windows clipboard history/cloud exclusion formats on its own captured items.
The shared Windows clipboard is still accessible to other applications for
the lifetime of each image. A paste destination, third-party clipboard
manager or operating-system component may retain information independently.

The v1 screenshot feature is not equivalent to preventing all capture,
recording, telemetry or persistence by other software. Always validate
Win+V history exclusion and clipboard expiry on the target Windows system
before using sensitive material.

v1 does **not** intercept Ctrl+C. Experimental text-capture research on
separate branches is not part of the supported release.
