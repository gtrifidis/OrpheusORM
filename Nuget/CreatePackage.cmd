@echo building solution
"%programfiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\msbuild.exe" ..\Orpheus.NetCore.sln /t:Clean,Build /p:Configuration=Release

mkdir 2.0.0\lib\netcoreapp2.2
@echo Nuget folder created.
xcopy ..\OrpheusAttributes\bin\release\netcoreapp2.2\OrpheusAttributes.dll 2.0.0\lib\netcoreapp2.2 /Y /Q
xcopy ..\OrpheusAttributes\bin\release\netcoreapp2.2\OrpheusAttributes.xml 2.0.0\lib\netcoreapp2.2 /Y /Q

xcopy ..\OrpheusCore\bin\release\netcoreapp2.2\OrpheusCore.dll 2.0.0\lib\netcoreapp2.2 /Y /Q
xcopy ..\OrpheusCore\bin\release\netcoreapp2.2\OrpheusCore.xml 2.0.0\lib\netcoreapp2.2 /Y /Q

xcopy ..\OrpheusInterfaces\bin\release\netcoreapp2.2\OrpheusInterfaces.dll 2.0.0\lib\netcoreapp2.2 /Y /Q
xcopy ..\OrpheusInterfaces\bin\release\netcoreapp2.2\OrpheusInterfaces.xml 2.0.0\lib\netcoreapp2.2 /Y /Q

xcopy ..\OrpheusMySQLDDLHelper\bin\release\netcoreapp2.2\OrpheusMySQLDDLHelper.dll 2.0.0\lib\netcoreapp2.2 /Y /Q
xcopy ..\OrpheusMySQLDDLHelper\bin\release\netcoreapp2.2\OrpheusMySQLDDLHelper.xml 2.0.0\lib\netcoreapp2.2 /Y /Q

xcopy ..\OrpheusSQLServerDDLHelper\bin\release\netcoreapp2.2\OrpheusSQLServerDDLHelper.dll 2.0.0\lib\netcoreapp2.2 /Y /Q
xcopy ..\OrpheusSQLServerDDLHelper\bin\release\netcoreapp2.2\OrpheusSQLServerDDLHelper.xml 2.0.0\lib\netcoreapp2.2 /Y /Q

@echo assemblies copied.
del *.nupkg /F
nuget.exe pack OrpheusORMInterfaces.nuspec
nuget.exe pack OrpheusORMAttributes.nuspec
nuget.exe pack OrpheusORM.nuspec
nuget.exe pack OrpheusORMMySQLServerHelper.nuspec
nuget.exe pack OrpheusORMSQLServerHelper.nuspec
@echo new nupkg file created.
pause