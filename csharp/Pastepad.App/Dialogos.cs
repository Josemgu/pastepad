using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Pastepad.Nucleo;
using Windows.System;

namespace Pastepad.App;

/// <summary>
/// Los dialogos de las maquetas 04 a 09. Se construyen a mano y no con
/// los botones de ContentDialog: las maquetas piden botones de 90x32 con
/// radio 10 y un rojo propio para Borrar, y el pie de ContentDialog no
/// deja llegar ahi sin rehacerle la plantilla entera.
/// </summary>
public static class Dialogos
{
    /// <summary>
    /// La caja de cualquiera de los seis dialogos.
    ///
    /// **Sin radio propio.** El redondeo lo pone la plantilla del
    /// ContentDialog con OverlayCornerRadius. Poner ademas el nuestro
    /// dejaba dos curvas distintas y, entre una y otra, asomaban dos
    /// cunas oscuras en las esquinas de arriba: lo que se ve ahi es la
    /// capa de humo que el dialogo pinta sobre el panel. Es el mismo
    /// defecto que ya se corrigio en el panel, y por el mismo motivo:
    /// un solo redondeo, y lo pone el sistema.
    ///
    /// **Ancho atado al del panel.** El dialogo vive dentro de la
    /// ventana y no puede desbordarla. Fijando el ancho aqui, el
    /// contenido puede llevar el mismo relleno a los dos lados y queda
    /// centrado por construccion, no por suerte.
    /// </summary>
    static ContentDialog Caja(XamlRoot raiz, UIElement contenido)
    {
        var caja = new ContentDialog
        {
            XamlRoot = raiz,
            Background = Estilo.Pincel(Estilo.Actual.Elevado),
            BorderBrush = Estilo.Pincel(Estilo.Actual.Borde),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(0),
            // Default es seguir a Windows; Light o Dark solo si el
            // usuario eligio un fondo a proposito. Mismo criterio que
            // el panel.
            RequestedTheme = Estilo.TemaPedido,
            Content = contenido,
        };

        // 8 px de aire a cada lado para que se lea como un dialogo y no
        // como otra pantalla. Con el minimo de panel (300) quedan 284,
        // que siguen dando de sobra para las etiquetas.
        double ancho = Math.Max(240, raiz.Size.Width - 16);

        caja.Resources["ContentDialogMinWidth"] = ancho;
        caja.Resources["ContentDialogMaxWidth"] = ancho;

        // El alto tambien: la plantilla trae su propio tope y, con el
        // panel estirado por encima de el, el dialogo se quedaba corto y
        // recortaba por abajo. Mismo criterio que el ancho: manda la
        // ventana, que es donde el dialogo vive.
        caja.Resources["ContentDialogMaxHeight"] = Disponible(raiz);

        return caja;
    }

    /// <summary>
    /// El alto que un dialogo puede ocupar: el del panel menos el aire de
    /// los bordes. Lo que no quepa aqui no se ve y no se alcanza.
    /// </summary>
    static double Disponible(XamlRoot raiz) => Math.Max(200, raiz.Size.Height - 16);

    static TextBlock Titulo(string texto) => new()
    {
        Text = texto,
        FontSize = Estilo.TTitulo,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        Foreground = Estilo.Pincel(Estilo.Actual.Texto),
        Margin = new Thickness(0, 0, 0, Estilo.E4),
    };

    static TextBlock Etiqueta(string texto) => new()
    {
        Text = texto,
        FontSize = Estilo.TMini,
        Foreground = Estilo.Pincel(Estilo.Actual.Medio),
        Margin = new Thickness(0, 0, 0, Estilo.E1),
    };

    static TextBlock Nota(string texto) => new()
    {
        Text = texto,
        FontSize = Estilo.TMini,
        Foreground = Estilo.Pincel(Estilo.Actual.Tenue),
        TextWrapping = TextWrapping.Wrap,
    };

    /// <summary>
    /// Un campo de texto con el aspecto de la paleta. Alto explicito:
    /// el minimo que WinUI le pone a un TextBox de una linea es 32 y las
    /// maquetas piden 36 en el de carpeta y 32 en los de campos.
    ///
    /// **El texto se pone al final, y no dentro del inicializador.** Un
    /// TextBox con AcceptsReturn en false se queda con la primera linea
    /// de lo que se le asigne, y en el inicializador Text se asignaba
    /// antes que AcceptsReturn: abrir un guardado de cien lineas en el
    /// editor cargaba una. Medido: 7990 caracteres entraban como 77, y
    /// pulsar Guardar escribia esos 77 encima de los 7990.
    /// </summary>
    static TextBox Campo(string valor = "", int lineas = 1, double alto = 36)
    {
        bool una = lineas <= 1;

        var caja = new TextBox
        {
            AcceptsReturn = !una,
            TextWrapping = una ? TextWrapping.NoWrap : TextWrapping.Wrap,
            FontSize = Estilo.TCuerpo,
            Foreground = Estilo.Pincel(Estilo.Actual.Texto),
            Background = Estilo.Pincel(Estilo.Actual.Tarjeta),
            BorderBrush = Estilo.Pincel(Estilo.Actual.Borde),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, una ? 0 : 10, 12, 0),
            MinHeight = 0,
            Height = una ? alto : double.NaN,
            VerticalContentAlignment = una
                ? VerticalAlignment.Center
                : VerticalAlignment.Top,
        };

