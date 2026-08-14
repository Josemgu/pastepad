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
    /// texto mientras no elija. Decide el grupo y el icono, y nada mas
    /// —lo que hace el clic es pegar, sea del tipo que sea—.
    /// </summary>
    public string Tipo { get; }

    public bool EsImagen { get; }
    public bool Fijada { get; }

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
                // que aqui el tipo solo puede deducirse.
                Tipo = Tipos.Deducir(Texto);

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
                Tipo = Tipos.De(snippet);

                // El titulo se guarda entero y se acorta aqui, igual que
                // ya se hacia con el historial: el recorte es de pantalla
                // y no tiene por que quedarse escrito en el archivo.
                Titulo = Modelo.UnaLinea(snippet.Titulo, 80);
                if (Titulo.Length == 0) Titulo = "—";

                Detalle = EsEnlace ? Modelo.DominioDe(Texto) : snippet.Categoria;
                Icono = IconoDe(Tipo);
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

    public double Alto => Compacta ? Estilo.AltoFilaMini : Estilo.AltoFila;

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
    public string TxtEditar => Textos.T("Editar y guardar...");
    public string TxtBorrar => Textos.T("Borrar");

    public Visibility VerAbrir =>
        EsEnlace ? Visibility.Visible : Visibility.Collapsed;

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
