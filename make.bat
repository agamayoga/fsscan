@echo off

rem Microsoft .NET 4.0 for 64-bit system
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe

rem Microsoft .NET 2.0 for 32-bit system
rem set CSC=%WINDIR%\Microsoft.NET\Framework\v2.0.50727\csc.exe

if not exist %CSC% goto error-csc

rem %CSC% /reference:System.Runtime.Serialization.dll /reference:lib\Newtonsoft.Json.dll /warn:0 /target:exe /out:bin\fsscan.exe src\fsscan.cs
%CSC% /nologo /reference:System.Runtime.Serialization.dll /warn:0 /target:exe /out:bin\fsscan.exe src\fsscan.cs
goto end

:error-csc
echo "csc.exe" cannot be found. Edit "build.bat" and change CSC path.
echo Path: %CSC%
goto end

:end