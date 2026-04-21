# MSIX installation guide

This project currently signs MSIX packages with a self-signed certificate for automated release builds.

## Install steps

1. Download `autojs6-dev-tools-signing.cer`.
2. Double-click the `.cer` file and choose **Install Certificate**.
3. Import the certificate into the **Trusted People** store.
4. After the certificate is trusted, double-click the desired `.msix` package to install it.

## Which file should I download?

- `autojs6-dev-tools-win-x64-portable.zip`: portable build for Windows x64.
- `autojs6-dev-tools-win-arm64-portable.zip`: portable build for Windows ARM64.
- `autojs6-dev-tools-win-x64-setup.exe`: installer for Windows x64.
- `autojs6-dev-tools-win-arm64-setup.exe`: installer for Windows ARM64.
- `autojs6-dev-tools-win-x64.msix`: MSIX package for Windows x64.
- `autojs6-dev-tools-win-arm64.msix`: MSIX package for Windows ARM64.

## Notes

- The ZIP and EXE installer builds are the easiest options if you want to download and use the app immediately.
- The MSIX package is provided for users who prefer Windows package installation and are willing to trust the self-signed certificate first.
