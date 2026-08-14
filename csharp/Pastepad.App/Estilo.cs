using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Pastepad.App;

/// <summary>
/// La paleta y las medidas, portadas de <c>pastepad/estilo.py</c>, que
/// es la fuente de verdad declarada en la especificacion de interfaz.
///
/// Los pinceles no se recrean al cambiar de tema: se les cambia el
/// color en el sitio. XAML resuelve un StaticResource una sola vez, asi
/// que sustituir el objeto no repintaria nada; mutarlo si.
/// </summary>
public static class Estilo
{
    /// <summary>Los ocho colores que definen un fondo.</summary>
    public sealed record Paleta(
        string Fondo,
        string Elevado,
        string Tarjeta,
        string Hover,
        string Borde,
        string Texto,
        string Medio,
        string Tenue,
        string Sombra);

    // Fijos en todos los temas: el peligro tiene que leerse igual de
    // rojo en claro que en oscuro, y la carpeta igual de ambar.
    public const string Rojo = "#EF4444";
    public const string Ambar = "#F5A524";

    /// <summary>Rojo del boton Borrar, distinto del de aviso.</summary>
    public const string Peligro = "#DC2626";

    /// <summary>
    /// Los 18 acentos. El segundo color es el del texto encima: blanco
    /// fijo sobre ambar o lima no llega al 4.5:1 de WCAG AA. Los 18
    /// pares estan comprobados por calculo, no a ojo: el peor es azul,
    /// con 5.38:1.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string Color, string Sobre)>
        Acentos = new Dictionary<string, (string, string)>
        {
            ["menta"] = ("#2DD4A7", "#052E23"),
            ["azul"] = ("#4B8DF8", "#04183C"),
            ["violeta"] = ("#9B7BF7", "#1E1046"),
            ["ambar"] = ("#F5A524", "#3A2606"),
            ["coral"] = ("#F76B5C", "#3D0F09"),
            ["rosa"] = ("#F472B6", "#3A1128"),
            ["cian"] = ("#22D3EE", "#062E36"),
            ["lima"] = ("#A3E635", "#1A2E05"),
            ["indigo"] = ("#818CF8", "#111539"),
            ["turquesa"] = ("#2DD4BF", "#042F2A"),
            ["durazno"] = ("#FB923C", "#3B1D06"),
            ["lavanda"] = ("#C084FC", "#2E1065"),
            ["esmeralda"] = ("#34D399", "#04291C"),
            ["cielo"] = ("#38BDF8", "#052F42"),
            ["oro"] = ("#FCD34D", "#3B2606"),
            ["fresa"] = ("#FB7185", "#3F0A16"),
            ["menta_fria"] = ("#5EEAD4", "#032F2A"),
            ["arena"] = ("#D6BC8A", "#332612"),
        };

    public const string AcentoDef = "menta";

    public static readonly Paleta Oscura = new(
        "#0B0B0D", "#141417", "#1B1B1F", "#26262B", "#242429",
        "#F4F4F5", "#9C9CA6", "#86868E", "#000000");

    public static readonly Paleta Clara = new(
        "#F6F6F4", "#FFFFFF", "#FFFFFF", "#EFEFEC", "#E6E6E2",
        "#141416", "#5C5C66", "#707079", "#94949E");

    static readonly Paleta Medianoche = new(
        "#0A0F1E", "#111A30", "#16213C", "#1F2D4D", "#1C2942",
        "#E8ECF5", "#93A0BC", "#7E89A1", "#000000");

    static readonly Paleta Grafito = new(
        "#141414", "#1C1C1C", "#242424", "#2E2E2E", "#2A2A2A",
        "#F0F0F0", "#A0A0A0", "#8B8B8B", "#000000");

    static readonly Paleta Bosque = new(
        "#0A140F", "#0F1D16", "#14281E", "#1D3A2B", "#1A3327",
        "#E8F2EC", "#8FB3A0", "#779082", "#000000");

    static readonly Paleta Papel = new(
        "#F5F1E8", "#FFFDF7", "#FFFDF7", "#EDE7D9", "#E2DACA",
        "#2B2620", "#645B4F", "#776C60", "#A89B87");

