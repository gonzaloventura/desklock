[Setup]
AppId={{D3SKL0CK-1234-5678-ABCD-DESKLOCK0001}
AppName=DeskLock
AppVersion=1.0.5
AppPublisher=Gonzalo Ventura
AppPublisherURL=https://github.com/gonzaloventura/desklock
DefaultDirName={autopf}\DeskLock
DefaultGroupName=DeskLock
UninstallDisplayIcon={app}\DeskLock.exe
OutputDir=..\build
OutputBaseFilename=DeskLock-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
SetupIconFile=DeskLock.ico
WizardStyle=modern

[Files]
Source: "..\build\windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\DeskLock"; Filename: "{app}\DeskLock.exe"
Name: "{group}\Uninstall DeskLock"; Filename: "{uninstallexe}"
Name: "{autodesktop}\DeskLock"; Filename: "{app}\DeskLock.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "startupicon"; Description: "Start DeskLock with Windows"; GroupDescription: "Startup:"; Flags: unchecked

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "DeskLock"; ValueData: """{app}\DeskLock.exe"""; Flags: uninsdeletevalue; Tasks: startupicon

[Run]
Filename: "{app}\DeskLock.exe"; Description: "Launch DeskLock"; Flags: nowait postinstall skipifsilent
