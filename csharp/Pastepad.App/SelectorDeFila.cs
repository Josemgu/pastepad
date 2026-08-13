using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Pastepad.App;

/// <summary>
/// Elige plantilla segun lo que haya en la lista: una tarjeta o la
/// cabecera de un grupo plegable. Van en la misma lista porque tienen
/// que desplazarse juntos; si el grupo viviera fuera del ListView, al
/// bajar por Guardados las cabeceras se quedarian clavadas.
/// </summary>
public sealed partial class SelectorDeFila : DataTemplateSelector
{
    public DataTemplate? DeFila { get; set; }
    public DataTemplate? DeGrupo { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) =>
        item is Grupo ? DeGrupo : DeFila;

    protected override DataTemplate? SelectTemplateCore(
        object item, DependencyObject contenedor) => SelectTemplateCore(item);
}