    static readonly Paleta Niebla = new(
        "#EEF1F5", "#FFFFFF", "#FFFFFF", "#E2E8F0", "#DBE2EA",
        "#1E293B", "#505C6E", "#636E80", "#94A3B8");

    static readonly Paleta ArenaFondo = new(
        "#F7F4EF", "#FFFFFF", "#FFFFFF", "#EEE9E0", "#E5DED2",
        "#33302B", "#645D53", "#776F65", "#A8A093");

    static readonly Paleta Lila = new(
        "#F4F1FA", "#FFFFFF", "#FFFFFF", "#E9E3F6", "#E1D9F2",
        "#2A2340", "#635682", "#73698E", "#A296C4");

    static readonly Paleta Salvia = new(
        "#EEF7F3", "#FFFFFF", "#FFFFFF", "#DFF0E8", "#D5E9DF",
        "#1E332B", "#496458", "#5D766A", "#93B3A4");

    static readonly Paleta Rubor = new(
        "#FBF2F4", "#FFFFFF", "#FFFFFF", "#F5E3E8", "#EEDAE0",
        "#3A2229", "#76555E", "#866871", "#B79AA2");

    /// <summary>
    /// Los 12 fondos. "auto" es null y sigue al tema de Windows; el
    /// resto manda sobre el sistema.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Paleta?> Temas =
        new Dictionary<string, Paleta?>
        {
            ["auto"] = null,
            ["oscuro"] = Oscura,
            ["medianoche"] = Medianoche,
            ["grafito"] = Grafito,
            ["bosque"] = Bosque,
            ["claro"] = Clara,
            ["papel"] = Papel,
            ["niebla"] = Niebla,
            ["arena"] = ArenaFondo,
            ["lila"] = Lila,
            ["salvia"] = Salvia,
            ["rubor"] = Rubor,
        };

    public const string TemaDef = "auto";

    /// <summary>
    /// Los que se leen sobre fondo claro. Hace falta decirselo a WinUI
    /// aparte de pintar: los menus y el cursor del buscador los dibuja
    /// el sistema segun ElementTheme, no segun nuestros pinceles.
    /// </summary>
    static readonly HashSet<string> Claros =
    [
        "claro", "papel", "niebla", "arena", "lila", "salvia", "rubor",
    ];

    // --- medidas. Salen de las maquetas SVG y de la especificacion.
    public const int RPanel = 20;
    public const int RTarjeta = 14;
    public const int RControl = 12;
    public const int RCapsula = 15;

    public const int E1 = 4;
    public const int E2 = 8;
    public const int E3 = 12;
    public const int E4 = 16;

    public const int AltoFila = 56;
    public const int AltoFilaMini = 42;
    public const int SepFila = 6;
    public const int BarraActiva = 3;

    // Los cuatro tamanos de la especificacion, seccion 3. Nada
    // intermedio: las maquetas dibujan 12.5 y 10 en algunos textos,
    // pero manda la especificacion.
    public const int TTitulo = 15;
    public const int TCuerpo = 13;
    public const int TMenor = 12;
    public const int TMini = 11;

    // Alturas explicitas y no deducidas del padding: dejarselas al
    // framework fue lo que inflo las pestanas a 49 px donde la maqueta
    // pedia 30, y el buscador a 66 donde pedia 42.
    public const int AltoBuscador = 42;
    public const int AltoPestana = 30;
    public const int AltoBoton = 26;
    public const int AltoCarpeta = 34;
    public const int AltoGrupo = 32;
    public const int LadoCabecera = 20;

    /// <summary>Debajo de este ancho la fila pasa a una sola linea.</summary>
    public const int AnchoCompacto = 340;

    /// <summary>Nombre del acento y del tema en uso.</summary>
    public static string Acento { get; private set; } = AcentoDef;
    public static string Tema { get; private set; } = TemaDef;

    /// <summary>True si el fondo que se esta viendo ahora es claro.</summary>
    public static bool EsClaro { get; private set; }

    /// <summary>La paleta que se esta viendo ahora.</summary>
    public static Paleta Actual { get; private set; } = Oscura;

    public static (string Color, string Sobre) ColorAcento { get; private set; } =
        Acentos[AcentoDef];

