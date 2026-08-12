@echo off
REM Genera el ejecutable y lo deja listo en instalador\dist.
REM
REM Usa "flet pack" y no "flet build": el segundo compila con el SDK de
REM Flutter, que hay que descargar entero (mas de un giga) y tarda una
REM eternidad la primera vez. "flet pack" empaqueta con PyInstaller y el
REM cliente de escritorio que Flet ya trae instalado.

echo.
echo   Instalando dependencias...
pip install -r requirements.txt
pip install pyinstaller

echo.
echo   Ejecutando las pruebas...
python prueba.py
if errorlevel 1 (
  echo.
  echo   Hay pruebas que fallan. Revisa antes de compilar.
  pause
  exit /b 1
)

echo.
echo   Compilando...
flet pack main.py --onedir --name pastepad ^
  --icon docs/pastepad.ico ^
  --product-name pastepad ^
  --file-description "pastepad - gestor de portapapeles" ^
  --product-version 3.0.1 --file-version 3.0.1 ^
  --company-name "Jose Miguel Ortiz" ^
  --copyright "MIT License" ^
  --distpath instalador/dist -y
if errorlevel 1 (
  echo.
  echo   Fallo la compilacion.
  pause
  exit /b 1
)

echo.
echo   Compilando la version portable (un solo archivo)...
flet pack main.py --name pastepad-portable ^
  --icon docs/pastepad.ico ^
  --product-name pastepad ^
  --file-description "pastepad - gestor de portapapeles" ^
  --product-version 3.0.1 --file-version 3.0.1 ^
  --company-name "Jose Miguel Ortiz" ^
  --copyright "MIT License" ^
  --distpath instalador/portable -y

echo.
echo   Calculando el hash para publicarlo junto al binario...
powershell -NoProfile -Command ^
  "(Get-FileHash instalador\dist\pastepad\pastepad.exe -Algorithm SHA256).Hash"

echo.
echo   Listo. Para instalarlo:  instalador\instalar.bat
echo.
pause
