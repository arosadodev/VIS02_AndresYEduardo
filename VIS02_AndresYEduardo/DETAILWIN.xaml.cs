// =============================================================================
// DETAILWIN.xaml.cs
// -----------------------------------------------------------------------------
// "Code-behind" de la ventana de detalle (el visor).
//
// Funcionamiento general:
//   1. Recibe en el constructor la ruta completa del archivo a mostrar.
//   2. Lee la extension y activa el visor adecuado:
//        - Imagen    (.jpg, .png, .gif, .bmp)
//        - Multimedia(.mp4, .avi, .mp3)
//        - Texto     (.txt)
//        - Markdown  (.md)
//        - Otros     se abren con la aplicacion predeterminada de Windows.
//   3. Crea por codigo los botones de control que necesita cada visor
//      (Zoom, Play, tamano de letra...).
//   4. Copia el tema (claro/oscuro) de la ventana principal.
// =============================================================================

using System;                              // Path, Uri, etc.
using System.Diagnostics;                  // Process: abrir archivos con la app predeterminada.
using System.IO;                           // Path, File.
using System.Windows;                      // Window, Visibility, RoutedEventArgs.
using System.Windows.Controls;             // Button y otros controles.
using System.Windows.Media;                // ScaleTransform (para el zoom).
using System.Windows.Media.Imaging;        // BitmapImage (para cargar imagenes).
using Application = System.Windows.Application; // Alias para dejar claro que es la Application de WPF.
using Button = System.Windows.Controls.Button;  // Alias para dejar claro que es el Button de WPF.

namespace VIS02_AndresYEduardo
{
    /// <summary>
    /// Ventana modal que muestra el contenido de un archivo.
    /// La crea NAVEWIN cuando el usuario hace clic en un archivo del arbol.
    /// </summary>
    public partial class DETAILWIN : Window
    {
        // Ruta completa del archivo que se esta mostrando.
        private string filePath;

        // Nivel de zoom actual de las imagenes (1.0 = tamano normal).
        private double currentZoom = 1.0;

        // Tamano de letra actual del visor de texto (14 = valor inicial del estilo).
        private double currentFontSize = 14.0;

        /// <summary>
        /// Constructor. Se ejecuta ANTES de que NAVEWIN asigne la propiedad Owner,
        /// porque el objeto se crea primero y despues se configuran sus propiedades.
        /// </summary>
        /// <param name="path">Ruta completa del archivo a visualizar.</param>
        public DETAILWIN(string path)
        {
            // Carga los controles definidos en DETAILWIN.xaml.
            InitializeComponent();

            // Guarda la ruta recibida para usarla en los demas metodos.
            filePath = path;

            // El evento Loaded se dispara cuando la ventana ya esta creada y visible.
            // Se aprovecha para sincronizar el tema: en ese momento Owner ya esta
            // asignado por NAVEWIN.
            Loaded += (s, e) => SincronizarTemaConOwner();

            // Elige y prepara el visor segun la extension del archivo.
            CargarContenidoSegunExtension();

            // Esta llamada se conserva por seguridad, pero normalmente no hace nada:
            // Owner todavia es null en este punto, asi que el "if" interno no se cumple.
            SincronizarTemaConOwner();
        }

        /// <summary>
        /// Aplica a esta ventana los colores del modo (normal o noche) que tenga
        /// la ventana principal NAVEWIN.
        /// Se llama al cargar la ventana y cuando NAVEWIN cambia de tema.
        /// </summary>
        public void SincronizarTemaConOwner()
        {
            // Solo se puede sincronizar si la ventana duena es realmente NAVEWIN.
            if (Owner is NAVEWIN naveWin)
            {
                // Segun el modo, se eligen las claves de los recursos de color.
                string bgKey = naveWin.EsModoNoche ? "NocheBackgroundBrush" : "NormalBackgroundBrush";
                string fgKey = naveWin.EsModoNoche ? "NocheForegroundBrush" : "NormalForegroundBrush";
                string ctrlBgKey = naveWin.EsModoNoche ? "NocheControlBgBrush" : "NormalControlBgBrush";
                string borderKey = naveWin.EsModoNoche ? "NocheBorderBrush" : "NormalBorderBrush";

                SolidColorBrush bgBrush = (SolidColorBrush)Application.Current.Resources[bgKey];
                SolidColorBrush fgBrush = (SolidColorBrush)Application.Current.Resources[fgKey];
                SolidColorBrush ctrlBgBrush = (SolidColorBrush)Application.Current.Resources[ctrlBgKey];
                SolidColorBrush borderBrush = (SolidColorBrush)Application.Current.Resources[borderKey];

                // Aplica los colores a la ventana completa, marco y visores
                this.Background = bgBrush;
                DetailGrid.Background = bgBrush;
                MarcoContenido.BorderBrush = borderBrush;
                MarcoContenido.Background = ctrlBgBrush;

                TxtViewer.Background = ctrlBgBrush;
                TxtViewer.Foreground = fgBrush;

                MdViewer.Background = ctrlBgBrush;
                MdViewer.Foreground = fgBrush;
            }
        }

