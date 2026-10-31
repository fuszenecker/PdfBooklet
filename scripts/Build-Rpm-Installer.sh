#!/usr/bin/env bash
# Builds the RPM package of PdfBooklet, with the same content as the Debian package (scripts/Build-Deb-Installer.sh):
# a self-contained build (no .NET is needed on the target) in /usr/lib/pdfbooklet, the launcher, the desktop entry
# (PdfBooklet is offered in "Open with" for PDF files), the icon and the license.
#
# Usage: scripts/Build-Rpm-Installer.sh [--version 1.0.1.0] [--runtime linux-x64|linux-arm64] [--release 1] [--skip-publish]
#
# The version defaults to version.txt; the package goes to installer/rpm/<arch>/. --skip-publish reuses publish/<runtime>.

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/.." && pwd)"

VERSION=""
RUNTIME="linux-x64"
RELEASE="1"
SKIP_PUBLISH="false"

while [[ $# -gt 0 ]]; do
  case "$1" in
    -v|--version) VERSION="$2"; shift 2 ;;
    -r|--runtime) RUNTIME="$2"; shift 2 ;;
    --release) RELEASE="$2"; shift 2 ;;
    --skip-publish) SKIP_PUBLISH="true"; shift ;;
    -h|--help) sed -n '2,8p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Unknown option: $1" >&2; exit 1 ;;
  esac
done

if [[ -z "${VERSION}" ]]; then
  VERSION="$(tr -d '\r\n' < "${REPO_ROOT}/version.txt")"
fi
[[ -n "${VERSION}" ]] || { echo "The version is empty: update version.txt or pass --version." >&2; exit 1; }
[[ "${RELEASE}" =~ ^[0-9]+$ ]] || { echo "The release must be a positive integer: ${RELEASE}" >&2; exit 1; }

case "${RUNTIME}" in
  linux-x64) ARCH="x86_64" ;;
  linux-arm64) ARCH="aarch64" ;;
  *) echo "Unsupported runtime: ${RUNTIME} (linux-x64 or linux-arm64)" >&2; exit 1 ;;
esac

command -v rpmbuild >/dev/null || { echo "rpmbuild is needed (package rpm-build)." >&2; exit 1; }

PACKAGE="pdfbooklet"
PUBLISH_DIR="${REPO_ROOT}/publish/${RUNTIME}"
OUTPUT_DIR="${REPO_ROOT}/installer/rpm"
TOPDIR="${OUTPUT_DIR}/build"
ROOT="${TOPDIR}/root"
SPEC="${TOPDIR}/SPECS/${PACKAGE}.spec"
PACKAGING="${REPO_ROOT}/packaging/linux"

echo "==> PdfBooklet ${VERSION}-${RELEASE}, ${RUNTIME} (${ARCH})"

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

rm -rf "${TOPDIR}"
mkdir -p "${TOPDIR}/SPECS" \
  "${ROOT}/usr/bin" \
  "${ROOT}/usr/lib/${PACKAGE}" \
  "${ROOT}/usr/share/applications" \
  "${ROOT}/usr/share/icons/hicolor/256x256/apps" \
  "${ROOT}/usr/share/licenses/${PACKAGE}"

cp -a "${PUBLISH_DIR}/." "${ROOT}/usr/lib/${PACKAGE}/"

# The native libraries bring their debug symbols, which the package does not need
find "${ROOT}/usr/lib/${PACKAGE}" -name '*.pdb' -delete

cat > "${ROOT}/usr/bin/${PACKAGE}" <<EOF
#!/bin/sh
exec /usr/lib/${PACKAGE}/PdfBooklet "\$@"
EOF

install -m 0644 "${PACKAGING}/pdfbooklet.desktop" "${ROOT}/usr/share/applications/${PACKAGE}.desktop"
install -m 0644 "${REPO_ROOT}/src/Assets/pdfbooklet-icon.png" "${ROOT}/usr/share/icons/hicolor/256x256/apps/${PACKAGE}.png"
install -m 0644 "${REPO_ROOT}/LICENSE" "${ROOT}/usr/share/licenses/${PACKAGE}/LICENSE"

find "${ROOT}" -type d -exec chmod 0755 {} +
find "${ROOT}/usr" -type f -exec chmod 0644 {} +
chmod 0755 "${ROOT}/usr/bin/${PACKAGE}" "${ROOT}/usr/lib/${PACKAGE}/PdfBooklet"

# The files are packaged as they are published: no stripping, no debug package, and the bundled libraries of the
# runtime neither provide nor require anything (only the libraries of the system below are required).
cat > "${SPEC}" <<EOF
%global debug_package %{nil}
%global __os_install_post %{nil}
%global _build_id_links none
%global __provides_exclude_from ^/usr/lib/${PACKAGE}/.*\$
%global __requires_exclude_from ^/usr/lib/${PACKAGE}/.*\$

Name:           ${PACKAGE}
Version:        ${VERSION}
Release:        ${RELEASE}%{?dist}
Summary:        Turns a PDF into a print-ready 2-up booklet
License:        AGPL-3.0-only
URL:            https://github.com/fuszenecker/PdfBooklet
BuildArch:      ${ARCH}
Requires:       glibc libstdc++ libgcc zlib libX11 libICE libSM fontconfig
Recommends:     desktop-file-utils

%description
PdfBooklet imposes the pages of a PDF two by two on landscape sheets, in the
order that gives a booklet when the printed stack is folded in the middle.
The sheet size, the margins, the binding edge, the scaling, the signatures and
the output order (duplex or manual duplex) are adjustable, with a live preview
that shows exactly what is exported.

%install
cp -a "${ROOT}/." %{buildroot}/

%files
%license /usr/share/licenses/${PACKAGE}/LICENSE
/usr/bin/${PACKAGE}
/usr/lib/${PACKAGE}
/usr/share/applications/${PACKAGE}.desktop
/usr/share/icons/hicolor/256x256/apps/${PACKAGE}.png

%changelog
* $(LC_ALL=C date "+%a %b %d %Y") Fuszenecker Róbert <robert.fuszenecker@gmail.com> - ${VERSION}-${RELEASE}
- Release ${VERSION}, see https://github.com/fuszenecker/PdfBooklet/releases
EOF

echo "==> Building the RPM package"
rpmbuild --define "_topdir ${TOPDIR}" --define "_rpmdir ${OUTPUT_DIR}" -bb "${SPEC}"

RPM="$(find "${OUTPUT_DIR}/${ARCH}" -maxdepth 1 -type f -name "${PACKAGE}-${VERSION}-${RELEASE}*.rpm" | head -n 1)"
[[ -n "${RPM}" ]] || { echo "rpmbuild finished, but the package was not found in ${OUTPUT_DIR}/${ARCH}." >&2; exit 1; }
echo "${RPM}"
