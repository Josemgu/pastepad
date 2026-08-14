# Pendiente

Lo que está decidido pero todavía no hecho. Se vacía por arriba: lo que
se hace, se borra de aquí y se cuenta en `CHANGELOG.md`.

No es una lista de ideas. Aquí solo entra lo que el usuario ha pedido
explícitamente.

## Para la próxima versión

### Quitar el nombre completo del instalador

`AppPublisher` en `instalador/pastepad.iss` dice `Jose Miguel Ortiz`.
Debe decir **`Josemgu`**, que es el usuario de GitHub y ya es público.
El usuario no quiere su nombre real ahí.

Ese campo es el que Windows enseña como «Editor» en Agregar o quitar
programas y en el aviso de SmartScreen, así que se ve.

**Ojo, no basta con cambiar el `.iss`.** El nombre completo está también
en, al menos:

- `csharp/Pastepad.App/Pastepad.App.csproj` — acaba dentro del `.exe`
  como `CompanyName`, y el propio CI lo imprime al comprobar lo
  publicado
- `README.md` y `docs/README.es.md`
- `csharp/Pastepad.Nucleo.Pruebas/PruebasNucleo.cs`

Hay que repasarlos todos en la misma pasada, o el cambio queda a medias.

Y decírselo al usuario tal cual: **el historial de git lleva su nombre y
su correo en cada commit**, y las releases ya publicadas llevan el
`AppPublisher` viejo dentro. Cambiarlo de ahora en adelante no lo borra
de lo que ya está publicado. Reescribir la historia de git es otra
decisión, y es suya.

### Firmar el ejecutable

Dejar preparada la firma con certificado de Windows, para cuando el
usuario tenga uno. Hoy no hay certificado, así que esto es solo dejar el
camino hecho:

- Inno Setup tiene `SignTool` y `SignedUninstaller`
- El `.exe` publicado se firma antes de empaquetarlo, no después
- El certificado **no puede acabar en el repo**. Va como secreto del
  repositorio de GitHub y se usa desde el workflow
- Sin firma, SmartScreen avisa al instalar; con firma deja de avisar
  cuando el certificado acumula reputación —o de inmediato con uno EV

Preguntar al usuario qué tipo de certificado consiguió antes de
implementar nada: OV y EV no se usan igual.

## Norma permanente, desde agosto de 2026

**Actualizaciones mensuales.** En cada una, antes de tocar nada, el
subagente **planificador** tiene dos encargos fijos:

1. **Buscar qué optimizar y qué limpiar** para que el programa no se
   vaya oxidando: dependencias, código que ya no se usa, medidas que se
   hayan quedado viejas.
2. **Comprobar qué ha publicado Microsoft del Windows App SDK y de
   WinUI 3** desde la versión anterior, y decir si toca subir y por qué.
   Hoy el proyecto va con 2.3.1 y la última estable era 2.4.0.

Está también en `.claude/agents/planificador.md`, que es donde el
subagente lo lee solo.
