using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Pastepad.App;

/// <summary>
/// Elige plantilla segun lo que haya en la lista: una fila, la cabecera
/// de un grupo plegable, o la tarjeta de un apunte. Van en la misma
/// lista porque tienen que desplazarse juntos; si el grupo viviera fuera
/// del ListView, al bajar por Guardados las cabeceras se quedarian
/// clavadas.
/// </summary>
public sealed partial class SelectorDeFila : DataTemplateSelector
{
    public DataTemplate? DeFila { get; set; }
    public DataTemplate? DeGrupo { get; set; }
    public DataTemplate? DeApunte { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) =>
        item is Grupo ? DeGrupo
        : item is Fila { EsApunte: true } ? DeApunte
        : DeFila;

    protected override DataTemplate? SelectTemplateCore(
        object item, DependencyObject contenedor) => SelectTemplateCore(item);
}
