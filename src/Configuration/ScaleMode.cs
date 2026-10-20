namespace PdfBooklet.Configuration;

public enum ScaleMode
{
    /// <summary>Each page is scaled (up or down) to fill the area inside the margins.</summary>
    FitToArea,

    /// <summary>Pages keep their original size (100 %).</summary>
    ActualSize,

    /// <summary>Pages are scaled by <see cref="LayoutSettings.CustomScalePercent"/>.</summary>
    Custom,
}
