# MSIX installation guide

This project signs MSIX packages with a self-signed certificate during release validation and automated packaging.

## Files you need

- `autojs6-dev-tools-signing.cer`
- one of:
  - `autojs6-dev-tools-win-x64.msix`
  - `autojs6-dev-tools-win-arm64.msix`

## Install steps

1. Download `autojs6-dev-tools-signing.cer`.
2. Double-click the certificate file and choose **Install Certificate**.
3. Import it into **Trusted People**.
4. Import the same certificate into **Trusted Root Certification Authorities**.
   - For local packaging verification, importing into **Current User / Trusted People** and **Current User / Root** is enough.
   - For normal end-user MSIX installation, **Local Machine / Trusted People** plus **Local Machine / Root** is the safest choice when available.
5. After the certificate is trusted, double-click the `.msix` file to install it.

## Which package should I choose?

- `autojs6-dev-tools-win-x64-portable.zip`: portable package for Windows x64
- `autojs6-dev-tools-win-arm64-portable.zip`: portable package for Windows ARM64
- `autojs6-dev-tools-win-x64-setup.exe`: Inno Setup installer for Windows x64
- `autojs6-dev-tools-win-arm64-setup.exe`: Inno Setup installer for Windows ARM64
- `autojs6-dev-tools-win-x64.msix`: MSIX package for Windows x64
- `autojs6-dev-tools-win-arm64.msix`: MSIX package for Windows ARM64

## Notes

- ZIP and EXE are still the easiest download options for most users.
- The MSIX package is best when you prefer package-style installation and are willing to trust the certificate first.
- If the MSIX package opens but refuses to install, re-check that the certificate subject matches the package publisher and that the certificate is trusted in both **Trusted People** and **Trusted Root Certification Authorities**.
