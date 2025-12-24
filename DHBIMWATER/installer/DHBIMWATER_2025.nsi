; DHBIMWATER Revit 2025 Addin Installer
; NSIS Modern User Interface

;--------------------------------
; Include Modern UI
!include "MUI2.nsh"

;--------------------------------
; General
Name "DHBIMWATER for Revit 2025"
OutFile "C:\BuildOutput\DHBIMWATER\DHBIMWATER_2025.exe"
Unicode True

; Default installation folder
InstallDir "C:\ProgramData\Autodesk\Revit\Addins\2025\DHBIMWATER"

; Define common variables
!define REVIT_ADDINS_PATH "C:\ProgramData\Autodesk\Revit\Addins\2025"

; Request application privileges
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
VIAddVersionKey "FileDescription" "DHBIMWATER Revit 2025 Addin"
VIAddVersionKey "FileVersion" "1.0.0.0"
VIAddVersionKey "LegalCopyright" "Copyright (C) 2025 DH Corporation"

;--------------------------------
; Installer Sections

Section "Install" SecInstall
    ; Copy main DLL to ProgramData
    SetOutPath "$INSTDIR"
    File "C:\BuildOutput\DHBIMWATER\DHBIMWATER.Revit.dll"

    ; Copy .addin file to ProgramData
    SetOutPath "${REVIT_ADDINS_PATH}"
    File "C:\BuildOutput\DHBIMWATER\DHBIMWATER.addin"

    ; Create uninstaller
    SetOutPath "$INSTDIR"
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ; Write registry keys for Add/Remove Programs
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DHBIMWATER" "DisplayName" "DHBIMWATER for Revit 2025"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DHBIMWATER" "UninstallString" "$INSTDIR\Uninstall.exe"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DHBIMWATER" "DisplayIcon" "$INSTDIR\DHBIMWATER.Revit.dll"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DHBIMWATER" "Publisher" "DH Corporation"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DHBIMWATER" "DisplayVersion" "1.0.0"
    WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DHBIMWATER" "NoModify" 1
    WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DHBIMWATER" "NoRepair" 1
SectionEnd

;--------------------------------
; Uninstaller Section

Section "Uninstall"
    ; Remove files
    Delete "$INSTDIR\DHBIMWATER.Revit.dll"
    Delete "$INSTDIR\Uninstall.exe"

    ; Remove .addin file
    Delete "${REVIT_ADDINS_PATH}\DHBIMWATER.addin"

    ; Remove directories
    RMDir "$INSTDIR"

    ; Remove registry keys
    DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DHBIMWATER"
SectionEnd
