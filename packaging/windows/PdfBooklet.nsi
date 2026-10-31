; The Windows installer of PdfBooklet (NSIS, after the one of Promptuarium's PStudio).
;
; Built by scripts/Build-Installer.ps1, which publishes PdfBooklet (win-x64, self-contained) and runs:
;   makensis -DAPP_VERSION=1.0.1.0 -DVI_VERSION=1.0.1.0 -DPUBLISH_DIR=<publish folder> -DOUT_FILE=<setup.exe> PdfBooklet.nsi
;
; It installs PdfBooklet for every user (64-bit Program Files), with a Start Menu entry, an optional desktop shortcut,
; PdfBooklet in "Open with" of PDF files (optional; the default PDF application is not changed) and an entry in
; Apps / Add or Remove Programs. Installing again (e.g. a newer version) removes the previous installation first.

Unicode True
SetCompressor /SOLID lzma

;--------------------------------
; Definitions (APP_VERSION, VI_VERSION, PUBLISH_DIR and OUT_FILE come from the command line)

!ifndef APP_VERSION
  !define APP_VERSION "0.0.0.0"
!endif
!ifndef PUBLISH_DIR
  !define PUBLISH_DIR "..\..\publish\win-x64"
!endif
!ifndef OUT_FILE
  !define OUT_FILE "PdfBooklet-${APP_VERSION}-win-x64-setup.exe"
!endif
; The four numbers of the file version (a pre-release suffix is left out by the build script)
!ifndef VI_VERSION
  !define VI_VERSION "0.0.0.0"
!endif

!define APP_NAME      "PdfBooklet"
!define APP_PUBLISHER "Fuszenecker Róbert"
!define APP_EXE       "PdfBooklet.exe"
!define APP_URL       "https://github.com/fuszenecker/PdfBooklet"
!define APP_ICON      "..\..\src\Assets\pdfbooklet-icon.ico"
!define REG_APP       "Software\PdfBooklet"
!define REG_UNINSTALL "Software\Microsoft\Windows\CurrentVersion\Uninstall\PdfBooklet"

; PdfBooklet is offered for PDF files in "Open with", through its own program ID
!define FILE_EXT      ".pdf"
!define FILE_PROGID   "PdfBooklet.Pdf"
!define FILE_TYPE     "PDF document"

;--------------------------------
; Includes

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "FileFunc.nsh"
!include "x64.nsh"

;--------------------------------
; The installer

Name             "${APP_NAME} ${APP_VERSION}"
Caption          "${APP_NAME} ${APP_VERSION} Setup"
OutFile          "${OUT_FILE}"
InstallDir       "$PROGRAMFILES64\${APP_NAME}"
InstallDirRegKey HKLM "${REG_APP}" "InstallDir"
RequestExecutionLevel admin
BrandingText     "${APP_NAME} ${APP_VERSION}"
ShowInstDetails  hide
ShowUninstDetails hide

VIProductVersion "${VI_VERSION}"
VIAddVersionKey "ProductName"     "${APP_NAME}"
VIAddVersionKey "ProductVersion"  "${APP_VERSION}"
VIAddVersionKey "FileVersion"     "${APP_VERSION}"
VIAddVersionKey "FileDescription" "${APP_NAME} Setup"
VIAddVersionKey "CompanyName"     "${APP_PUBLISHER}"
VIAddVersionKey "LegalCopyright"  "Copyright 2026, ${APP_PUBLISHER}"

;--------------------------------
; Pages

!define MUI_ICON   "${APP_ICON}"
!define MUI_UNICON "${APP_ICON}"
!define MUI_ABORTWARNING
!define MUI_COMPONENTSPAGE_NODESC
!define MUI_FINISHPAGE_RUN      "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Start ${APP_NAME}"
!define MUI_FINISHPAGE_LINK          "PdfBooklet on GitHub"
!define MUI_FINISHPAGE_LINK_LOCATION "${APP_URL}"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\..\LICENSE"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

;--------------------------------
; Initialization: a 64-bit Windows, the 64-bit registry, and the folder of an earlier installation

Function .onInit
  ${IfNot} ${RunningX64}
    MessageBox MB_ICONSTOP "${APP_NAME} needs a 64-bit Windows."
    Abort
  ${EndIf}

  SetRegView 64
  ReadRegStr $0 HKLM "${REG_APP}" "InstallDir"
  ${If} $0 != ""
    StrCpy $INSTDIR $0
  ${EndIf}
FunctionEnd

Function un.onInit
  SetRegView 64
FunctionEnd

;--------------------------------
; Sections

Section "${APP_NAME}" SecMain
  SectionIn RO

  ; An earlier installation is removed first, so that no file of the old version stays behind
  ${If} ${FileExists} "$INSTDIR\uninstall.exe"
    ExecWait '"$INSTDIR\uninstall.exe" /S _?=$INSTDIR'
    Delete "$INSTDIR\uninstall.exe"
  ${EndIf}

  SetOutPath "$INSTDIR"
  File /r "${PUBLISH_DIR}\*.*"
  File "..\..\LICENSE"
  WriteUninstaller "$INSTDIR\uninstall.exe"

  WriteRegStr HKLM "${REG_APP}" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "${REG_APP}" "Version"    "${APP_VERSION}"

  ; Apps / Add or Remove Programs
  WriteRegStr   HKLM "${REG_UNINSTALL}" "DisplayName"          "${APP_NAME}"
  WriteRegStr   HKLM "${REG_UNINSTALL}" "DisplayVersion"       "${APP_VERSION}"
  WriteRegStr   HKLM "${REG_UNINSTALL}" "Publisher"            "${APP_PUBLISHER}"
  WriteRegStr   HKLM "${REG_UNINSTALL}" "InstallLocation"      "$INSTDIR"
  WriteRegStr   HKLM "${REG_UNINSTALL}" "DisplayIcon"          "$INSTDIR\${APP_EXE}"
  WriteRegStr   HKLM "${REG_UNINSTALL}" "UninstallString"      '"$INSTDIR\uninstall.exe"'
  WriteRegStr   HKLM "${REG_UNINSTALL}" "QuietUninstallString" '"$INSTDIR\uninstall.exe" /S'
  WriteRegStr   HKLM "${REG_UNINSTALL}" "URLInfoAbout"         "${APP_URL}"
  WriteRegDWORD HKLM "${REG_UNINSTALL}" "NoModify" 1
  WriteRegDWORD HKLM "${REG_UNINSTALL}" "NoRepair" 1

  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKLM "${REG_UNINSTALL}" "EstimatedSize" "$0"

  ; The Start Menu entry (for every user)
  SetShellVarContext all
  CreateShortcut "$SMPROGRAMS\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0
SectionEnd

Section "Offer ${APP_NAME} in $\"Open with$\" for PDF files" SecOpenWith
  ; The program ID of PdfBooklet and the application; the default program of .pdf is left as it is
  WriteRegStr HKLM "Software\Classes\${FILE_PROGID}" "" "${FILE_TYPE}"
  WriteRegStr HKLM "Software\Classes\${FILE_PROGID}\DefaultIcon" "" "$INSTDIR\${APP_EXE},0"
  WriteRegStr HKLM "Software\Classes\${FILE_PROGID}\shell\open\command" "" '"$INSTDIR\${APP_EXE}" "%1"'
  WriteRegStr HKLM "Software\Classes\${FILE_EXT}\OpenWithProgids" "${FILE_PROGID}" ""
  WriteRegStr HKLM "Software\Classes\Applications\${APP_EXE}" "FriendlyAppName" "${APP_NAME}"
  WriteRegStr HKLM "Software\Classes\Applications\${APP_EXE}\shell\open\command" "" '"$INSTDIR\${APP_EXE}" "%1"'
  WriteRegStr HKLM "Software\Classes\Applications\${APP_EXE}\SupportedTypes" "${FILE_EXT}" ""

  ; Explorer shows the new program at once
  System::Call 'shell32::SHChangeNotify(i 0x08000000, i 0, p 0, p 0)'
SectionEnd

Section /o "Desktop shortcut" SecDesktop
  SetShellVarContext all
  CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0
SectionEnd

;--------------------------------
; Uninstaller

Section "Uninstall"
  ; A running PdfBooklet would keep its files
  nsExec::Exec 'taskkill /F /IM "${APP_EXE}"'

  SetShellVarContext all
  Delete "$SMPROGRAMS\${APP_NAME}.lnk"
  Delete "$DESKTOP\${APP_NAME}.lnk"

  ; "Open with"
  DeleteRegValue HKLM "Software\Classes\${FILE_EXT}\OpenWithProgids" "${FILE_PROGID}"
  DeleteRegKey /ifempty HKLM "Software\Classes\${FILE_EXT}\OpenWithProgids"
  DeleteRegKey HKLM "Software\Classes\${FILE_PROGID}"
  DeleteRegKey HKLM "Software\Classes\Applications\${APP_EXE}"
  System::Call 'shell32::SHChangeNotify(i 0x08000000, i 0, p 0, p 0)'

  DeleteRegKey HKLM "${REG_UNINSTALL}"
  DeleteRegKey HKLM "${REG_APP}"

  ; The folder is removed only if it is the folder of PdfBooklet (it has PdfBooklet.exe), never e.g. Program Files
  ${If} ${FileExists} "$INSTDIR\${APP_EXE}"
    RMDir /r "$INSTDIR"
  ${Else}
    Delete "$INSTDIR\uninstall.exe"
    RMDir "$INSTDIR"
  ${EndIf}
SectionEnd
