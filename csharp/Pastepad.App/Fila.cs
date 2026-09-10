using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Pastepad.Nucleo;

namespace Pastepad.App;

/// <summary>Lo que puede aparecer en la lista: una fila o una cabecera.</summary>
public abstract class ItemLista
{
}

/// <summary>
/// La cabecera que abre y cierra un grupo dentro de Guardados.
///
/// Los marcadores y las notas viven en la misma pestana pero no se usan
/// igual: un marcador se abre en el navegador y una nota se pega.
/// Separarlos deja encontrar cada cosa sin leerlas todas.
/// </summary>
public sealed class Grupo(string clave, string etiqueta, int cuantos,
                          bool abierto, string icono, bool enAcento) : ItemLista
{
    public string Clave { get; } = clave;
    public string Etiqueta { get; } = etiqueta;
    public string Cuantos { get; } = cuantos.ToString();
    public bool Abierto { get; } = abierto;
    public string Icono { get; } = icono;

    public string Chevron =>
        Abierto ? Estilo.Iconos.AbajoV : Estilo.Iconos.DerechaV;

    /// <summary>
    /// El grupo de marcadores lleva su icono en acento; el de notas, en
    /// medio. Es la misma senia que usa la fila de enlace.
    /// </summary>
    public Brush ColorIcono => enAcento
        ? Estilo.Pincel(Estilo.ColorAcento.Color)
        : Estilo.Pincel(Estilo.Actual.Medio);
}

