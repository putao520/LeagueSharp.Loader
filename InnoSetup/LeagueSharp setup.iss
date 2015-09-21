#dim Version[4]
#expr ParseVersion("..\bin\Release\LeagueSharp.Loader.exe", Version[0], Version[1], Version[2], Version[3])
#define MyAppVersion Str(Version[0]) + "." + Str(Version[1]) + "." + Str(Version[2]) + "." + Str(Version[3])
#define MyAppName "LeagueSharp"
#define MyAppExeName "Loader.exe"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppId={#MyAppName}
DefaultDirName="{src}\LeagueSharp"
Compression=lzma2
SolidCompression=yes
DisableReadyPage=no
DisableReadyMemo=no
DisableStartupPrompt=yes
DisableFinishedPage=yes
Uninstallable=no
OutputDir=Output\
OutputBaseFilename=LeagueSharp-update
PrivilegesRequired=admin

[Files]
;Loader
Source: "..\bin\Release\loader.exe"; DestName: "loader.exe"; Excludes: *.vshost.exe; DestDir: {app}; Flags: ignoreversion
Source: "..\bin\Release\loader.exe.config"; DestName: "loader.exe.config"; Excludes: *.vshost.exe.config;  DestDir: {app}; Flags: ignoreversion
Source: "..\bin\Release\*.dll"; DestDir: "{app}\bin\"; Flags: ignoreversion
Source: "..\bin\Release\NativeBinaries\x86\*.dll"; DestDir: "{app}\bin\"; Flags: ignoreversion

;System
Source: "..\bin\Release\System\*.dll"; DestDir: "{app}\System\"; Flags: ignoreversion
Source: "..\tools\sn.exe"; DestDir: "{app}\System\"; Flags: ignoreversion
Source: "..\Resources\key.snk"; DestDir: "{app}\System\"; Flags: ignoreversion
Source: "..\translations\*.xml"; DestDir: "{app}\translations\"; Flags: ignoreversion

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"

;Dependencies
#include "Scripts\products.iss"
#include "Scripts\products\msiproduct.iss"
#include "Scripts\products\stringversion.iss"
#include "Scripts\products\winversion.iss"
#include "Scripts\products\fileversion.iss"
#include "Scripts\products\dotnetfxversion.iss"
#include "Scripts\products\msi31.iss"
#include "Scripts\products\dotnetfx45.iss"
#include "Scripts\products\vcredist2010.iss"
#include "Scripts\products\vcredist2013.iss"
#include "Scripts\products\vcredist2015.iss"
#include "scripts\products\detectDirectX.iss"

[Registry]
Root: HKCR; Subkey: "ls"; ValueType: "string"; ValueData: "URL:Custom Protocol"; Flags: uninsdeletekey
Root: HKCR; Subkey: "ls"; ValueType: "string"; ValueName: "URL Protocol"; ValueData: ""
Root: HKCR; Subkey: "ls\DefaultIcon"; ValueType: "string"; ValueData: "{app}\{#MyAppExeName},0"
Root: HKCR; Subkey: "ls\shell\open\command"; ValueType: "string"; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

[Run]
Filename: {app}\{#MyAppExeName}; Flags: shellexec nowait; 

[Code]
function InitializeSetup(): Boolean;
begin
	initwinversion();
	msi31('3.1');
	dotnetfx45(1);
	vcredist2010();
	vcredist2013();
	vcredist2015();
	directX();
	Result := true;
end;
