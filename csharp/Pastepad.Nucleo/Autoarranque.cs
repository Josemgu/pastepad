namespace Pastepad.Nucleo;

/// <summary>
/// Si pastepad debe arrancar con Windows, segun la preferencia
/// <c>autoarranque</c> de config.json.
///
/// La decision vive en el nucleo y no junto al registro de Windows por
/// dos motivos: se puede probar sin tocar HKCU —que es global y es del
/// usuario, no de la prueba— y deja en un solo sitio la regla que la
/// version anterior tenia suelta en el arranque.
///
/// Manda la aplicacion y solo ella. El instalador ya no escribe en
/// HKCU\...\Run: con dos duenos de un mismo valor, uno lo pone y el otro
/// lo borra, y ademas desinstalar se llevaba por delante una preferencia
/// que el usuario habia elegido.
/// </summary>
public static class Autoarranque
{
    /// <summary>
    /// De fabrica se arranca con Windows. Es lo que hacia la version
    /// anterior y lo razonable para algo que vive en la bandeja: un
    /// gestor de portapapeles que hay que abrir a mano no sustituye a
    /// Win+V.
    /// </summary>
    public const string PorDefecto = "si";

    /// <summary>Como se llama la preferencia en config.json.</summary>
    public const string Clave = "autoarranque";

    /// <summary>
    /// Solo un "si" enciende el arranque automatico, igual que en la
    /// version anterior, que comparaba la preferencia con "si" tal cual.
    /// Aqui se ignoran mayusculas y espacios de sobra —un config.json se
    /// edita a mano y "Si " no deberia significar otra cosa—, pero el
    /// criterio no cambia: lo que no sea un si, no lo es.
    /// </summary>
    public static bool Quiere(string? preferencia) =>
        (preferencia ?? PorDefecto).Trim()
            .Equals(PorDefecto, StringComparison.OrdinalIgnoreCase);
}
