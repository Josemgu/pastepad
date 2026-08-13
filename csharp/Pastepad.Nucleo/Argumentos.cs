namespace Pastepad.Nucleo;

/// <summary>
/// Componer la linea de comandos con la que Windows tiene que volver a
/// abrirnos despues de una actualizacion.
///
/// Esto vive en el nucleo y no junto a la llamada al sistema por lo de
/// siempre: es la parte que se puede equivocar en silencio. Si el
/// entrecomillado falla, pastepad no revienta —vuelve a abrirse
/// tranquilamente sobre OTRA carpeta de datos—, y la carpeta de al lado
/// es el historial real del usuario.
///
/// La ruta de pruebas con la que se comprobo todo esto era la corta de
/// 8.3 y no llevaba un solo espacio, asi que la rama que importa no se
/// ejecuto ni una vez. El escritorio del usuario si los lleva.
/// </summary>
public static class Argumentos
{
    /// <summary>
    /// Une los argumentos en una sola linea, entrecomillando los que
    /// llevan espacios. El nombre del ejecutable se deja fuera a
    /// proposito: RegisterApplicationRestart dice "do not include the
    /// name of the executable in the command line; this function adds it
    /// for you".
    ///
    /// Devuelve null si no hay nada que registrar, porque para esa API
    /// una cadena vacia y null significan lo mismo —borrar lo registrado—
    /// y asi el que llama no tiene que acordarse.
    /// </summary>
    public static string? Componer(IEnumerable<string> partes)
    {
        var linea = string.Join(' ', partes.Select(Entrecomillar));
        return linea.Length == 0 ? null : linea;
    }

    static string Entrecomillar(string parte)
    {
        // Sin espacios no se toca: entrecomillar de mas tampoco es
        // gratis, hay programas que se comen las comillas literalmente.
        if (!parte.Contains(' ')) return parte;

        // Si ya venia entrecomillado, no se envuelve dos veces.
        if (parte.Length >= 2 && parte[0] == '"' && parte[^1] == '"')
            return parte;

        return '"' + parte + '"';
    }
}