/// <summary>
/// Una fila de la lista, ya resuelta a texto y color. No guarda
/// referencias a controles: la lista se reconstruye entera al
/// refrescar, y lo que cambia sin reconstruirla (activa, hover,
/// marcada) avisa por PropertyChanged.
/// </summary>
public sealed class Fila : ItemLista, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public Elemento Dato { get; }

    /// <summary>True si viene del historial; false si es un guardado.</summary>
    public bool EsHist { get; }

    public string Titulo { get; }
    public string Detalle { get; }

    /// <summary>El texto plano, para pegar, copiar o abrir.</summary>
    public string Texto { get; }

    /// <summary>
    /// El texto entero es una direccion. Es un hecho sobre lo que hay
    /// escrito, no una preferencia: por eso sigue mandando sobre si se
    /// ofrece «Abrir en el navegador» y sobre si debajo del titulo va el
    /// dominio. Un correo que el usuario haya archivado como tal se
    /// puede abrir igual si resulta que es una url.
    /// </summary>
    public bool EsEnlace { get; }

    /// <summary>
    /// Lleva [[campos]] dentro, o sea que al usarla pregunta antes de
    /// pegar. Se ve en la fila porque hasta ahora no habia forma de
    /// saber cual iba a preguntar y cual no hasta pulsarla.
    ///
    /// Tampoco depende del tipo elegido: quien archive una plantilla
    /// como correo sigue teniendo que rellenar sus campos.
    /// </summary>
    public bool EsPlantilla { get; }

    /// <summary>
    /// De que es esto: lo que eligio el usuario, o lo que se deduce del
    /// texto mientras no elija. Decide el grupo, el icono, y —solo en un
    /// guardado— si el clic abre o pega: un marcador guardado se abre en
    /// el navegador, todo lo demas se pega. El historial siempre pega,
    /// aunque lo copiado sea una direccion.
    /// </summary>
    public string Tipo { get; }

    public bool EsImagen { get; }
    public bool Fijada { get; }

    /// <summary>
    /// Es un apunte del bloc. No se pega nunca: pulsarlo lo abre para
    /// editarlo, y el menu no ofrece pegar.
    ///
    /// Lo pidio el usuario asi —«no debe tener la opcion de pegar porque
    /// son notas»— y ademas evita el problema que tendria ofrecerlo:
    /// pegar deja el panel y devuelve el foco a otra ventana, que es
    /// justo lo contrario de lo que se quiere al escribir un apunte.
    /// </summary>
    public bool EsApunte { get; }

    /// <summary>Vacio cuando la fila no lleva icono, que es lo normal.</summary>
    public string Icono { get; }

    public bool Compacta { get; }

    public Fila(Elemento dato, bool compacta)
    {
        Dato = dato;
        Compacta = compacta;

        switch (dato)
        {
            case Entrada entrada:
                EsHist = true;
                Fijada = entrada.Pin;
                EsImagen = entrada.EsImagen;

                if (entrada.EsImagen)
                {
                    Titulo = Textos.T("Imagen copiada");
                    Detalle = Textos.T("captura");
                    Texto = "";
                    Tipo = Tipos.Nota;
                    Icono = Estilo.Iconos.Imagen;
                    break;
                }

                Texto = entrada.Texto ?? "";
                EsEnlace = Modelo.EsEnlace(Texto);
                EsPlantilla = !EsEnlace && Modelo.CamposDe(Texto).Count > 0;

                // Lo que pasa por el portapapeles no lo archiva nadie, asi
                // que aqui el tipo solo puede deducirse. Y se deduce de lo
                // que ya se acaba de calcular: Tipos.Deducir(Texto) volveria
                // a recorrer el texto entero buscando lo mismo dos veces.
                Tipo = Tipos.DeducirDe(EsEnlace, EsPlantilla);

                Titulo = Modelo.UnaLinea(Texto, 80);
                if (Titulo.Length == 0) Titulo = "—";

                Detalle = EsEnlace
                    ? Modelo.DominioDe(Texto)
                    : Textos.T("%d caracteres", Texto.Length);

                Icono = IconoDe(Tipo);
                break;

            case Snippet snippet:
                Texto = Modelo.TextoDe(snippet.Runs);
                EsEnlace = Modelo.EsEnlace(Texto);
                EsPlantilla = !EsEnlace && Modelo.CamposDe(Texto).Count > 0;
                // Tipos.De(snippet) volveria a unir todos los fragmentos
                // con TextoDe —el texto ya esta arriba— y a repetir las dos
                // busquedas. Aqui se aprovecha todo lo hecho.
                Tipo = Tipos.Vale(snippet.Tipo)
                    ? snippet.Tipo!
                    : Tipos.DeducirDe(EsEnlace, EsPlantilla);

                // El titulo se guarda entero y se acorta aqui, igual que
                // ya se hacia con el historial: el recorte es de pantalla
                // y no tiene por que quedarse escrito en el archivo.
                Titulo = Modelo.UnaLinea(snippet.Titulo, 80);
                if (Titulo.Length == 0) Titulo = "—";

                Detalle = EsEnlace ? Modelo.DominioDe(Texto) : snippet.Categoria;
                Icono = IconoDe(Tipo);
                break;

            case Nota apunte:
                EsApunte = true;
                Texto = apunte.Texto;
                Vista = Modelo.Vistazo(Texto);

                Titulo = Modelo.UnaLinea(Texto, 80);
                if (Titulo.Length == 0) Titulo = "—";

                // Ni el dominio ni la carpeta: un apunte no tiene
                // ninguna de las dos. Lo util debajo del titulo es
                // cuando se toco, como en las notas rapidas de Windows.
                Detalle = Fechas.Legible(apunte.Editada, DateTimeOffset.Now);

                Tipo = Tipos.Nota;

                // Sin icono, por la misma regla que las notas guardadas:
                // en esta pestaña TODAS las filas son apuntes, asi que un
                // icono en cada una seria una columna de ruido que no
                // distingue nada.
                Icono = "";
                break;

            default:
                Titulo = "";
                Detalle = "";
                Texto = "";
                Tipo = Tipos.Nota;
                Icono = "";
                break;
        }
    }

    /// <summary>
    /// Los cuatro tipos con nombre llevan icono; la nota, ninguno. Una
    /// fila con icono en todas seria una columna de ruido, y la nota es
    /// la mayoria.
    /// </summary>
    static string IconoDe(string tipo) => tipo switch
    {
        Tipos.Marcador => Estilo.Iconos.Enlace,
        Tipos.Plantilla => Estilo.Iconos.Plantilla,
        Tipos.Correo => Estilo.Iconos.Correo,
        Tipos.Prompt => Estilo.Iconos.Prompt,
        _ => "",
    };

    // ------------------------------------------------------ el estado

    bool _activa;
    bool _encima;
    bool _marcando;
    bool _marcada;

    public bool Activa
    {
        get => _activa;
        set { if (_activa != value) { _activa = value; TodoCambio(); } }
    }

    public bool Encima
    {
        get => _encima;
        set { if (_encima != value) { _encima = value; TodoCambio(); } }
    }

    public bool Marcando
    {
        get => _marcando;
        set { if (_marcando != value) { _marcando = value; TodoCambio(); } }
    }

    public bool Marcada
    {
        get => _marcada;
        set { if (_marcada != value) { _marcada = value; TodoCambio(); } }
    }

    // ------------------------------------------------------ el aspecto

    public double Alto =>
        EsApunte ? Estilo.AltoApunte
        : Compacta ? Estilo.AltoFilaMini
        : Estilo.AltoFila;

    // ------------------------------------------ la tarjeta del apunte

    /// <summary>
    /// Las primeras lineas del apunte, con sus saltos. Vacio en todo lo
    /// que no sea un apunte: la plantilla de tarjeta solo se usa ahi.
    /// </summary>
    public string Vista { get; } = "";

    /// <summary>
    /// El acento de fondo, como en las notas rapidas de Windows. La
    /// tarjeta activa lo lleva entero y el resto rebajado, que es lo que
    /// deja ver cual esta elegida sin cambiar de color.
    /// </summary>
    public Brush FondoApunte => Estilo.Pincel(
        Estilo.ColorAcento.Color, Activa ? 1.0 : Encima ? 0.85 : 0.72);

    /// <summary>
    /// El color que la paleta tiene comprobado para escribir ENCIMA del
    /// acento. Sobre una tarjeta de acento, el color de texto normal no
    /// tiene contraste garantizado; este si.
    /// </summary>
    public Brush ColorApunte => Estilo.Pincel(Estilo.ColorAcento.Sobre);

    public Brush ColorFechaApunte =>
        Estilo.Pincel(Estilo.ColorAcento.Sobre, 0.75);

    /// <summary>
    /// 16 sin icono y 22 con el, seccion 3 de la especificacion. Cuenta
    /// el icono de tipo, no la barra de activa ni la casilla.
    /// </summary>
    public Thickness Relleno =>
        new(Icono.Length > 0 && !Marcando ? 22 : 16, 4, 8, 4);

    public Brush Fondo => Activa
        ? Estilo.Pincel(Estilo.ColorAcento.Color)
        : Estilo.Pincel(Encima ? Estilo.Actual.Hover : Estilo.Actual.Tarjeta);

    /// <summary>
    /// En tema claro la tarjeta es blanca sobre fondo casi blanco: sin
    /// borde no se despega. En oscuro sobra.
    /// </summary>
    public Brush BordeColor => Estilo.Pincel(Estilo.Actual.Borde);

    public Thickness BordeGrosor =>
        Estilo.EsClaro && !Activa ? new Thickness(1) : new Thickness(0);

    public Brush ColorTitulo => Activa
        ? Estilo.Pincel(Estilo.ColorAcento.Sobre)
        : Estilo.Pincel(Estilo.Actual.Texto);

    public Windows.UI.Text.FontWeight PesoTitulo =>
        Activa ? Microsoft.UI.Text.FontWeights.SemiBold
               : Microsoft.UI.Text.FontWeights.Normal;

    /// <summary>
    /// El dominio va en acento y no en tenue: distingue de un vistazo la
    /// fila que lleva una direccion dentro de la que lleva una carpeta o
    /// una cuenta de caracteres.
    /// </summary>
    public Brush ColorDetalle => Activa
        ? Estilo.Pincel(Estilo.ColorAcento.Sobre, 0.75)
        : Estilo.Pincel(EsEnlace ? Estilo.ColorAcento.Color : Estilo.Actual.Tenue);

    /// <summary>
    /// En acento el icono de tipo —marcador, plantilla, correo—, porque
    /// dice algo que hay que ver. El de la imagen no: ahi el icono es
    /// todo lo que hay en la fila y en acento se comeria el titulo. Era
    /// asi antes de que existieran los tipos y sigue siendolo.
    /// </summary>
    public Brush ColorIcono => Activa
        ? Estilo.Pincel(Estilo.ColorAcento.Sobre)
        : Estilo.Pincel(EsImagen
            ? Estilo.Actual.Tenue
            : Estilo.ColorAcento.Color);

    public Brush ColorCasilla => Activa
        ? Estilo.Pincel(Estilo.ColorAcento.Sobre)
        : Estilo.Pincel(Marcada ? Estilo.ColorAcento.Color : Estilo.Actual.Tenue);

    public Brush ColorAlfiler => Activa
        ? Estilo.Pincel(Estilo.ColorAcento.Sobre)
        : Estilo.Pincel(Estilo.ColorAcento.Color);

    public Brush ColorMenu => Activa
        ? Estilo.Pincel(Estilo.ColorAcento.Sobre)
        : Estilo.Pincel(Estilo.Actual.Tenue);

    /// <summary>
    /// La barra blanca de 3 px. No es adorno: es la senia de foco que
    /// no depende del color, para quien distingue mal los colores.
    /// </summary>
    public Visibility VerBarra =>
        Activa && !Marcando ? Visibility.Visible : Visibility.Collapsed;

    public Visibility VerCasilla =>
        Marcando ? Visibility.Visible : Visibility.Collapsed;

    public Visibility VerIcono =>
        !Marcando && Icono.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// El alfiler se queda visible siempre que algo este fijado: si no,
    /// habria que pasar el raton por cada fila para saber cual lo esta.
    /// </summary>
    public Visibility VerAlfiler =>
        Fijada && !Marcando ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// El boton de tres puntos solo asoma al pasar el raton o en la
    /// fila activa: con el siempre puesto la lista se ve cargada.
    /// </summary>
    public Visibility VerMenu => !Marcando && (Activa || Encima)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility VerDetalle =>
        Compacta ? Visibility.Collapsed : Visibility.Visible;

    public string GlifoCasilla =>
        Marcada ? Estilo.Iconos.CasillaMarcada : Estilo.Iconos.Casilla;

    // Los rotulos del menu de la fila. Van aqui y no como literal en
    // el XAML porque el idioma se elige en caliente y un literal en el
    // marcado no se vuelve a leer.
    public string TextoFijar =>
        Textos.T(Fijada ? "Quitar de arriba" : "Fijar arriba");

    public string TxtAbrir => Textos.T("Abrir en el navegador");
    public string TxtPegar => Textos.T("Pegar");
    public string TxtPegarPlano => Textos.T("Pegar sin formato");
    public string TxtCopiar => Textos.T("Copiar");
    public string TxtEditar =>
        Textos.T(EsApunte ? "Editar" : "Editar y guardar...");
    public string TxtBorrar => Textos.T("Borrar");

    /// <summary>
    /// Pegar y pegar sin formato. Se esconden en los apuntes, que es lo
    /// unico que separa al bloc del resto del panel.
    /// </summary>
    public Visibility VerPegar =>
        EsApunte ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// «Abrir en el navegador» tampoco: un apunte que mencione una
    /// direccion sigue siendo un apunte, y abrirlo se lleva el foco
    /// fuera igual que pegar.
    /// </summary>
    public Visibility VerAbrir =>
        EsEnlace && !EsApunte ? Visibility.Visible : Visibility.Collapsed;

    public Visibility VerFijar =>
        EsHist ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Solo se puede editar lo que es texto: una imagen del historial
    /// no tiene nada que abrir en el editor.
    /// </summary>
    public Visibility VerEditar =>
        EsImagen ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Vuelve a leer la paleta. Los pinceles de la fila se construyen
    /// desde codigo, asi que un cambio de tema de Windows no los toca
    /// solo: hay que decirselo.
    /// </summary>
    public void Refrescar() => TodoCambio();

    void TodoCambio([CallerMemberName] string? _ = null)
    {
        // Todo lo visible depende de estas cuatro banderas, asi que se
        // avisa de golpe con la cadena vacia: es lo que XAML entiende
        // como "vuelve a leerlo todo".
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
    }
}
