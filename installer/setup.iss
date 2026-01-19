; Script d'installation Inno Setup pour Collectivite
; Version 1.0.0

#define MyAppName "Collectivite"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "ANAFIC"
#define MyAppURL "https://www.example.com/"
#define MyAppExeName "Collectivite.exe"
#define MyAppId "{{A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D}"

[Setup]
; Note: La valeur de AppId identifie de maniere unique cette application.
; Ne changez pas la valeur de AppId lors de la publication de mises a jour de votre application.
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=
InfoBeforeFile=
InfoAfterFile=
OutputDir=Output
OutputBaseFilename=Collectivite-Setup-{#MyAppVersion}
SetupIconFile=..\Collectivite\app_icon_256.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Note: Ne pas utiliser "Flags: ignoreversion" sur les fichiers partages si vous souhaitez
; verifier que le programme installe est plus recent, en fonction de sa version.

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
  NetFrameworkVersion: String;
begin
  // Verification de .NET 8.0 Runtime
  Result := True;
  if not RegQueryStringValue(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost', 'Version', NetFrameworkVersion) then
  begin
    if MsgBox('.NET 8.0 Runtime n''est pas installe.' + #13#10 +
              'Souhaitez-vous telecharger .NET 8.0 Runtime maintenant ?', 
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
    Result := False;
  end;
  
  // Verification de MySQL/MariaDB (optionnel - peut etre installe separement)
  // Note: MySQL n'est pas verifie car il peut etre installe separement
end;

function InitializeUninstall(): Boolean;
begin
  Result := True;
  // Ajouter des verifications avant desinstallation si necessaire
end;

