# Pendiente

Lo que está decidido pero todavía no hecho. Se vacía por arriba: lo que
se hace, se borra de aquí y se cuenta en `CHANGELOG.md`.

No es una lista de ideas. Aquí solo entra lo que el usuario ha pedido
explícitamente.

### DESCARTADO: que el panel no le robe el protagonismo

**Decisión del usuario, 14 ago 2026: no es viable, se deja.** No volver a
investigarlo sin que él lo pida.

Se estudió a fondo y queda escrito para que nadie repita el camino:

- **Win+V lo consigue porque no es una aplicación.** Medido: no cambia el
  primer plano, ni la ventana activa, ni el foco, y **no crea ninguna
  ventana** — lo pinta `TextInputHost.exe`, el host de entrada de texto
  del sistema, sobre una superficie que ya existía. Recibe teclas por
  estar dentro de la cadena de entrada, no por tener foco.
- **Sin activarse, el teclado no llega.** «The system posts keyboard
  messages to the message queue of the foreground thread that created the
  window with the keyboard focus». El ratón sí funciona; el teclado no.
- **Nadie más lo consigue.** PowerToys Run roba el foco y tiene cuatro
  incidencias abiertas por ello; CopyQ y Ditto hacen lo mismo que
  pastepad. El único que lo logra —una reimplementación de Ditto en
  AutoHotkey— **renunció al buscador**: solo Ctrl+1..5 y ratón.
- **Había una vía y funcionaba**: registrar cada tecla con
  `RegisterHotKey` mientras el panel está abierto. Medido: 53 de 53
  teclas sueltas aceptadas, 0,3 ms por apertura, se traga la tecla, y
  convive con el atajo del programa. Las tildes también, verificado con
  la distribución española: `´` + `a` da `á`.
- **Se descartó por el riesgo, no por imposible.** Lo que no registres se
  cuela en el documento del usuario, y si pastepad se cuelga con el panel
  abierto no hay letras en todo Windows hasta que reviva. En una máquina
  con agentes de seguridad de por medio, eso es demasiado.

De todo esto sí se quedó lo que valía: quitar la llamada que se colgaba.
Ver la 4.9.0.

## Para la próxima versión

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
