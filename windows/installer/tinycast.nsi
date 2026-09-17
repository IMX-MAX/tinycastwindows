!define APP_NAME "Tinycast"
!define APP_PUBLISHER "Tinycast"
!define APP_VERSION "1.0.0"
!ifndef SOURCE_DIR
  !define SOURCE_DIR "..\dist\publish"
!endif
!ifndef OUT_DIR
  !define OUT_DIR "..\dist"
!endif

Name "${APP_NAME}"
OutFile "${OUT_DIR}\TinycastSetup.exe"
InstallDir "$PROGRAMFILES64\Tinycast"
RequestExecutionLevel admin
SetCompressor /SOLID lzma
Icon "${SOURCE_DIR}\tinycast.ico"
UninstallIcon "${SOURCE_DIR}\tinycast.ico"

Page directory
Page instfiles
UninstPage uninstConfirm
UninstPage instfiles

Section "Install"
  SetOutPath "$INSTDIR"
  File /r "${SOURCE_DIR}\*.*"
  CreateDirectory "$SMPROGRAMS\Tinycast"
  CreateShortCut "$SMPROGRAMS\Tinycast\Tinycast.lnk" "$INSTDIR\Tinycast.exe" "" "$INSTDIR\tinycast.ico"
  CreateShortCut "$DESKTOP\Tinycast.lnk" "$INSTDIR\Tinycast.exe" "" "$INSTDIR\tinycast.ico"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Tinycast" "DisplayName" "Tinycast"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Tinycast" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Tinycast" "DisplayIcon" "$INSTDIR\tinycast.ico"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Tinycast" "Publisher" "${APP_PUBLISHER}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Tinycast" "DisplayVersion" "${APP_VERSION}"
SectionEnd

Section "Uninstall"
  Delete "$SMPROGRAMS\Tinycast\Tinycast.lnk"
  RMDir "$SMPROGRAMS\Tinycast"
  Delete "$DESKTOP\Tinycast.lnk"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Tinycast"
  RMDir /r "$INSTDIR"
SectionEnd
