; DHBIMWATER Revit 2025-2026 Addin Installer
; Installs DHBIMWATER only. Does not install DH-Water.

!include "MUI2.nsh"

;--------------------------------
; General
Name "DHBIMWATER for Revit 2025-2026"
OutFile "C:\BuildOutput\DHBIMWATER\DHBIMWATER_25~26.exe"
Unicode True

InstallDir "C:\ProgramData\Autodesk\Revit\Addins\DHBIMWATER_25~26"

!define BUILD_OUTPUT_DIR "C:\BuildOutput\DHBIMWATER"
!define REVIT_2025_ADDINS_PATH "C:\ProgramData\Autodesk\Revit\Addins\2025"
!define REVIT_2026_ADDINS_PATH "C:\ProgramData\Autodesk\Revit\Addins\2026"
!define REVIT_2025_INSTALL_DIR "${REVIT_2025_ADDINS_PATH}\DHBIMWATER"
!define REVIT_2026_INSTALL_DIR "${REVIT_2026_ADDINS_PATH}\DHBIMWATER"
!define UNINSTALL_REG_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\DHBIMWATER_25~26"

RequestExecutionLevel user

;--------------------------------
; Interface Settings
!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

;--------------------------------
; Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "License.txt"
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

;--------------------------------
; Languages
!insertmacro MUI_LANGUAGE "Korean"

;--------------------------------
; Version Information
VIProductVersion "1.0.0.0"
VIAddVersionKey "ProductName" "DHBIMWATER"
VIAddVersionKey "CompanyName" "DH Corporation"
VIAddVersionKey "FileDescription" "DHBIMWATER Revit 2025-2026 Addin"
VIAddVersionKey "FileVersion" "1.0.0.0"
VIAddVersionKey "LegalCopyright" "Copyright (C) 2026 DH Corporation"

;--------------------------------
; Installer Sections

Section "Install" SecInstall
    ; Revit 2025
    SetOutPath "${REVIT_2025_INSTALL_DIR}"
    File "${BUILD_OUTPUT_DIR}\DHBIMWATER.Revit.dll"

    SetOutPath "${REVIT_2025_ADDINS_PATH}"
    Delete "${REVIT_2025_ADDINS_PATH}\DHBIMWATER.addin"
    Delete "${REVIT_2025_ADDINS_PATH}\02_DHBIMWATER.addin"
    File /oname=DHBIMWATER.addin "${BUILD_OUTPUT_DIR}\DHBIMWATER.addin"

    ; Revit 2026
    SetOutPath "${REVIT_2026_INSTALL_DIR}"
    File "${BUILD_OUTPUT_DIR}\DHBIMWATER.Revit.dll"

    SetOutPath "${REVIT_2026_ADDINS_PATH}"
    Delete "${REVIT_2026_ADDINS_PATH}\DHBIMWATER.addin"
    Delete "${REVIT_2026_ADDINS_PATH}\02_DHBIMWATER.addin"
    File /oname=DHBIMWATER.addin "${BUILD_OUTPUT_DIR}\DHBIMWATER.addin"

    ; Shared uninstaller
    SetOutPath "$INSTDIR"
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ; Write registry keys for Add/Remove Programs
    WriteRegStr HKCU "${UNINSTALL_REG_KEY}" "DisplayName" "DHBIMWATER for Revit 2025-2026"
    WriteRegStr HKCU "${UNINSTALL_REG_KEY}" "UninstallString" "$INSTDIR\Uninstall.exe"
    WriteRegStr HKCU "${UNINSTALL_REG_KEY}" "DisplayIcon" "${REVIT_2026_INSTALL_DIR}\DHBIMWATER.Revit.dll"
    WriteRegStr HKCU "${UNINSTALL_REG_KEY}" "Publisher" "DH Corporation"
    WriteRegStr HKCU "${UNINSTALL_REG_KEY}" "DisplayVersion" "1.0.0"
    WriteRegDWORD HKCU "${UNINSTALL_REG_KEY}" "NoModify" 1
    WriteRegDWORD HKCU "${UNINSTALL_REG_KEY}" "NoRepair" 1
SectionEnd

;--------------------------------
; Uninstaller Section

Section "Uninstall"
    ; Revit 2025
    Delete "${REVIT_2025_INSTALL_DIR}\DHBIMWATER.Revit.dll"
    Delete "${REVIT_2025_ADDINS_PATH}\DHBIMWATER.addin"
    Delete "${REVIT_2025_ADDINS_PATH}\02_DHBIMWATER.addin"
    RMDir "${REVIT_2025_INSTALL_DIR}"

    ; Revit 2026
    Delete "${REVIT_2026_INSTALL_DIR}\DHBIMWATER.Revit.dll"
    Delete "${REVIT_2026_ADDINS_PATH}\DHBIMWATER.addin"
    Delete "${REVIT_2026_ADDINS_PATH}\02_DHBIMWATER.addin"
    RMDir "${REVIT_2026_INSTALL_DIR}"

    ; Shared uninstaller
    Delete "$INSTDIR\Uninstall.exe"
    RMDir "$INSTDIR"

    DeleteRegKey HKCU "${UNINSTALL_REG_KEY}"
SectionEnd
