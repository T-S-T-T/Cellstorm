namespace Cellstorm.Debugging
{
    /// <summary>
    /// The text overlays from the test harnesses are hidden by default now that the real HUD exists.
    /// F12 in the Game scene toggles them. The debug keys always work, only the text is hidden.
    /// </summary>
    public static class DebugVisibility
    {
        public static bool Show { get; set; }
    }
}
