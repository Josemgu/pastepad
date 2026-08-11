@echo off
REM Genera pastepad.exe (un solo archivo, sin consola)
REM Doble clic para ejecutar.

echo Instalando dependencias...
pip install -r requirements.txt
pip install pyinstaller

echo.
echo Generando el ejecutable...
pyinstaller --onefile --noconsole --name pastepad --collect-all customtkinter pastepad.pyw

echo.
echo Listo. El programa esta en la carpeta "dist".
pause
