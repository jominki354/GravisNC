using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GCode.Core.Models;
using System.Collections.Generic;

namespace GCode.App.WPF.Views
{
    public partial class SettingsWindow : Window
    {
        public EditorSettings ResultSettings { get; private set; } = null!;
        private EditorSettings _settings = null!;

        public SettingsWindow(EditorSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            ResultSettings = settings; // Set initial value
            LoadCurrentSettings(_settings);
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void LoadCurrentSettings(EditorSettings settings)
        {
            // Font Families
            foreach (var font in Fonts.SystemFontFamilies.OrderBy(f => f.Source))
            {
                ComboFontFamily.Items.Add(font.Source);
            }
            ComboFontFamily.SelectedItem = settings.FontFamily;

            // Font Sizes
            var sizes = new List<double> { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 28, 32, 48, 72 };
            ComboFontSize.ItemsSource = sizes;
            ComboFontSize.SelectedItem = settings.FontSize;

            // Font Weight
            ComboFontWeight.SelectedIndex = settings.FontWeight == "Bold" ? 1 : 0;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var selectedWeightIndex = ComboFontWeight.SelectedIndex;
            var weightString = selectedWeightIndex == 1 ? "Bold" : "Normal";

            ResultSettings = new EditorSettings
            {
                FontFamily = ComboFontFamily.SelectedItem as string ?? "Consolas",
                FontSize = (double)ComboFontSize.SelectedItem,
                FontWeight = weightString
            };
            
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
