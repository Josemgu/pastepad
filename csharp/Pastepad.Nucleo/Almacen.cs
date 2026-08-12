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

    public Almacen(Rutas? rutas = null)
    {
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

    T? Leer<T>(string ruta) where T : class
    {
        // Que el archivo no exista es normal: primer arranque. Que
        // exista y no se pueda leer es otra cosa muy distinta, y hay que
        // distinguirlas.
        bool existe;

        try
        {
            existe = File.Exists(ruta);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Ni siquiera se pudo mirar. Se trata como ilegible.
            Incidencia?.Invoke($"no se pudo comprobar {ruta}", e);
            _ilegibles.Add(ruta);
            return null;
        }

        if (!existe) return null;

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(ruta));
        }
        catch (Exception e) when (e is IOException
                                    or JsonException
                                    or UnauthorizedAccessException)
        {
            // El programa arranca igual, pero marcado: a partir de aqui
            // este archivo no se toca.
            Incidencia?.Invoke($"no se pudo leer {ruta}; no se escribira "
                             + "sobre el en toda la sesion", e);
            _ilegibles.Add(ruta);
            return null;
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
