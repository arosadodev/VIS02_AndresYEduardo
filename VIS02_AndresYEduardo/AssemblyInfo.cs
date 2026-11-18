using System.Windows;

// ThemeInfo es un atributo a nivel de ensamblado que le dice a WPF donde buscar
// los diccionarios de recursos (ResourceDictionary) que definen el aspecto de
// los controles. Aqui se usan los valores predeterminados de una app WPF.
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,            // Diccionarios de temas especificos: no hay carpeta aparte.
                                                // Se usaria si no se encuentra un recurso en la pagina
                                                // ni en los diccionarios de recursos de la aplicacion.
    ResourceDictionaryLocation.SourceAssembly   // Diccionario de recursos generico: dentro de este ensamblado.
                                                // Se usa si el recurso no esta en la pagina ni en la app.
)]
