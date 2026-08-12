using System.Text;

namespace Pastepad.Nucleo;

public enum TipoResultado
{
    /// <summary>Un snippet guardado a proposito.</summary>
    Guardado,

    /// <summary>Una entrada del historial.</summary>
    Historial,
}

public readonly record struct Resultado(Elemento Dato, TipoResultado Tipo);

/// <summary>Busqueda con puntuacion. Independiente de la interfaz.</summary>
public static class Busqueda
{
    public const int TopeTexto = 4000;

    /// <summary>
    /// Minusculas y sin tildes, para que "informacion" encuentre
    /// "información" y al reves.
    /// </summary>
    public static string Normalizar(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return "";

        string corte = texto.Length > TopeTexto ? texto[..TopeTexto] : texto;

        var salida = new StringBuilder(corte.Length);

        foreach (char c in corte.ToLowerInvariant())
        {
            salida.Append(c switch
            {
                'á' => 'a',
                'é' => 'e',
                'í' => 'i',
                'ó' => 'o',
                'ú' or 'ü' => 'u',
                'ñ' => 'n',
                _ => c,
            });
        }

        return salida.ToString();
    }

    static readonly char[] _cortesTitulo = [' ', '-', '_', '.', ':', ',', '/', '(', ')'];
    static readonly char[] _cortesCuerpo = [' ', '-', '_', '.', ':', ',', '/', '(', ')', '\n'];

    /// <summary>
    /// Cuanto se parece, o null si no coincide. Cada palabra tiene que
    /// estar en algun lado. Pesa mas si aparece en el titulo, si empieza
    /// una palabra y si esta cerca del principio.
    /// </summary>
    public static int? Puntuar(
        IReadOnlyList<string> palabras, string titulo, string cuerpo)
    {
        int total = 0;

        foreach (var palabra in palabras)
        {
            int posT = titulo.IndexOf(palabra, StringComparison.Ordinal);
            int posC = cuerpo.IndexOf(palabra, StringComparison.Ordinal);

            if (posT < 0 && posC < 0) return null;

            if (posT >= 0)
            {
                total += 100;

                if (posT == 0 || _cortesTitulo.Contains(titulo[posT - 1]))
                    total += 60;

                total += Math.Max(0, 25 - posT / 2);
            }
            else
            {
                total += 30;

                if (posC == 0 || _cortesCuerpo.Contains(cuerpo[posC - 1]))
                    total += 20;

                total += Math.Max(0, 15 - posC / 40);
            }
        }

        // La frase entera y en orden vale mas que las palabras sueltas.
        if (palabras.Count > 1 &&
            (titulo + " " + cuerpo).Contains(
                string.Join(" ", palabras), StringComparison.Ordinal))
            total += 80;

        return total;
    }

    public static string[] Palabras(string consulta) =>
        Normalizar(consulta).Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries);
}

/// <summary>
/// Guarda el texto ya normalizado de cada entrada. Normalizar 80 textos
/// largos cuesta unos 25 ms; sin esta cache eso pasaba cada vez que se
/// copiaba algo.
/// </summary>
public sealed class Indice(Almacen almacen)
{
    readonly record struct Guardado(object? Marca, string Titulo, string Cuerpo);

    readonly record struct Fila(
        Elemento Dato, TipoResultado Tipo, string Titulo, string Cuerpo);

    readonly Almacen _almacen = almacen;

    // La clave es la identidad del objeto, no su contenido: es lo que
    // permite notar que una entrada cambio de texto sin recorrerlo.
    readonly Dictionary<Elemento, Guardado> _cache =
        new(ReferenceEqualityComparer.Instance);

    List<Fila>? _lista;

    public void Invalidar() => _lista = null;

    Guardado Normalizado(Elemento dato, TipoResultado tipo)
    {
        object? marca = dato switch
        {
            Entrada e => e.Texto,
            Snippet s => s.Titulo,
            _ => null,
        };

        if (_cache.TryGetValue(dato, out var guardado) &&
            ReferenceEquals(guardado.Marca, marca))
            return guardado;

        Guardado nuevo;

        if (tipo == TipoResultado.Guardado)
        {
            var s = (Snippet)dato;
            nuevo = new Guardado(
                marca,
                Busqueda.Normalizar(s.Titulo),
                Busqueda.Normalizar(Modelo.TextoDe(s.Runs)));
        }
        else
        {
            var e = (Entrada)dato;

            if (e.EsImagen)
            {
                nuevo = new Guardado(marca, "imagen captura", "");
            }
            else
            {
                string t = e.Texto ?? "";
                nuevo = new Guardado(
                    marca,
                    Busqueda.Normalizar(Modelo.UnaLinea(t, 80)),
                    Busqueda.Normalizar(t));
            }
        }

        _cache[dato] = nuevo;
        return nuevo;
    }

    List<Fila> Entradas()
    {
        if (_lista is not null) return _lista;

        var lista = new List<Fila>();

        foreach (var g in _almacen.Snippets)
        {
            var n = Normalizado(g, TipoResultado.Guardado);
            lista.Add(new Fila(g, TipoResultado.Guardado, n.Titulo, n.Cuerpo));
        }

        foreach (var h in _almacen.HistOrdenado())
        {
            var n = Normalizado(h, TipoResultado.Historial);
            lista.Add(new Fila(h, TipoResultado.Historial, n.Titulo, n.Cuerpo));
        }

        // Lo que ya no esta en el almacen se saca de la cache, o creceria
        // sin limite mientras el programa siga abierto.
        var vivos = new HashSet<Elemento>(
            lista.Select(f => f.Dato), ReferenceEqualityComparer.Instance);

        foreach (var muerto in _cache.Keys.Where(k => !vivos.Contains(k)).ToList())
            _cache.Remove(muerto);

        _lista = lista;
        return lista;
    }

    /// <summary>Cuantas entradas hay indexadas ahora mismo.</summary>
    public int Cuantas() => Entradas().Count;

    /// <summary>Los resultados ordenados por parecido.</summary>
    public List<Resultado> Buscar(string consulta)
    {
        var palabras = Busqueda.Palabras(consulta);
        if (palabras.Length == 0) return [];

        var puntuados = new List<(int Punto, Fila Fila)>();

        foreach (var fila in Entradas())
        {
            int? p = Busqueda.Puntuar(palabras, fila.Titulo, fila.Cuerpo);
            if (p is null) continue;

            // Lo guardado a proposito pesa mas que lo copiado de paso.
            if (fila.Tipo == TipoResultado.Guardado) p += 40;

            puntuados.Add((p.Value, fila));
        }

        // OrderByDescending es estable: a igual puntuacion se conserva el
        // orden de entrada, como hacia el sort de Python.
        return [.. puntuados
            .OrderByDescending(x => x.Punto)
            .Select(x => new Resultado(x.Fila.Dato, x.Fila.Tipo))];
    }
}
