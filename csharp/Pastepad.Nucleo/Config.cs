namespace Pastepad.Nucleo;

/// <summary>
/// Limites y valores por defecto. Los colores y las medidas de la
/// interfaz NO estan aqui: viven en la especificacion de UI, que es su
/// unica fuente. Este archivo llego a tener una copia con valores
/// distintos y solo servia para confundir.
/// </summary>
public static class Config
{
    public const string App = "pastepad";

    /// <summary>
    /// Sale del ensamblado, que a su vez sale del tag de git. Nunca
    /// escrita a mano: se publico una release con binarios viejos por
    /// tenerla clavada en doce sitios.
    /// </summary>
    public static string Version { get; } =
        typeof(Config).Assembly
            .GetCustomAttributes(
                typeof(System.Reflection.AssemblyInformationalVersionAttribute),
                false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion
            .Split('+')[0]
        ?? "0.0.0";

    // --- ventana

    /// <summary>
    /// Lo que mide el panel la primera vez, antes de que el usuario haya
    /// arrastrado nada.
    ///
    /// Es un valor suelto y no un preajuste elegible: los preajustes
    /// (mini, chico, mediano, grande) se retiraron porque el panel es
    /// adaptable —se estira por los bordes y recuerda el tamaño—, y unas
    /// medidas fijas al lado de algo que ya se ajusta solo sobraban.
    /// Vive junto a los limites de abajo porque es de su misma especie:
    /// una medida del panel, no una opcion.
    /// </summary>
    public const int AnchoDef = 380;
    public const int AltoDef = 560;

    public const int MinAncho = 300;
    public const int MinAlto = 340;
    public const int MaxAncho = 720;
    public const int MaxAlto = 1100;

    /// <summary>Atajos que se hacen con una sola mano.</summary>
    public static readonly IReadOnlyDictionary<string, string> Atajos =
        new Dictionary<string, string>
        {
            ["ctrl+shift+v"] = "Ctrl + Shift + V",
            ["ctrl+q"] = "Ctrl + Q",
            ["ctrl+space"] = "Ctrl + Espacio",
            ["ctrl+shift+space"] = "Ctrl + Shift + Espacio",
            ["alt+q"] = "Alt + Q",
            ["ctrl+alt+v"] = "Ctrl + Alt + V",
        };

    public const string AtajoDef = "ctrl+shift+v";

    /// <summary>
    /// La carpeta que se crea sola cuando no hay ninguna.
    ///
    /// No se traduce, y por eso vive aqui y no en <see cref="Textos"/>:
    /// acaba escrita en snippets.json como categoria de cada texto.
    /// Traducirla partiria los datos en cuatro, porque cambiar de idioma
    /// dejaria los textos guardados en una carpeta que ya no se llama
    /// asi. Es un dato con valor inicial, no un rotulo.
    /// </summary>
    public const string CarpetaDef = "Mis textos";

    // --- limites
    public const int MaxHist = 80;
    public const int MaxCaracteres = 200_000;

    // --- tipografia
    public const string FuenteAlt = "Segoe UI";   // Windows 10
    public const string FuenteDef = "Calibri";
    public const int TamDef = 11;
    public const string ColorDef = "#000000";
}

/// <summary>
/// Donde vive todo. Se pasa al <see cref="Almacen"/> en vez de vivir en
/// variables globales: asi las pruebas usan una carpeta temporal sin
/// tocar estado compartido, que en la version en Python obligaba a
/// reasignar el modulo de configuracion entero.
/// </summary>
public sealed record Rutas(
    string Datos,
    string Historial,
    string Preferencias,
    string Imagenes)
{
    /// <summary>
    /// %LOCALAPPDATA%\pastepad. En Archivos de programa Windows bloquea
    /// la escritura sin avisar: el programa arrancaria pero no guardaria
    /// nada.
    /// </summary>
    public static Rutas Predeterminadas()
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            Config.App);

        return EnCarpeta(baseDir);
    }

    public static Rutas EnCarpeta(string carpeta) => new(
        Path.Combine(carpeta, "snippets.json"),
        Path.Combine(carpeta, "historial.json"),
        Path.Combine(carpeta, "config.json"),
        Path.Combine(carpeta, "imagenes"));

    /// <summary>
    /// La carpeta que las contiene a todas. Se deduce y no se guarda
    /// aparte para que no puedan discrepar: las cuatro rutas salen
    /// siempre de la misma, incluso cuando vienen de <c>--datos</c>.
    /// </summary>
    public string Carpeta => Path.GetDirectoryName(Datos) ?? "";
}
