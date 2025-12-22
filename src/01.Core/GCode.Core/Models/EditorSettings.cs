namespace GCode.Core.Models
{
    public class EditorSettings
    {
        public string FontFamily { get; set; } = "Consolas";
        public double FontSize { get; set; } = 14.0;
        public string FontWeight { get; set; } = "Normal"; // Normal, Bold
        public string Theme { get; set; } = "Dark"; // Reserved for future
        public string LastDirectory { get; set; } = ""; // Last opened directory
        public System.Collections.Generic.List<string> OpenFiles { get; set; } = new(); // Session persistence
    }
}
