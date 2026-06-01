[Setup]
AppName=Control Laboratorio BVE
AppVersion=1.0.5
DefaultDirName={pf}\ControlLaboratorio
DefaultGroupName=Control Laboratorio
UninstallDisplayIcon={app}\ControlLaboratorio.Agent.exe
Compression=lzma2
SolidCompression=yes
OutputDir=C:\Users\FAMHURP\Desktop\PROYECTOS CFQY\Control-Laboratorio\Instalador
OutputBaseFilename=Instalador_ControlLaboratorio_1.0.5
PrivilegesRequired=admin

[Files]
Source: "C:\Users\FAMHURP\Desktop\PROYECTOS CFQY\Control-Laboratorio\Agent\bin\Release\net9.0-windows\win-x64\publish\ControlLaboratorio.Agent.exe"; DestDir: "{app}"; Flags: ignoreversion

[Run]
Filename: "{app}\ControlLaboratorio.Agent.exe"; Description: "Lanzar Control de Laboratorio ahora"; Flags: nowait postinstall skipifsilent