        if (!una)
        {
            caja.MinHeight = alto;

            // La caja de varias lineas se desplaza por dentro. Sin esto
            // crecia con el texto hasta salirse de la ventana: se veia
            // el principio del texto y nada mas, y el resto no habia
            // manera de alcanzarlo.
            ScrollViewer.SetVerticalScrollBarVisibility(caja, ScrollBarVisibility.Auto);
        }

        // Ahora si: con AcceptsReturn ya puesto, el texto entra entero.
        caja.Text = valor;

        return caja;
    }

    static Button Boton(string texto, string estilo, double ancho = 90)
    {
        var b = new Button
        {
            Content = texto,
            Height = 32,
            MinHeight = 32,
            MinWidth = 0,
            Width = ancho,
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(0),
            FontSize = Estilo.TMenor,
            Padding = new Thickness(Estilo.E4, 0, Estilo.E4, 0),
        };

        switch (estilo)
        {
            case "acento":
                b.Background = Estilo.Pincel(Estilo.ColorAcento.Color);
                b.Foreground = Estilo.Pincel(Estilo.ColorAcento.Sobre);
                b.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                break;

            case "peligro":
                b.Background = Estilo.Pincel(Estilo.Peligro);
                b.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                b.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                break;

            default:
                b.Background = Estilo.Pincel(Estilo.Actual.Tarjeta);
                b.Foreground = Estilo.Pincel(Estilo.Actual.Texto);

                // En las paletas claras la tarjeta y el fondo elevado del
                // dialogo son el mismo blanco —"#FFFFFF" los dos en la
                // paleta Clara—, asi que Cancelar se quedaba sin
                // superficie y se leia como texto suelto al lado de un
                // Guardar relleno. El borde es lo que ya usan las
                // tarjetas de ajustes y las filas por este mismo motivo.
                b.BorderBrush = Estilo.Pincel(Estilo.Actual.Borde);
                b.BorderThickness = new Thickness(Estilo.EsClaro ? 1 : 0);
                break;
        }

        return b;
    }

    /// <summary>Cancelar a la izquierda, la accion a la derecha.</summary>
    static Grid Pie(Button cancelar, Button aceptar)
    {
        var pie = new Grid { Margin = new Thickness(0, Estilo.E4 + Estilo.E2, 0, 0) };
        pie.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pie.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pie.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(cancelar, 0);
        Grid.SetColumn(aceptar, 2);

        pie.Children.Add(cancelar);
        pie.Children.Add(aceptar);
        return pie;
    }

    static StackPanel Cuerpo() => new()
    {
        Spacing = 0,
        Padding = new Thickness(24, 24, 24, 24),
    };

    /// <summary>
    /// El cuerpo de los dialogos que llevan algo grande dentro —una caja
    /// de texto, una lista de carpetas—. Va en rejilla y no apilado a
    /// proposito.
    ///
    /// **Apilado, el pie se iba de la ventana.** La caja crecia con el
    /// contenido y empujaba hacia abajo todo lo que venia detras: pegar
    /// treinta lineas dejaba Cancelar y Agregar fuera de la pantalla, y
    /// sin ningun sitio por donde desplazarse hasta ellos —estos
    /// dialogos no llevaban ScrollViewer—. La unica salida era borrar
    /// texto o pulsar Escape y perder lo pegado. Medido con 60 lineas:
    /// la caja ocupaba 447 px de los 567 del panel y los dos botones no
    /// llegaban a dibujarse.
    ///
    /// Aqui lo de arriba y lo de abajo ocupan lo suyo y el hueco que
    /// sobra es del elemento del medio, que se desplaza por dentro. El
    /// pie no se mueve nunca, tenga el texto una linea o cien.
    /// </summary>
    static Grid CuerpoConHueco(
        double alto,
        IReadOnlyList<UIElement> arriba,
        FrameworkElement medio,
        IReadOnlyList<UIElement> abajo)
    {
        var rejilla = new Grid
        {
            Padding = new Thickness(24, 24, 24, 24),
            MaxHeight = alto,
        };

        void Poner(UIElement hijo, GridLength suyo)
        {
            rejilla.RowDefinitions.Add(new RowDefinition { Height = suyo });
            Grid.SetRow((FrameworkElement)hijo, rejilla.RowDefinitions.Count - 1);
            rejilla.Children.Add(hijo);
        }

        foreach (var hijo in arriba) Poner(hijo, GridLength.Auto);

        // El minimo del elemento se respeta —una caja de una linea no
        // sirve para pegar— pero la fila de estrella lo recorta cuando
        // no cabe. Medido con el panel en su alto minimo, 340: la caja
        // se queda en lo que sobra y el pie sigue dibujandose.
        medio.VerticalAlignment = VerticalAlignment.Stretch;
        Poner(medio, new GridLength(1, GridUnitType.Star));

        foreach (var hijo in abajo) Poner(hijo, GridLength.Auto);

        return rejilla;
    }

    // ------------------------------------------------------ una linea

    /// <summary>
    /// El dialogo de la maqueta 05: titulo, etiqueta, un campo, y
    /// Cancelar / Aceptar. Devuelve null si se cancelo.
    /// </summary>
    public static async Task<string?> UnaLinea(
        XamlRoot raiz, string titulo, string etiqueta, string valor = "")
    {
        var campo = Campo(valor);
        campo.SelectAll();

        var cuerpo = Cuerpo();
        cuerpo.Children.Add(Titulo(titulo));
        cuerpo.Children.Add(Etiqueta(etiqueta));
        cuerpo.Children.Add(campo);

        var cancelar = Boton(Textos.T("Cancelar"), "normal");
        var aceptar = Boton(Textos.T("Aceptar"), "acento");
        cuerpo.Children.Add(Pie(cancelar, aceptar));

        var dialogo = Caja(raiz, cuerpo);

        string? salida = null;

        cancelar.Click += (_, _) => dialogo.Hide();
        aceptar.Click += (_, _) => { salida = campo.Text.Trim(); dialogo.Hide(); };

        // Enter acepta: escribir el nombre y pulsar Enter es el gesto
        // natural en un dialogo de una sola linea.
        campo.KeyDown += (_, a) =>
        {
            if (a.Key != VirtualKey.Enter) return;
            a.Handled = true;
            salida = campo.Text.Trim();
            dialogo.Hide();
        };

        dialogo.Opened += (_, _) => campo.Focus(FocusState.Programmatic);

        await dialogo.ShowAsync();

        return string.IsNullOrEmpty(salida) ? null : salida;
    }

    // ------------------------------------------------------- confirmar

    /// <summary>La maqueta 09: aviso, Cancelar y Si, borrar en rojo.</summary>
    public static async Task<bool> Confirmar(XamlRoot raiz, string mensaje)
    {
        var cuerpo = Cuerpo();
        cuerpo.Children.Add(Titulo(Textos.T("Confirmar")));
        cuerpo.Children.Add(new TextBlock
        {
            Text = mensaje,
            FontSize = Estilo.TMenor,
            Foreground = Estilo.Pincel(Estilo.Actual.Texto),
            TextWrapping = TextWrapping.Wrap,
        });

        var cancelar = Boton(Textos.T("Cancelar"), "normal");
        var borrar = Boton(Textos.T("Sí, borrar"), "peligro", 100);
        cuerpo.Children.Add(Pie(cancelar, borrar));

        var dialogo = Caja(raiz, cuerpo);

        bool si = false;

        cancelar.Click += (_, _) => dialogo.Hide();
        borrar.Click += (_, _) => { si = true; dialogo.Hide(); };

        await dialogo.ShowAsync();
        return si;
    }

    // ---------------------------------------------------- texto nuevo

    /// <summary>
    /// La maqueta 04: carpeta, area de texto y la nota de las
    /// plantillas. Devuelve null si se cancelo.
    /// </summary>
    public static async Task<Snippet?> Texto(
        XamlRoot raiz,
        string titulo,
        IReadOnlyList<string> carpetas,
        string carpeta,
        Snippet? original = null)
    {
        var elegirCarpeta = new ComboBox
        {
            MinWidth = 120,
            Height = 28,
            MinHeight = 28,
            CornerRadius = new CornerRadius(10),
            FontSize = Estilo.TMenor,
            Padding = new Thickness(12, 0, 8, 0),
        };

        foreach (var c in carpetas) elegirCarpeta.Items.Add(c);

        // "Mis textos" no pasa por Textos.T y no es un olvido: es el
        // nombre de la carpeta que se crea, y acaba escrito en
        // snippets.json. Traducirlo partiria los datos en cuatro —cambiar
        // de idioma dejaria los textos en una carpeta que ya no se llama
        // asi—. Es un dato con valor inicial, no un rotulo.
        if (carpetas.Count == 0)
        {
            elegirCarpeta.Items.Add(Config.CarpetaDef);
            carpeta = Config.CarpetaDef;
        }

        elegirCarpeta.SelectedItem = carpetas.Contains(carpeta)
            ? carpeta
            : elegirCarpeta.Items[0];

        var fila = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Estilo.E3,
            Margin = new Thickness(0, 0, 0, Estilo.E3),
        };
        fila.Children.Add(new TextBlock
        {
            Text = Textos.T("Guardar en"),
            FontSize = Estilo.TMini,
            Foreground = Estilo.Pincel(Estilo.Actual.Medio),
            VerticalAlignment = VerticalAlignment.Center,
        });
        fila.Children.Add(elegirCarpeta);

        string texto = original is null ? "" : Modelo.TextoDe(original.Runs);
        var caja = Campo(texto, lineas: 10, alto: 220);

        // El nombre del marcador. Solo asoma cuando lo que hay escrito es
        // un enlace: en un texto normal la primera linea ya hace de
        // titulo y pedirlo aparte seria un campo mas que rellenar para
        // guardar dos frases.
        var nombre = Campo();
        nombre.PlaceholderText = Textos.T("Cómo quieres llamarlo");

        var bloqueNombre = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, Estilo.E3),
            Visibility = Visibility.Collapsed,
        };
        bloqueNombre.Children.Add(Etiqueta(Textos.T("Nombre del marcador")));
        bloqueNombre.Children.Add(nombre);

        // Un titulo que es el propio enlace no es un titulo: lo puso el
        // programa por no haber otro. Se ofrece en blanco para que el
        // marcador que ya existe pueda estrenar nombre.
        if (original is not null
            && original.Titulo != Modelo.PrimeraLinea(texto))
        {
            nombre.Text = original.Titulo;
        }

        void MirarSiEsEnlace()
        {
            bloqueNombre.Visibility = Modelo.EsEnlace(caja.Text.Trim())
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        MirarSiEsEnlace();
        caja.TextChanged += (_, _) => MirarSiEsEnlace();

        var cancelar = Boton(Textos.T("Cancelar"), "normal");
        var guardar = Boton(Textos.T("Guardar"), "acento", 94);

        var cuerpo = CuerpoConHueco(
            Disponible(raiz),
            [Titulo(titulo), fila, bloqueNombre],
            caja,
            [
                new Border { Height = Estilo.E3, Background = null },
                Nota(Textos.T(
                    "Escribe [[algo]] y el programa te lo preguntará antes de pegar")),
                Pie(cancelar, guardar),
            ]);

        var dialogo = Caja(raiz, cuerpo);

        Snippet? salida = null;

        cancelar.Click += (_, _) => dialogo.Hide();

        guardar.Click += (_, _) =>
        {
            string valor = caja.Text.Trim();
            if (valor.Length == 0) return;

            // Lo arma el nucleo porque acaba en snippets.json. Del
            // nombre solo se hace caso si hay enlace: si el usuario
            // escribio uno y luego cambio el texto, el campo se escondio
            // pero lo escrito sigue ahi.
            string comoSeLlama =
                bloqueNombre.Visibility == Visibility.Visible ? nombre.Text : "";

            // Guardar sin haber tocado el texto no puede cambiar el
            // titulo. Sin esto, abrir un guardado con nombre propio y
            // pulsar Guardar se lo llevaba por delante y lo dejaba en la
            // primera linea del texto.
            if (comoSeLlama.Trim().Length == 0
                && original is not null
                && Modelo.NormalizarSaltos(valor)
                   == Modelo.NormalizarSaltos(texto).Trim())
            {
                comoSeLlama = original.Titulo;
            }

            salida = Modelo.CrearSnippet(
                valor,
                elegirCarpeta.SelectedItem as string ?? Config.CarpetaDef,
                comoSeLlama);

            dialogo.Hide();
        };

        dialogo.Opened += (_, _) => caja.Focus(FocusState.Programmatic);

        await dialogo.ShowAsync();
        return salida;
    }

    // --------------------------------------------------------- campos

    /// <summary>
    /// La maqueta 06: un campo por cada [[marcador]] de la plantilla.
    /// Devuelve null si se cancelo.
    /// </summary>
    public static async Task<Dictionary<string, string>?> Campos(
        XamlRoot raiz, IReadOnlyList<string> campos)
    {
        var cuerpo = Cuerpo();
        cuerpo.Children.Add(Titulo(Textos.T("Completar antes de pegar")));

        var cajas = new Dictionary<string, TextBox>();

        foreach (var nombre in campos)
        {
            cuerpo.Children.Add(Etiqueta(nombre));

            var caja = Campo(alto: 32);
            caja.Margin = new Thickness(0, 0, 0, Estilo.E3);

            cajas[nombre] = caja;
            cuerpo.Children.Add(caja);
        }

        var cancelar = Boton(Textos.T("Cancelar"), "normal");
        var pegar = Boton(Textos.T("Pegar"), "acento", 82);
        cuerpo.Children.Add(Pie(cancelar, pegar));

        var dialogo = Caja(raiz, cuerpo);

        Dictionary<string, string>? salida = null;

        cancelar.Click += (_, _) => dialogo.Hide();

        pegar.Click += (_, _) =>
        {
            salida = cajas.ToDictionary(p => p.Key, p => p.Value.Text);
            dialogo.Hide();
        };

        dialogo.Opened += (_, _) =>
        {
            if (cajas.Count > 0)
                cajas.Values.First().Focus(FocusState.Programmatic);
        };

        await dialogo.ShowAsync();
        return salida;
    }

    // ---------------------------------------------------- lista masiva

    /// <summary>
    /// La maqueta 07: pegar varias lineas y crear una nota por cada una
    /// o una sola con todo. Devuelve null si se cancelo.
    /// </summary>
    public static async Task<List<string>?> Lista(XamlRoot raiz, string carpeta)
    {
        var caja = Campo(lineas: 8, alto: 150);

        var porLinea = new RadioButton
        {
            Content = Textos.T("Una nota por cada línea"),
            IsChecked = true,
            FontSize = Estilo.TMenor,
            MinHeight = 26,
        };

        var juntas = new RadioButton
        {
            Content = Textos.T("Todo junto en una sola nota"),
            FontSize = Estilo.TMenor,
            MinHeight = 26,
        };

        var limpiar = new CheckBox
        {
            Content = Textos.T("Quitar numeración y viñetas"),
            IsChecked = true,
            FontSize = Estilo.TMenor,
            MinHeight = 26,
        };

        var cuenta = new TextBlock
        {
            Text = Textos.T("%d notas", 0),
            FontSize = Estilo.TMenor,
            Foreground = Estilo.Pincel(Estilo.Actual.Tenue),
            VerticalAlignment = VerticalAlignment.Center,
        };

        List<string> Trocear()
        {
            string bruto = caja.Text;
            if (bruto.Trim().Length == 0) return [];

            if (juntas.IsChecked == true) return [bruto.Trim()];

            var salida = new List<string>();

            // Las lineas las parte el nucleo: lo que devuelve el TextBox
            // viene separado por \r a secas y partirlo por \n dejaba las
            // sesenta lineas convertidas en una sola nota.
            foreach (var linea in Modelo.LineasDe(bruto))
            {
                string l = limpiar.IsChecked == true ? SinVineta(linea) : linea;
                if (l.Length > 0) salida.Add(l);
            }

            return salida;
        }

        void Recontar()
        {
            int n = Trocear().Count;
            cuenta.Text = Textos.T(n == 1 ? "%d nota" : "%d notas", n);
        }

        caja.TextChanged += (_, _) => Recontar();
        porLinea.Checked += (_, _) => Recontar();
        juntas.Checked += (_, _) => Recontar();
        limpiar.Checked += (_, _) => Recontar();
        limpiar.Unchecked += (_, _) => Recontar();

        var cancelar = Boton(Textos.T("Cancelar"), "normal");
        var agregar = Boton(Textos.T("Agregar"), "acento", 86);

        var cuerpo = CuerpoConHueco(
            Disponible(raiz),
            [Titulo(Textos.T("Agregar a %s", carpeta))],
            caja,
            [
                new Border { Height = Estilo.E3 },
                porLinea,
                juntas,
                limpiar,
                cuenta,
                Pie(cancelar, agregar),
            ]);

        var dialogo = Caja(raiz, cuerpo);

        List<string>? salida = null;

        cancelar.Click += (_, _) => dialogo.Hide();

        agregar.Click += (_, _) =>
        {
            var trozos = Trocear();
            if (trozos.Count == 0) return;

            salida = trozos;
            dialogo.Hide();
        };

        dialogo.Opened += (_, _) => caja.Focus(FocusState.Programmatic);

        await dialogo.ShowAsync();
        return salida;
    }

    /// <summary>Quita "1. ", "- ", "• " y demas del principio.</summary>
    static string SinVineta(string linea)
    {
        int i = 0;

        while (i < linea.Length && char.IsDigit(linea[i])) i++;

        if (i > 0 && i < linea.Length && (linea[i] == '.' || linea[i] == ')'))
            return linea[(i + 1)..].TrimStart();

        if (linea.Length > 1 && (linea[0] is '-' or '*' or '•' or '·'))
            return linea[1..].TrimStart();

        return linea;
    }

    // ------------------------------------------------------- carpetas

    /// <summary>
    /// Lo que el usuario decidio sobre una carpeta: como se llama ahora
    /// y si se va. Nombre es como se llamaba al abrir el dialogo, que es
    /// por donde el almacen la encuentra.
    /// </summary>
    public sealed record CambioCarpeta(string Nombre, string Nuevo, bool Quitada);

    /// <summary>Un boton de solo icono, para las acciones de una fila.</summary>
    static Button IconoFila(string glifo, string rotulo, string color)
    {
        var b = new Button
        {
            Content = new TextBlock
            {
                Text = glifo,
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 14,
                Foreground = Estilo.Pincel(color),
            },
            Width = 32,
            Height = 32,
            MinWidth = 32,
            MinHeight = 32,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(Estilo.RControl),
            Background = Estilo.Pincel(Estilo.Actual.Tarjeta),
            BorderBrush = Estilo.Pincel(Estilo.Actual.Borde),
            // Igual que el boton secundario: en claro la tarjeta es del
            // mismo blanco que el dialogo y sin borde no se ve.
            BorderThickness = new Thickness(Estilo.EsClaro ? 1 : 0),
        };

        ToolTipService.SetToolTip(b, rotulo);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(b, rotulo);

        return b;
    }

    /// <summary>
    /// Editar las carpetas: renombrarlas y quitar las que sobran, todas
    /// en la misma pantalla.
    ///
    /// Existe porque no habia forma de llegar a esto. Renombrar y
    /// eliminar estaban en el desplegable de carpetas, pero solo de la
    /// carpeta que estuviera puesta: con "Todas las carpetas" —que es lo
    /// que hay al abrir— no salia ninguna de las dos, y desde que se
    /// elige una hay que volver a abrir el desplegable para verlas. El
    /// usuario lo conto como que las carpetas no tienen boton de editar.
    ///
    /// Nada se pierde hasta pulsar Guardar: quitar marca la fila y se
    /// puede deshacer. Es lo que hacia la version anterior, y un clic de
    /// mas no cuesta nada.
    /// </summary>
    public static async Task<List<CambioCarpeta>?> Carpetas(
        XamlRoot raiz, IReadOnlyList<(string Nombre, int Cuantos)> carpetas)
    {
        var campos = new List<(string Nombre, TextBox Campo)>();
        var quitadas = new HashSet<string>();

        var lista = new StackPanel { Spacing = Estilo.E1 };

        foreach (var (nombre, cuantos) in carpetas)
        {
            var campo = Campo(nombre, alto: 32);
            campos.Add((nombre, campo));

            var cuenta = new TextBlock
            {
                Text = Textos.T(cuantos == 1 ? "%d texto" : "%d textos", cuantos),
                FontSize = Estilo.TMini,
                Foreground = Estilo.Pincel(Estilo.Actual.Tenue),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(Estilo.E2, 0, Estilo.E2, 0),
            };

            var quitar = IconoFila(
                Estilo.Iconos.Papelera, Textos.T("Quitar"), Estilo.Rojo);

            var fila = new Grid();
            fila.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
            });
            fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(cuenta, 1);
            Grid.SetColumn(quitar, 2);

            fila.Children.Add(campo);
            fila.Children.Add(cuenta);
            fila.Children.Add(quitar);

            string cual = nombre;

            quitar.Click += (_, _) =>
            {
                bool fuera = !quitadas.Remove(cual);
                if (fuera) quitadas.Add(cual);

                campo.IsEnabled = !fuera;
                fila.Opacity = fuera ? 0.45 : 1.0;

                if (quitar.Content is TextBlock icono)
                {
                    icono.Text = fuera
                        ? Estilo.Iconos.Deshacer
                        : Estilo.Iconos.Papelera;

                    icono.Foreground = Estilo.Pincel(
                        fuera ? Estilo.Actual.Medio : Estilo.Rojo);
                }

                ToolTipService.SetToolTip(
                    quitar, Textos.T(fuera ? "Recuperar" : "Quitar"));
            };

            lista.Children.Add(fila);
        }

        if (carpetas.Count == 0)
            lista.Children.Add(Nota(Textos.T("Todavía no hay carpetas.")));

        var cancelar = Boton(Textos.T("Cancelar"), "normal");
        var guardar = Boton(Textos.T("Guardar"), "acento", 94);

        // La lista va dentro del hueco que sobra: con veinte carpetas se
        // desplaza por dentro y el pie sigue donde estaba. La barra, en
        // Hidden como en el resto de los dialogos; medido, la rueda y el
        // teclado siguen desplazando.
        var rodillo = new ScrollViewer
        {
            Content = lista,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
        };

        var cuerpo = CuerpoConHueco(
            Disponible(raiz),
            [Titulo(Textos.T("Carpetas"))],
            rodillo,
            [
                new Border { Height = Estilo.E3 },
                Nota(Textos.T("Al guardar se eliminan las carpetas quitadas "
                            + "y los textos que tengan dentro.")),
                Pie(cancelar, guardar),
            ]);

        var dialogo = Caja(raiz, cuerpo);

        List<CambioCarpeta>? salida = null;

        cancelar.Click += (_, _) => dialogo.Hide();

        guardar.Click += (_, _) =>
        {
            var cambios = new List<CambioCarpeta>();

            foreach (var (nombre, campo) in campos)
            {
                bool fuera = quitadas.Contains(nombre);
                string nuevo = campo.Text.Trim();

                // Un nombre en blanco no borra la carpeta: eso se pide
                // con la papelera, que si avisa de lo que se lleva por
                // delante.
                if (nuevo.Length == 0) nuevo = nombre;

                if (!fuera && nuevo == nombre) continue;

                cambios.Add(new CambioCarpeta(nombre, nuevo, fuera));
            }

            salida = cambios;
            dialogo.Hide();
        };

        await dialogo.ShowAsync();
        return salida;
    }

    /// <summary>
    /// Lo que hay dentro de una carpeta, para elegir que se toca.
    /// Devuelve el texto elegido, o null si se cerro sin elegir.
    ///
    /// Es una lista y no un editor: el editor ya existe —el mismo
    /// dialogo de Editar texto— y dos ContentDialog no pueden estar
    /// abiertos a la vez, asi que este se cierra y abre aquel. El
    /// usuario pidio llegar desde el menu de la carpeta a "actualizar
    /// alguna informacion" de dentro, y hasta ahora habia que salir del
    /// menu, filtrar por la carpeta y buscar la fila.
    /// </summary>
    public static async Task<Snippet?> Contenido(
        XamlRoot raiz, string carpeta, IReadOnlyList<Snippet> textos)
    {
        var lista = new StackPanel { Spacing = Estilo.E1 };

        Snippet? elegido = null;
        ContentDialog? dialogo = null;

        foreach (var texto in textos)
        {
            var nombre = new TextBlock
            {
                Text = Modelo.UnaLinea(texto.Titulo, 60),
                FontSize = Estilo.TMenor,
                Foreground = Estilo.Pincel(Estilo.Actual.Texto),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var editar = IconoFila(
                Estilo.Iconos.Editar, Textos.T("Editar texto"),
                Estilo.Actual.Medio);

            var fila = new Grid
            {
                Background = Estilo.Pincel(Estilo.Actual.Tarjeta),
                BorderBrush = Estilo.Pincel(Estilo.Actual.Borde),
                BorderThickness = new Thickness(Estilo.EsClaro ? 1 : 0),
                CornerRadius = new CornerRadius(Estilo.RControl),
                Padding = new Thickness(Estilo.E3, 0, Estilo.E1, 0),
                Height = 40,
            };

            fila.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
            });
            fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(editar, 1);
            fila.Children.Add(nombre);
            fila.Children.Add(editar);

            var cual = texto;

            editar.Click += (_, _) =>
            {
                elegido = cual;
                dialogo?.Hide();
            };

            lista.Children.Add(fila);
        }

        if (textos.Count == 0)
            lista.Children.Add(Nota(Textos.T("La carpeta está vacía.")));

        var cerrar = Boton(Textos.T("Cerrar"), "normal");

        var pie = new Grid { Margin = new Thickness(0, Estilo.E4 + Estilo.E2, 0, 0) };
        pie.Children.Add(cerrar);
        cerrar.HorizontalAlignment = HorizontalAlignment.Right;

        var cuerpo = CuerpoConHueco(
            Disponible(raiz),
            [Titulo(Textos.T("Contenido de %s", carpeta))],
            new ScrollViewer
            {
                Content = lista,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            },
            [pie]);

        dialogo = Caja(raiz, cuerpo);

        cerrar.Click += (_, _) => dialogo.Hide();

        await dialogo.ShowAsync();
        return elegido;
    }

    // ----------------------------------------------------- apariencia

    public sealed record Preferencias(
        string Acento, string Tema, string Atajo, string Carpetas, string Idioma);

    /// <summary>
    /// Una fila de ajuste al estilo de la Configuracion de Windows 11:
    /// rotulo arriba, control debajo, sobre una tarjeta. Es el lenguaje
    /// al que se parece el resto del panel, y deja leer el dialogo de
    /// arriba abajo sin buscar donde esta cada cosa.
    ///
    /// **Siempre apilado, tambien con el panel ancho.** Hubo un intento
    /// de ponerlo en dos columnas cuando cabia, y se retiro despues de
    /// medirlo: la fila de dos columnas no entra sin romper algo hasta
    /// un panel de unos 455, muy por encima de los 380 de fabrica.
    /// Por debajo de eso, o el rotulo se parte en dos y tres lineas, o
    /// el ComboBox recorta su valor **por la izquierda** —"l + Shift +
    /// V" en vez de "Ctrl + Shift + V"—, que es perder el dato sin
    /// avisar. Una sola disposicion en todo el rango 300-720 es ademas
    /// una clase entera de fallos que no hay que volver a comprobar a
    /// siete anchos distintos.
    /// </summary>
    static Border Ajuste(string rotulo, string? detalle, FrameworkElement control)
    {
        var texto = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

        // El rotulo se parte en varias lineas antes que cortarse: con el
        // panel en su ancho minimo, 300, la columna de la etiqueta se
        // queda en unos 70 px y "Atajo para abrir" no cabe de una pieza.
        // Una etiqueta en dos lineas se lee; una cortada, no.
        texto.Children.Add(new TextBlock
        {
            Text = rotulo,
            FontSize = Estilo.TCuerpo,
            Foreground = Estilo.Pincel(Estilo.Actual.Texto),
            TextWrapping = TextWrapping.WrapWholeWords,
        });

        if (detalle is not null)
        {
            texto.Children.Add(new TextBlock
            {
                Text = detalle,
                FontSize = Estilo.TMini,
                Foreground = Estilo.Pincel(Estilo.Actual.Medio),
                TextWrapping = TextWrapping.Wrap,
            });
        }

        // El control se estira a todo el ancho de la tarjeta, asi que
        // pierde el ancho fijo con el que viene.
        control.Width = double.NaN;
        control.HorizontalAlignment = HorizontalAlignment.Stretch;

        texto.Margin = new Thickness(0, 0, 0, Estilo.E2);

        var dentro = new StackPanel();
        dentro.Children.Add(texto);
        dentro.Children.Add(control);

        return new Border
        {
            Background = Estilo.Pincel(Estilo.Actual.Tarjeta),
            BorderBrush = Estilo.Pincel(Estilo.Actual.Borde),
            BorderThickness = new Thickness(Estilo.EsClaro ? 1 : 0),
            CornerRadius = new CornerRadius(Estilo.RControl),
            Padding = new Thickness(Estilo.E4, Estilo.E3, Estilo.E4, Estilo.E3),
            Margin = new Thickness(0, 0, 0, Estilo.E1),
            MinHeight = 52,
            Child = dentro,
        };
    }

    /// <summary>El rotulo de un grupo de ajustes.</summary>
    static TextBlock Seccion(string texto, bool primera = false) => new()
    {
        Text = texto,
        FontSize = Estilo.TMenor,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        Foreground = Estilo.Pincel(Estilo.Actual.Medio),
        Margin = new Thickness(0, primera ? 0 : Estilo.E4 + Estilo.E1, 0, Estilo.E2),
    };

    /// <summary>
    /// 190 es lo que pide el valor mas largo que puede salir dentro,
    /// "Ctrl + Shift + Espacio", con su flecha y su relleno.
    /// </summary>
    static ComboBox Desplegable(int ancho = 190) => new()
    {
        Width = ancho,
        Height = 32,
        MinHeight = 32,
        CornerRadius = new CornerRadius(Estilo.RControl),
        FontSize = Estilo.TMenor,
        VerticalAlignment = VerticalAlignment.Center,
    };

    /// <summary>
    /// La maqueta 08, reorganizada. Tres grupos, en el orden en que se
    /// tocan: lo que se ve, como se ordenan las carpetas, y lo que
    /// depende del sistema.
    ///
    /// Ya no hay selector de tamano: el panel se estira arrastrando sus
    /// bordes y se acuerda solo, asi que unos preajustes fijos al lado
    /// de algo que ya se adapta sobran y confunden.
    /// </summary>
    public static async Task<Preferencias?> Apariencia(
        XamlRoot raiz, Preferencias actual)
    {
        var elegido = actual;

        var cuerpo = Cuerpo();

        cuerpo.Children.Add(Titulo(Textos.T("Apariencia")));

        // ------------------------------------------------------ colores
        cuerpo.Children.Add(Seccion(Textos.T("Color de acento"), primera: true));

        var bolitas = new VariableSizedWrapGrid
        {
            Orientation = Orientation.Horizontal,
            MaximumRowsOrColumns = 9,
            Margin = new Thickness(0, 0, 0, Estilo.E1),
        };

        var marcas = new Dictionary<string, TextBlock>();

        foreach (var (nombre, par) in Estilo.Acentos)
        {
            var marca = new TextBlock
            {
                Text = Estilo.Iconos.Marca,
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 13,
                Foreground = Estilo.Pincel(par.Sobre),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = nombre == actual.Acento
                    ? Visibility.Visible
                    : Visibility.Collapsed,
            };

            marcas[nombre] = marca;

            var bolita = new Button
            {
                Width = 34,
                Height = 34,
                MinWidth = 34,
                MinHeight = 34,
                Margin = new Thickness(0, 0, Estilo.E2, Estilo.E2),
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(17),
                BorderThickness = new Thickness(0),
                Background = Estilo.Pincel(par.Color),
                Content = marca,
                Tag = nombre,
            };

            // El globo y el nombre accesible, del mismo texto: una bolita
            // de color sin rotulo no se puede elegir de oido.
            ToolTipService.SetToolTip(bolita, nombre);
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(bolita, nombre);

            bolita.Click += (r, _) =>
            {
                if (r is not Button b || b.Tag is not string cual) return;

                foreach (var m in marcas) m.Value.Visibility = Visibility.Collapsed;
                marcas[cual].Visibility = Visibility.Visible;

                elegido = elegido with { Acento = cual };
            };

            bolitas.Children.Add(bolita);
        }

        cuerpo.Children.Add(bolitas);

        var fondos = Desplegable();

        foreach (var clave in Estilo.Temas.Keys)
            fondos.Items.Add(Textos.T(Estilo.NombreTema(clave)));

        fondos.SelectedItem = Textos.T(Estilo.NombreTema(actual.Tema));

        fondos.SelectionChanged += (_, _) =>
        {
            foreach (var clave in Estilo.Temas.Keys)
            {
                if (Textos.T(Estilo.NombreTema(clave))
                    == fondos.SelectedItem as string)
                {
                    elegido = elegido with { Tema = clave };
                    return;
                }
            }
        };

        cuerpo.Children.Add(Ajuste(
            Textos.T("Fondo"),
            null,
            fondos));

        // ------------------------------------------------------ carpetas
        cuerpo.Children.Add(Seccion(Textos.T("Carpetas")));

        var modos = Desplegable();
        modos.Items.Add(Textos.T("Lista desplegable"));
        modos.Items.Add(Textos.T("Fichas en fila"));
        modos.SelectedIndex = actual.Carpetas == "fichas" ? 1 : 0;

        modos.SelectionChanged += (_, _) =>
            elegido = elegido with
            {
                Carpetas = modos.SelectedIndex == 1 ? "fichas" : "menu",
            };

        cuerpo.Children.Add(Ajuste(
            Textos.T("Cómo se enseñan"),
            null,
            modos));

        // ------------------------------------------------------- sistema
        cuerpo.Children.Add(Seccion(Textos.T("Sistema")));

        var atajos = Desplegable();

        foreach (var par in Config.Atajos) atajos.Items.Add(par.Value);

        atajos.SelectedItem = Config.Atajos.TryGetValue(actual.Atajo, out var legible)
            ? legible
            : Config.Atajos[Config.AtajoDef];

        atajos.SelectionChanged += (_, _) =>
        {
            foreach (var par in Config.Atajos)
            {
                if (par.Value == atajos.SelectedItem as string)
                {
                    elegido = elegido with { Atajo = par.Key };
                    return;
                }
            }
        };

        cuerpo.Children.Add(
            Ajuste(Textos.T("Atajo para abrir"), null, atajos));

        var lenguas = Desplegable();

        foreach (var nombre in Textos.Nombres.Values) lenguas.Items.Add(nombre);

        lenguas.SelectedItem =
            Textos.Nombres.TryGetValue(actual.Idioma, out var comoSeLlama)
                ? comoSeLlama
                : Textos.Nombres[Textos.IdiomaDef];

        lenguas.SelectionChanged += (_, _) =>
        {
            foreach (var (codigo, nombre) in Textos.Nombres)
            {
                if (nombre == lenguas.SelectedItem as string)
                {
                    elegido = elegido with { Idioma = codigo };
                    return;
                }
            }
        };

        cuerpo.Children.Add(Ajuste(Textos.T("Idioma"), null, lenguas));

        var cancelar = Boton(Textos.T("Cancelar"), "normal");
        var aplicar = Boton(Textos.T("Aplicar"), "acento", 94);
        cuerpo.Children.Add(Pie(cancelar, aplicar));

        // Hidden y no Auto: con Auto la barra se queda puesta mientras
        // el contenido no quepa, que es siempre en este dialogo. Medido:
        // seguia ahi 5,5 s despues de abrir, sin tocar nada. Probado
        // tambien con ScrollView, el control nuevo, y es peor: ademas de
        // quedarse, su barra ocupa ancho real y estrechaba el contenido.
        //
        // Con Hidden **el desplazamiento se conserva** —rueda, teclado y
        // tactil siguen funcionando, lo dice la tabla de visibilidad de
        // la documentacion— y lo unico que desaparece es la barra.
        //
        // El alto se ata al del panel y no a un numero fijo: el dialogo
        // vive dentro de la ventana y no puede ser mas alto que ella.
        var dialogo = Caja(raiz, new ScrollViewer
        {
            Content = cuerpo,
            MaxHeight = Math.Max(200, raiz.Size.Height - 16),
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
        });

        Preferencias? salida = null;

        cancelar.Click += (_, _) => dialogo.Hide();
        aplicar.Click += (_, _) => { salida = elegido; dialogo.Hide(); };

        await dialogo.ShowAsync();
        return salida;
    }
}
