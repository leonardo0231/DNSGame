#define AppName "DNSGame"
#define AppVersion "0.1.0"
#define AppPublisher "Jalil Ahmad Afshar"
#define AppExeName "DNSGame.exe"

[Setup]
AppId={{C2A75C38-CB2A-4B3F-9D54-B4090E1805FE}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\DNSGame
DefaultGroupName=DNSGame
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=DNSGame-Setup-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\DNSGame"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\DNSGame"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch DNSGame"; Flags: nowait postinstall skipifsilent
