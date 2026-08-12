@echo off
REM Genera el ejecutable con Flet.
REM Flet trae su propio empaquetador: no usa PyInstaller.

echo Instalando dependencias...
pip install -r requirements.txt

echo.
echo Ejecutando las pruebas...
python prueba.py
if errorlevel 1 (
  echo.
  echo Hay pruebas que fallan. Revisa antes de compilar.
  pause
  exit /b 1
)

echo.
echo Generando el ejecutable...
flet build windows --project pastepad --product pastepad ^
  --company "Jose Miguel Ortiz" --copyright "MIT License" ^
  --build-version 3.0.0

echo.
echo Listo. El programa esta en build\windows.
pause
