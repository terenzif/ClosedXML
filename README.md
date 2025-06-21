# ClosedXML – Font Measurement Refactor (Experimental)

This fork explores a non-invasive refactor of font measurement logic in response to [ClosedXML#1805](https://github.com/ClosedXML/ClosedXML/issues/1805), aiming to remove dependency on `System.Drawing.Common` and improve cross-platform support.

## Objective

Introduce a clean interface for text measurement (e.g., `ITextMeasurer`) with:
- a minimal built-in fallback implementation (no external deps),
- optional adapters (e.g., [SixLabors.Fonts](https://github.com/SixLabors/Fonts), [SkiaSharp](https://github.com/mono/SkiaSharp), ...) distributed externally.

## Status

Work-in-progress. The goal is to make this approach adoptable upstream with no functional regressions or forced dependencies.

## License

Same as ClosedXML (MIT).
