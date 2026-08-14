using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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

        // El subrayado de foco lo pinta la plantilla del TextBox con el
        // acento de **Windows**, no con el nuestro. Medido sobre la
        // captura del editor de carpeta: 491 px de #F38064 —el naranja
        // del sistema— debajo de la caja, en una aplicacion cuyo acento
        // era el menta #2DD4A7. El buscador del panel ya se enmarca en el
        // nuestro; los campos de los dialogos no.
        caja.Resources["TextControlBorderBrushFocused"] =
            Estilo.Pincel(Estilo.ColorAcento.Color);

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

    /// <summary>
    /// La caja de texto con formato.
    ///
    /// **Sobre papel blanco en todos los temas, y no sobre la tarjeta.**
    /// Los colores de un guardado son absolutos —el de fabrica es negro
    /// #000000— porque su destino es un correo, que se lee sobre blanco.
    /// Pintados sobre el fondo oscuro del panel, el texto de fabrica
    /// quedaria negro sobre #1B1B1F: 1.1:1 de contraste, o sea invisible.
    /// Aqui lo que se ve es lo que se pega, que es la razon de tener
    /// barra de formato.
    /// </summary>
    static RichEditBox CajaRica()
    {
        var blanco = new SolidColorBrush(Microsoft.UI.Colors.White);

        var caja = new RichEditBox
        {
            // Sin FontSize. Medido: con FontSize=13 el documento arrancaba
            // en 10, porque el tamaño del control va en pixeles y el del
            // documento en puntos —13 px son 9,75 pt— y el segundo se
            // quedaba con el primero. Quien manda es el formato de
            // caracter por defecto, que es Calibri 11 pt, que es lo que
            // luego viaja a Outlook.
            // La fuente si se declara aqui, y no solo en el formato de
            // caracter por defecto del documento: medido, el nombre del
            // control gana al del documento y el desplegable de la barra
            // salia en blanco porque leia una fuente que no es ninguna de
            // las suyas.
            FontFamily = new FontFamily(Config.FuenteDef),
            Background = blanco,
            BorderBrush = Estilo.Pincel(Estilo.Actual.Borde),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 8, 10, 8),

            // Eran 180 hasta la 4.3.0. Pedir el nombre en todos los
            // guardados y no solo en los enlaces cuesta 51 px de alto, y
            // con 180 aqui el boton Guardar se quedaba medio debajo del
            // pliegue en el panel de fabrica: habia que deslizar para
            // guardar. Estos 40 son justo lo que hacia falta.
            //
            // La caja se desplaza por dentro, asi que lo que se pierde no
            // es sitio para escribir: es cuanto se ve de una vez.
            MinHeight = 140,
            IsSpellCheckEnabled = false,
            TextWrapping = TextWrapping.Wrap,
            // El cursor, la seleccion y el menu del boton derecho los
            // pinta el sistema segun el tema pedido, no segun nuestro
            // fondo: sobre papel blanco tienen que ser los de claro.
            RequestedTheme = ElementTheme.Light,
        };

        // La plantilla trae sus propios fondos por estado y volvian gris
        // el papel al pasar el raton por encima.
        foreach (var clave in (string[])[
            "TextControlBackground",
            "TextControlBackgroundPointerOver",
            "TextControlBackgroundFocused"])
        {
            caja.Resources[clave] = blanco;
        }

        caja.Resources["TextControlBorderBrushFocused"] =
            Estilo.Pincel(Estilo.ColorAcento.Color);

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

        var caja = CajaRica();

        string texto = original is null ? "" : Modelo.TextoDe(original.Runs);

        // El nombre. Se pide para cualquier guardado, no solo para los
        // enlaces, y dejarlo en blanco se comporta como siempre: el
        // titulo sale de la primera linea. Asi quien guarda dos frases no
        // rellena nada de mas —que era el motivo de esconderlo— y quien
        // monta una biblioteca puede distinguir sus piezas.
        //
        // Lo pidio el usuario con un caso concreto: cinco cuerpos de
        // correo que empiezan todos por "Hi team," son cinco filas
        // identicas recortadas a 80 caracteres. La carpeta dice que son
        // cuerpos; no dice cual es cual.
        var nombre = Campo();
        nombre.PlaceholderText = Textos.T("Cómo quieres llamarlo");

        var etiquetaNombre = Etiqueta(Textos.T("Nombre"));
        etiquetaNombre.VerticalAlignment = VerticalAlignment.Center;
        etiquetaNombre.Margin = new Thickness(0);

        // De que es esto. Se propone leyendo el texto —lo que pastepad
        // hacia solo y sin poder discutirse— y el usuario lo cambia si no
        // acierta. El caso que lo pedia: un cuerpo de correo es texto
        // corriente, no hay nada dentro que lo separe de una nota, y sin
        // poder decirlo cinco correos eran cinco notas mas.
        var elegirTipo = new ComboBox
        {
            MinWidth = 112,
            Height = 28,
            MinHeight = 28,
            CornerRadius = new CornerRadius(10),
            FontSize = Estilo.TMenor,
            Padding = new Thickness(12, 0, 8, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        foreach (var t in Tipos.Todos) elegirTipo.Items.Add(RotuloTipo(t));

        // Comparte linea con la etiqueta del nombre, asi que no tiene un
        // rotulo propio que leer. Sin esto, un lector de pantalla anuncia
        // un desplegable sin decir de que — y con «Marcador» dentro, lo
        // razonable es pensar que es el nombre. La carpeta de al lado si
        // tiene su «Guardar en» delante.
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
            elegirTipo, Textos.T("Tipo"));

        // El tipo va en la MISMA linea que la etiqueta del nombre, y no
        // en una fila propia como la carpeta. Una fila entera cuesta unos
        // 40 px de alto y aqui no sobran: la 4.3.0 tuvo que recortar la
        // caja de escribir de 180 a 140 para que el boton Guardar no se
        // quedara medio debajo del pliegue. Compartir la linea de la
        // etiqueta cuesta la diferencia entre un rotulo y un desplegable,
        // que son unos 13.
        var cabeceraNombre = new Grid();
        cabeceraNombre.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cabeceraNombre.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(elegirTipo, 1);
        cabeceraNombre.Children.Add(etiquetaNombre);
        cabeceraNombre.Children.Add(elegirTipo);

        var bloqueNombre = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, Estilo.E3),
            Spacing = Estilo.E1,
        };
        bloqueNombre.Children.Add(cabeceraNombre);
        bloqueNombre.Children.Add(nombre);

        // Un titulo que es la primera linea del propio texto no es un
        // nombre: lo puso el programa por no haber otro. Se ofrece en
        // blanco para que un guardado que ya existe pueda estrenarlo, y
        // relleno cuando de verdad le pusieron uno.
        if (original is not null
            && original.Titulo != Modelo.PrimeraLinea(texto))
        {
            nombre.Text = original.Titulo;
        }

        // Mientras el usuario no toque el desplegable, el tipo sigue a lo
        // que se escribe: pegar una direccion lo pone en Marcador,
        // escribir [[campos]] en Plantilla. En cuanto lo toca, deja de
        // moverse — una propuesta que se corrige sola no es una eleccion.
        bool loEligioElUsuario = Tipos.Vale(original?.Tipo);
        bool poniendoloNosotros = false;

        void PonerTipo(string tipo)
        {
            poniendoloNosotros = true;
            elegirTipo.SelectedIndex = Math.Max(0, Array.IndexOf(Tipos.Todos, tipo));
            poniendoloNosotros = false;
        }

        elegirTipo.SelectionChanged += (_, _) =>
        {
            if (!poniendoloNosotros) loEligioElUsuario = true;
        };

        string TipoElegido() =>
            Tipos.Todos[Math.Max(0, elegirTipo.SelectedIndex)];

        // En un enlace el nombre no es opcional en la practica: la
        // primera linea ES la direccion, y una lista de direcciones no se
        // busca por nombre. La etiqueta lo dice cuando toca.
        void AjustarEtiqueta()
        {
            string plano = Formato.TextoPlano(caja).Trim();

            etiquetaNombre.Text = Modelo.EsEnlace(plano)
                ? Textos.T("Nombre del marcador")
                : Textos.T("Nombre");

            if (!loEligioElUsuario) PonerTipo(Tipos.Deducir(plano));
        }

        PonerTipo(original is not null
            ? Tipos.De(original)
            : Tipos.Deducir(texto));

        AjustarEtiqueta();
        caja.TextChanged += (_, _) => AjustarEtiqueta();

        var cancelar = Boton(Textos.T("Cancelar"), "normal");
        var guardar = Boton(Textos.T("Guardar"), "acento", 94);

        // La rejilla toma su alto natural SIEMPRE, y de recortar se
        // encarga el ScrollViewer de abajo. Antes esto solo pasaba por
        // debajo de 520 px de hueco, y por encima se le daba el alto
        // disponible para que la rejilla lo repartiera.
        //
        // Repartir no funcionaba. La caja tiene 180 de minimo, asi que su
        // fila no podia ceder ni un pixel, y lo que cedia era el PIE.
        // Medido sobre la 4.2.0 ya publicada, con el panel de fabrica y
        // un enlace escrito —que es cuando asomaba el campo del nombre—:
        // Cancelar y Guardar en 19 px de alto en vez de 32. Con el campo
        // visible siempre, eso pasaria de ser un caso raro a ser el caso
        // normal.
        //
        // Y lo que se perdia repartiendo no era nada: medido a 560 y a
        // 900 de panel, la caja daba 180 en los dos. Nunca crecio con la
        // ventana, aunque este comentario dijera que si.
        var cuerpo = CuerpoConHueco(
            double.PositiveInfinity,
            [Titulo(titulo), fila, bloqueNombre, BarraDeFormato(caja)],
            caja,
            [
                new Border { Height = Estilo.E3, Background = null },
                Nota(Textos.T(
                    "Escribe [[algo]] y el programa te lo preguntará antes de pegar")),
                Pie(cancelar, guardar),
            ]);

        var dialogo = Caja(raiz, new ScrollViewer
        {
            Content = cuerpo,
            MaxHeight = Disponible(raiz),
            // Hidden y no Auto, como en Apariencia: la barra se quedaria
            // puesta siempre y la rueda y el teclado siguen desplazando.
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
        });

        Snippet? salida = null;

        cancelar.Click += (_, _) => dialogo.Hide();

        guardar.Click += (_, _) =>
        {
            var runs = Formato.Leer(caja);

            string valor = Modelo.TextoDe(runs).Trim();
            if (valor.Length == 0) return;

            // Lo arma el nucleo porque acaba en snippets.json. Antes se
            // miraba si el campo estaba visible, porque se escondia al
            // dejar de ser un enlace y lo escrito seguia ahi. Ya no se
            // esconde nunca, asi que lo escrito es siempre lo que quiso
            // el usuario.
            string comoSeLlama = nombre.Text;

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
                runs,
                elegirCarpeta.SelectedItem as string ?? Config.CarpetaDef,
                comoSeLlama,
                TipoElegido());

            dialogo.Hide();
        };

        // El texto se carga **con el dialogo ya abierto**, no al
        // construir la caja. Medido: cargado antes, el guardado salia con
        // sus negritas pero sin sus colores —una linea guardada en
        // #C00000 se abria en negro y se guardaba en negro—, porque el
        // RichEditBox reparte su propio Foreground por el documento
        // cuando entra en el arbol visual, y eso pasa despues. Es el
        // mismo tipo de trampa que el TextBox con AcceptsReturn.
        dialogo.Opened += (_, _) =>
        {
            Formato.Cargar(caja, original?.Runs ?? []);
            AjustarEtiqueta();
            caja.Focus(FocusState.Programmatic);
        };

        await dialogo.ShowAsync();
        return salida;
    }

    /// <summary>
    /// Como se llama cada tipo en pantalla. La constante que va al
    /// archivo no se traduce nunca —cambiar de idioma dejaria los
    /// guardados clasificados con una palabra que ya no se reconoce—,
    /// igual que pasa con el nombre de la carpeta de fabrica.
    /// </summary>
    static string RotuloTipo(string tipo) => tipo switch
    {
        Tipos.Marcador => Textos.T("Marcador"),
        Tipos.Plantilla => Textos.T("Plantilla"),
        Tipos.Correo => Textos.T("Correo"),
        Tipos.Prompt => Textos.T("Prompt IA"),
        _ => Textos.T("Nota"),
    };

    // ------------------------------------------------ barra de formato

    /// <summary>
    /// Los diez colores de la paleta de texto, comprobados por calculo
    /// sobre papel blanco, que es donde acaba el correo: el peor de los
    /// diez es el oro #8A6D00 con 4.92:1, por encima del 4.5:1 que pide
    /// WCAG AA para texto normal. El mejor es el negro, con 21:1.
    /// </summary>
    static readonly (string Hex, string Nombre)[] Tintas =
    [
        ("#000000", "Negro"), ("#595959", "Gris"), ("#C00000", "Rojo"),
        ("#B45309", "Naranja"), ("#8A6D00", "Oro"), ("#2E7D32", "Verde"),
        ("#0F766E", "Turquesa"), ("#1F4E79", "Azul"), ("#6A1B9A", "Morado"),
        ("#7F4F24", "Marrón"),
    ];

    /// <summary>Un boton cuadrado de la barra.</summary>
    static Button BotonBarra(string rotulo, UIElement dentro)
    {
        var b = new Button
        {
            Content = dentro,
            // 26 y no 30: los nueve botones mas su separacion tienen que
            // caber en los 264 px utiles del dialogo con el panel en su
            // ancho de fabrica. Medido con 30 px, la barra pedia 302 y el
            // ultimo boton —quitar el formato— se quedaba fuera de la
            // vista. Con 26 y 3 de hueco son 258 y entran los nueve. Es
            // ademas la medida de los botones del pie.
            Width = 26,
            Height = 26,
            MinWidth = 26,
            MinHeight = 26,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(Estilo.RControl),
            Background = Estilo.Pincel(Estilo.Actual.Tarjeta),
            BorderBrush = Estilo.Pincel(Estilo.Actual.Borde),
            BorderThickness = new Thickness(Estilo.EsClaro ? 1 : 0),
        };

        ToolTipService.SetToolTip(b, rotulo);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(b, rotulo);

        return b;
    }

    /// <summary>
    /// La letra de un boton de la barra. N, K y S y no iconos: son las
    /// mismas letras que usa Word en español, se leen a cualquier tamaño
    /// y no dependen de que un glifo exista en la fuente de iconos —dos
    /// veces se colo un icono equivocado por darlo por bueno sin
    /// dibujarlo—.
    /// </summary>
    static TextBlock Letra(
        string texto, bool negrita = false, bool cursiva = false,
        bool subrayado = false, double tam = 14)
    {
        var t = new TextBlock
        {
            Text = texto,
            FontSize = tam,
            Foreground = Estilo.Pincel(Estilo.Actual.Texto),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        if (negrita) t.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
        if (cursiva) t.FontStyle = Windows.UI.Text.FontStyle.Italic;

        if (subrayado)
            t.TextDecorations = Windows.UI.Text.TextDecorations.Underline;

        return t;
    }

    static TextBlock Glifo(string codigo) => new()
    {
        Text = codigo,
        FontFamily = new FontFamily("Segoe Fluent Icons"),
        FontSize = 15,
        Foreground = Estilo.Pincel(Estilo.Actual.Texto),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    /// <summary>
    /// La barra de formato de la maqueta que pidio el usuario: fuente,
    /// tamaño, negrita, cursiva, subrayado, color, viñetas, numeracion,
    /// sangrias y quitar el formato.
    ///
    /// **En dos filas y con la segunda desplazable.** El panel baja hasta
    /// 300 px de ancho, que dejan 236 utiles dentro del dialogo, y once
    /// controles en una fila no caben ni de lejos. Es la misma salida que
    /// ya usan las fichas de carpeta: se desliza en horizontal en vez de
    /// apretarse hasta no poder pulsarse.
    /// </summary>
    static FrameworkElement BarraDeFormato(RichEditBox caja)
    {
        bool sincronizando = false;

        var fuentes = new ComboBox
        {
            Height = 32,
            MinHeight = 32,
            MinWidth = 110,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            CornerRadius = new CornerRadius(Estilo.RControl),
            FontSize = Estilo.TMenor,
        };

        foreach (var f in Formato.Fuentes) fuentes.Items.Add(f);
        fuentes.SelectedItem = Config.FuenteDef;
        ToolTipService.SetToolTip(fuentes, Textos.T("Fuente"));
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
            fuentes, Textos.T("Fuente"));

        var tamanos = new ComboBox
        {
            Width = 64,
            Height = 32,
            MinHeight = 32,
            CornerRadius = new CornerRadius(Estilo.RControl),
            FontSize = Estilo.TMenor,
        };

        foreach (var t in Formato.Tamanos) tamanos.Items.Add(t);
        tamanos.SelectedItem = Config.TamDef;
        ToolTipService.SetToolTip(tamanos, Textos.T("Tamaño"));
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
            tamanos, Textos.T("Tamaño"));

        fuentes.SelectionChanged += (_, _) =>
        {
            if (!sincronizando && fuentes.SelectedItem is string n)
                Formato.Fuente(caja, n);
        };

        tamanos.SelectionChanged += (_, _) =>
        {
            if (!sincronizando && tamanos.SelectedItem is int t)
                Formato.Tamano(caja, t);
        };

        var arriba = new Grid { ColumnSpacing = Estilo.E2 };
        arriba.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star),
        });
        arriba.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(tamanos, 1);
        arriba.Children.Add(fuentes);
        arriba.Children.Add(tamanos);

        // ------------------------------------------------ los botones
        var negrita = BotonBarra(Textos.T("Negrita"), Letra("N", negrita: true));
        negrita.Click += (_, _) => Formato.Negrita(caja);

        var cursiva = BotonBarra(Textos.T("Cursiva"), Letra("K", cursiva: true));
        cursiva.Click += (_, _) => Formato.Cursiva(caja);

        // Sin boton de subrayado: lo quito el usuario. El soporte de
        // Formato y la clave "u" del archivo SE QUEDAN — un guardado que
        // ya lo lleve tiene que conservarlo al abrirse y guardarse, y
        // quitar la lectura lo borraria en silencio al primer editado.
        // Lo que desaparece es la forma de ponerlo, no de conservarlo.

        // La "A" con su franja de color debajo, como en Word: dice a la
        // vez que es color de texto y cual esta puesto.
        var franja = new Border
        {
            Height = 3,
            Width = 14,
            CornerRadius = new CornerRadius(1),
            Background = Estilo.Pincel(Tintas[0].Hex),
        };

        var muestra = new StackPanel
        {
            Spacing = 1,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        muestra.Children.Add(Letra("A", tam: 13));
        muestra.Children.Add(franja);

        var color = BotonBarra(Textos.T("Color del texto"), muestra);
        color.Flyout = TintasFlyout(caja, franja);

        var vinetas = BotonBarra(Textos.T("Viñetas"), Letra("•", tam: 17));
        vinetas.Click += (_, _) => Formato.Vinetas(caja);

        var numeros = BotonBarra(Textos.T("Numeración"), Letra("1.", tam: 12));
        numeros.Click += (_, _) => Formato.Numeros(caja);

        var menos = BotonBarra(Textos.T("Menos sangría"), Glifo(""));
        menos.Click += (_, _) => Formato.Sangria(caja, false);

        var mas = BotonBarra(Textos.T("Más sangría"), Glifo(""));
        mas.Click += (_, _) => Formato.Sangria(caja, true);

        var limpiar = BotonBarra(
            Textos.T("Quitar el formato"), Glifo(Estilo.Iconos.Escoba));
        limpiar.Click += (_, _) => Formato.Limpiar(caja);

        var botones = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
        };

        foreach (var b in new[]
        {
            negrita, cursiva, color,
            vinetas, numeros, menos, mas, limpiar,
        })
        {
            botones.Children.Add(b);
        }

        var abajo = new ScrollViewer
        {
            Content = botones,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            HorizontalScrollMode = ScrollMode.Enabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollMode = ScrollMode.Disabled,
            Margin = new Thickness(0, Estilo.E1, 0, 0),
        };

        // Los desplegables siguen al cursor: con el caret dentro de una
        // palabra en Arial 14, la barra no puede seguir diciendo Calibri
        // 11 — es la diferencia entre informar y mentir.
        caja.SelectionChanged += (_, _) =>
        {
            var cf = Formato.Seleccion(caja)?.CharacterFormat;
            if (cf is null) return;

            sincronizando = true;

            try
            {
                string nombre = cf.Name;

                fuentes.SelectedItem =
                    Formato.Fuentes.Contains(nombre) ? nombre : null;

                int tam = (int)Math.Round(cf.Size);
                tamanos.SelectedItem =
                    Formato.Tamanos.Contains(tam) ? tam : (object?)null;

                franja.Background = Estilo.Pincel(Formato.AHex(cf.ForegroundColor));
            }
            finally
            {
                sincronizando = false;
            }
        };

        var todo = new StackPanel { Margin = new Thickness(0, 0, 0, Estilo.E3) };
        todo.Children.Add(arriba);
        todo.Children.Add(abajo);

        return todo;
    }

    static Flyout TintasFlyout(RichEditBox caja, Border franja)
    {
        var rejilla = new VariableSizedWrapGrid
        {
            Orientation = Orientation.Horizontal,
            MaximumRowsOrColumns = 5,
        };

        var flyout = new Flyout
        {
            Content = rejilla,
            Placement = FlyoutPlacementMode.Bottom,
        };

        foreach (var (hex, nombre) in Tintas)
        {
            var bolita = new Button
            {
                Width = 28,
                Height = 28,
                MinWidth = 28,
                MinHeight = 28,
                Margin = new Thickness(0, 0, Estilo.E1, Estilo.E1),
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(14),
                Background = Estilo.Pincel(hex),
                BorderBrush = Estilo.Pincel(Estilo.Actual.Borde),
                BorderThickness = new Thickness(1),
            };

            ToolTipService.SetToolTip(bolita, Textos.T(nombre));
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
                bolita, Textos.T(nombre));

            string cual = hex;

            bolita.Click += (_, _) =>
            {
                Formato.Color(caja, cual);
                franja.Background = Estilo.Pincel(cual);
                flyout.Hide();
            };

            rejilla.Children.Add(bolita);
        }

        return flyout;
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
    /// La carpeta entera en una sola caja, una nota por linea, cargada
    /// con lo que ya hay. Devuelve el texto que el usuario deja, o null
    /// si se cancelo.
    ///
    /// Sustituye a la lista con un lapiz por nota de la 4.0.1, que era lo
    /// que se habia entendido mal: con 3000 notas, quitar cien de una en
    /// una son trescientos clics. Aqui es el mismo gesto que "Agregar a
    /// esta carpeta" —la caja que el usuario ya conoce— pero precargada.
    ///
    /// **Sin las opciones de "Agregar".** Ahi se elige entre una nota por
    /// linea y todo junto en una sola; aqui "todo junto" convertiria 3000
    /// notas en una y no hay forma de deshacerlo. Y quitar numeracion y
    /// vinetas sirve para material recien pegado de fuera, no para lo que
    /// el propio programa guardo.
    ///
    /// La confirmacion va en bucle y no anidada: dos ContentDialog no
    /// pueden estar abiertos a la vez, asi que este se cierra, pregunta,
    /// y si el usuario se echa atras vuelve a abrirse **con lo que habia
    /// escrito**. Perder cien ediciones por haber dicho que no seria el
    /// mismo fallo que se venia a arreglar.
    /// </summary>
    public static async Task<string?> EditarCarpeta(
        XamlRoot raiz, string carpeta, Modelo.CarpetaEnLineas antes)
    {
        string valor = antes.Texto;

        while (true)
        {
            var (texto, seVan) = await PasadaCarpeta(raiz, carpeta, antes, valor);

            if (texto is null) return null;
            if (seVan == 0) return texto;

            string aviso = Textos.T(
                seVan == 1
                    ? "¿Guardar? Se eliminará %d nota de %s. Esto no se puede deshacer."
                    : "¿Guardar? Se eliminarán %d notas de %s. Esto no se puede deshacer.",
                seVan, carpeta);

            if (await Confirmar(raiz, aviso)) return texto;

            valor = texto;
        }
    }

    /// <summary>
    /// Una apertura del editor de carpeta. Devuelve lo escrito y cuantas
    /// notas desapareceran, o null si se cancelo.
    /// </summary>
    static async Task<(string? Texto, int SeVan)> PasadaCarpeta(
        XamlRoot raiz, string carpeta, Modelo.CarpetaEnLineas antes, string valor)
    {
        var caja = Campo(valor, lineas: 10, alto: 200);

        var cuenta = new TextBlock
        {
            FontSize = Estilo.TMenor,
            Foreground = Estilo.Pincel(Estilo.Actual.Tenue),
        };

        // El numero de bajas va aparte y en rojo. Es el dato por el que
        // este dialogo puede hacer daño: guardar aqui borra de golpe todo
        // lo que ya no este escrito, y eso tiene que verse antes de
        // pulsar, no despues.
        var bajas = new TextBlock
        {
            FontSize = Estilo.TMenor,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = Estilo.Pincel(Estilo.Rojo),
            TextWrapping = TextWrapping.Wrap,
            Visibility = Visibility.Collapsed,
        };

        var cancelar = Boton(Textos.T("Cancelar"), "normal");
        var guardar = Boton(Textos.T("Guardar"), "acento", 94);

        int seVan = 0;

        void Recontar()
        {
            var f = Modelo.FusionarCarpeta(antes, caja.Text, carpeta);

            seVan = f.Quitadas.Count;

            cuenta.Text = Textos.T(
                f.Resultado.Count == 1 ? "%d nota" : "%d notas", f.Resultado.Count);

            bajas.Visibility = seVan == 0 ? Visibility.Collapsed : Visibility.Visible;

            if (seVan > 0)
            {
                bajas.Text = Textos.T(
                    seVan == 1
                        ? "Se eliminará %d nota al guardar"
                        : "Se eliminarán %d notas al guardar",
                    seVan);
            }

            // Y el boton se vuelve rojo cuando guardar borra: es la misma
            // señal que ya lleva "Sí, borrar", sin depender de leer.
            guardar.Background = Estilo.Pincel(
                seVan > 0 ? Estilo.Peligro : Estilo.ColorAcento.Color);

            guardar.Foreground = seVan > 0
                ? new SolidColorBrush(Microsoft.UI.Colors.White)
                : Estilo.Pincel(Estilo.ColorAcento.Sobre);
        }

        Recontar();
        caja.TextChanged += (_, _) => Recontar();

        var abajo = new List<UIElement>
        {
            new Border { Height = Estilo.E3 },
            Nota(Textos.T("Una nota por línea. Al guardar, la carpeta "
                        + "queda como lo que dejes aquí.")),
        };

        // Solo cuando las hay: una nota al pie que casi siempre dice
        // "cero" es ruido que se deja de leer.
        if (antes.DeVariasLineas.Count > 0)
        {
            abajo.Add(Nota(Textos.T(
                "Las notas de varias líneas (%d) se quedan como están.",
                antes.DeVariasLineas.Count)));
        }

        abajo.Add(cuenta);
        abajo.Add(bajas);
        abajo.Add(Pie(cancelar, guardar));

        var cuerpo = CuerpoConHueco(
            Disponible(raiz),
            [Titulo(Textos.T("Contenido de %s", carpeta))],
            caja,
            abajo);

        var dialogo = Caja(raiz, cuerpo);

        string? salida = null;

        cancelar.Click += (_, _) => dialogo.Hide();

        guardar.Click += (_, _) =>
        {
            salida = caja.Text;
            dialogo.Hide();
        };

        dialogo.Opened += (_, _) => caja.Focus(FocusState.Programmatic);

        await dialogo.ShowAsync();

        return (salida, seVan);
    }

    // ----------------------------------------------------- apariencia

    public sealed record Preferencias(
        string Acento, string Tema, string Atajo, string Carpetas, string Idioma,
        bool AvisarNovedades);

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

        // El interruptor del aviso de versiones, visible desde el primer
        // dia que existe el aviso. Un programa que llama a casa sin que
        // se pueda decir que no es un programa que no respeta a quien lo
        // usa, y el detalle dice a donde llama.
        var novedades = new ToggleSwitch
        {
            IsOn = actual.AvisarNovedades,
            OnContent = null,
            OffContent = null,
            MinWidth = 0,
            VerticalAlignment = VerticalAlignment.Center,
        };

        novedades.Toggled += (_, _) =>
            elegido = elegido with { AvisarNovedades = novedades.IsOn };

        cuerpo.Children.Add(Ajuste(
            Textos.T("Avisar de versiones nuevas"),
            Textos.T("Comprueba una vez al día en GitHub"),
            novedades));

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
