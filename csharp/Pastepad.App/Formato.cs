using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Pastepad.Nucleo;
using Windows.UI;

namespace Pastepad.App;

/// <summary>
/// El puente entre el <see cref="RichEditBox"/> y los fragmentos que van
/// a snippets.json.
///
/// El editor de texto enriquecido lo pone WinUI: <c>RichEditBox</c> con
/// su <c>ITextDocument</c>. La barra de botones no, y no es un olvido —
/// la propia documentacion del control lo dice: "los botones de formato
/// no son parte del rich edit box; deberias ofrecer al menos un juego
/// minimo y programar sus acciones".
///
/// Lo que se guarda sigue siendo lo de siempre: una lista de
/// <see cref="Fragmento"/> con fuente, tamaño, negrita, cursiva,
/// subrayado y color. No hace falta tocar el formato del archivo.
/// </summary>
internal static class Formato
{
    /// <summary>
    /// Fuentes que hay en cualquier Windows y que Outlook entiende sin
    /// sustituir nada. Calibri primero: es la de fabrica.
    /// </summary>
    public static readonly string[] Fuentes =
    [
        "Calibri", "Arial", "Segoe UI", "Times New Roman",
        "Georgia", "Verdana", "Tahoma", "Courier New",
    ];

    public static readonly int[] Tamanos =
        [8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 36];

    /// <summary>
    /// El editor separa los parrafos con \r a secas y lo guardado lleva
    /// \r\n. Sin convertirlo, las posiciones se corren un caracter por
    /// cada salto y el formato se aplica desplazado.
    /// </summary>
    static string ParaElEditor(string t) =>
        Modelo.NormalizarSaltos(t).Replace("\r\n", "\r");

    public static string AHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    // ------------------------------------------------------- cargar

    public static void Cargar(RichEditBox caja, IReadOnlyList<Fragmento> runs)
    {
        var doc = caja.Document;

        var trozos = runs
            .Select(f => (Frag: f, Texto: ParaElEditor(f.T)))
            .Where(x => x.Texto.Length > 0)
            .ToList();

        // Lo que se escriba en un documento vacio sale con esto: sin
        // fijarlo, una nota nueva se guardaba con la fuente que el
        // control trae de serie y no con la Calibri 11 de siempre.
        var porDefecto = doc.GetDefaultCharacterFormat();
        Vestir(porDefecto, Modelo.CrearFragmento(""));
        doc.SetDefaultCharacterFormat(porDefecto);

        doc.BatchDisplayUpdates();

        try
        {
            doc.SetText(TextSetOptions.None,
                        string.Concat(trozos.Select(x => x.Texto)));

            int i = 0;

            foreach (var (frag, texto) in trozos)
            {
                var rango = doc.GetRange(i, i + texto.Length);

                var cf = rango.CharacterFormat;
                Vestir(cf, frag);
                rango.CharacterFormat = cf;

                i += texto.Length;
            }
        }
        finally
        {
            doc.ApplyDisplayUpdates();
        }

        // El cursor al principio y sin seleccionar nada: abrir un
        // guardado con todo el texto marcado invita a borrarlo de un
        // teclazo.
        doc.Selection.SetRange(0, 0);
    }

    static void Vestir(ITextCharacterFormat cf, Fragmento f)
    {
        cf.Name = f.F;
        cf.Size = f.S;
        cf.Bold = f.B != 0 ? FormatEffect.On : FormatEffect.Off;
        cf.Italic = f.I != 0 ? FormatEffect.On : FormatEffect.Off;
        cf.Underline = f.U != 0 ? UnderlineType.Single : UnderlineType.None;
        cf.ForegroundColor = Estilo.Desde(f.C);
    }

    // -------------------------------------------------------- leer

