//
// vim: ts=4 et
// Copyright (c) 2025 Petr Vanek
// @author Petr Vanek, petr@fotoventus.cz

using DisplayEditorApp.Helpers;
using DisplayEditorApp.Settings;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DisplayEditorApp
{
    /// <summary>
    /// Provides unified operations for loading and saving file content with CP1250 encoding.
    /// Handles content formatting according to display grid settings (rows/columns).
    /// Used by MainViewModel and EditDialogViewModel for file operations.
    /// </summary>
    internal class ContentOperation
    {
        /// <summary>
        /// Asynchronously loads file content with CP1250 encoding and formats it for editor display.
        /// Handles file existence validation and error recovery.
        /// </summary>
        /// <param name="filePath">Path to the file to load</param>
        /// <param name="maxColumns">Maximum number of columns for display formatting</param>
        /// <param name="maxRows">Maximum number of rows for display formatting</param>
        /// <returns>Formatted content string ready for editor display, or error message, or empty string</returns>
        public static async Task<string> LoadDataAsync(string filePath, int maxColumns, int maxRows)
        {
            // Return empty string for invalid file paths
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return string.Empty;
            }

            try
            {
                // Load raw content using CP1250 encoding
                var rawContent = await EncodingHelper.ReadFileCP1250Async(filePath);
                
                // Format content according to grid settings for editor display
                return GlobalSettings.FormatContentForEditor(rawContent, maxColumns, maxRows);
            }
            catch (Exception ex)
            {
                // Return error message if file loading fails
                return $"Error loading file: {ex.Message}";
            }
        }

        /// <summary>
        /// Asynchronously saves editor content to file with CP1250 encoding.
        /// Converts formatted editor content back to original file format before saving.
        /// </summary>
        /// <param name="filePath">Path where to save the file</param>
        /// <param name="content">Editor content to save</param>
        /// <param name="maxColumns">Maximum number of columns used for content conversion</param>
        /// <param name="maxRows">Maximum number of rows used for content conversion</param>
        /// <exception cref="ArgumentException">Thrown when filePath or content is null or empty</exception>
        /// <exception cref="Exception">Thrown when file saving fails</exception>
        public static async Task SaveDataAsync(string filePath, string content, int maxColumns, int maxRows)
        {
            // Validate input parameters
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(content))
                throw new ArgumentException("File path and content cannot be null or empty.");
            
            try
            {
                // Convert editor format back to original file format
                var formattedContent = GlobalSettings.ConvertFromEditorFormat(content, maxColumns, maxRows);
                
                // Save content using CP1250 encoding
                await File.WriteAllTextAsync(filePath, formattedContent, EncodingHelper.CP1250);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving file {filePath}: {ex.Message}", ex);
            }
        }
    }
}
