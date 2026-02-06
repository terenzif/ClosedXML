# ClosedXML – Graphics Engine Refactor

This repository contains a refactored version of ClosedXML that introduces a pluggable graphics engine architecture. This change removes the hard dependency on `SixLabors.Fonts` from the core library, making it lightweight and more flexible for different environments.

## Key Changes

*   **Pluggable Architecture**: The `IXLGraphicEngine` interface has been extracted, allowing different implementations for text measurement and image handling.
*   **Dependency Removal**: The core `ClosedXML` package no longer depends on `SixLabors.Fonts`.
*   **Default Implementation**: An embedded `ApproximateGraphicEngine` is used by default. It uses precomputed metrics for the Carlito font and requires no external dependencies.
*   **New Packages**:
    *   `ClosedXML.Graphic.SixLabors`: The original implementation using `SixLabors.Fonts`.
    *   `ClosedXML.Graphic.Skia`: An implementation using `SkiaSharp`.
    *   `ClosedXML.Graphic.GDI`: An implementation using `System.Drawing.Common` (GDI+), utilizing installed system fonts.

## Usage

### Default (No Dependencies)
By default, `ClosedXML` uses the `ApproximateGraphicEngine`. This works out-of-the-box without any additional configuration or packages.

```csharp
using var workbook = new XLWorkbook();
// Uses ApproximateGraphicEngine
```

### Using SixLabors.Fonts (Original Behavior)
To use the original accurate text measurement:

1.  Install the `ClosedXML.Graphic.SixLabors` package.
2.  Configure the engine at startup or per-workbook.

```csharp
// Global configuration
LoadOptions.DefaultGraphicEngine = new SixLaborsGraphicEngine();

// Or per-workbook
var options = new LoadOptions
{
    GraphicEngine = new SixLaborsGraphicEngine()
};
using var workbook = new XLWorkbook(options);
```

### Using SkiaSharp
To use SkiaSharp for rendering metrics:

1.  Install the `ClosedXML.Graphic.Skia` package.
2.  Configure the engine.

```csharp
LoadOptions.DefaultGraphicEngine = new SkiaGraphicEngine();
```

### Using GDI+ (System.Drawing)
To use installed system fonts on Windows (or configured GDI+ environments):

1.  Install the `ClosedXML.Graphic.GDI` package.
2.  Configure the engine.

```csharp
LoadOptions.DefaultGraphicEngine = new GdiGraphicEngine();
```

## License

Same as ClosedXML (MIT).
