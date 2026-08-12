@echo off
REM Genera pastepad.exe (un solo archivo, sin consola)
REM Doble clic para ejecutar.

echo Instalando dependencias...
pip install -r requirements.txt
pip install pyinstaller

echo.
echo Generando el ejecutable...
pyinstaller --onefile --noconsole --name pastepad ^
  --icon docs\pastepad.ico ^
  --add-data "docs\pastepad.ico;." ^
  --version-file version.txt ^
  --collect-all customtkinter ^
  pastepad.pyw

echo.
echo Listo. El programa esta en la carpeta "dist".
echo.
echo Si Windows Defender lo marca, es un falso positivo conocido de
echo PyInstaller. Puedes reportarlo aqui para que lo revisen:
echo   https://www.microsoft.com/en-us/wdsi/filesubmission
pause