    /// <summary>
    /// El texto plano, que es lo que se pega sin formato, lo que se
    /// busca y de donde sale el titulo.
    /// </summary>
    public static string TextoPlano(RichEditBox caja)
    {
        caja.Document.GetText(TextGetOptions.None, out string t);

        // El documento siempre acaba en un salto de parrafo que el
        // usuario no escribio.
        return t.TrimEnd('\r');
    }

    /// <summary>
    /// El documento convertido en fragmentos. Se recorre por tramos de
    /// formato igual —<c>TextRangeUnit.CharacterFormat</c>, que la
    /// documentacion define como "un tramo de caracteres con las mismas
    /// propiedades de formato"—, no caracter a caracter: con una
    /// plantilla de cien lineas serian miles de llamadas.
    /// </summary>
    public static List<Fragmento> Leer(RichEditBox caja)
    {
        var doc = caja.Document;
        string texto = TextoPlano(caja);

        var salida = new List<Fragmento>();
        if (texto.Length == 0) return salida;

        int i = 0;

        // Tope de vueltas: si el recorrido por tramos no avanzara, esto
        // seria un bucle infinito con la ventana colgada.
        for (int vuelta = 0; i < texto.Length && vuelta <= texto.Length; vuelta++)
        {
            var marca = doc.GetRange(i, i);
            marca.MoveEnd(TextRangeUnit.CharacterFormat, 1);

            int fin = marca.EndPosition;
            if (fin <= i || fin > texto.Length) fin = texto.Length;

            var f = De(doc.GetRange(i, fin).CharacterFormat, texto[i..fin]);

            if (salida.Count > 0 && Modelo.MismoFormato(salida[^1], f))
                salida[^1].T += f.T;
            else
                salida.Add(f);

            i = fin;
        }

        // El formato es un adorno; el texto no. Si el recorrido no
        // reprodujo el documento carácter por carácter, se guarda plano
        // antes que guardar mal — y se deja dicho por que.
        string rearmado = Modelo.TextoDe(salida);

        if (rearmado != texto)
        {
            Registro.Anotar(
                $"el recorrido por tramos de formato dio {rearmado.Length} "
                + $"caracteres y el documento tiene {texto.Length}; "
                + "se guarda sin formato para no perder texto");

            return [Modelo.CrearFragmento(texto)];
        }

        return salida;
    }

    static Fragmento De(ITextCharacterFormat cf, string texto)
    {
        string fuente = cf.Name;
        if (string.IsNullOrWhiteSpace(fuente)) fuente = Config.FuenteDef;

        int tam = (int)Math.Round(cf.Size);
        if (tam <= 0) tam = Config.TamDef;

        return Modelo.CrearFragmento(
            texto,
            fuente,
            tam,
            cf.Bold == FormatEffect.On ? 1 : 0,
            cf.Italic == FormatEffect.On ? 1 : 0,
            cf.Underline is UnderlineType.None or UnderlineType.Undefined ? 0 : 1,
            AHex(cf.ForegroundColor));
    }

    // ------------------------------------------------- la seleccion

    /// <summary>Lo que hay seleccionado, o donde este el cursor.</summary>
    public static ITextSelection Seleccion(RichEditBox caja) =>
        caja.Document.Selection;

    public static void Negrita(RichEditBox caja) =>
        Cambiar(caja, cf => cf.Bold = Alternar(cf.Bold));

    public static void Cursiva(RichEditBox caja) =>
        Cambiar(caja, cf => cf.Italic = Alternar(cf.Italic));

    public static void Subrayado(RichEditBox caja) =>
        Cambiar(caja, cf => cf.Underline =
            cf.Underline == UnderlineType.Single
                ? UnderlineType.None
                : UnderlineType.Single);

    public static void Fuente(RichEditBox caja, string nombre) =>
        Cambiar(caja, cf => cf.Name = nombre);

    public static void Tamano(RichEditBox caja, int tam) =>
        Cambiar(caja, cf => cf.Size = tam);

    public static void Color(RichEditBox caja, string hex) =>
        Cambiar(caja, cf => cf.ForegroundColor = Estilo.Desde(hex));

