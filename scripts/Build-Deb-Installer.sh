#!/usr/bin/env bash
# Builds the Debian package of PdfBooklet (after the one of Promptuarium's PStudio): a self-contained build (no .NET
# is needed on the target) in /usr/lib/pdfbooklet, the launcher, the desktop entry (PdfBooklet is offered in "Open
# with" for PDF files), the icon, the copyright and the changelog.
#
# Usage: scripts/Build-Deb-Installer.sh [--version 1.0.1.0] [--runtime linux-x64|linux-arm64] [--skip-publish]
#
# The version defaults to version.txt; the package goes to installer/deb/. --skip-publish reuses publish/<runtime>.

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/.." && pwd)"

VERSION=""
RUNTIME="linux-x64"
SKIP_PUBLISH="false"

while [[ $# -gt 0 ]]; do
  case "$1" in
    -v|--version) VERSION="$2"; shift 2 ;;
    -r|--runtime) RUNTIME="$2"; shift 2 ;;
    --skip-publish) SKIP_PUBLISH="true"; shift ;;
    -h|--help) sed -n '2,8p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Unknown option: $1" >&2; exit 1 ;;
  esac
done

if [[ -z "${VERSION}" ]]; then
  VERSION="$(tr -d '\r\n' < "${REPO_ROOT}/version.txt")"
fi
[[ -n "${VERSION}" ]] || { echo "The version is empty: update version.txt or pass --version." >&2; exit 1; }

case "${RUNTIME}" in
  linux-x64) ARCH="amd64" ;;
  linux-arm64) ARCH="arm64" ;;
  *) echo "Unsupported runtime: ${RUNTIME} (linux-x64 or linux-arm64)" >&2; exit 1 ;;
esac

command -v dpkg-deb >/dev/null || { echo "dpkg-deb is needed (package dpkg)." >&2; exit 1; }

PACKAGE="pdfbooklet"
PUBLISH_DIR="${REPO_ROOT}/publish/${RUNTIME}"
OUTPUT_DIR="${REPO_ROOT}/installer/deb"
WORK_DIR="${OUTPUT_DIR}/build"
ROOT="${WORK_DIR}/${PACKAGE}_${VERSION}_${ARCH}"
DEB="${OUTPUT_DIR}/${PACKAGE}_${VERSION}_${ARCH}.deb"
PACKAGING="${REPO_ROOT}/packaging/linux"

echo "==> PdfBooklet ${VERSION}, ${RUNTIME} (${ARCH})"

if [[ "${SKIP_PUBLISH}" != "true" ]]; then
  echo "==> Publishing"
  rm -rf "${PUBLISH_DIR}"
  dotnet publish "${REPO_ROOT}/src/PdfBooklet.csproj" \
    -c Release \
    -r "${RUNTIME}" \
    --self-contained true \
    -p:Version="${VERSION}" \
    -p:DebugType=none \
    -o "${PUBLISH_DIR}"
fi
[[ -d "${PUBLISH_DIR}" ]] || { echo "No publish output in ${PUBLISH_DIR}." >&2; exit 1; }

rm -rf "${WORK_DIR}"
mkdir -p "${ROOT}/DEBIAN" \
  "${ROOT}/usr/bin" \
  "${ROOT}/usr/lib/${PACKAGE}" \
  "${ROOT}/usr/share/applications" \
  "${ROOT}/usr/share/icons/hicolor/256x256/apps" \
  "${ROOT}/usr/share/doc/${PACKAGE}" \
  "${ROOT}/usr/share/lintian/overrides"

cp -a "${PUBLISH_DIR}/." "${ROOT}/usr/lib/${PACKAGE}/"

# The native libraries bring their debug symbols, which the package does not need
find "${ROOT}/usr/lib/${PACKAGE}" -name '*.pdb' -delete

cat > "${ROOT}/usr/bin/${PACKAGE}" <<EOF
#!/bin/sh
exec /usr/lib/${PACKAGE}/PdfBooklet "\$@"
EOF

install -m 0644 "${PACKAGING}/pdfbooklet.desktop" "${ROOT}/usr/share/applications/${PACKAGE}.desktop"
install -m 0644 "${REPO_ROOT}/src/Assets/pdfbooklet-icon.png" "${ROOT}/usr/share/icons/hicolor/256x256/apps/${PACKAGE}.png"
install -m 0644 "${REPO_ROOT}/LICENSE" "${ROOT}/usr/share/doc/${PACKAGE}/copyright"
install -m 0644 "${PACKAGING}/lintian-overrides" "${ROOT}/usr/share/lintian/overrides/${PACKAGE}"

cat > "${WORK_DIR}/changelog" <<EOF
${PACKAGE} (${VERSION}) unstable; urgency=medium

  * Release ${VERSION}, see https://github.com/fuszenecker/PdfBooklet/releases

 -- Fuszenecker Róbert <robert.fuszenecker@gmail.com>  $(LC_ALL=C date -R)
EOF
gzip -9n -c "${WORK_DIR}/changelog" > "${ROOT}/usr/share/doc/${PACKAGE}/changelog.gz"

# dpkg-deb wants the usual permissions, whatever the umask of the build was
find "${ROOT}" -type d -exec chmod 0755 {} +
find "${ROOT}/usr" -type f -exec chmod 0644 {} +
chmod 0755 "${ROOT}/usr/bin/${PACKAGE}" "${ROOT}/usr/lib/${PACKAGE}/PdfBooklet"

cat > "${ROOT}/DEBIAN/control" <<EOF
Package: ${PACKAGE}
Version: ${VERSION}
Section: graphics
Priority: optional
Architecture: ${ARCH}
Maintainer: Fuszenecker Róbert <robert.fuszenecker@gmail.com>
Depends: libc6, libstdc++6, libgcc-s1, zlib1g, libx11-6, libice6, libsm6, libfontconfig1
Recommends: desktop-file-utils
Installed-Size: $(du -sk "${ROOT}/usr" | cut -f1)
Homepage: https://github.com/fuszenecker/PdfBooklet
Description: Turns a PDF into a print-ready 2-up booklet
 PdfBooklet imposes the pages of a PDF two by two on landscape sheets, in the
 order that gives a booklet when the printed stack is folded in the middle.
 The sheet size, the margins, the binding edge, the scaling, the signatures and
 the output order (duplex or manual duplex) are adjustable, with a live preview
 that shows exactly what is exported.
EOF

echo "==> Building ${DEB}"
dpkg-deb --build --root-owner-group "${ROOT}" "${DEB}"
echo "${DEB}"
