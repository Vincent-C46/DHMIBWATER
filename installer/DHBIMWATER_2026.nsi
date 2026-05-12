; DHBIMWATER Revit 2026 Addin Installer
; NSIS Modern User Interface

;--------------------------------
; Include Modern UI
!include "MUI2.nsh"

;--------------------------------
; General
Name "DHBIMWATER for Revit 2026"
OutFile "C:\BuildOutput\DHBIMWATER\DHBIMWATER_2026.exe"
Unicode True

; Default installation folder
InstallDir "C:\ProgramData\Autodesk\Revit\Addins\2026\DHBIMWATER"

; Define common variables
!define REVIT_ADDINS_PATH "C:\ProgramData\Autodesk\Revit\Addins\2026"
!define COLLAB_ADDIN_SOURCE_DIR "D:\03.업무\05.개발\01.상하수도부\@@개발성과\02.박대리\00.설치파일"

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
VIAddVersionKey "FileDescription" "DHBIMWATER Revit 2026 Addin"
VIAddVersionKey "FileVersion" "1.0.0.0"
VIAddVersionKey "LegalCopyright" "Copyright (C) 2026 DH Corporation"

;--------------------------------
; Installer Sections

Section "Install" SecInstall
   ; 1) DH-Water
   SetOutPath "${REVIT_ADDINS_PATH}"
   File "${COLLAB_ADDIN_SOURCE_DIR}\DH_Revit_test.dll"
   File /oname=01_DH_Revit_test.addin "${COLLAB_ADDIN_SOURCE_DIR}\DH_Revit_test.addin"

    ; 2) DHBIMWATER
    SetOutPath "$INSTDIR"
    File "C:\BuildOutput\DHBIMWATER\DHBIMWATER.Revit.dll"

    ; 3) DHBIMWATER.addin
    SetOutPath "${REVIT_ADDINS_PATH}"
    File /oname=02_DHBIMWATER.addin "C:\BuildOutput\DHBIMWATER\DHBIMWATER.addin"

    ; Create uninstaller
    SetOutPath "$INSTDIR"
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ; Write registry keys for Add/Remove Programs
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DHBIMWATER" "DisplayName" "DHBIMWATER for Revit 2026"
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
    Delete "$INSTDIR\DHBIMWATER.Revit.dll"
    Delete "$INSTDIR\Uninstall.exe"

    ; Delete my Addon
    Delete "${REVIT_ADDINS_PATH}\02_DHBIMWATER.addin"

    ; Delete his Addon
    Delete "${REVIT_ADDINS_PATH}\DH_Revit_test.dll"
    Delete "${REVIT_ADDINS_PATH}\01_DH_Revit_test.addin"

    RMDir "$INSTDIR"
    ; Remove registry keys
    DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DHBIMWATER"
    SectionEnd
