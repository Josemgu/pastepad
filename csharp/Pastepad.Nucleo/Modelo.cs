namespace Pastepad.Nucleo;

/// <summary>
/// Las reglas de los datos, sin nada de interfaz. Todo lo de aqui se
/// puede probar sin abrir una ventana.
/// </summary>
public static class Modelo
{
    public static Fragmento CrearFragmento(
        string texto,
        string? fuente = null,
        int? tam = null,
        int negrita = 0,
        int cursiva = 0,
        int subrayado = 0,
        string? color = null) => new()
        {
            T = texto,
            F = fuente ?? Config.FuenteDef,
            S = tam ?? Config.TamDef,
            B = negrita,
            I = cursiva,
            U = subrayado,
            C = color ?? Config.ColorDef,
        };

    public static string TextoDe(IEnumerable<Fragmento> fragmentos) =>
        string.Concat(fragmentos.Select(f => f.T));

    /// <summary>
    /// Resumen de una linea. Corta antes de separar en palabras: con
    /// textos de miles de lineas, hacerlo al reves cuesta casi un
    /// segundo.
    /// </summary>
    public static string UnaLinea(string texto, int tope = 52)
    {
        if (string.IsNullOrEmpty(texto)) return "";

        int corte = tope * 4;
        string crudo = texto.Length > corte ? texto[..corte] : texto;

        string limpio = string.Join(" ", crudo.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (texto.Length > crudo.Length || limpio.Length > tope)
            return limpio[..Math.Min(tope, limpio.Length)] + "...";

        return limpio;
    }

    // ---------------------------------------------------------- enlaces

    /// <summary>
    /// True si el texto es una direccion web y nada mas. Solo si el
    /// texto entero es el enlace: un parrafo que menciona una url de
    /// pasada no cuenta, porque abrirlo no seria lo que el usuario
    /// espera al hacer clic.
    /// </summary>
    public static bool EsEnlace(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return false;

        string t = texto.Trim();

        if (t.Contains(' ') || t.Contains('\n') || t.Length > 2000)
            return false;

        string bajo = t.ToLowerInvariant();

        return bajo.StartsWith("http://", StringComparison.Ordinal)
            || bajo.StartsWith("https://", StringComparison.Ordinal)
            || bajo.StartsWith("www.", StringComparison.Ordinal);
    }

    /// <summary>La direccion lista para abrir en el navegador.</summary>
    public static string UrlDe(string texto)
    {
        string t = texto.Trim();

        return t.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? "https://" + t
            : t;
    }

    /// <summary>El dominio suelto, para mostrarlo debajo del titulo.</summary>
    public static string DominioDe(string texto)
    {
        string t = UrlDe(texto);

        foreach (var prefijo in new[] { "https://", "http://" })
        {
            if (t.StartsWith(prefijo, StringComparison.Ordinal))
            {
                t = t[prefijo.Length..];
                break;
            }
        }

        if (t.StartsWith("www.", StringComparison.Ordinal))
            t = t[4..];

        string dominio = t.Split('/')[0];

        return dominio.Length > 60 ? dominio[..60] : dominio;
    }

    // ------------------------------------------------------- plantillas

    /// <summary>
    /// Los [[campos]] de una plantilla, en orden y sin repetir.
    /// </summary>
    public static List<string> CamposDe(string texto)
    {
        var campos = new List<string>();
        string resto = texto;

        while (true)
        {
            int i = resto.IndexOf("[[", StringComparison.Ordinal);
            if (i < 0) break;

            int j = resto.IndexOf("]]", i, StringComparison.Ordinal);

            // La version en Python no comprobaba esto y reventaba con un
            // texto como "]] [[x", donde el cierre va antes que la
            // apertura. Aqui simplemente se deja de buscar.
            if (j < 0) break;

            string nombre = resto[(i + 2)..j].Trim();

            if (nombre.Length > 0 && !campos.Contains(nombre))
                campos.Add(nombre);

            resto = resto[(j + 2)..];
        }

        return campos;
    }

    public static List<Fragmento> Rellenar(
        IEnumerable<Fragmento> fragmentos,
        IReadOnlyDictionary<string, string> valores)
    {
        var salida = new List<Fragmento>();

        foreach (var f in fragmentos)
        {
            string t = f.T;

            foreach (var (clave, valor) in valores)
                t = t.Replace("[[" + clave + "]]", valor, StringComparison.Ordinal);

            salida.Add(new Fragmento
            {
                T = t,
                F = f.F,
                S = f.S,
                B = f.B,
                I = f.I,
                U = f.U,
                C = f.C,
            });
        }

        return salida;
    }
}
