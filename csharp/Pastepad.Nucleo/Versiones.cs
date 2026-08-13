using System.Globalization;

namespace Pastepad.Nucleo;

/// <summary>
/// Comparar versiones y decidir cuando toca comprobar y cuando toca
/// avisar.
///
/// Vive en el nucleo y separado de la parte que habla con GitHub porque
/// es lo unico que se puede probar sin red: la comparacion es donde de
/// verdad se puede meter la pata, y en silencio.
/// </summary>
public static class Versiones
{
    /// <summary>Como se llaman las preferencias en config.json.</summary>
    public const string ClaveAvisar = "avisar_novedades";
    public const string ClaveComprobacion = "ultima_comprobacion";
    public const string ClaveAvisada = "version_avisada";

    /// <summary>
    /// De fabrica se avisa. Quien no lo quiera lo apaga en Apariencia:
    /// un programa que llama a casa sin que se pueda decir que no es un
    /// programa que no respeta a quien lo usa.
    /// </summary>
    public const bool AvisarDef = true;

    /// <summary>Como se guarda la fecha: ISO, sin depender del idioma.</summary>
    public const string FormatoFecha = "yyyy-MM-dd";

    /// <summary>
    /// El tag de GitHub viene como "v4.0.1"; la version del ensamblado
    /// es "4.0.1". Se quita la v y nada mas.
    /// </summary>
    public static string SinLaV(string? tag)
    {
        string t = (tag ?? "").Trim();

        return t.StartsWith('v') || t.StartsWith('V') ? t[1..] : t;
    }

    /// <summary>
    /// Compara como numeros y no como texto. Es la trampa de esto: como
    /// cadenas, "4.0.10" sale MENOR que "4.0.9", porque compara el "1"
    /// con el "9". Con diez publicaciones de parches basta para que el
    /// aviso deje de aparecer y nadie se entere.
    ///
    /// Lo que no se pueda interpretar —una version con letras, un tag
    /// con sufijo de prueba— devuelve false: mejor no avisar que avisar
    /// de cualquier cosa.
    /// </summary>
    public static bool HayNovedad(string? instalada, string? publicada)
    {
        if (!Interpretar(instalada, out var mia)) return false;
        if (!Interpretar(publicada, out var suya)) return false;

        return suya > mia;
    }

    /// <summary>
    /// Con las cuatro partes siempre puestas. System.Version trata las
    /// que faltan como -1, asi que "4.0.1" le sale menor que "4.0.1.0" y
    /// se avisaria de una version que es la misma.
    /// </summary>
    static bool Interpretar(string? texto, out Version version)
    {
        version = new Version(0, 0);

        if (!Version.TryParse(SinLaV(texto), out var v)) return false;

        version = new Version(
            v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));

        return true;
    }

    /// <summary>
    /// Una vez al dia. Si la fecha guardada no se entiende o no existe,
    /// toca: es el primer arranque o alguien edito el archivo a mano.
    /// </summary>
    public static bool TocaComprobar(string? ultima, DateTimeOffset ahora)
    {
        if (!DateTime.TryParseExact(ultima, FormatoFecha,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
        {
            return true;
        }

        return fecha.Date < ahora.Date;
    }

    public static string Hoy(DateTimeOffset ahora) =>
        ahora.ToString(FormatoFecha, CultureInfo.InvariantCulture);

    /// <summary>
    /// Se avisa de cada version una sola vez. Sin esto, quien no quiera
    /// actualizar todavia se come la misma banda cada dia, y una banda
    /// que sale siempre se deja de leer.
    /// </summary>
    public static bool TocaAvisar(
        string? instalada, string? publicada, string? yaAvisada)
    {
        if (!HayNovedad(instalada, publicada)) return false;

        return !Interpretar(yaAvisada, out var avisada)
               || !Interpretar(publicada, out var suya)
               || suya != avisada;
    }
}
