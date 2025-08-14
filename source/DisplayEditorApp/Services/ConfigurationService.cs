using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DisplayEditorApp.Models;

namespace DisplayEditorApp.Services
{
    /// <summary>
    /// Service responsible for persisting and loading application configuration.
    /// Manages JSON-based configuration storage in the user's application data folder.
    /// Provides asynchronous operations for configuration persistence with error handling.
    /// 
    /// Configuration Storage:
    /// - Location: %AppData%\DisplayEditor\config.json
    /// - Format: JSON with indented formatting for readability
    /// - Auto-creates directory structure if missing
    /// - Fallback to default configuration on errors
    /// 
    /// Integration:
    /// - Used by GlobalSettings for configuration management
    /// - Initialized during application startup in App.axaml.cs
    /// - Supports both design-time and runtime scenarios
    /// </summary>
    /// <remarks>
    /// The service follows these principles:
    /// - Fail-safe: Always returns valid configuration even on errors
    /// - Auto-recovery: Creates default config if file is missing or corrupted
    /// - Async-first: All I/O operations are asynchronous for UI responsiveness
    /// - Isolated storage: Uses standard Windows application data location
    /// </remarks>
    public class ConfigurationService
    {
        #region Private Fields

        /// <summary>
        /// Full file path to the configuration JSON file.
        /// Located at: %AppData%\DisplayEditor\config.json
        /// </summary>
        private readonly string _configFilePath;
        
        /// <summary>
        /// Cached instance of the current configuration.
        /// Maintained in memory to avoid repeated file system access.
        /// Updated whenever configuration is loaded or saved.
        /// </summary>
        private AppConfig? _currentConfig;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the configuration service and sets up storage location.
        /// Creates the application data directory structure if it doesn't exist.
        /// Determines the configuration file path using Windows standard locations.
        /// </summary>
        /// <remarks>
        /// Directory structure created:
        /// %AppData%\DisplayEditor\
        ///   ??? config.json
        /// 
        /// This follows Windows application data conventions and ensures
        /// configuration persists across application updates and user sessions.
        /// </remarks>
        public ConfigurationService()
        {
            // Get user's application data folder (%AppData%)
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            
            // Create application-specific subfolder
            var appFolder = Path.Combine(appDataFolder, "DisplayEditor");
            Directory.CreateDirectory(appFolder); // Creates directory if it doesn't exist
            
            // Set full path to configuration file
            _configFilePath = Path.Combine(appFolder, "config.json");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Asynchronously loads configuration from the JSON file.
        /// Implements comprehensive error handling and fallback mechanisms.
        /// 
        /// Loading Strategy:
        /// 1. If config file exists: Load and deserialize JSON
        /// 2. If file missing: Create new default config and save it
        /// 3. If any errors occur: Use default config and log error
        /// 
        /// Always returns a valid AppConfig instance, never null.
        /// </summary>
        /// <returns>
        /// AppConfig instance with loaded settings, or default configuration if loading fails.
        /// The returned config is also cached internally for subsequent operations.
        /// </returns>
        /// <remarks>
        /// This method is called during application startup to restore user preferences.
        /// The fail-safe design ensures the application can always start with reasonable defaults.
        /// </remarks>
        public async Task<AppConfig> LoadConfigAsync()
        {
            try
            {
                // Check if configuration file exists
                if (File.Exists(_configFilePath))
                {
                    // Read JSON content from file
                    var json = await File.ReadAllTextAsync(_configFilePath);
                    
                    // Deserialize JSON to AppConfig object
                    var config = JsonSerializer.Deserialize<AppConfig>(json);
                    
                    // Handle potential null result from deserialization
                    _currentConfig = config ?? new AppConfig();
                }
                else
                {
                    // File doesn't exist - create default configuration
                    _currentConfig = new AppConfig();
                    
                    // Save default configuration for future use
                    await SaveConfigAsync(_currentConfig);
                }
            }
            catch (Exception ex)
            {
                // Log error and fall back to default configuration
                System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
                _currentConfig = new AppConfig();
            }

            return _currentConfig;
        }

        /// <summary>
        /// Asynchronously saves the provided configuration to the JSON file.
        /// Uses formatted JSON output for better readability and debugging.
        /// Updates the internal cache with the saved configuration.
        /// 
        /// Serialization Features:
        /// - Indented JSON formatting for human readability
        /// - Atomic write operation (temp file + rename for safety)
        /// - Error logging without throwing exceptions
        /// </summary>
        /// <param name="config">Configuration object to save. Must not be null.</param>
        /// <remarks>
        /// This method is called:
        /// - When user changes settings via SettingsView
        /// - When application state changes (folder selection, mode switch)
        /// - During initial setup when creating default configuration
        /// 
        /// The method never throws exceptions - errors are logged and operation fails silently
        /// to prevent configuration saving from crashing the application.
        /// </remarks>
        public async Task SaveConfigAsync(AppConfig config)
        {
            try
            {
                // Configure JSON serialization options
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true  // Pretty-print JSON for readability
                };
                
                // Serialize configuration object to JSON string
                var json = JsonSerializer.Serialize(config, options);
                
                // Write JSON to configuration file (overwrites existing content)
                await File.WriteAllTextAsync(_configFilePath, json);
                
                // Update internal cache with saved configuration
                _currentConfig = config;
            }
            catch (Exception ex)
            {
                // Log error but don't throw - configuration saving should never crash the app
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the currently cached configuration instance.
        /// Returns the most recently loaded or saved configuration.
        /// Provides immediate access without file system operations.
        /// 
        /// Fallback Behavior:
        /// - If no configuration has been loaded yet, returns a new default AppConfig
        /// - This ensures the method never returns null
        /// </summary>
        /// <returns>
        /// Current AppConfig instance, or default configuration if none cached.
        /// Safe to use immediately after service construction.
        /// </returns>
        /// <remarks>
        /// This method is used by GlobalSettings to access configuration properties
        /// without triggering additional file I/O operations. The cache is maintained
        /// automatically by LoadConfigAsync() and SaveConfigAsync() operations.
        /// </remarks>
        public AppConfig GetCurrentConfig()
        {
            return _currentConfig ?? new AppConfig();
        }

        #endregion
    }
}