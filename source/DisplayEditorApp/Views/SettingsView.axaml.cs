//
// vim: ts=4 et
// Copyright (c) 2025 Petr Vanek
// @author Petr Vanek, petr@fotoventus.cz

using Avalonia.Controls;
using Avalonia.Interactivity;
using DisplayEditorApp.Settings;

namespace DisplayEditorApp.Views;

/// <summary>
/// Settings dialog window for configuring display grid parameters.
/// Allows users to adjust column and row limits for both Simple and Extended modes.
/// Uses code-behind approach instead of MVVM for simple UI operations.
/// </summary>
    public partial class SettingsView : Window
{
    /// <summary>
    /// Initializes the settings dialog and loads current configuration values.
    /// </summary>
    public SettingsView()
    {
        InitializeComponent();
        LoadCurrentSettings();
    }

    /// <summary>
    /// Loads current settings from GlobalSettings into the UI sliders.
    /// Maps configuration values to corresponding slider controls.
    /// </summary>
    private void LoadCurrentSettings()
    {
        // Find slider controls by name from XAML
        var columnsSlider = this.FindControl<Slider>("ColumnsSlider");           // Simple mode columns
        var rowsSlider = this.FindControl<Slider>("RowsSlider");                 // Simple mode rows
        var columnsSliderExt = this.FindControl<Slider>("ColumnsSliderExt");     // Extended mode columns
        var rowsSliderExt = this.FindControl<Slider>("RowsSliderExt");           // Extended mode rows

        // Set slider values from global settings (with null checks)
        if (columnsSlider != null) columnsSlider.Value = GlobalSettings.MaxColumns;
        if (rowsSlider != null) rowsSlider.Value = GlobalSettings.MaxRows;
        if (columnsSliderExt != null) columnsSliderExt.Value = GlobalSettings.MaxColumnsExt;
        if (rowsSliderExt != null) rowsSliderExt.Value = GlobalSettings.MaxRowsExt;
    }

    /// <summary>
    /// Handles OK button click - saves current slider values to global settings.
    /// Persists configuration to storage and closes dialog with positive result.
    /// </summary>
    /// <param name="sender">Button that triggered the event</param>
    /// <param name="e">Event arguments</param>
    private async void OnOkClick(object? sender, RoutedEventArgs e)
    {
        // Retrieve current slider values
        var columnsSlider = this.FindControl<Slider>("ColumnsSlider");
        var rowsSlider = this.FindControl<Slider>("RowsSlider");
        var columnsSliderExt = this.FindControl<Slider>("ColumnsSliderExt");
        var rowsSliderExt = this.FindControl<Slider>("RowsSliderExt");

        // Update global settings with slider values (cast to int)
        if (columnsSlider != null) GlobalSettings.MaxColumns = (int)columnsSlider.Value;
        if (rowsSlider != null) GlobalSettings.MaxRows = (int)rowsSlider.Value;
        if (columnsSliderExt != null) GlobalSettings.MaxColumnsExt = (int)columnsSliderExt.Value;
        if (rowsSliderExt != null) GlobalSettings.MaxRowsExt = (int)rowsSliderExt.Value;

        // Persist settings to configuration file
        await GlobalSettings.SaveAsync();
        
        // Close dialog with success result (true)
        Close(true);
    }

    /// <summary>
    /// Handles Cancel button click - closes dialog without saving changes.
    /// Returns negative result to indicate user cancellation.
    /// </summary>
    /// <param name="sender">Button that triggered the event</param>
    /// <param name="e">Event arguments</param>
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        // Close dialog with cancel result (false)
        Close(false);
    }

    /// <summary>
    /// Handles Reset button click - restores all settings to default values.
    /// Updates both global settings and UI sliders to reflect defaults.
    /// Does not automatically save - user must click OK to persist changes.
    /// </summary>
    /// <param name="sender">Button that triggered the event</param>
    /// <param name="e">Event arguments</param>
    private void OnResetClick(object? sender, RoutedEventArgs e)
    {
        // Reset global settings to default values
        GlobalSettings.ResetToDefaults();
        
        // Update UI sliders to show the reset values
        LoadCurrentSettings();
    }
}