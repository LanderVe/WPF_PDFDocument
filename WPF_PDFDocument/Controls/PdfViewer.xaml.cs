using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WPF_PDFDocument.Controls
{
  /// <summary>
  /// Interaction logic for PdfViewer.xaml
  /// </summary>
  public partial class PdfViewer : UserControl
  {

    #region Bindable Properties

    public string PdfPath
    {
      get { return (string)GetValue(PdfPathProperty); }
      set { SetValue(PdfPathProperty, value); }
    }

    // Using a DependencyProperty as the backing store for PdfPath.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PdfPathProperty =
        DependencyProperty.Register("PdfPath", typeof(string), typeof(PdfViewer), new PropertyMetadata(null, propertyChangedCallback: OnPdfPathChanged));

    private static void OnPdfPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var pdfDrawer = (PdfViewer)d;

      if (!string.IsNullOrEmpty(pdfDrawer.PdfPath))
      {
        //making sure it's an absolute path
        var path = System.IO.Path.GetFullPath(pdfDrawer.PdfPath);

        StorageFile.GetFileFromPathAsync(path).AsTask()
          //load pdf document on background thread
          .ContinueWith(t => PdfDocument.LoadFromFileAsync(t.Result).AsTask()).Unwrap()
          //display on UI Thread
          .ContinueWith(t2 => PdfToImages(pdfDrawer, t2.Result), TaskScheduler.FromCurrentSynchronizationContext());
      }

    }

    #endregion


    public PdfViewer()
    {
      InitializeComponent();
    }

    private async static Task PdfToImages(PdfViewer pdfViewer, PdfDocument pdfDoc)
    {
      var items = pdfViewer.PagesContainer.Items;
      items.Clear();

      if (pdfDoc == null) return;

      for (uint i = 0; i < pdfDoc.PageCount; i++)
      {
        using (var page = pdfDoc.GetPage(i))
        {
          var bitmap = await PageToBitmapAsync(page);
          var image = new Image
          {
            Source = bitmap,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 4),
            MaxWidth = 800
          };
          items.Add(image);
        }
      }
    }

    private static async Task<BitmapImage> PageToBitmapAsync(PdfPage page)
    {
      BitmapImage image = new BitmapImage();

      using (var stream = new InMemoryRandomAccessStream())
      {
        await page.RenderToStreamAsync(stream);

        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream.AsStream();
        image.EndInit();
      }

      return image;
    }

  }
}
