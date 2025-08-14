//
// vim: ts=4 et
// Copyright (c) 2025 Petr Vanek
// @author Petr Vanek, petr@fotoventus.cz

using System.Text.Json.Serialization;

namespace DisplayEditorApp.Models
{
    /// <summary>
    /// Configuration model class for the DisplayEditor application.
    /// Contains all user preferences and application settings that persist between sessions.
    /// Serialized to JSON format and stored in %AppData%\DisplayEditor\config.json.
    /// 
    /// This class supports two distinct operating modes:
    /// - Simple Mode: For viewing/editing individual text files with basic grid constraints
    /// - Extended Mode: For managing hierarchical file structures with larger display areas
    /// 
    /// All properties have sensible defaults to ensure the application works out-of-the-box.
    /// JSON property names use camelCase convention for web compatibility.
    /// 
    /// Used by:
    /// - ConfigurationService: For JSON serialization/deserialization
    /// - GlobalSettings: As the underlying data store for application settings
    /// - SettingsView: For displaying and modifying user preferences
    /// </summary>
    /// <remarks>
    /// The class must be public for JSON serialization to work correctly.
    /// Property setters are required for JSON deserialization.
    /// Default values ensure graceful fallback if config file is missing or corrupted.
    /// 
    /// Configuration persistence workflow:
    /// 1. App startup → ConfigurationService.LoadConfigAsync() → Deserialize from JSON
    /// 2. User changes settings → SettingsView → GlobalSettings → ConfigurationService.SaveConfigAsync()
    /// 3. JSON file updated with new values
    /// 4. Settings persist across application restarts
    /// </remarks>
    public class AppConfig 
    {
        #region Display Grid Settings - Simple Mode

        /// <summary>
        /// Maximum number of columns for text display in Simple Mode.
        /// Determines the width of the text grid when viewing individual files from the "zalmy" folder.
        /// 
        /// Usage Context:
        /// - Controls text wrapping in the main content viewer
        /// - Used by ContentOperation for formatting file content
        /// - Affects font size calculation for optimal display
        /// - Configurable via Settings dialog (10-40 range)
        /// 
        /// Default: 14 columns (suitable for short text snippets)
        /// </summary>
        [JsonPropertyName("maxColumns")]
        public int MaxColumns { get; set; } = 14;

        /// <summary>
        /// Maximum number of rows for text display in Simple Mode.
        /// Determines the height of the text grid when viewing individual files from the "zalmy" folder.
        /// 
        /// Usage Context:
        /// - Limits vertical text display area
        /// - Used by EditDialogView for text validation and formatting
        /// - Affects font size calculation for optimal display
        /// - Configurable via Settings dialog (2-20 range)
        /// 
        /// Default: 7 rows (compact display for quick browsing)
        /// </summary>
        [JsonPropertyName("maxRows")]
        public int MaxRows { get; set; } = 7;

        #endregion

        #region Display Grid Settings - Extended Mode

        /// <summary>
        /// Maximum number of columns for text display in Extended Mode.
        /// Determines the width of the text grid when viewing files from "kancional" subdirectories.
        /// Typically larger than Simple Mode to accommodate more detailed content.
        /// 
        /// Usage Context:
        /// - Controls text wrapping in Extended Mode content viewer
        /// - Used when editing files within kancional subdirectories (1.txt to 9.txt)
        /// - Affects automatic font sizing for dual-pane layout
        /// - Configurable via Settings dialog (10-40 range)
        /// 
        /// Default: 28 columns (double Simple Mode for extended content)
        /// </summary>
        [JsonPropertyName("maxColumnsExt")]
        public int MaxColumnsExt { get; set; } = 28;

        /// <summary>
        /// Maximum number of rows for text display in Extended Mode.
        /// Determines the height of the text grid when viewing files from "kancional" subdirectories.
        /// Slightly larger than Simple Mode to provide more viewing area.
        /// 
        /// Usage Context:
        /// - Limits vertical text display in Extended Mode
        /// - Used by EditDialogView when editing kancional files
        /// - Affects layout calculations for dual-pane interface
        /// - Configurable via Settings dialog (2-20 range)
        /// 
        /// Default: 8 rows (one more than Simple Mode for extended viewing)
        /// </summary>
        [JsonPropertyName("maxRowsExt")]
        public int MaxRowsExt { get; set; } = 8;

        #endregion

        #region Typography Settings

        /// <summary>
        /// Font size for text display throughout the application.
        /// Currently defined but not actively used in the UI - reserved for future implementation.
        /// 
        /// Intended Usage:
        /// - Control text size in content viewers
        /// - Override automatic font size calculation
        /// - Provide accessibility options for users
        /// 
        /// Note: The application currently uses automatic font sizing based on available space
        /// and grid constraints. This property is maintained for potential future features.
        /// 
        /// Default: 14.0 points (standard readable size)
        /// </summary>
        [JsonPropertyName("fontSize")]
        public double FontSize { get; set; } = 14.0;

        /// <summary>
        /// Font family name for text display throughout the application.
        /// Currently defined but not actively used in the UI - reserved for future implementation.
        /// 
        /// Intended Usage:
        /// - Allow users to choose preferred monospace font
        /// - Support different fonts for better readability
        /// - Accommodate accessibility requirements
        /// 
        /// Note: The application currently uses Consolas font by default.
        /// This property is maintained for potential future customization features.
        /// 
        /// Default: "Consolas" (monospace font for consistent character spacing)
        /// </summary>
        [JsonPropertyName("fontFamily")]
        public string FontFamily { get; set; } = "Consolas";

        #endregion

        #region Application State

        /// <summary>
        /// Path to the last selected root folder.
        /// Automatically restored when the application starts, providing seamless user experience.
        /// 
        /// Expected Folder Structure:
        /// - Should contain "zalmy" subfolder for Simple Mode (001.txt to 996.txt files)
        /// - Should contain "kancional" subfolder for Extended Mode (001 to 900 subdirectories)
        /// - Each kancional subdirectory should contain files 1.txt to 9.txt
        /// 
        /// Usage Context:
        /// - Restored by MainViewModel during application startup
        /// - Updated when user selects new folder via "Select Folder" button
        /// - Persisted immediately when changed to prevent data loss
        /// 
        /// Default: Empty string (no folder selected initially)
        /// </summary>
        [JsonPropertyName("lastSelectedFolder")]
        public string LastSelectedFolder { get; set; } = string.Empty;

        /// <summary>
        /// Current operating mode of the application.
        /// Determines which file structure and display layout to use.
        /// 
        /// Mode Behaviors:
        /// - True (Simple Mode): 
        ///   * Single ListBox showing zalmy files (001.txt to 996.txt)
        ///   * Uses MaxColumns/MaxRows for display constraints
        ///   * Export functionality available
        ///   * Compact interface for quick file browsing
        /// 
        /// - False (Extended Mode):
        ///   * Dual ListBox interface (directories + files)
        ///   * Shows kancional subdirectories (001 to 900)
        ///   * Second ListBox shows files within selected subdirectory (1.txt to 9.txt)
        ///   * Uses MaxColumnsExt/MaxRowsExt for larger display area
        ///   * More complex file management interface
        /// 
        /// Usage Context:
        /// - Controls UI layout and visibility in MainView
        /// - Determines which file loading logic to use
        /// - Affects settings application (which max columns/rows to use)
        /// - User can switch modes via radio buttons in main interface
        /// 
        /// Default: true (Simple Mode for easier initial user experience)
        /// </summary>
        [JsonPropertyName("isSimpleMode")]
        public bool IsSimpleMode { get; set; } = true;

        #endregion
    }
}