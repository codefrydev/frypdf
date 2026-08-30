using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Services;

public interface ISmartPlacementService
{
    /// <summary>
    /// Calculates the optimal (X, Y) placement coordinates for a newly created or pasted element.
    /// </summary>
    (double X, double Y) GetPlacementPosition(PageViewModel page, double elementWidth, double elementHeight, bool isContextMenuTriggered = false);

    /// <summary>
    /// Updates the currently visible viewport center and dimensions in unscaled page coordinates.
    /// </summary>
    void UpdateViewport(double pageCenterX, double pageCenterY, double visibleWidth, double visibleHeight);

    /// <summary>
    /// Registers the exact page coordinates where a right-click context menu was opened.
    /// </summary>
    void SetContextMenuPointer(double pageX, double pageY);

    /// <summary>
    /// Clears any pending context menu pointer coordinates.
    /// </summary>
    void ClearContextMenuPointer();

    /// <summary>
    /// Resets the sequential cascading offset counter.
    /// </summary>
    void ResetCascade();
}
