
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
class Program
{
    static void GenerateSadConsoleFontFile(string fontFileName, string pngFileName, string fontName,
    int glyphWidth, int glyphHeight, int glyphPadding, int columns)
    {
        //  .font file struct for SadConsole 
        var fontConfig = new Dictionary<string, object>
        {
            ["$type"] = "SadConsole.SadFont, SadConsole",
            ["Name"] = fontName,
            ["FilePath"] = pngFileName,
            ["GlyphWidth"] = glyphWidth,
            ["GlyphHeight"] = glyphHeight,
            ["GlyphPadding"] = glyphPadding,
            ["Columns"] = columns,
            ["SolidGlyphIndex"] = 219, // CP437 solid block 
            ["IsSadExtended"] = false
        };

        string json = System.Text.Json.JsonSerializer.Serialize(fontConfig, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        System.IO.File.WriteAllText(fontFileName, json);
    }


    static void Main(string[] args)
    {
        if (args.Length < 1)
        {

            Console.WriteLine("SadFontsUtil - Generate font sprite sheet with grid");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  SadFontsUtil.exe --font <path_to_ttf> [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --font <path>           Path to TTF/FON font file (required)");
            Console.WriteLine("  --charHeight <pixels>   Font size in pixels (default: 16)");
            Console.WriteLine("  --chars <from-to>       Defines which ASCII characters to render");
            Console.WriteLine("                          Example: --chars 32-126");
            Console.WriteLine("  --grid <WxH>            Grid dimensions in characters (default: 16x16)");
            Console.WriteLine("                          Example: --grid 32x8");
            Console.WriteLine("  --gridcell <WxH>        Cell size in pixels (default: 8x16)");
            Console.WriteLine("                          Example: --gridcell 8x16");
            Console.WriteLine("  --gridlines             Draw 1px grid lines between characters");
            Console.WriteLine("  --preview               Auto-open default image viewer to preview output");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  SadFontsUtil.exe --font \"C:\\Fonts\\IBM_VGA.ttf\"");
            Console.WriteLine("  SadFontsUtil.exe --font font.ttf --charHeight 16 --gridlines");
            Console.WriteLine("  SadFontsUtil.exe --font font.ttf --grid 32x8 --gridcell 8x14 --gridlines --preview");
            Console.WriteLine();
            Console.WriteLine("Output:");
            Console.WriteLine("  Generated PNG and .font file will be saved in the current directory.");




            return;
        }

        // Default values
        string fontPath = "";

        int charHeight = 16;

        int gridSizeX = 16;
        int gridSizeY = 16;

        int gridCellWidth = 8;
        int gridCellHeight = 16;

        int gridLineWidth = 1;
        int charsFrom = 0;
        int charsTo = 255;

        if (args.Contains("--font") && args.Length > Array.IndexOf(args, "--font") + 1)
        {
            fontPath = args[Array.IndexOf(args, "--font") + 1];
            if (string.IsNullOrEmpty(fontPath) || !System.IO.File.Exists(fontPath))
            {
                Console.WriteLine($"Error: Font file not found: {fontPath}");
                return;
            }
        }

        if (args.Contains("--charHeight") && args.Length > Array.IndexOf(args, "--charHeight") + 1)
        {
            _ = int.TryParse(args[Array.IndexOf(args, "--charHeight") + 1], out charHeight);
        }

        if (args.Contains("--chars") && args.Length > Array.IndexOf(args, "--chars") + 1)
        {
            string chars = args[Array.IndexOf(args, "--chars") + 1];
            if (chars.Split('-').Length != 2)
            {
                Console.WriteLine($"Error: chars format must be from-to: {chars}");
                return;
            }
            _ = int.TryParse(chars.Split('-')[0], out charsFrom);
            _ = int.TryParse(chars.Split('-')[1], out charsTo);
        }

        if (args.Contains("--grid") && args.Length > Array.IndexOf(args, "--grid") + 1)
        {
            string grid = args[Array.IndexOf(args, "--grid") + 1];
            if (grid.Split('x').Length != 2)
            {
                Console.WriteLine($"Error: chars format must be XxY: {grid}");
                return;
            }
            _ = int.TryParse(grid.Split('x')[0], out gridSizeX);
            _ = int.TryParse(grid.Split('x')[1], out gridSizeY);
        }

        if (args.Contains("--gridcell") && args.Length > Array.IndexOf(args, "--gridcell") + 1)
        {
            string grid = args[Array.IndexOf(args, "--gridcell") + 1];
            if (grid.Split('x').Length != 2)
            {
                Console.WriteLine($"Error: chars format must be XxY: {grid}");
                return;
            }
            _ = int.TryParse(grid.Split('x')[0], out gridCellWidth);
            _ = int.TryParse(grid.Split('x')[1], out gridCellHeight);
        }
        if (args.Contains("--gridlines")) gridLineWidth = 1; else gridLineWidth = 0;


        // Load font from file
        PrivateFontCollection fontCollection = new PrivateFontCollection();
        fontCollection.AddFontFile(fontPath);
        FontFamily fontFamily = fontCollection.Families[0];

        // Image size calculation:
        int imageWidth = gridSizeX * gridCellWidth + (gridSizeX + 1) * gridLineWidth;
        int imageHeight = gridSizeY * gridCellHeight + (gridSizeY + 1) * gridLineWidth;

        Bitmap bitmap = new Bitmap(imageWidth, imageHeight, PixelFormat.Format32bppArgb);

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


            Font font = new Font(fontFamily, charHeight, FontStyle.Regular, GraphicsUnit.Pixel);
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

        //Save the image
        string fontName = System.IO.Path.GetFileNameWithoutExtension(fontPath);
        string outputPNGName = System.IO.Path.GetFileNameWithoutExtension(fontPath) + ".png";
        string outputFONTName = System.IO.Path.GetFileNameWithoutExtension(fontPath) + ".font";
        bitmap.Save(outputPNGName, ImageFormat.Png);
        bitmap.Dispose();
        fontCollection.Dispose();

        GenerateSadConsoleFontFile(outputFONTName, outputPNGName, fontName, gridCellWidth, gridCellHeight, gridLineWidth, gridSizeX);

        if (args.Contains("--preview"))
        {
            // Open in default application
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = outputPNGName,
                UseShellExecute = true
            });
        }

        Console.WriteLine($"Saved: {outputPNGName} ({imageWidth}x{imageHeight}px)");
    }
}
