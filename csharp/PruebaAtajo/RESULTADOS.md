# Paso 1 — resultados

Banco de pruebas del atajo global y el portapapeles. Deliberadamente
feo: si llevara interfaz, un fallo del atajo volvería a tener veinte
sospechosos.

Medido el 12 ago 2026. Windows 11 build 26200, .NET 10.0.400, Windows
App SDK 2.3.1, compilación **Debug** desempaquetada y self-contained.

---

## Lo que se quería responder

WinUI 3 no expone `WndProc`. La pregunta sin respuesta en la
documentación era si su bomba de mensajes despacha los mensajes de una
ventana **ajena al XAML** creada en el mismo hilo. De eso dependía todo
el diseño.

**Sí despacha.**

---

## Medidas

| Prueba | Resultado |
|---|---|
| 100 pulsaciones sintéticas de `Ctrl+Shift+V` | **100 de 100** recibidas |
| Hilo de entrega | Siempre el de interfaz (hilo 2), el mismo que creó el buzón |
| 25 pulsaciones con la ventana **oculta** | **25 de 25** |
| Segunda instancia lanzada | Se retira sola; queda 1 proceso |
| Excepciones registradas | Ninguna |

El montaje quedó anotado en el log sin un solo fallo:

```
buzon creado, hwnd 0x1300EE
Ctrl+Shift+V registrado
escucha del portapapeles activa
```

---

## Lo que cambia respecto a la versión en Python

La versión anterior llamaba a `RegisterHotKey` con `hWnd` nulo, que
registra un atajo **de hilo**. Su `WM_HOTKEY` llega con `hwnd` nulo y
`DispatchMessage` no lo entrega a ningún procedimiento de ventana: la
bomba de XAML lo habría tirado. Pasar un HWND real no es un detalle del
portado, es el cambio de fondo.

---

## Por qué la cuenta a ojo sale más alta que el log

Al probarlo a mano salieron 150 pulsaciones contadas frente a 106
registradas. No eran pérdidas: es `MOD_NOREPEAT` haciendo su trabajo.
Medido con tres patrones de teclado distintos:

| Patrón | Enviadas | Recibidas |
|---|---|---|
| Ciclo completo: pulsar y soltar las tres teclas | 20 | **20** |
| `Ctrl+Shift` mantenidos, repicando solo la `V` | 20 | **20** |
| `V` mantenida pulsada (31 autorrepeticiones) | 31 | **1** |

Mantener el atajo pulsado cuenta **una sola vez**, por muy larga que sea
la pulsación. Es justo lo que se quiere: sin `MOD_NOREPEAT`, dejar el
dedo puesto medio segundo abriría y cerraría el panel treinta veces.

La lección para medir: **no vale contar de cabeza**. Hay que usar el
botón «Poner a cero» y comparar contra el contador de pantalla.

---

## Hallazgo no previsto: el portapapeles avisa de más

`WM_CLIPBOARDUPDATE` **no** llega una vez por copia del usuario:

| Cómo se copió | Copias | Avisos |
|---|---|---|
| `Set-Clipboard` de PowerShell | 20 | **60** |
| `clip.exe` | 5 | 6 |
| `clip.exe`, segunda tanda | 10 | 10 |

Depende de cuántas sesiones de portapapeles abra el programa que copia,
no del oyente. No es un fallo, pero **obliga a filtrar**: sin ello, una
sola copia podría entrar tres veces en el historial.

El filtro ya está decidido y probado en la versión anterior —
`GetClipboardSequenceNumber`, que `TRASPASO.md` recoge—. Lo que cambia
es el motivo: allí evitaba el sondeo, aquí evita duplicados.

---

## Consumo: pendiente, y con una advertencia

| | |
|---|---|
| Working set | 178,3 MB |
| Memoria privada | 129,1 MB |

**Esta cifra no vale como conclusión** — es Debug, self-contained y sin
`PublishReadyToRun`. Pero tampoco la ignoro: `TRASPASO.md` habla de 40
a 60 MB, y de momento no se parece. Se mide en serio en el paso 5, en
Release, y si no baja se corrige el documento en vez de repetir la
cifra.

---

## Lo que falta para dar el paso 1 por bueno

Las pulsaciones sintéticas prueban el mecanismo, no la resistencia. La
versión anterior también pasó 45 de 45 en aislamiento y luego murió
dentro de la aplicación. Queda por hacer, y necesita tiempo real:

- [ ] 8 horas residente, comprobando que sigue contando
- [ ] Bloquear y desbloquear la sesión
- [ ] Suspender y reanudar el equipo
- [ ] Pulsaciones de verdad, con teclado, desde varias aplicaciones

Para arrancarlo:

```
csharp\PruebaAtajo\bin\Debug\net10.0-windows10.0.26100.0\win-x64\PruebaAtajo.exe
```

El log se escribe junto al ejecutable, en `prueba-atajo.log`, con la
marca de tiempo y el hilo de cada mensaje.

**Mientras esté abierto se queda con `Ctrl+Shift+V` en todo el
sistema**, así que no conviene dejarlo puesto sin querer.
