using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

    private void SldPreviewScale_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
    {
        // Prevent UI updates during drag for better performance
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

            // Generate the font image
            _currentBitmap = new System.Drawing.Bitmap(imageWidth, imageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var fontCollection = new PrivateFontCollection())
            using (var g = System.Drawing.Graphics.FromImage(_currentBitmap))
            {
                fontCollection.AddFontFile(_selectedFontPath);
                var fontFamily = fontCollection.Families[0];

                g.Clear(System.Drawing.Color.Transparent);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

                // Draw grid lines if enabled
                if (gridLineWidth > 0)
                {
                    using (var gridPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(128, 128, 255), gridLineWidth))
                    {
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
                    }
                }

                using (var font = new System.Drawing.Font(fontFamily, charHeight, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel))
                using (var brush = System.Drawing.Brushes.White)
                using (var sf = new System.Drawing.StringFormat())
                {
                    sf.Alignment = System.Drawing.StringAlignment.Center;
                    sf.LineAlignment = System.Drawing.StringAlignment.Center;

                    for (int i = 0; i < 256; i++)
                    {
                        if (i < charsFrom || i > charsTo) continue;
                        int col = i % gridSizeX;
                        int row = i / gridSizeX;

                        int cellX = col * (gridCellWidth + gridLineWidth) + gridLineWidth;
                        int cellY = row * (gridCellHeight + gridLineWidth) + gridLineWidth;

                        System.Drawing.RectangleF cellRect = new System.Drawing.RectangleF(cellX, cellY, gridCellWidth, gridCellHeight);
                        g.DrawString(((char)i).ToString(), font, brush, cellRect, sf);
                    }
                }
            }

            // Display in preview
            var bitmapImage = BitmapSourceConvert.ToBitmapImage(_currentBitmap);
            imgPreview.Source = bitmapImage;
            btnSave.IsEnabled = true;

            _generatedFontName = System.IO.Path.GetFileNameWithoutExtension(_selectedFontPath);
        }
        catch (Exception ex)
        {
            string errorMsg = $"Error generating preview:\n{ex.Message}\n\nDetails:\nType: {ex.GetType().Name}";
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
