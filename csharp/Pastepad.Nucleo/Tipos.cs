namespace Pastepad.Nucleo;

/// <summary>
/// De que es cada guardado: un marcador, una plantilla, un correo o una
/// nota.
///
/// Hasta la 4.3.0 esto no se elegia, se deducia del texto —una url era
/// un marcador, unos [[campos]] una plantilla, y lo demas una nota— y no
/// habia forma de llevarle la contraria. Funcionaba mientras los cuatro
/// grupos se pudieran leer en el propio texto, y dejo de funcionar en
/// cuanto aparecio uno que no: un cuerpo de correo es texto corriente y
/// no hay nada dentro que lo distinga de una nota.
///
/// Asi que el tipo se sigue proponiendo solo, pero ahora es una
/// propuesta. El usuario la acepta o la cambia, y lo que elija manda.
///
/// **Lo que no se elige no se escribe.** El campo es nulo mientras nadie
/// lo toque, y entonces vale lo deducido, que es lo que pastepad hacia
/// desde siempre: un snippets.json de hoy sigue leyendose igual y
/// vuelve a escribirse igual, sin una clave de mas.
/// </summary>
public static class Tipos
{
    public const string Marcador = "marcador";
    public const string Plantilla = "plantilla";
    public const string Correo = "correo";
    public const string Nota = "nota";

    /// <summary>
    /// En el orden en el que se enseñan, que es tambien el de los grupos
    /// del panel: de lo mas especifico a lo mas general.
    /// </summary>
    public static readonly string[] Todos =
        [Marcador, Plantilla, Correo, Nota];

    /// <summary>
    /// El tipo que se propone leyendo el texto. Es exactamente lo que la
    /// interfaz venia haciendo por su cuenta, traido aqui para que haya
    /// un solo sitio que lo decida y para que se pueda probar.
    ///
    /// Un enlace con [[campos]] cuenta como marcador: entre los dos gana
    /// el que se reconoce con mas certeza.
    ///
    /// «Correo» no se deduce nunca. No hay nada en un cuerpo de correo
    /// que lo separe de una nota, y proponerlo por que lleve una arroba
    /// convertiria cualquier texto con una direccion dentro en un correo.
    /// Es el tipo que existe precisamente para elegirlo a mano.
    /// </summary>
    public static string Deducir(string? texto)
    {
        if (Modelo.EsEnlace(texto)) return Marcador;

        return Modelo.CamposDe(texto ?? "").Count > 0 ? Plantilla : Nota;
    }

    /// <summary>
    /// El tipo que vale: el elegido, si se eligio uno y es de los
    /// cuatro; si no, el deducido.
    ///
    /// Se comprueba que sea de los cuatro porque config.json y
    /// snippets.json se editan a mano y llegan de versiones que aun no
    /// existen. Un tipo desconocido no puede dejar el guardado fuera de
    /// los cuatro grupos, que es como desaparecer de la lista.
    /// </summary>
    public static string De(string? elegido, string? texto) =>
        Vale(elegido) ? elegido! : Deducir(texto);

    /// <summary>Lo mismo, para un guardado.</summary>
    public static string De(Snippet snippet) =>
        De(snippet.Tipo, Modelo.TextoDe(snippet.Runs));

    public static bool Vale(string? tipo) =>
        tipo is not null && Array.IndexOf(Todos, tipo) >= 0;

    /// <summary>
    /// Lo que se guarda en el archivo: nulo cuando lo elegido coincide
    /// con lo que se habria deducido igualmente.
    ///
    /// Sin esto, abrir un guardado y pulsar Guardar sin tocar nada le
    /// añadiria una clave "tipo" que antes no tenia. No cambia el
    /// comportamiento, pero ensucia el archivo de todo el que edite algo
    /// y hace ruido en cualquier comparacion.
    /// </summary>
    public static string? ParaGuardar(string? elegido, string? texto) =>
        Vale(elegido) && elegido != Deducir(texto) ? elegido : null;
}