    /// <summary>Vuelve todo a Calibri 11 negro y sin adornos.</summary>
    public static void Limpiar(RichEditBox caja) => Cambiar(caja, cf =>
    {
        cf.Name = Config.FuenteDef;
        cf.Size = Config.TamDef;
        cf.Bold = FormatEffect.Off;
        cf.Italic = FormatEffect.Off;
        cf.Underline = UnderlineType.None;
        cf.ForegroundColor = Estilo.Desde(Config.ColorDef);
    });

    /// <summary>
    /// <c>FormatEffect.Undefined</c> sale cuando la seleccion mezcla
    /// trozos en negrita y trozos sin ella. Ahi se pone negrita a todo,
    /// que es lo que hace Word.
    /// </summary>
    static FormatEffect Alternar(FormatEffect actual) =>
        actual == FormatEffect.On ? FormatEffect.Off : FormatEffect.On;

    static void Cambiar(RichEditBox caja, Action<ITextCharacterFormat> que)
    {
        var sel = caja.Document.Selection;
        if (sel is null) return;

        var cf = sel.CharacterFormat;
        que(cf);
        sel.CharacterFormat = cf;

        caja.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
    }

    // ------------------------------------------- vinetas y sangrias

    public static void Vinetas(RichEditBox caja) =>
        PorLineas(caja, Modelo.AlternarVinetas);

    public static void Numeros(RichEditBox caja) =>
        PorLineas(caja, Modelo.AlternarNumeros);

    public static void Sangria(RichEditBox caja, bool mas) =>
        PorLineas(caja, b => Modelo.Sangrar(b, mas));

    /// <summary>
    /// Aplica un cambio de principio de linea a los parrafos tocados.
    ///
    /// **Solo se reescribe el trocito que cambia**, no la linea entera.
    /// Sustituir el parrafo completo le pone a todo el formato del primer
    /// caracter: poner una viñeta a una linea con una palabra en negrita
    /// se llevaba la negrita por delante.
    /// </summary>
    static void PorLineas(RichEditBox caja, Func<string, string> cambio)
    {
        var doc = caja.Document;
        var sel = doc.Selection;
        if (sel is null) return;

        int a = Math.Min(sel.StartPosition, sel.EndPosition);
        int b = Math.Max(sel.StartPosition, sel.EndPosition);

        var bloque = doc.GetRange(a, b);
        bloque.Expand(TextRangeUnit.Paragraph);

        int inicio = bloque.StartPosition;
        string antes = bloque.Text;

        // Expand se lleva tambien el salto final del ultimo parrafo, y
        // con el aparecia una linea vacia de mas al final.
        bool salto = antes.EndsWith('\r');
        if (salto) antes = antes[..^1];

        string despues = cambio(antes);
        if (despues == antes) return;

        var viejas = antes.Split('\r');
        var nuevas = despues.Split('\r');

        if (viejas.Length != nuevas.Length) return;

        doc.BatchDisplayUpdates();

        try
        {
            // De atras hacia delante: cada cambio mueve lo que viene
            // despues, y las posiciones de delante siguen valiendo.
            int desde = inicio + antes.Length;

            for (int i = viejas.Length - 1; i >= 0; i--)
            {
                desde -= viejas[i].Length;

                int comun = 0;

                while (comun < viejas[i].Length && comun < nuevas[i].Length
                       && viejas[i][^(comun + 1)] == nuevas[i][^(comun + 1)])
                {
                    comun++;
                }

                int quita = viejas[i].Length - comun;
                string pon = nuevas[i][..(nuevas[i].Length - comun)];

                if (quita > 0 || pon.Length > 0)
                    doc.GetRange(desde, desde + quita).Text = pon;

                desde--;   // el \r que separa esta linea de la anterior
            }
        }
        finally
        {
            doc.ApplyDisplayUpdates();
        }

        caja.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
    }
}
