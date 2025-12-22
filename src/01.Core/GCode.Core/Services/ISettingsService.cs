using GCode.Core.Models;

namespace GCode.Core.Services
{
    public interface ISettingsService
    {
        EditorSettings LoadSettings();
        void SaveSettings(EditorSettings settings);
    }
}
