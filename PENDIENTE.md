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

### Reportados el 14 ago 2026, sin investigar todavía

Los tres son del usuario, describiéndolos él mismo. **Buscar la opción
antes de tocar código** — es su norma y se le olvidó a Claude una vez.

**1. «Copiado, pero no pegado» la primera vez.** No siempre, pero cuando
pasa es *la primera vez* que elige un sitio donde pegar; después va bien.
Significa que `Foco.Devolver` devolvió false.

La 4.9.0 le puso delante `PorLasBuenas`, que puede haberlo arreglado de
paso — **está sin comprobar**. Lo primero es preguntarle si sigue
pasando con la 4.9.0 antes de investigar nada.

Si sigue: el aviso ya sale por la bandeja desde la 4.6.0, así que ahora
sí se lee. Y en el log queda `SetForegroundWindow rechazado para 0x…`.

**2. El diálogo de texto nuevo se corta con la ventana pequeña.** La
caja tiene `MinHeight = 140` y va en la fila de estrella de
`CuerpoConHueco`, que la recorta cuando no cabe. Con el panel en su
mínimo no entra: título, carpeta, nombre, tipo, barra de formato, caja,
nota y pie.

Ya está medido en el `CHANGELOG` de la 4.3.0 que en el mínimo de 340 los
botones quedan bajo el pliegue. Ahora además el desplegable de tipo
añade altura. Opciones que ni se han evaluado: que la caja encoja por
debajo de 140 cuando no hay sitio, que la barra de formato se pliegue, o
que el diálogo tenga su propio desplazamiento en vez de recortar.

**3. No se puede mover la ventana con un diálogo abierto.** El
`ContentDialog` cubre el panel entero, incluida `ZonaArrastre`, que es
la franja por la que se arrastra. No queda nada de dónde agarrar.

### Lo de la lentitud tras el reposo, a medias

Descartado que sea la memoria: vaciar el working set entero cuesta ~14 ms
y sale en `asomar`, nunca en `cola`. El `cola` de 15–47 ms coincide con
la resolución por defecto del temporizador (15,625 ms), y el arreglo
—`SetProcessInformation` con `ProcessPowerThrottling`— **está
identificado pero sin probar que funcione**: una muestra a favor y una en
contra no son prueba.

Falta también saber si la máquina del trabajo es portátil y va con
batería. Cambia el cuadro: el ahogo automático de Windows solo baja la
frecuencia con batería, y en un sobremesa sin núcleos eficientes no
aplica.

### El arranque de 2.810 ms en la máquina del trabajo

Ni tocado. En casa son 450–500 ms. Se mide comparando el arranque con y
sin una exclusión del antivirus sobre `%LOCALAPPDATA%\Programs\pastepad`,
y eso lo decide el usuario con su IT, no nosotros.

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