        /// <summary>
        /// Decide que visor usar segun la extension del archivo y prepara sus
        /// botones de control.
        /// </summary>
        private void CargarContenidoSegunExtension()
        {
            // Path.GetExtension devuelve la extension con punto (".jpg").
            // ToLower la pasa a minusculas para comparar sin distinguir mayusculas.
            string ext = Path.GetExtension(filePath).ToLower();

            // -----------------------------------------------------------------
            // IMAGENES
            // -----------------------------------------------------------------
            if (ext == ".jpg" || ext == ".png" || ext == ".gif" || ext == ".bmp")
            {
                // Se hace visible el visor de imagenes (los demas siguen ocultos).
                ImgViewer.Visibility = Visibility.Visible;

                // BitmapImage carga el archivo de imagen desde su ruta (Uri).
                ImgViewer.Source = new BitmapImage(new Uri(filePath));

                // Boton para acercar la imagen.
                Button btnZoomIn = new Button
                {
                    Content = "Zoom +",
                    // El estilo se busca en los recursos de esta ventana
                    // (DetailButtonStyle, definido en el XAML).
                    Style = (Style)FindResource("DetailButtonStyle"),
                    Margin = new Thickness(0, 0, 5, 0) // 5 px de separacion a la derecha
                };
                // Se suscribe el clic: cada pulsacion aumenta el zoom un 10 %.
                btnZoomIn.Click += (s, e) =>
                {
                    currentZoom += 0.1;
                    // LayoutTransform escala la imagen y tambien el espacio que ocupa.
                    ImgViewer.LayoutTransform = new ScaleTransform(currentZoom, currentZoom);
                };
                PanelControles.Children.Add(btnZoomIn); // Se agrega a la barra de botones

                // Boton para alejar la imagen.
                Button btnZoomOut = new Button
                {
                    Content = "Zoom -",
                    Style = (Style)FindResource("DetailButtonStyle"),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                btnZoomOut.Click += (s, e) =>
                {
                    // Se limita el zoom minimo a 0.2 (20 %) para evitar valores absurdos.
                    if (currentZoom > 0.2)
                    {
                        currentZoom -= 0.1;
                        ImgViewer.LayoutTransform = new ScaleTransform(currentZoom, currentZoom);
                    }
                };
                PanelControles.Children.Add(btnZoomOut);
            }
            // -----------------------------------------------------------------
            // AUDIO Y VIDEO
            // -----------------------------------------------------------------
            else if (ext == ".mp4" || ext == ".avi" || ext == ".mp3")
            {
                MediaViewer.Visibility = Visibility.Visible;
                // Source indica que archivo debe reproducir el MediaElement.
                MediaViewer.Source = new Uri(filePath);

                // Boton Play: inicia o reanuda la reproduccion.
                Button btnPlay = new Button
                {
                    Content = "Play",
                    Style = (Style)FindResource("DetailButtonStyle"),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                btnPlay.Click += (s, e) => MediaViewer.Play();
                PanelControles.Children.Add(btnPlay);

                // Boton Pausa: detiene temporalmente, conservando la posicion.
                Button btnPause = new Button
                {
                    Content = "Pausa",
                    Style = (Style)FindResource("DetailButtonStyle"),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                btnPause.Click += (s, e) => MediaViewer.Pause();
                PanelControles.Children.Add(btnPause);

                // Boton Stop: detiene y vuelve al principio.
                Button btnStop = new Button
                {
                    Content = "Stop",
                    Style = (Style)FindResource("DetailButtonStyle"),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                btnStop.Click += (s, e) => MediaViewer.Stop();
                PanelControles.Children.Add(btnStop);
            }
            // -----------------------------------------------------------------
            // TEXTO PLANO
            // -----------------------------------------------------------------
            else if (ext == ".txt")
            {
                TxtViewer.Visibility = Visibility.Visible;

                // File.ReadAllText lee TODO el archivo de una vez y lo devuelve como string.
                TxtViewer.Text = File.ReadAllText(filePath);

                // Boton para aumentar el tamano de la letra.
                Button btnFontPlus = new Button
                {
                    Content = "Texto +",
                    Style = (Style)FindResource("DetailButtonStyle"),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                btnFontPlus.Click += (s, e) =>
                {
                    currentFontSize += 2;
                    TxtViewer.FontSize = currentFontSize;
                };
                PanelControles.Children.Add(btnFontPlus);

                // Boton para reducir el tamano de la letra.
                Button btnFontMinus = new Button
                {
                    Content = "Texto -",
                    Style = (Style)FindResource("DetailButtonStyle"),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                btnFontMinus.Click += (s, e) =>
                {
                    // Limite minimo de 6 para que el texto siga siendo legible.
                    if (currentFontSize > 6)
                    {
                        currentFontSize -= 2;
                        TxtViewer.FontSize = currentFontSize;
                    }
                };
                PanelControles.Children.Add(btnFontMinus);
            }
            // -----------------------------------------------------------------
            // MARKDOWN
            // -----------------------------------------------------------------
            else if (ext == ".md")
            {
                MdViewer.Visibility = Visibility.Visible;

                // El control MdXaml recibe el texto Markdown y lo dibuja con formato
                // (titulos, negritas, listas, enlaces, etc.).
                MdViewer.Markdown = File.ReadAllText(filePath);
            }
            // -----------------------------------------------------------------
            // CUALQUIER OTRO TIPO
            // -----------------------------------------------------------------
            else
            {
                // UseShellExecute = true hace que Windows abra el archivo con el
                // programa asociado a esa extension (por ejemplo, un .pdf con el
                // lector de PDF). Despues se cierra esta ventana, porque el archivo
                // se vera en otra aplicacion.
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                this.Close();
            }
        }

        /// <summary>
        /// Evento del boton Cancelar: cierra la ventana de detalle.
        /// Gracias a IsCancel="True" en el XAML, tambien se ejecuta con la tecla Esc.
        /// </summary>
        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
