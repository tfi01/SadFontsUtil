# SadFontsUtil

A command-line utility for generating bitmap font sprite sheets compatible with [SadConsole](https://sadconsole.com/), a .NET-based ASCII/ANSI console engine for roguelikes and text-based games.

## Features

- ✨ Convert any TrueType Font (TTF) to a SadConsole-compatible sprite sheet
- 🎨 Automatically generates both PNG and `.font` metadata files
- 📐 Customizable grid dimensions and cell sizes
- 🔲 Optional grid lines for visual debugging
- 🎯 Character range filtering (render only specific ASCII ranges)
- 🖼️ Built-in preview support

## Requirements

- Windows OS
- .NET 8.0 or later
- `System.Drawing.Common` package

## Installation

### Build from source

```bash
git clone https://github.com/yourusername/SadFontsUtil.git
cd SadFontsUtil
dotnet build -c Release
```

### Font Selection

For optimal results, use **monospace (fixed-width) bitmap fonts** such as:
- IBM VGA/EGA/CGA fonts
- Terminal/Console fonts
- Classic PC BIOS fonts

#### Recommended Font Pack

The [**Ultimate Oldschool PC Font Pack**](https://int10h.org/oldschool-pc-fonts/download/) is an excellent free resource containing over 200 classic PC fonts perfectly suited for use with this tool. It includes:

- **IBM PC fonts** - VGA, EGA, CGA, MDA (8×8, 8×14, 8×16, 9×14, 9×16)
- **OEM fonts** - Compaq, Tandy, Olivetti, Toshiba, and more
- **Terminal fonts** - DOS codepage variants (CP437, CP850, CP866, etc.)
- **BIOS ROM fonts** - Authentic dumps from vintage hardware

**Download:** [https://int10h.org/oldschool-pc-fonts/download/](https://int10h.org/oldschool-pc-fonts/download/)

These fonts are provided in TrueType (.ttf) format and work perfectly with SadFontsUtil. Try starting with `Px437_IBM_VGA_8x16.ttf` for the classic DOS look.

**Example with Ultimate Oldschool PC Font Pack:**
```bash
SadFontsUtil.exe --font "Px437_IBM_VGA_8x16.ttf" --gridcell 8x16 --gridlines --preview
