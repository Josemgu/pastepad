using Microsoft.UI.Xaml;
using Pastepad.App.Sistema;
using Pastepad.Nucleo;

namespace Pastepad.App;

/// <summary>
/// Coordina todo: el almacen, el buzon de Windows, la bandeja y el
/// panel. Es el unico sitio donde esas cuatro cosas se conocen.
/// </summary>
public partial class App : Application
{
    internal static App Actual => (App)Current;

    internal Almacen Almacen { get; private set; } = null!;
    internal Indice Indice { get; private set; } = null!;

    Buzon? _buzon;
    Bandeja? _bandeja;
    Cierre? _cierre;
    Panel? _panel;
    Timer? _volcado;
    Timer? _novedades;

    /// <summary>
    /// Cuanto se espera antes de la primera comprobacion de version, y
    /// cada cuanto despues.
    ///
    /// El retraso no es por prudencia: el arranque esta medido en ~420 ms
    /// y no se toca por una consulta a internet que nadie ha pedido y a
    /// nadie le corre prisa.
    /// </summary>
    static readonly TimeSpan PrimeraComprobacion = TimeSpan.FromSeconds(45);

    static readonly TimeSpan CadaDia = TimeSpan.FromHours(24);

    /// <summary>
    /// La ventana que tenia el foco cuando se pulso el atajo. Se guarda
    /// en ese instante y no despues: en cuanto el panel aparece, esta
    /// informacion ya se perdio.
    /// </summary>
    nint _ventanaPrevia;

    /// <summary>
    /// La ultima secuencia del portapapeles que escribimos nosotros. Sin
    /// esto, pegar volveria a meter en el historial lo que se acaba de
    /// pegar.
    /// </summary>
    uint _secuenciaPropia;

    uint _ultimaSecuencia;

