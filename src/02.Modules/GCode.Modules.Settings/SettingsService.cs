using System;
using System.IO;
using System.Text.Json;
using GCode.Core.Models;
using GCode.Core.Services;

namespace GCode.Modules.Settings
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsPath;

        public SettingsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "GravisNC");
            
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            _settingsPath = Path.Combine(folder, "settings.json");
        }

        public EditorSettings LoadSettings()
        {
            if (!File.Exists(_settingsPath))
            {
                return new EditorSettings();
            }

            try
            {
                string json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<EditorSettings>(json);
                return settings ?? new EditorSettings();
            }
            catch
            {
                // 로드 실패 시 기본값 반환
                return new EditorSettings();
            }
        }

        public void SaveSettings(EditorSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception)
            {
                // 저장 실패 처리 (필요시 로깅)
            }
        }
    }
}
