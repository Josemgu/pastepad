using System.Text.Json.Serialization;

namespace Pastepad.Nucleo;

/// <summary>
/// Base comun de lo que puede aparecer en una lista o en un resultado
/// de busqueda: un snippet guardado o una entrada del historial.
/// </summary>
public abstract class Elemento
{
}

/// <summary>
/// Un trozo de texto con su formato. Un snippet es una lista de estos.
/// Las claves van de una letra para que el JSON no engorde, y son las
/// mismas que escribia la version en Python: quien ya use pastepad no
/// puede perder nada.
/// </summary>
public sealed class Fragmento
{
    [JsonPropertyName("t")] public string T { get; set; } = "";
    [JsonPropertyName("f")] public string F { get; set; } = Config.FuenteDef;
    [JsonPropertyName("s")] public int S { get; set; } = Config.TamDef;
    [JsonPropertyName("b")] public int B { get; set; }
    [JsonPropertyName("i")] public int I { get; set; }
    [JsonPropertyName("u")] public int U { get; set; }
    [JsonPropertyName("c")] public string C { get; set; } = Config.ColorDef;
}

/// <summary>Un texto guardado a proposito, dentro de una carpeta.</summary>
public sealed class Snippet : Elemento
{
    [JsonPropertyName("titulo")] public string Titulo { get; set; } = "";

    [JsonPropertyName("categoria")] public string Categoria { get; set; } = "";

    [JsonPropertyName("runs")] public List<Fragmento> Runs { get; set; } = [];

    /// <summary>
    /// De que es esto, si el usuario lo dijo: ver <see cref="Tipos"/>.
    ///
    /// Nulo mientras nadie lo elija, y entonces se deduce del texto como
    /// se ha hecho siempre. Por eso no se escribe cuando es nulo: un
    /// snippets.json de antes de esta version se vuelve a escribir tal
    /// cual, sin una clave que no estaba.
    /// </summary>
    [JsonPropertyName("tipo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tipo { get; set; }

    /// <summary>
    /// Solo lo traen los archivos de versiones viejas, de antes de que
    /// un snippet pudiera llevar formato. Al cargar se convierte en un
    /// unico fragmento; no se vuelve a escribir.
    /// </summary>
    [JsonPropertyName("texto")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Texto { get; set; }
}

/// <summary>Algo que paso por el portapapeles.</summary>
public sealed class Entrada : Elemento
{
    public const string Texto_ = "texto";
    public const string Imagen = "imagen";

    [JsonPropertyName("tipo")] public string Tipo { get; set; } = Texto_;

    [JsonPropertyName("texto")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Texto { get; set; }

    [JsonPropertyName("ruta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ruta { get; set; }

    /// <summary>
    /// Se omite al escribir cuando es false, igual que hacia el JSON de
    /// Python, donde la clave sencillamente no estaba.
    /// </summary>
    [JsonPropertyName("pin")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Pin { get; set; }

    /// <summary>
    /// Calculada, no un campo del archivo. Sin este JsonIgnore se
    /// escribia como una clave mas y ensuciaba el JSON de la v3.
    /// </summary>
    [JsonIgnore]
    public bool EsImagen => Tipo == Imagen;
}

/// <summary>El contenido de snippets.json.</summary>
internal sealed class Coleccion
{
    [JsonPropertyName("categorias")]
    public List<string> Categorias { get; set; } = [];

    [JsonPropertyName("snippets")]
    public List<Snippet> Snippets { get; set; } = [];
}