    public App()
    {
        InitializeComponent();

        UnhandledException += (_, a) =>
        {
            Registro.Fallo("Application.UnhandledException", a.Exception);

            // No se marca como manejada: un fallo en el arbol de XAML que
            // no sabemos tratar no debe dejarse pasar en silencio.
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Almacen = new Almacen(
            Program.CarpetaDatos is { } carpeta ? Rutas.EnCarpeta(carpeta) : null,
            Registro.Fallo,
            Registro.Anotar);
        Indice = new Indice(Almacen);

        // Antes de crear el panel: sus textos se resuelven al construirlo.
        Textos.Idioma = Almacen.Pref("idioma", Textos.IdiomaDef)
                        ?? Textos.IdiomaDef;

        _panel = new Panel();

        // Si los datos no se pudieron leer, el usuario tiene que
        // enterarse ahora y no cuando descubra que falta medio
        // historial. Se abre el panel a proposito: un aviso que nadie
        // mira no es un aviso.
        if (Almacen.Problema is not null)
        {
            Registro.Anotar("ARRANQUE DEGRADADO: " + Almacen.Problema);
            _panel.Avisar(Almacen.Problema);
            _panel.Asomar();
        }

        // El buzon se crea en el hilo de interfaz, que es donde
        // RegisterHotKey acepta el HWND y donde la bomba de WinUI
        // despacha sus mensajes.
        _buzon = new Buzon();
        _buzon.Atajo += AlAtajo;
        _buzon.Portapapeles += AlPortapapeles;
        _buzon.Mostrarse += () => MostrarPanel();
        _buzon.Salir += Cerrar;

        string atajo = Almacen.Pref("atajo", Config.AtajoDef) ?? Config.AtajoDef;

        if (!_buzon.PonerAtajo(atajo) && _buzon.PonerAtajo(Config.AtajoDef))
            Registro.Anotar($"'{atajo}' no se pudo poner; se uso el de fabrica");

        _bandeja = new Bandeja(_buzon.Handle, "pastepad");

        // La escucha del cierre va aparte del buzon porque el buzon no
        // puede oirla: ver el comentario de Cierre.cs. Y el registro para
        // que nos reabran va aqui, no antes, para que la linea del log
        // salga despues de la del arranque.
        _cierre = new Cierre();
        _cierre.Volcar += () => Almacen.Volcar(forzar: true);
        _cierre.Terminar += Cerrar;

        Cierre.PedirQueNosReabran();

        AplicarAutoarranque();

        _ultimaSecuencia = Portapapeles.Secuencia();

        // El historial se vuelca cada pocos segundos en vez de en cada
        // copia: escribir el JSON entero costaba 7,8 ms y mas de un mega
        // por cada Ctrl+C.
        _volcado = new Timer(
            _ => _panel?.DispatcherQueue.TryEnqueue(() => Almacen.Volcar()),
            null, Almacen.IntervaloVolcado, Almacen.IntervaloVolcado);

        if (_buzon.Problema is not null)
            _panel.Avisar(_buzon.Problema);

        // Todo lo que toca el almacen va por el hilo de interfaz, que es
        // desde donde se toca en todos los demas sitios. Lo unico que
        // sale de ahi es la peticion, que se espera con await.
        _novedades = new Timer(
            _ => _panel?.DispatcherQueue.TryEnqueue(async () =>
                await ComprobarNovedades()),
            null, PrimeraComprobacion, CadaDia);

        // Cuanto tarda en estar listo, desde la primera linea de Main.
        // En caliente sale una cosa y en frio otra muy distinta: el
        // arranque en frio de verdad son 476 archivos que nadie ha
        // tocado todavia, y solo se mide reiniciando la maquina y
        // leyendo esta linea.
        Registro.Anotar(string.Format(
            "listo en {0:F0} ms",
            System.Diagnostics.Stopwatch
                .GetElapsedTime(Program.Arranque).TotalMilliseconds));
    }

    /// <summary>
    /// Arrancar con Windows, segun la preferencia del usuario. La decide
    /// la aplicacion en cada arranque y nadie mas: el instalador ya no
    /// escribe en HKCU\...\Run.
    ///
    /// Se reescribe siempre, exista ya o no. Esa es la forma de corregir
    /// una ruta vieja, y con instalador de por medio pasa de verdad:
    /// quien tuviera la version anterior en %LOCALAPPDATA%\pastepad y
    /// ponga la nueva en \Programs\pastepad se queda con la entrada
    /// apuntando a un ejecutable que ya no esta. La version anterior
    /// tenia el mismo problema y se resolvia a mano — "si mueves la
    /// carpeta, abrelo una vez desde el sitio nuevo".
    /// </summary>
    void AplicarAutoarranque()
    {
        // --datos existe para probar sin tocar lo del usuario, y hasta la
        // 4.2.0 no lo cumplia: aislaba los datos pero reescribia igual la
        // entrada del registro, dejando el autoarranque apuntando a una
        // compilacion de pruebas que manana ya no esta. Paso dos veces en
        // una misma sesion.
        //
        // El registro es de la instalacion de verdad. Una sesion con
        // --datos no tiene nada que decir ahi.
        if (Program.CarpetaDatos is not null)
        {
            Registro.Anotar("sesion con --datos: no se toca el autoarranque");
            return;
        }

        string preferencia =
            Almacen.Pref(Autoarranque.Clave, Autoarranque.PorDefecto)
            ?? Autoarranque.PorDefecto;

        bool quiere = Autoarranque.Quiere(preferencia);

        // La tarea programada, en un hilo de fondo y sin esperarla: lanza
        // schtasks, que cuesta mas que todo el arranque junto. Nada de lo
        // que viene detras depende de ella.
        _ = Task.Run(() => Arranque.AsegurarTarea(quiere));

        // El valor que habia, para poder decir de que a que. Se lee y se
        // vuelve a leer despues de escribir en vez de componer la ruta a
        // mano: asi los dos lados se comparan en el mismo formato, con
        // las comillas que Windows guarda.
        string? antes = Arranque.ValorActual();

        if (quiere)
        {
            Arranque.Poner(true);

            string ahora = Arranque.ValorActual()
                           ?? Environment.ProcessPath
                           ?? "(ruta desconocida)";

            // Reescribir la entrada de otro programa no puede pasar sin
            // dejar rastro: quien lo mire despues tiene derecho a saber
            // que habia antes y cuando cambio.
            Registro.Anotar(
                antes is null
                    ? $"arranque con Windows: registrado en {ahora}"
                    : antes == ahora
                        ? $"arranque con Windows: ya estaba puesto en {ahora}"
                        : $"arranque con Windows: se ha reescrito de {antes} a {ahora}");

            return;
        }

        if (antes is not null)
        {
            Arranque.Poner(false);

            // Con el valor que tenia: si el usuario cambia de idea, ahi
            // esta lo que habia que devolver.
            Registro.Anotar(
                $"arranque con Windows: quitado ({Autoarranque.Clave}"
                + $"='{preferencia}'); apuntaba a {antes}");

            return;
        }

        Registro.Anotar(
            $"arranque con Windows: no se registra ({Autoarranque.Clave}"
            + $"='{preferencia}')");
    }

    // ------------------------------------------- version nueva

    /// <summary>
    /// Mira si hay version nueva y, si la hay y no se ha avisado ya de
    /// esa, la enseña en la banda del panel.
    ///
    /// Nunca lanza: es lo que se le pide a algo que corre solo, de
    /// fondo, y que el usuario no ha pedido. Un fallo aqui no puede
    /// tumbar la aplicacion ni salir en pantalla; se anota y se
    /// reintenta mañana.
    /// </summary>
    async Task ComprobarNovedades()
    {
        try
        {
            if (!Almacen.Pref(Versiones.ClaveAvisar, Versiones.AvisarDef))
                return;

            var ahora = DateTimeOffset.Now;
            string? ultima = Almacen.Pref<string>(Versiones.ClaveComprobacion);

            if (!Versiones.TocaComprobar(ultima, ahora)) return;

            var publicada = await Actualizacion.Consultar();

            // La fecha se guarda solo si la consulta salio bien. Si
            // fallo, que se vuelva a intentar y no se pierda un dia.
            if (publicada is null) return;

            Almacen.PonerPref(Versiones.ClaveComprobacion, Versiones.Hoy(ahora));

            string? avisada = Almacen.Pref<string>(Versiones.ClaveAvisada);

            if (!Versiones.TocaAvisar(Config.Version, publicada.Version, avisada))
            {
                // Tambien se anota lo que sale bien: el dia que el aviso
                // deje de funcionar, el fallo es que nadie se entera de
                // que nadie se entera.
                Registro.Anotar(
                    $"actualizaciones: instalada {Config.Version}, publicada "
                    + $"{publicada.Version}; nada que avisar");

                return;
            }

            Almacen.PonerPref(Versiones.ClaveAvisada, publicada.Version);

            Registro.Anotar(
                $"actualizaciones: hay {publicada.Version} y se tiene "
                + $"{Config.Version}; se avisa");

            _panel?.AvisarNovedad(publicada.Version, publicada.Pagina);
        }
        catch (Exception e)
        {
            Registro.Fallo("comprobar si hay version nueva", e);
        }
    }

    // ------------------------------------------------------- el atajo

    void AlAtajo()
    {
        // Lo primero de todo, antes de tocar ninguna ventana.
        _ventanaPrevia = Foco.VentanaActiva();

        if (_panel is null) return;

        if (_panel.EstaVisible)
        {
            _panel.Esconder();
            return;
        }

        // El camino entero, en tres trozos, porque hasta ahora solo se
        // medía el de en medio —«panel en 25 ms»— y el usuario decía que
        // tardaba. Los dos que faltaban son justo donde puede estar:
        //
        //   cola     lo que la pulsacion espero a que la recogieramos.
        //            Si el proceso lleva diez minutos parado y Windows lo
        //            ha echado de la memoria o lo ha frenado por estar en
        //            segundo plano, el retraso aparece aqui y en ningun
        //            otro sitio.
        //   asomar   lo de siempre: pedir la ventana y darle el foco.
        //   dibujo   desde que Asomar vuelve hasta que hay pixeles. Show()
        //            no espera al primer fotograma.
        //
        // Y al lado, cuanto llevaba el programa sin hacer nada. Sin ese
        // dato las cifras no se pueden agrupar en «recien usado» y «diez
        // minutos parado», que es la comparacion que hace falta.
        int cola = _buzon?.EsperaDelAtajo ?? 0;
        var parado = System.Diagnostics.Stopwatch.GetElapsedTime(_ultimaActividad);

        long marca = System.Diagnostics.Stopwatch.GetTimestamp();

        MostrarPanel();

        double asomar = System.Diagnostics.Stopwatch
            .GetElapsedTime(marca).TotalMilliseconds;

        _panel.AlPrimerFotograma(() =>
        {
            Registro.Anotar(string.Format(
                "panel: cola {0} ms + asomar {1:F1} ms + dibujo {2:F1} ms "
                + "= {3:F1} ms (llevaba {4:F1} min parado)",
                cola,
                asomar,
                System.Diagnostics.Stopwatch.GetElapsedTime(marca).TotalMilliseconds
                    - asomar,
                cola + System.Diagnostics.Stopwatch
                    .GetElapsedTime(marca).TotalMilliseconds,
                parado.TotalMinutes));

            // Solo en la primera apertura del proceso, que es la unica
            // en la que se ha visto la cabecera sin pintar.
            if (_panel?.Diagnostico() is { } detalle) Registro.Anotar(detalle);
        });

        Actividad();
    }

    /// <summary>
    /// Cuando el programa hizo algo por ultima vez. Lo mueven el atajo y
    /// cada copia: son las dos cosas que lo despiertan.
    /// </summary>
    long _ultimaActividad = System.Diagnostics.Stopwatch.GetTimestamp();

    void Actividad() =>
        _ultimaActividad = System.Diagnostics.Stopwatch.GetTimestamp();

    void MostrarPanel()
    {
        _panel?.Asomar();
    }

    // -------------------------------------------------- el portapapeles

    void AlPortapapeles()
    {
        uint secuencia = Portapapeles.Secuencia();

        // WM_CLIPBOARDUPDATE no llega una vez por copia sino una por
        // cada sesion que abre quien copia: PowerShell dispara tres.
        // El contador de Windows es lo que distingue una copia de
        // verdad de un aviso repetido.
        if (secuencia == _ultimaSecuencia) return;
        _ultimaSecuencia = secuencia;

        // Una copia tambien despierta al programa: si el usuario copio
        // hace diez segundos, no llevaba diez minutos parado por mucho
        // que no haya tocado el atajo.
        Actividad();

        // Lo que acabamos de poner nosotros para pegarlo no se reanota.
        if (secuencia == _secuenciaPropia) return;

        if (Almacen.Pref("pausado", false)) return;

        var contenido = Portapapeles.Leer();

        switch (contenido.Tipo)
        {
            case TipoContenido.Privado:
                Registro.Anotar("copia marcada como privada: no se guarda");
                break;

            case TipoContenido.Texto when contenido.Texto is not null:
                if (Almacen.Anotar(new Entrada
                {
                    Tipo = Entrada.Texto_,
                    Texto = contenido.Texto,
                }))
                {
                    RefrescarLista();
                }
                break;

            case TipoContenido.Imagen when contenido.Imagen is not null:
                if (Almacen.GuardarImagen(Portapapeles.DibABmp(contenido.Imagen)))
                    RefrescarLista();
                break;
        }
    }

    /// <summary>
    /// El indice cachea el texto normalizado de cada entrada, asi que
    /// hay que avisarle cuando el almacen cambia.
    /// </summary>
    internal void RefrescarLista()
    {
        Indice.Invalidar();
        _panel?.Refrescar();
    }

    // ------------------------------------------------------- el pegado

    /// <summary>
    /// Copia la entrada, devuelve el foco a donde estaba y pega alli.
    /// </summary>
    internal async void Pegar(Entrada entrada)
    {
        try
        {
            bool copiado = entrada.EsImagen
                ? entrada.Ruta is not null && Portapapeles.CopiarImagen(entrada.Ruta)
                : Portapapeles.Copiar(
                    [Modelo.CrearFragmento(entrada.Texto ?? "")], sinFormato: true);

            await TrasCopiar(copiado);
        }
        catch (Exception e)
        {
            Registro.Fallo("Pegar", e);
        }
    }

    /// <summary>
    /// Pega un texto con formato: es lo que distingue un guardado de una
    /// entrada del historial, que siempre va plana.
    /// </summary>
    internal async void PegarFragmentos(
        IReadOnlyList<Fragmento> fragmentos, bool sinFormato)
    {
        try
        {
            await TrasCopiar(Portapapeles.Copiar(fragmentos, sinFormato));
        }
        catch (Exception e)
        {
            Registro.Fallo("PegarFragmentos", e);
        }
    }

    /// <summary>
    /// Devuelve el foco a donde estaba y pega alli. Es la parte
    /// delicada del ciclo y por eso vive en un solo sitio: guardar el
    /// hwnd anterior, engancharle el hilo y soltar Ctrl+V.
    /// </summary>
    async Task TrasCopiar(bool copiado)
    {
        if (!copiado)
        {
            _panel?.Avisar(Textos.T("No se pudo copiar al portapapeles."));
            return;
        }

        _secuenciaPropia = Portapapeles.Secuencia();
        _ultimaSecuencia = _secuenciaPropia;

        _panel?.Esconder();

        if (!Foco.Devolver(_ventanaPrevia))
        {
            // Queda copiado: el usuario puede pegar a mano. Es peor
            // no decirselo que decirlo.
            _panel?.Avisar(Textos.T("Copiado, pero no pude volver a la ventana "
                                  + "anterior. Pega con Ctrl+V."));
            return;
        }

        // Windows necesita un instante para asentar el primer plano
        // antes de aceptar las teclas.
        await Task.Delay(60);

        Foco.PegarConTeclado();
    }

    /// <summary>Deja el contenido en el portapapeles sin pegarlo.</summary>
    internal void CopiarSolo(Elemento elemento)
    {
        try
        {
            bool copiado = elemento switch
            {
                Entrada { EsImagen: true, Ruta: { } ruta } =>
                    Portapapeles.CopiarImagen(ruta),

                Entrada entrada => Portapapeles.Copiar(
                    [Modelo.CrearFragmento(entrada.Texto ?? "")], sinFormato: true),

                Snippet snippet => Portapapeles.Copiar(snippet.Runs),

                _ => false,
            };

            if (!copiado)
            {
                _panel?.Avisar(Textos.T("No se pudo copiar al portapapeles."));
                return;
            }

            _secuenciaPropia = Portapapeles.Secuencia();
            _ultimaSecuencia = _secuenciaPropia;

            _panel?.Esconder();
        }
        catch (Exception e)
        {
            Registro.Fallo("CopiarSolo", e);
        }
    }

    /// <summary>
    /// Abre el enlace en el navegador. Se ofrece solo cuando el texto
    /// entero es la direccion: un parrafo que la menciona de pasada no
    /// cuenta.
    ///
    /// Es una accion mas del menu de la fila, no lo que hace el clic.
    /// Cuando lo era, un enlace no se podia pegar en ningun sitio.
    /// </summary>
    internal void AbrirEnlace(string texto)
    {
        _panel?.Esconder();

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = Modelo.UrlDe(texto),
                UseShellExecute = true,
            });
        }
        catch (Exception e)
        {
            Registro.Fallo("AbrirEnlace", e);
            _panel?.Avisar(Textos.T("No se pudo abrir el enlace."));
        }
    }

    /// <summary>
    /// Cambia el atajo global en caliente. False si Windows no lo da
    /// —normalmente porque otro programa se lo quedo—, y entonces la
    /// interfaz tiene que decirlo en vez de callarse.
    /// </summary>
    internal bool PonerAtajo(string atajo) => _buzon?.PonerAtajo(atajo) ?? false;

    // -------------------------------------------------------- el cierre

    bool _cerrando;

    void Cerrar()
    {
        // Ya no lo pide solo la bandeja: tambien el Restart Manager, y
        // ese puede mandar WM_ENDSESSION y despues WM_CLOSE. Entrar dos
        // veces desecharia dos veces y volcaria sobre un almacen a medio
        // soltar.
        if (_cerrando) return;
        _cerrando = true;

        Registro.Anotar("cerrando");

        Almacen.Volcar(forzar: true);

        _volcado?.Dispose();
        _novedades?.Dispose();
        _bandeja?.Dispose();
        _cierre?.Dispose();
        _buzon?.Dispose();

        Exit();
    }
}
