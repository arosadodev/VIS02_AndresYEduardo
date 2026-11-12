using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

namespace VIS02_AndresYEduardo
{
    public partial class DETAILWIN : Window
    {
        private string filePath;
        private double currentZoom = 1.0;
        private double currentFontSize = 14.0;

        public DETAILWIN(string path)
        {
            InitializeComponent();
            filePath = path;
            Loaded += (s, e) => SincronizarTemaConOwner();
            CargarContenidoSegunExtension();
            SincronizarTemaConOwner();
        }

        public void SincronizarTemaConOwner()
        {
            if (Owner is NAVEWIN naveWin)
            {
                string bgKey = naveWin.EsModoNoche ? "NocheBackgroundBrush" : "NormalBackgroundBrush";
                string fgKey = naveWin.EsModoNoche ? "NocheForegroundBrush" : "NormalForegroundBrush";
                string ctrlBgKey = naveWin.EsModoNoche ? "NocheControlBgBrush" : "NormalControlBgBrush";

                DetailGrid.Background = (SolidColorBrush)Application.Current.Resources[bgKey];
                TxtViewer.Background = (SolidColorBrush)Application.Current.Resources[ctrlBgKey];
                TxtViewer.Foreground = (SolidColorBrush)Application.Current.Resources[fgKey];
            }
        }

        private void CargarContenidoSegunExtension()
        {
            string ext = Path.GetExtension(filePath).ToLower();

            if (ext == ".jpg" || ext == ".png" || ext == ".gif" || ext == ".bmp")
            {
                ImgViewer.Visibility = Visibility.Visible;
                ImgViewer.Source = new BitmapImage(new Uri(filePath));

                Button btnZoomIn = new Button
                {
                    Content = "Zoom +",
                    Style = (Style)FindResource("DetailButtonStyle"),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                btnZoomIn.Click += (s, e) =>
                {
                    currentZoom += 0.1;
                    ImgViewer.LayoutTransform = new ScaleTransform(currentZoom, currentZoom);
                };
                PanelControles.Children.Add(btnZoomIn);

                Button btnZoomOut = new Button
                {
                    Content = "Zoom -",
                    Style = (Style)FindResource("DetailButtonStyle"),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                btnZoomOut.Click += (s, e) =>
                {
                    if (currentZoom > 0.2)
                    {
                        currentZoom -= 0.1;
                        ImgViewer.LayoutTransform = new ScaleTransform(currentZoom, currentZoom);
                    }
                };
                PanelControles.Children.Add(btnZoomOut);
            }
            else if (ext == ".mp4" || ext == ".avi" || ext == ".mp3")
            {
                MediaViewer.Visibility = Visibility.Visible;
                MediaViewer.Source = new Uri(filePath);

                Button btnPlay = new Button
                {
                    Content = "Play",
                    Style = (Style)FindResource("DetailButtonStyle"),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                btnPlay.Click += (s, e) => MediaViewer.Play();
                PanelControles.Children.Add(btnPlay);

                Button btnPause = new Button
                {
                    Content = "Pausa",
                    Style = (Style)FindResource("DetailButtonStyle"),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                btnPause.Click += (s, e) => MediaViewer.Pause();
                PanelControles.Children.Add(btnPause);

                Button btnStop = new Button
                {
                    Content = "Stop",
                    Style = (Style)FindResource("DetailButtonStyle"),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                btnStop.Click += (s, e) => MediaViewer.Stop();
                PanelControles.Children.Add(btnStop);
            }
            else if (ext == ".txt")
            {
                TxtViewer.Visibility = Visibility.Visible;
                TxtViewer.Text = File.ReadAllText(filePath);

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

                Button btnFontMinus = new Button
                {
                    Content = "Texto -",
                    Style = (Style)FindResource("DetailButtonStyle"),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                btnFontMinus.Click += (s, e) =>
                {
                    if (currentFontSize > 6)
                    {
                        currentFontSize -= 2;
                        TxtViewer.FontSize = currentFontSize;
                    }
                };
                PanelControles.Children.Add(btnFontMinus);
            }
            else if (ext == ".md")
            {
                MdViewer.Visibility = Visibility.Visible;
                MdViewer.Markdown = File.ReadAllText(filePath);
            }
            else
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                this.Close();
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
