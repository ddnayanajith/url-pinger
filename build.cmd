@echo off
setlocal

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
)

if not exist "%CSC%" (
    echo Could not find the .NET Framework C# compiler.
    exit /b 1
)

"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /out:"URLPinger.exe" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "UrlPingerApp.cs"
