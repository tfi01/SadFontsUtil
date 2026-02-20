using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Brush = System.Drawing.Brush;
using Brushes = System.Drawing.Brushes;
using Color = System.Drawing.Color;
using FontFamily = System.Drawing.FontFamily;
using Pen = System.Drawing.Pen;

namespace SadFontsUtilGUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[SupportedOSPlatform("windows")]
public partial class MainWindow : Window
{
    private string? _selectedFontPath;
    private Bitmap? _currentBitmap;
    private string _generatedFontName = "";

    public MainWindow()
    {
        InitializeComponent();
        UpdatePreviewScale();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Font files (*.ttf,*.fon)|*.ttf;*.fon|All files (*.*)|*.*",
            Title = "Select a TrueType Font file"
        };

        if (dialog.ShowDialog() == true)
        {
            txtFontPath.Text = dialog.FileName;
            _selectedFontPath = dialog.FileName;
            _generatedFontName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
            txtSelectedFont.Text = System.IO.Path.GetFileName(dialog.FileName);

            // Auto-generate preview after selecting font
            GeneratePreview();
        }
    }


    private void SldPreviewScale_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
    {
        UpdatePreviewScale();
    }

    private void UpdatePreviewScale()
    {
        previewScaleTransform.ScaleX = sldPreviewScale.Value / 100.0;
        previewScaleTransform.ScaleY = sldPreviewScale.Value / 100.0;
    }

    private bool TryGetInt(TextBox textBox, out int value, int defaultValue, bool mustBePositive = true)
    {
        if (!int.TryParse(textBox.Text, out value))
        {
            value = defaultValue;
            return false;
        }
        if (mustBePositive && value <= 0)
        {
            value = defaultValue;
            return false;
        }
        return true;
    }

    private void BtnGenerate_Click(object sender, RoutedEventArgs e)
    {
        GeneratePreview();
    }

    private void GeneratePreview()
    {
        _currentBitmap?.Dispose();
        _currentBitmap = null;

        if (string.IsNullOrEmpty(_selectedFontPath) || !System.IO.File.Exists(_selectedFontPath))
        {
            MessageBox.Show("Please select a valid font file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            // Parse parameters
            if (!TryGetInt(txtCharHeight, out int charHeight, 16))
            {
                MessageBox.Show("Character height must be a positive integer.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Character range - separate fields
            if (!TryGetInt(txtCharFrom, out int charsFrom, 32, false) || !TryGetInt(txtCharTo, out int charsTo, 126, false))
            {
                MessageBox.Show("Character range values must be valid integers.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (charsFrom < 0 || charsTo < 0 || charsFrom > charsTo)
            {
                MessageBox.Show("Character range is invalid. 'From' must be <= 'To' and both must be >= 0.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Grid size - separate fields
            if (!TryGetInt(txtGridSizeX, out int gridSizeX, 16) || !TryGetInt(txtGridSizeY, out int gridSizeY, 16))
            {
                MessageBox.Show("Grid size must be valid positive integers.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Grid cell size - separate fields
            if (!TryGetInt(txtGridCellX, out int gridCellWidth, 8) || !TryGetInt(txtGridCellY, out int gridCellHeight, 16))
            {
                MessageBox.Show("Grid cell size must be valid positive integers.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int gridLineWidth = chkGridLines.IsChecked == true ? 1 : 0;

            // Calculate image size
            int imageWidth = gridSizeX * gridCellWidth + (gridSizeX + 1) * gridLineWidth;
            int imageHeight = gridSizeY * gridCellHeight + (gridSizeY + 1) * gridLineWidth;

            if (imageWidth <= 0 || imageHeight <= 0)
            {
                MessageBox.Show("Calculated image size is invalid. Check your parameters.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            

            PrivateFontCollection fontCollection = new PrivateFontCollection();
            fontCollection.AddFontFile(_selectedFontPath);
            FontFamily fontFamily = fontCollection.Families[0];
            Bitmap bitmap = new Bitmap(imageWidth, imageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

                // Draw grid lines if enabled
                if (gridLineWidth > 0)
                {
                    Pen gridPen = new Pen(Color.FromArgb(128, 128, 255), gridLineWidth);
                    for (int x = 0; x <= gridSizeX; x++)
                    {
                        int xPos = x * (gridCellWidth + gridLineWidth);
                        g.DrawLine(gridPen, xPos, 0, xPos, imageHeight - 1);
                    }

                    for (int y = 0; y <= gridSizeY; y++)
                    {
                        int yPos = y * (gridCellHeight + gridLineWidth);
                        g.DrawLine(gridPen, 0, yPos, imageWidth - 1, yPos);
                    }
                    gridPen.Dispose();
                }


                Font font = new Font(fontFamily, charHeight, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);
                Brush brush = Brushes.White;

                // StringFormat for centring text in cells
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                // Render chars in cell between grid lines
                for (int i = 0; i < 256; i++)
                {
                    if (i < charsFrom || i > charsTo) continue;
                    int col = i % gridSizeX;
                    int row = i / gridSizeX;

                    // cell position 
                    int cellX = col * (gridCellWidth + gridLineWidth) + gridLineWidth;
                    int cellY = row * (gridCellHeight + gridLineWidth) + gridLineWidth;

                    RectangleF cellRect = new RectangleF(cellX, cellY, gridCellWidth, gridCellHeight);
                    g.DrawString(((char)i).ToString(), font, brush, cellRect, sf);
                }

                sf.Dispose();
                font.Dispose();
            }

            // Display in preview
            var bitmapImage = BitmapSourceConvert.ToBitmapImage(bitmap);
            imgPreview.Source = bitmapImage;
            btnSave.IsEnabled = true;

            txtPNGResolution.Text = bitmap.Width + " x " + bitmap.Height;

            _generatedFontName = System.IO.Path.GetFileNameWithoutExtension(_selectedFontPath);
        }
        catch (Exception ex)
        {
            string errorMsg = $"Error generating preview:\n{ex.Message}\n\nDetails:\nType: {ex.GetType().Name}\n\n";
            errorMsg += $"Parameters used:\n";
            errorMsg += $"  Character Height: {txtCharHeight.Text}\n";
            errorMsg += $"  Character From: {txtCharFrom.Text}\n";
            errorMsg += $"  Character To: {txtCharTo.Text}\n";
            errorMsg += $"  Grid Size X: {txtGridSizeX.Text}\n";
            errorMsg += $"  Grid Size Y: {txtGridSizeY.Text}\n";
            errorMsg += $"  Grid Cell X: {txtGridCellX.Text}\n";
            errorMsg += $"  Grid Cell Y: {txtGridCellY.Text}\n";
            errorMsg += $"  Grid Lines: {(chkGridLines.IsChecked == true ? "Yes" : "No")}\n";
            if (ex.InnerException != null)
            {
                errorMsg += $"\nInner: {ex.InnerException.Message}";
            }
            MessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (_currentBitmap == null)
        {
            MessageBox.Show("No preview available to save. Please generate first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            // Parse parameters for metadata
            if (!TryGetInt(txtCharHeight, out int charHeight, 16)) charHeight = 16;
            if (!TryGetInt(txtGridSizeX, out int gridSizeX, 16)) gridSizeX = 16;
            if (!TryGetInt(txtGridSizeY, out int gridSizeY, 16)) gridSizeY = 16;
            if (!TryGetInt(txtGridCellX, out int gridCellWidth, 8)) gridCellWidth = 8;
            if (!TryGetInt(txtGridCellY, out int gridCellHeight, 16)) gridCellHeight = 16;
            int gridLineWidth = chkGridLines.IsChecked == true ? 1 : 0;

            string outputPNGName = _generatedFontName + ".png";
            string outputFONTName = _generatedFontName + ".font";

            // Save PNG
            _currentBitmap.Save(outputPNGName, System.Drawing.Imaging.ImageFormat.Png);

            // Save FONT metadata
            string fontMetadata = $@"{{
  ""Name"": ""{_generatedFontName}"",
  ""FilePath"": ""{outputPNGName}"",
  ""GlyphHeight"": {gridCellHeight},
  ""GlyphPadding"": {gridLineWidth},
  ""GlyphWidth"": {gridCellWidth},
  ""Columns"": {gridSizeX},
  ""IsSadExtended"": true,
  ""SolidGlyphIndex"": 219
}}";

            System.IO.File.WriteAllText(outputFONTName, fontMetadata);

            MessageBox.Show($"Files saved successfully:\n- {outputPNGName}\n- {outputFONTName}",
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            // Auto-open preview
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = outputPNGName,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore preview errors
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
