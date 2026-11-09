@echo off
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /out:HeroiPet.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll HeroiPet.cs
if errorlevel 1 (echo ERRO na compilacao & pause) else echo OK: HeroiPet.exe criado
