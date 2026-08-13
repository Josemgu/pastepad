using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Pastepad.Nucleo;

/// <summary>
/// Todo el estado de la aplicacion en un solo sitio. Antes cada parte
/// de la interfaz leia y escribia los archivos por su cuenta; ahora
/// pasan por aqui, que es lo unico que toca el disco.
/// </summary>
public sealed class Almacen
{
    /// <summary>Cada cuanto se vuelca el historial pendiente.</summary>
    public static readonly TimeSpan IntervaloVolcado = TimeSpan.FromSeconds(3);

    static readonly JsonSerializerOptions _formato = new()
    {
        WriteIndented = true,
        IndentSize = 1,
        // Equivale al ensure_ascii=False de Python: las tildes y los
        // acentos se escriben tal cual en vez de como \uXXXX.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Cualquier fallo que el almacen se traga para poder seguir. La
    /// aplicacion lo engancha a su registro: ningun catch mudo, que fue
    /// la razon de que el fallo del atajo pasara tres intentos sin
    /// diagnostico.
    /// </summary>
    public event Action<string, Exception>? Incidencia;

    /// <summary>
    /// Lo que merece una linea en el registro sin ser un fallo: sobre
    /// todo, que una lectura que habia fallado saliera bien al reintento.
    /// Ese dato es el que cierra el diagnostico de por que a veces no se
    /// podia leer el historial.
    /// </summary>
    public event Action<string>? Aviso;

    /// <summary>
    /// Cuantas veces se reintenta una lectura que da <see
    /// cref="IOException"/>, y cuanto se espera entre una y otra.
    ///
    /// La hipotesis del fallo abierto es una violacion de uso compartido
    /// con la instancia anterior todavia muriendose: el archivo se libera
    /// en milisegundos. Si es eso, el segundo intento entra y el registro
    /// lo dice; si no lo es, tres intentos cuestan 200 ms una vez en el
    /// arranque y no se pierde nada.
    /// </summary>
    const int Intentos = 3;

    static readonly TimeSpan EsperaEntreIntentos = TimeSpan.FromMilliseconds(100);

    public Rutas Rutas { get; }

    public List<string> Carpetas { get; private set; } = [];
    public List<Snippet> Snippets { get; private set; } = [];
    public List<Entrada> Hist { get; set; } = [];

    JsonObject _prefs = [];

    bool _histSucio;
    long _ultimoVolcado;

    /// <summary>
    /// Archivos que existen pero no se pudieron leer. Sobre estos NO se
    /// escribe: hacerlo cambiaria "no pude leer tus datos" por "acabo de
    /// borrarlos".
    ///
    /// Paso de verdad: una instancia arranco sin poder acceder a su
    /// carpeta, mostro un historial vacio, y al cerrarse guardo ese
    /// vacio encima de las entradas reales. En silencio absoluto,
    /// porque el registro de errores vivia en la misma carpeta que
    /// tampoco funcionaba.
    /// </summary>
    readonly HashSet<string> _ilegibles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// True si algun archivo existia y no se pudo leer. Cuando pasa, lo
    /// cargado NO es el estado real del usuario.
    /// </summary>
    public bool LecturaIncompleta => _ilegibles.Count > 0;

    /// <summary>
    /// Que contarle al usuario, si hay algo que contarle. La interfaz
    /// tiene que enseñarlo: un fallo de datos que no se ve es peor que
    /// uno que se ve.
    /// </summary>
    public string? Problema => LecturaIncompleta
        ? "No pude leer tus datos guardados ("
          + string.Join(", ", _ilegibles.Select(Path.GetFileName))
          + "). Para no perderlos, no voy a escribir sobre ellos: lo de "
          + "esta sesion no se guardara. Cierra el programa y vuelve a "
          + "abrirlo."
        : null;

    /// <summary>
    /// Los dos avisadores se reciben aqui y no se enganchan despues a
    /// proposito: todas las lecturas pasan en este constructor. Suscrito
    /// desde fuera, el registro se perdia entero justo el dia que hacia
    /// falta —el arranque en el que no se pudo leer el historial es el
    /// unico que tiene algo que contar— y quedaba un Problema en pantalla
    /// sin una sola linea en errores.log que lo explicara.
    /// </summary>
    public Almacen(
        Rutas? rutas = null,
        Action<string, Exception>? incidencia = null,
        Action<string>? aviso = null)
    {
        if (incidencia is not null) Incidencia += incidencia;
        if (aviso is not null) Aviso += aviso;

        Rutas = rutas ?? Rutas.Predeterminadas();

        _prefs = Leer<JsonObject>(Rutas.Preferencias) ?? [];
        Hist = Leer<List<Entrada>>(Rutas.Historial) ?? [];

        var coleccion = Leer<Coleccion>(Rutas.Datos) ?? new Coleccion();
        Carpetas = coleccion.Categorias;
        Snippets = coleccion.Snippets;

        // Los snippets de versiones viejas traen "texto" y no "runs".
        foreach (var s in Snippets)
        {
            if (s.Runs.Count == 0)
                s.Runs = [Modelo.CrearFragmento(s.Texto ?? "")];
        }
    }

    // ------------------------------------------------------- archivos

    /// <summary>
    /// Que el archivo no exista es normal: primer arranque. Que exista y
    /// no se pueda leer es otra cosa muy distinta, y hay que
    /// distinguirlas — de eso depende que se escriba encima o no.
    ///
    /// Se abre una sola vez en lugar de preguntar con File.Exists y leer
    /// despues. La referencia de .NET lo dice literal: File.Exists
    /// devuelve false si no hay permiso para leer, y no lanza. Con la
    /// pregunta delante, un archivo al que se nos deniega el acceso
    /// pasaba por "primer arranque": el programa cargaba un historial
    /// vacio, no lo marcaba como ilegible, y al cerrarse lo escribia
    /// encima del bueno. Abriendolo, "no existe" y "no puedo" llegan como
    /// dos excepciones distintas y no hay forma de confundirlas.
    /// </summary>
    T? Leer<T>(string ruta) where T : class
    {
        for (int intento = 1; ; intento++)
        {
            try
            {
                // FileShare.ReadWrite: solo se lee, y bloquear a quien
                // esta escribiendo no ayudaria a nadie.
                using var flujo = new FileStream(
                    ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                var dato = JsonSerializer.Deserialize<T>(flujo);

                if (intento > 1)
                {
                    Aviso?.Invoke($"{ruta} se leyo bien al intento {intento} "
                                + $"de {Intentos}");
                }

                return dato;
            }
            catch (Exception e) when (e is FileNotFoundException
                                        or DirectoryNotFoundException)
            {
                // El unico caso benigno, y el unico que no se marca.
                return null;
            }
            catch (IOException e) when (intento < Intentos)
            {
                Incidencia?.Invoke(
                    $"no se pudo leer {ruta} (intento {intento} de {Intentos}); "
                    + $"se reintenta en {EsperaEntreIntentos.TotalMilliseconds:F0} ms",
                    e);

                Thread.Sleep(EsperaEntreIntentos);
            }
            catch (Exception e)
            {
                // Cualquier otra cosa es ilegible, sin lista de tipos que
                // acertar: el programa arranca igual, pero a partir de
                // aqui este archivo no se toca. Equivocarse por este lado
                // cuesta una sesion; por el otro, el historial entero.
                Incidencia?.Invoke($"no se pudo leer {ruta}; no se escribira "
                                 + "sobre el en toda la sesion", e);
                _ilegibles.Add(ruta);
                return null;
            }
        }
    }

    /// <summary>
    /// Escribe primero en un archivo aparte y luego lo mueve. Si el
    /// programa se cierra a mitad de la escritura, el archivo bueno
    /// sigue intacto en vez de quedar cortado.
    /// </summary>
    bool Escribir<T>(string ruta, T datos)
    {
        // Lo que no se pudo leer no se sobrescribe. Preferimos perder lo
        // de esta sesion a perder lo de todas las anteriores.
        if (_ilegibles.Contains(ruta))
        {
            Incidencia?.Invoke(
                $"no se escribe {ruta}: no se pudo leer al arrancar",
                new InvalidOperationException("archivo marcado como ilegible"));

            return false;
        }

        string temporal = ruta + ".tmp";

        try
        {
            string carpeta = Path.GetDirectoryName(ruta) ?? "";
            if (carpeta.Length > 0) Directory.CreateDirectory(carpeta);

            File.WriteAllText(temporal,
                JsonSerializer.Serialize(datos, _formato));

            File.Move(temporal, ruta, overwrite: true);
            return true;
        }
        catch (Exception e) when (e is IOException
                                    or UnauthorizedAccessException
                                    or NotSupportedException)
        {
            Incidencia?.Invoke($"no se pudo escribir {ruta}", e);

            try
            {
                if (File.Exists(temporal)) File.Delete(temporal);
            }
            catch (IOException borrado)
            {
                Incidencia?.Invoke($"no se pudo limpiar {temporal}", borrado);
            }

            return false;
        }
    }

    // --------------------------------------------------- preferencias

    public T? Pref<T>(string clave, T? defecto = default)
    {
        if (!_prefs.TryGetPropertyValue(clave, out var valor) || valor is null)
            return defecto;

        try
        {
            return valor.GetValue<T>();
        }
        catch (Exception e) when (e is InvalidOperationException
                                    or FormatException)
        {
            Incidencia?.Invoke($"preferencia '{clave}' con tipo raro", e);
            return defecto;
        }
    }

    public void PonerPref<T>(string clave, T valor)
    {
        _prefs[clave] = JsonValue.Create(valor);
        Escribir(Rutas.Preferencias, _prefs);
    }

    // -------------------------------------------------------- guardar

    public void GuardarDatos() => Escribir(Rutas.Datos, new Coleccion
    {
        Categorias = Carpetas,
        Snippets = Snippets,
    });

    /// <summary>
    /// Marca el historial como pendiente; el disco puede esperar. Antes
    /// cada copia reescribia el JSON entero: con 80 entradas de 20 KB
    /// son 7,8 ms y 1,2 MB en cada Ctrl+C.
    ///
    /// Con <paramref name="ya"/> se escribe en el acto: lo que el
    /// usuario hizo a proposito (fijar, borrar, vaciar) tiene que
    /// sobrevivir aunque el programa muera un segundo despues. Lo que se
    /// difiere es solo la captura automatica del portapapeles.
    /// </summary>
    public void GuardarHist(bool ya = false)
    {
        _histSucio = true;
        if (ya) Volcar(true);
    }

    /// <summary>
    /// Escribe el historial pendiente. True si toco el disco.
    /// </summary>
    public bool Volcar(bool forzar = false)
    {
        if (!_histSucio) return false;

        long ahora = Stopwatch.GetTimestamp();

        if (!forzar &&
            Stopwatch.GetElapsedTime(_ultimoVolcado, ahora) < IntervaloVolcado)
            return false;

        Escribir(Rutas.Historial, Hist);
        _histSucio = false;
        _ultimoVolcado = ahora;
        return true;
    }

    // ------------------------------------------------------- carpetas

    public bool CrearCarpeta(string nombre)
    {
        if (string.IsNullOrEmpty(nombre) || Carpetas.Contains(nombre))
            return false;

        Carpetas.Add(nombre);
        GuardarDatos();
        return true;
    }

    public bool RenombrarCarpeta(string viejo, string nuevo)
    {
        if (string.IsNullOrEmpty(nuevo) ||
            Carpetas.Contains(nuevo) ||
            !Carpetas.Contains(viejo))
            return false;

        Carpetas[Carpetas.IndexOf(viejo)] = nuevo;

        foreach (var s in Snippets)
        {
            if (s.Categoria == viejo) s.Categoria = nuevo;
        }

        GuardarDatos();
        return true;
    }

    public List<Snippet> ContenidoDe(string carpeta) =>
        Snippets.Where(s => s.Categoria == carpeta).ToList();

    /// <summary>Se lleva la carpeta y todo lo que tenga dentro.</summary>
    public int BorrarCarpeta(string carpeta)
    {
        var dentro = ContenidoDe(carpeta);

        foreach (var s in dentro) Snippets.Remove(s);

        Carpetas.Remove(carpeta);
        GuardarDatos();
        return dentro.Count;
    }

    // ------------------------------------------------------- snippets

    public void AnadirSnippet(Snippet snippet)
    {
        CrearCarpeta(snippet.Categoria);
        Snippets.Add(snippet);
        GuardarDatos();
    }

    public bool ReemplazarSnippet(Snippet viejo, Snippet nuevo)
    {
        int i = Snippets.IndexOf(viejo);
        if (i < 0) return false;

        Snippets[i] = nuevo;
        CrearCarpeta(nuevo.Categoria);
        GuardarDatos();
        return true;
    }

    // ------------------------------------------------------ historial

    /// <summary>
    /// Mete algo copiado al principio, debajo de los fijados.
    /// </summary>
    public bool Anotar(Entrada entrada)
    {
        // Solo mira las cuatro primeras: repetir algo copiado hace
        // veinte entradas es legitimo, repetir lo de hace un momento es
        // casi siempre el mismo Ctrl+C contado dos veces.
        foreach (var x in Hist.Take(4))
        {
            if (entrada.Tipo == Entrada.Texto_ && x.Texto == entrada.Texto)
                return false;
        }

        int fijados = Hist.Count(x => x.Pin);
        Hist.Insert(fijados, entrada);

        Recortar();
        GuardarHist();
        return true;
    }

    /// <summary>
    /// Deja solo las ultimas MaxHist sueltas. Las fijadas no cuentan.
    /// </summary>
    void Recortar()
    {
        var libres = Hist.Where(x => !x.Pin).ToList();

        foreach (var viejo in libres.Skip(Config.MaxHist))
        {
            BorrarImagen(viejo);
            Hist.Remove(viejo);
        }
    }

    void BorrarImagen(Entrada entrada)
    {
        if (!entrada.EsImagen || string.IsNullOrEmpty(entrada.Ruta)) return;

        try
        {
            File.Delete(entrada.Ruta);
        }
        catch (Exception e) when (e is IOException
                                    or UnauthorizedAccessException)
        {
            // Que quede un bmp huerfano no justifica perder la entrada.
            Incidencia?.Invoke($"no se pudo borrar {entrada.Ruta}", e);
        }
    }

    public void Fijar(Entrada entrada)
    {
        entrada.Pin = !entrada.Pin;
        GuardarHist(true);
    }

    public bool Borrar(Elemento elemento)
    {
        switch (elemento)
        {
            case Entrada entrada:
                BorrarImagen(entrada);
                if (!Hist.Remove(entrada)) return false;
                GuardarHist(true);
                return true;

            case Snippet snippet:
                if (!Snippets.Remove(snippet)) return false;
                GuardarDatos();
                return true;

            default:
                return false;
        }
    }

    public int BorrarVarios(IEnumerable<Elemento> elementos) =>
        elementos.Count(Borrar);

    /// <summary>Los fijados sobreviven: para eso estan.</summary>
    public void VaciarHistorial()
    {
        foreach (var x in Hist)
        {
            if (!x.Pin) BorrarImagen(x);
        }

        Hist = Hist.Where(x => x.Pin).ToList();
        GuardarHist(true);
    }

    public List<Entrada> HistOrdenado() =>
        [.. Hist.Where(x => x.Pin), .. Hist.Where(x => !x.Pin)];

    /// <summary>
    /// Guarda un mapa de bits y lo anota en el historial. Recibe los
    /// bytes ya convertidos: convertir imagenes es cosa de la capa
    /// grafica, y este ensamblado no la conoce.
    /// </summary>
    public bool GuardarImagen(byte[] bmp)
    {
        try
        {
            Directory.CreateDirectory(Rutas.Imagenes);

            string ruta = Path.Combine(
                Rutas.Imagenes,
                $"img_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.bmp");

            File.WriteAllBytes(ruta, bmp);

            return Anotar(new Entrada { Tipo = Entrada.Imagen, Ruta = ruta });
        }
        catch (Exception e) when (e is IOException
                                    or UnauthorizedAccessException)
        {
            Incidencia?.Invoke("no se pudo guardar la imagen", e);
            return false;
        }
    }
}
