@echo off
rem Reconstroi o HeroiPet.exe e o instalador (usa o compilador que ja vem no Windows)
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist installer\saida mkdir installer\saida
"%CSC%" /nologo /target:winexe /optimize+ /out:installer\saida\HeroiPet.exe /win32icon:installer\heroi.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll HeroiPet.cs installer\InfoDoPrograma.cs || goto erro
"%CSC%" /nologo /target:winexe /optimize+ /out:installer\saida\Instalador-HeroiDoDesktop.exe /win32icon:installer\heroi.ico /win32manifest:installer\app.manifest /resource:installer\saida\HeroiPet.exe,HeroiPet.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll installer\Instalador.cs || goto erro
echo OK: installer\saida\Instalador-HeroiDoDesktop.exe
goto fim
:erro
echo ERRO na compilacao
:fim
pause