    /// <summary>
    /// Que tema pedirle a WinUI. <c>Default</c> significa seguir al de
    /// Windows y cambiar con el en caliente: es lo que hace "auto", y es
    /// el motivo de estar en WinUI 3. Light y Dark solo cuando el usuario
    /// elige a proposito.
    /// </summary>
    public static ElementTheme TemaPedido { get; private set; } =
        ElementTheme.Default;

    /// <summary>
    /// True cuando el fondo lo pone el sistema y hay que dejar ver Mica.
    /// Con uno de los diez fondos propios no: ahi el usuario pidio un
    /// color concreto y taparlo con el material del sistema seria no
    /// darselo.
    /// </summary>
    public static bool UsaMica { get; private set; } = true;

    /// <summary>
    /// Deja las dos paletas en los diccionarios de tema. No decide cual
    /// se ve: eso lo hace WinUI segun <see cref="TemaPedido"/>, y cambia
    /// solo cuando Windows cambia.
    /// </summary>
    public static void Aplicar(string? acento, string? tema)
    {
        if (!string.IsNullOrEmpty(tema)) Tema = tema;
        if (!string.IsNullOrEmpty(acento)) Acento = acento;

        ColorAcento = Acentos.TryGetValue(Acento, out var a)
            ? a
            : Acentos[AcentoDef];

        // De fabrica, cada diccionario lleva la paleta que le toca.
        var claraEfectiva = Clara;
        var oscuraEfectiva = Oscura;

        if (Temas.TryGetValue(Tema, out var propia) && propia is not null)
        {
            if (Claros.Contains(Tema))
            {
                claraEfectiva = propia;
                TemaPedido = ElementTheme.Light;
            }
            else
            {
                oscuraEfectiva = propia;
                TemaPedido = ElementTheme.Dark;
            }

            // "claro" y "oscuro" son las paletas del sistema elegidas a
            // mano: siguen mereciendo Mica. Los otros nueve no.
            UsaMica = Tema is "claro" or "oscuro";
        }
        else
        {
            TemaPedido = ElementTheme.Default;
            UsaMica = true;
        }

        Volcar("Light", claraEfectiva);
        Volcar("Dark", oscuraEfectiva);
    }

    static void Volcar(string clave, Paleta paleta)
    {
        if (Application.Current.Resources.ThemeDictionaries[clave]
            is not ResourceDictionary dic)
        {
            return;
        }

        Poner(dic, "PpFondo", paleta.Fondo);
        Poner(dic, "PpElevado", paleta.Elevado);
        Poner(dic, "PpTarjeta", paleta.Tarjeta);
        Poner(dic, "PpHover", paleta.Hover);
        Poner(dic, "PpBorde", paleta.Borde);
        Poner(dic, "PpTexto", paleta.Texto);
        Poner(dic, "PpMedio", paleta.Medio);
        Poner(dic, "PpTenue", paleta.Tenue);

        // El acento es nuestro, no del sistema, asi que va igual en las
        // dos paletas. En alto contraste no se toca: ahi manda Windows.
        Poner(dic, "PpAcento", ColorAcento.Color);
        Poner(dic, "PpSobre", ColorAcento.Sobre);
    }

    /// <summary>
    /// Apunta <see cref="Actual"/> a la paleta que se esta viendo. Se
    /// llama al arrancar y en cada ActualThemeChanged: lo que pintamos
    /// desde codigo no lo reevalua nadie por nosotros.
    /// </summary>
    public static void Sincronizar(ElementTheme efectivo)
    {
        EsClaro = efectivo == ElementTheme.Light;

        if (Temas.TryGetValue(Tema, out var propia) && propia is not null)
        {
            // Con un fondo propio, el efectivo y el pedido coinciden
            // siempre: TemaPedido lo fijo Aplicar().
            Actual = propia;
            return;
        }

        Actual = EsClaro ? Clara : Oscura;
    }

    static void Poner(ResourceDictionary recursos, string clave, string hex)
    {
        var color = Desde(hex);

        if (recursos.TryGetValue(clave, out var valor)
            && valor is SolidColorBrush pincel)
        {
            pincel.Color = color;
            return;
        }

        recursos[clave] = new SolidColorBrush(color);
    }

