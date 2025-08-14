//
// vim: ts=4 et
// Copyright (c) 2025 Petr Vanek
// @author Petr Vanek, petr@fotoventus.cz

using DisplayEditorApp.Models;
using DisplayEditorApp.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DisplayEditorApp.Settings
{
    /// <summary>
    /// Static class providing global access to application settings and configuration.
    /// Acts as a facade over AppConfig and ConfigurationService, offering convenient property access
    /// and utility methods for text formatting and character validation.
    /// 
    /// Architecture:
    /// - Wraps AppConfig instance for type-safe property access
    /// - Integrates with ConfigurationService for persistence operations
    /// - Provides text formatting utilities for editor display
    /// - Maintains character validation rules for input filtering
    /// 
    /// Usage Pattern:
    /// 1. Initialize() called during app startup with services
    /// 2. Properties accessed throughout application for current settings
    /// 3. SaveAsync() called when settings change to persist modifications
    /// 4. Utility methods used for text processing and validation
    /// </summary>
    public static class GlobalSettings
    {
        private static AppConfig _config = new AppConfig();
        private static ConfigurationService? _configService;

        public static void Initialize(ConfigurationService configService, AppConfig config)
        {
            _configService = configService;
            _config = config;
        }

        public static int MaxColumns
        {
            get => _config.MaxColumns;
            set => _config.MaxColumns = value;
        }

        public static int MaxRows
        {
            get => _config.MaxRows;
            set => _config.MaxRows = value;
        }

        public static int MaxColumnsExt
        {
            get => _config.MaxColumnsExt;
            set => _config.MaxColumnsExt = value;
        }

        public static int MaxRowsExt
        {
            get => _config.MaxRowsExt;
            set => _config.MaxRowsExt = value;
        }

        public static double FontSize
        {
            get => _config.FontSize;
            set => _config.FontSize = value;
        }

        public static string FontFamily
        {
            get => _config.FontFamily;
            set => _config.FontFamily = value;
        }

        public static string LastSelectedFolder
        {
            get => _config.LastSelectedFolder;
            set => _config.LastSelectedFolder = value;
        }

        public static bool IsSimpleMode
        {
            get => _config.IsSimpleMode;
            set => _config.IsSimpleMode = value;
        }

        public static async Task SaveAsync()
        {
            if (_configService != null)
            {
                await _configService.SaveConfigAsync(_config);
            }
        }

        public static AppConfig GetConfig() => _config;


        public static HashSet<char> AllowedCharacters { get; set; } = new HashSet<char>
    {
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        

        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',


        ' ','!', '"', '#', '$', '%', '&',  '\'', '(', ')', '*', '+',',','-','.','/',':', ';','<','=','>','?','@','[','\\',']','^','_','{', '|' ,'}','~',
            
        'á', 'č', 'ď', 'é', 'ě', 'í', 'ň', 'ó', 'ř', 'š', 'ť', 'ú', 'ů', 'ý', 'ž',
        'Á', 'Č', 'Ď', 'É', 'Ě', 'Í', 'Ň', 'Ó', 'Ř', 'Š', 'Ť', 'Ú', 'Ů', 'Ý', 'Ž'
    };


        public static bool IsCharacterAllowed(char character)
        {
            return AllowedCharacters.Contains(character);
        }




        public static string FormatContentForEditor(string raw, int maxColumns, int maxRows)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;

            // remove all cr lf, if nneded 
            var cleanText = raw.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
            var result = new StringBuilder();
            var currentRow = 0;

            //formating via row, column settings
            for (int i = 0; i < cleanText.Length && currentRow < maxRows; i += maxColumns)
            {
                var line = cleanText.Substring(i, Math.Min(maxColumns, cleanText.Length - i));
                if (currentRow > 0) result.Append('\n');

                result.Append(line.TrimEnd());
                currentRow++;
            }

            return result.ToString();
        }

        public static string ConvertFromEditorFormat(string editorContent, int maxColumns, int maxRows)
        {
            if (string.IsNullOrEmpty(editorContent))
                return string.Empty;


            var lines = editorContent
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n');

            var result = new StringBuilder();

            for (int i = 0; i < maxRows; i++)
            {
                string line = i < lines.Length ? lines[i] : string.Empty;

                if (line.Length > maxColumns)
                    line = line.Substring(0, maxColumns);
                else
                    line = line.PadRight(maxColumns, ' ');

                result.Append(line);
            }

            return result.ToString();
        }

        public static string ConvertFromEditorFormat(string editorContent)
        {
            if (string.IsNullOrEmpty(editorContent)) return string.Empty;

            return editorContent.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
        }

        public static void ResetToDefaults()
        {
            MaxColumns =14;
            MaxRows = 7;
            MaxColumnsExt = 28;
            MaxRowsExt = 8;
            FontSize = 14;
            FontFamily = "Consolas";
        }

    }



}