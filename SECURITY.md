# Security Policy

FryPDF is a privacy-first, fully offline desktop application — documents are
never uploaded to a cloud service, and AI/OCR features run entirely on-device.
We still take security seriously, particularly around PDF parsing of untrusted
files, the third-party plugin system, and local data handling.

## Supported Versions

FryPDF is pre-1.0 and moving quickly. Only the latest tagged
[release](https://github.com/CodeFryDev/FryPDF/releases) receives security
fixes; please upgrade before reporting an issue to confirm it still reproduces.

| Version | Supported |
| --- | --- |
| Latest release | ✅ |
| Older releases | ❌ |

## Reporting a Vulnerability

**Please do not open a public issue for security vulnerabilities.**

Instead, use GitHub's private vulnerability reporting for this repository:

1. Go to the repository's **Security** tab.
2. Click **Report a vulnerability**.
3. Include reproduction steps, the affected version/platform, and the
   potential impact. A minimal, synthetic sample PDF (no real personal data)
   is very helpful if the issue is triggered by a specific file.

You can expect an initial response within a few days. Once a fix is confirmed
we'll coordinate a disclosure timeline with you and credit you in the release
notes, unless you'd prefer to remain anonymous.

## Scope

Reports are especially welcome for:

- Malicious or malformed PDF input triggering memory-unsafe behavior, crashes,
  or code execution during parsing/deconstruction (`PdfEditorApp.Core`)
- Third-party `.fryplugin` loading, sandboxing, or assembly-resolution issues
- Local data handling (autosave, crash recovery, form data) that could leak
  information outside the user's machine
- Known-vulnerable dependencies listed in
  [`Directory.Packages.props`](Directory.Packages.props)

## Out of Scope

- Issues that require physical access to an already-unlocked device
- Social engineering aimed at maintainers or users
- Findings from purely automated scanners without a working reproduction