    /// <summary>"#RRGGBB" o "#AARRGGBB" a Color.</summary>
    public static Color Desde(string hex)
    {
        string h = hex.TrimStart('#');

        byte a = 255;
        int i = 0;

        if (h.Length == 8)
        {
            a = Convert.ToByte(h[..2], 16);
            i = 2;
        }

        return Color.FromArgb(
            a,
            Convert.ToByte(h.Substring(i, 2), 16),
            Convert.ToByte(h.Substring(i + 2, 2), 16),
            Convert.ToByte(h.Substring(i + 4, 2), 16));
    }

    public static SolidColorBrush Pincel(string hex) => new(Desde(hex));

    /// <summary>
    /// Un pincel del mismo color con menos opacidad. Se usa para el
    /// subtitulo de la fila activa, que va sobre el acento: bajar la
    /// opacidad del color "sobre" lo separa del titulo sin inventar un
    /// color que no este en la paleta.
    /// </summary>
    public static SolidColorBrush Pincel(string hex, double opacidad)
    {
        var color = Desde(hex);
        return new SolidColorBrush(
            Color.FromArgb((byte)Math.Round(opacidad * 255), color.R, color.G, color.B));
    }

    /// <summary>El nombre legible de un fondo, para el dialogo.</summary>
    public static string NombreTema(string codigo) => codigo switch
    {
        "auto" => "Según Windows",
        "oscuro" => "Oscuro",
        "medianoche" => "Medianoche",
        "grafito" => "Grafito",
        "bosque" => "Bosque",
        "claro" => "Claro",
        "papel" => "Papel",
        "niebla" => "Niebla",
        "arena" => "Arena",
        "lila" => "Lila",
        "salvia" => "Salvia",
        "rubor" => "Rubor",
        _ => codigo,
    };

    /// <summary>
    /// Glifos de Segoe Fluent Icons, la fuente de iconos de Windows 11.
    /// Cada uno esta comprobado dibujandolo, no leyendo su nombre: los
    /// que habia antes venian de memoria y uno de ellos (E8C8) era el
    /// icono de "copiar" haciendo de icono de "texto".
    /// </summary>
    public static class Iconos
    {
        public const string Buscar = "\uE721";       // Search
        public const string Pausa = "\uE769";        // Pause
        public const string Reanudar = "\uE768";     // Play
        public const string Paleta = "\uE790";       // Color
        public const string Cerrar = "\uE8BB";       // ChromeClose
        public const string Enlace = "\uE71B";       // Link
        public const string Imagen = "\uEB9F";       // Photo2
        public const string Fijar = "\uE718";        // Pin
        public const string Soltar = "\uE77A";       // Unpin
        public const string TresPuntos = "\uE712";   // More
        public const string Carpeta = "\uE8B7";      // Folder
        public const string CarpetaAbierta = "\uE838";
        public const string CarpetaNueva = "\uE8F4"; // NewFolder
        public const string Lista = "\uE8FD";        // BulletedList
        public const string Escoba = "\uEA99";       // Broom
        public const string Casilla = "\uE739";      // Checkbox
        public const string CasillaMarcada = "\uE73A";
        public const string Mas = "\uE710";          // Add
        public const string AbajoV = "\uE70D";       // ChevronDown
        public const string ArribaV = "\uE70E";      // ChevronUp
        public const string DerechaV = "\uE76C";     // ChevronRight
        public const string Nota = "\uE70B";         // QuickNote
        public const string Portapapeles = "\uE77F"; // Paste
        public const string Seleccionar = "\uE762";  // CheckList
        public const string Marca = "\uE73E";           // CheckMark
        public const string Editar = "\uE70F";       // Edit
        public const string Plantilla = "\uE943";    // Code
        public const string Correo = "\uE715";       // Mail

        /// <summary>
        /// ChatSparkle. Es la familia \u00ABsparkle\u00BB con la que Windows marca
        /// lo de IA, asi que se lee como tal sin tener que explicarlo.
        /// Verificado en la lista oficial de Segoe Fluent Icons.
        /// </summary>
        public const string Prompt = "\uEAB7";       // ChatSparkle
        public const string Papelera = "\uE74D";     // Delete
        public const string Deshacer = "\uE7A7";     // Undo
    }
}
