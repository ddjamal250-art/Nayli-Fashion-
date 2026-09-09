[Setup]
AppName=Nayli Fashion ERP
AppVersion=1.0.0
AppPublisher=Jamal Art
DefaultDirName={autopf}\NayliFashion
DefaultGroupName=Nayli Fashion
OutputBaseFilename=NayliFashion-Setup-v1.0.0
OutputDir=..\publish\installer
Compression=lzma2/ultra64
SolidCompression=yes
SetupIconFile=app_icon.ico
UninstallDisplayIcon={app}\NayliFashion.Wpf.exe
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\win-x64\NayliFashion.Wpf.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Nayli Fashion"; Filename: "{app}\NayliFashion.Wpf.exe"
Name: "{group}\{cm:UninstallProgram,Nayli Fashion}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Nayli Fashion"; Filename: "{app}\NayliFashion.Wpf.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\NayliFashion.Wpf.exe"; Description: "{cm:LaunchProgram,Nayli Fashion}"; Flags: nowait postinstall skipifsilent