; Inno Setup 6 Script for FryPDF by CodeFryDev
#ifndef MyAppVersion
#define MyAppVersion "1.0.0"
#endif

#ifndef MyPublishDir
#define MyPublishDir "..\..\publish\win-x64"
#endif

#define MyAppName "FryPDF"
#define MyAppPublisher "CodeFryDev"
#define MyAppURL "https://codefrydev.in"
#define MyAppSupportURL "mailto:codefrydev@gmail.com"
#define MyAppExeName "PdfEditorApp.exe"

[Setup]
; Basic Application Info
AppId={{D37E88A1-1B2F-4A92-875D-876E842109AB}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppSupportURL}
AppUpdatesURL={#MyAppURL}

; Destination Directories
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Output Configuration
OutputDir=.
OutputBaseFilename=FryPDF-Setup-{#MyAppVersion}
SetupIconFile=app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; Compression & Modern Wizard Style
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; File Associations
ChangesAssociations=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "fileassoc_proj"; Description: "Associate with FryPDF project files (.pdfproj)"; GroupDescription: "File Associations:"
Name: "fileassoc_pdf"; Description: "Associate with Adobe / standard PDF documents (.pdf)"; GroupDescription: "File Associations:"; Flags: unchecked

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; .pdfproj Association
Root: HKA; Subkey: "Software\Classes\.pdfproj"; ValueType: string; ValueName: ""; ValueData: "FryPDF.Project"; Flags: uninsdeletevalue; Tasks: fileassoc_proj
Root: HKA; Subkey: "Software\Classes\FryPDF.Project"; ValueType: string; ValueName: ""; ValueData: "FryPDF Project File"; Flags: uninsdeletekey; Tasks: fileassoc_proj
Root: HKA; Subkey: "Software\Classes\FryPDF.Project\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Flags: uninsdeletekey; Tasks: fileassoc_proj
Root: HKA; Subkey: "Software\Classes\FryPDF.Project\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Flags: uninsdeletekey; Tasks: fileassoc_proj

; .pdf Association (optional)
Root: HKA; Subkey: "Software\Classes\.pdf\OpenWithProgids"; ValueType: string; ValueName: "FryPDF.Document"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc_pdf
Root: HKA; Subkey: "Software\Classes\FryPDF.Document"; ValueType: string; ValueName: ""; ValueData: "PDF Document"; Flags: uninsdeletekey; Tasks: fileassoc_pdf
Root: HKA; Subkey: "Software\Classes\FryPDF.Document\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Flags: uninsdeletekey; Tasks: fileassoc_pdf
Root: HKA; Subkey: "Software\Classes\FryPDF.Document\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Flags: uninsdeletekey; Tasks: fileassoc_pdf

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
