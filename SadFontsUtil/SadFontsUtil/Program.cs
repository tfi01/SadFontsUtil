
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

        bool preview = false;


        #region Input handling - Font File Name
        if (args.Contains("--font") && args.Length > Array.IndexOf(args, "--font") + 1)
        {
            fontPath = args[Array.IndexOf(args, "--font") + 1];
            if (string.IsNullOrEmpty(fontPath) || !System.IO.File.Exists(fontPath))
            {
                Console.WriteLine($"Error: Font file not found: {fontPath}");
                return;
            }
        }
        else
        {
            // No --font argument provided, search for TTF files in current directory
            string exeDirectory = AppContext.BaseDirectory;
            string[] ttfFiles = Directory.GetFiles(exeDirectory, "*.ttf");

            if (ttfFiles.Length == 0)
            {
                Console.WriteLine("Error: No font file specified and no .ttf files found in the current directory.");
                Console.WriteLine("Usage: --font <path_to_font.ttf>");
                return;
            }
            else if (ttfFiles.Length == 1)
            {
                fontPath = ttfFiles[0];
                Console.WriteLine($"No font file specified. Found '{Path.GetFileName(fontPath)}' and will convert it.");
            }
            else
            {
                // Multiple TTF files found
                Console.WriteLine("No font file specified. Found multiple .ttf files:");
                for (int i = 0; i < ttfFiles.Length; i++)
                {
                    Console.WriteLine($"{i + 1}. {Path.GetFileName(ttfFiles[i])}");
                }
                Console.Write(Environment.NewLine + "Enter the number of the font to convert [1]: ");

                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    fontPath = ttfFiles[0];
                    Console.WriteLine($"Using default: {Path.GetFileName(fontPath)}");
                }
                else if (int.TryParse(input, out int selection) && selection >= 1 && selection <= ttfFiles.Length)
                {
                    fontPath = ttfFiles[selection - 1];
                    Console.WriteLine($"Selected: {Path.GetFileName(fontPath)}");
                }
                else
                {
                    Console.WriteLine("Error: Invalid selection.");
                    return;
                }
            }
        }

        #endregion

        #region Input handling - Character Height
        if (args.Contains("--charHeight") && args.Length > Array.IndexOf(args, "--charHeight") + 1)
        {
            _ = int.TryParse(args[Array.IndexOf(args, "--charHeight") + 1], out charHeight);
        }
        else
        {
            Console.Write($"Enter character height in pixels (or press Enter for default: 16): ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                charHeight = 16;
                Console.WriteLine("Using default: 16");
            }
            else if (int.TryParse(input, out int parsedHeight) && parsedHeight > 0)
            {
                charHeight = parsedHeight;
            }
            else
            {
                Console.WriteLine("Error: Invalid character height. Must be a positive integer.");
                return;
            }
        }

        #endregion

        #region Input handling - Character Range
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
        else
        {
            Console.Write("Enter ASCII character range (or press Enter for default: 0-255): ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                charsFrom = 0;
                charsTo = 255;
                Console.WriteLine("Using default: 0-255");
            }
            else
            {
                string[] parts = input.Split('-');
                if (parts.Length != 2)
                {
                    Console.WriteLine("Error: Character range format must be from-to (e.g., 32-126)");
                    return;
                }

                if (!int.TryParse(parts[0], out charsFrom) || !int.TryParse(parts[1], out charsTo))
                {
                    Console.WriteLine("Error: Invalid character range. Both values must be integers.");
                    return;
                }

                if (charsFrom < 0 || charsTo > 255 || charsFrom > charsTo)
                {
                    Console.WriteLine("Error: Character range must be between 0-255 and from <= to.");
                    return;
                }
            }
        }

        #endregion

        #region Input handling - Grid Size
        if (args.Contains("--grid") && args.Length > Array.IndexOf(args, "--grid") + 1)
        {
            string grid = args[Array.IndexOf(args, "--grid") + 1];
            if (grid.Split('x').Length != 2)
            {
                Console.WriteLine($"Error: grid format must be XxY: {grid}");
                return;
            }
            _ = int.TryParse(grid.Split('x')[0], out gridSizeX);
            _ = int.TryParse(grid.Split('x')[1], out gridSizeY);
        }
        else
        {
            Console.Write("Enter grid size XxY [16x16]: ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                gridSizeX = 16;
                gridSizeY = 16;
                Console.WriteLine("Using default: 16x16");
            }
            else
            {
                string[] parts = input.Split('x');
                if (parts.Length != 2)
                {
                    Console.WriteLine("Error: Grid format must be XxY (e.g., 16x16 or 32x8)");
                    return;
                }

                if (!int.TryParse(parts[0], out gridSizeX) || !int.TryParse(parts[1], out gridSizeY))
                {
                    Console.WriteLine("Error: Invalid grid size. Both X and Y must be positive integers.");
                    return;
                }

                if (gridSizeX <= 0 || gridSizeY <= 0)
                {
                    Console.WriteLine("Error: Grid size must be greater than 0.");
                    return;
                }
            }
        }

        #endregion

        #region Input handling - Grid Cell size
        if (args.Contains("--gridcell") && args.Length > Array.IndexOf(args, "--gridcell") + 1)
        {
            string grid = args[Array.IndexOf(args, "--gridcell") + 1];
            if (grid.Split('x').Length != 2)
            {
                Console.WriteLine($"Error: gridcell format must be XxY: {grid}");
                return;
            }
            _ = int.TryParse(grid.Split('x')[0], out gridCellWidth);
            _ = int.TryParse(grid.Split('x')[1], out gridCellHeight);
        }
        else
        {
            Console.Write("Enter grid cell size XxY [8x16]: ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                gridCellWidth = 8;
                gridCellHeight = 16;
                Console.WriteLine("Using default: 8x16");
            }
            else
            {
                string[] parts = input.Split('x');
                if (parts.Length != 2)
                {
                    Console.WriteLine("Error: Grid cell format must be XxY (e.g., 8x16 or 10x20)");
                    return;
                }

                if (!int.TryParse(parts[0], out gridCellWidth) || !int.TryParse(parts[1], out gridCellHeight))
                {
                    Console.WriteLine("Error: Invalid grid cell size. Both width and height must be positive integers.");
                    return;
                }

                if (gridCellWidth <= 0 || gridCellHeight <= 0)
                {
                    Console.WriteLine("Error: Grid cell size must be greater than 0.");
                    return;
                }
            }
        }

        #endregion

        #region Input handling - Grid Lines Visible?
        if (args.Contains("--gridlines"))
        {
            gridLineWidth = 1;
        }
        else
        {
            Console.Write("Show grid lines? (1=yes, 0=no) [1]: ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                gridLineWidth = 1;
                Console.WriteLine("Using default: yes (1)");
            }
            else if (input == "1" || input.ToLower() == "yes" || input.ToLower() == "y")
            {
                gridLineWidth = 1;
            }
            else if (input == "0" || input.ToLower() == "no" || input.ToLower() == "n")
            {
                gridLineWidth = 0;
            }
            else
            {
                Console.WriteLine("Error: Invalid input. Enter 1 (yes) or 0 (no).");
                return;
            }
        }
        #endregion


        #region Input handling - Preview after generation?
        if (args.Contains("--preview"))
        {
            preview = true;
        }
        else
        {
            Console.Write("Open PNG file after generation? (1=yes, 0=no) [1]: ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                preview = true;
                Console.WriteLine("Using default: yes (1)");
            }
            else if (input == "1" || input.ToLower() == "yes" || input.ToLower() == "y")
            {
                preview = true;
            }
            else if (input == "0" || input.ToLower() == "no" || input.ToLower() == "n")
            {
                preview = false;
            }
            else
            {
                Console.WriteLine("Error: Invalid input. Enter 1 (yes) or 0 (no).");
                return;
            }
        }
        #endregion


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

        if (preview)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = outputPNGName,
                    UseShellExecute = true
                });
                Console.WriteLine("Opening preview...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not open preview: {ex.Message}");
            }
        }

        Console.WriteLine($"Saved: {outputPNGName} ({imageWidth}x{imageHeight}px)");
    }
}
