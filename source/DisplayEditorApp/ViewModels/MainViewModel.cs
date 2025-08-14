//
// vim: ts=4 et
// Copyright (c) 2025 Petr Vanek
// @author Petr Vanek, petr@fotoventus.cz

using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayEditorApp.Helpers;
using DisplayEditorApp.Settings;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.Generic;

namespace DisplayEditorApp.ViewModels;

/// <summary>
/// Main ViewModel for the DisplayEditor application.
/// Manages file and folder operations, mode switching (Simple/Extended), 
/// text display formatting, and user interface state.
/// Supports two operating modes:
/// - Simple Mode: Displays files from "zalmy" folder (001.txt to 996.txt)
/// - Extended Mode: Displays subdirectories from "kancional" folder (001 to 900) with nested files (1.txt to 9.txt)
/// Integrates with ContentOperation for file I/O using CP1250 encoding.
/// </summary>
    public partial class MainViewModel : ViewModelBase
{
    #region Dependencies

    /// <summary>
    /// Delegate for folder picker functionality, injected from MainWindow.
    /// </summary>
    private readonly Func<Task<string?>> _pickFolderFunc;
    
    /// <summary>
    /// Delegate for CSV file save functionality, injected from MainWindow.
    /// </summary>
    private readonly Func<Task<string?>> _saveCsvFileFunc;

    #endregion

    #region Constructors

    /// <summary>
    /// Main constructor with dependency injection for file system operations.
    /// Initializes the ViewModel with required delegates and loads saved configuration.
    /// </summary>
    /// <param name="pickFolderFunc">Delegate for folder selection dialog</param>
    /// <param name="saveCsvFileFunc">Delegate for CSV file save dialog</param>
    public MainViewModel(Func<Task<string?>> pickFolderFunc, Func<Task<string?>> saveCsvFileFunc)
    {
        _pickFolderFunc = pickFolderFunc;
        _saveCsvFileFunc = saveCsvFileFunc;
        LoadSavedConfiguration();
    }

    /// <summary>
    /// Parameterless constructor for design-time support.
    /// Provides null delegates for XAML designer compatibility.
    /// </summary>
    public MainViewModel() : this(() => Task.FromResult<string?>(null), () => Task.FromResult<string?>(null)) { }

    #endregion

    #region Inner Classes

    /// <summary>
    /// Represents a file or directory item in the application's list controls.
    /// Provides visual state information for UI binding (colors, enabled state).
    /// Used in both Simple mode (for files) and Extended mode (for directories and files).
    /// </summary>
    public class FileItem
    {
        /// <summary>Display name of the file or directory (e.g., "001.txt", "001")</summary>
        public string Name { get; set; }
        
        /// <summary>Full file system path to the item</summary>
        public string FullPath { get; set; }
        
        /// <summary>Whether the file or directory physically exists on disk</summary>
        public bool Exists { get; set; }
        
        /// <summary>UI enablement state - enabled only if item exists</summary>
        public bool IsEnabled => Exists;

        /// <summary>Text color for UI display - Blue for existing, Red for missing items</summary>
        public string TextColor => Exists ? "Blue" : "Red";
        
        /// <summary>Background color for UI display - LightBlue for existing, LightPink for missing items</summary>
        public string BackgroundColor => Exists ? "LightBlue" : "LightPink";

        /// <summary>
        /// Creates a new FileItem with specified properties.
        /// </summary>
        /// <param name="name">Display name</param>
        /// <param name="fullPath">Full file system path</param>
        /// <param name="exists">Whether item exists on disk</param>
        public FileItem(string name, string fullPath, bool exists)
        {
            Name = name;
            FullPath = fullPath;
            Exists = exists;
        }
    }

    #endregion

    #region Collections and Selection

    /// <summary>
    /// Primary collection for the first ListBox.
    /// In Simple mode: Contains files (001.txt to 996.txt) from zalmy folder.
    /// In Extended mode: Contains subdirectories (001 to 900) from kancional folder.
    /// </summary>
    private ObservableCollection<FileItem> _filesInFolder = new();
    public ObservableCollection<FileItem> FilesInFolder
    {
        get => _filesInFolder;
        set => SetProperty(ref _filesInFolder, value);
    }

    /// <summary>
    /// Secondary collection for the second ListBox (Extended mode only).
    /// Contains files (1.txt to 9.txt) within the selected subdirectory.
    /// Empty and hidden in Simple mode.
    /// </summary>
    private ObservableCollection<FileItem> _subFolderFiles = new();
    public ObservableCollection<FileItem> SubFolderFiles
    {
        get => _subFolderFiles;
        set => SetProperty(ref _subFolderFiles, value);
    }

    /// <summary>
    /// Currently selected item in the primary FilesInFolder collection.
    /// In Simple mode: Selected file for content display.
    /// In Extended mode: Selected subdirectory that populates SubFolderFiles.
    /// </summary>
    private FileItem? _selectedFile;
    public FileItem? SelectedFile
    {
        get => _selectedFile;
        set
        {
            SetProperty(ref _selectedFile, value);
            
            // Update command availability based on selection
            _newCommand?.NotifyCanExecuteChanged();
            _editCommand?.NotifyCanExecuteChanged();
            _deleteCommand?.NotifyCanExecuteChanged();

            // Handle mode-specific selection behavior
            if (IsSimpleMode)
            {
                // In Simple mode, load file content directly
                LoadFileContentAsync(value?.FullPath);
            }
            else
            {
                // In Extended mode, populate subdirectory files
                LoadSubFolderFiles(value?.FullPath);
            }
        }
    }

    /// <summary>
    /// Currently selected file in the secondary SubFolderFiles collection (Extended mode only).
    /// Represents the actual file to display/edit within the selected subdirectory.
    /// </summary>
    private FileItem? _selectedSubFile;
    public FileItem? SelectedSubFile
    {
        get => _selectedSubFile;
        set
        {
            SetProperty(ref _selectedSubFile, value);
            
            // Update command availability based on selection
            _newCommand?.NotifyCanExecuteChanged();
            _editCommand?.NotifyCanExecuteChanged(); 
            _deleteCommand?.NotifyCanExecuteChanged(); 
            
            // Load content of selected file
            LoadFileContentAsync(value?.FullPath);
        }
    }

    /// <summary>
    /// Content of the currently selected file, formatted for display.
    /// Formatted according to current mode's column/row constraints.
    /// Updated when file selection changes or after editing operations.
    /// </summary>
    private string? _selectedFileContent;
    public string? SelectedFileContent
    {
        get => _selectedFileContent;
        set => SetProperty(ref _selectedFileContent, value);
    }

    private string? _selectedFileTitle;
    public string? SelectedFileTitle
    {
        get => _selectedFileTitle;
        set => SetProperty(ref _selectedFileTitle, value);
    }

    

    /// <summary>
    /// Collection for file information display (currently unused but available for future features).
    /// Could be used to show file metadata, size, dates, etc.
    /// </summary>
    private ObservableCollection<FileInfoItem> _selectedFileInfo = new();
    public ObservableCollection<FileInfoItem> SelectedFileInfo
    {
        get => _selectedFileInfo;
        set => SetProperty(ref _selectedFileInfo, value);
    }

    #endregion

    #region Mode Management

    /// <summary>
    /// Current operating mode of the application.
    /// True: Simple mode (zalmy files, single ListBox)
    /// False: Extended mode (kancional subdirectories, dual ListBox)
    /// </summary>
    private bool _isSimpleMode = true;
    public bool IsSimpleMode
    {
        get => _isSimpleMode;
        set
        {
            if (SetProperty(ref _isSimpleMode, value))
            {
                // Notify related properties of mode change
                OnPropertyChanged(nameof(IsExtendedMode));
                OnPropertyChanged(nameof(IsSubFolderVisible));
                OnPropertyChanged(nameof(MaxRowsFromSettings));
                OnPropertyChanged(nameof(MaxLengthFromSettings));

                // Reload content for new mode if folder is selected
                if (!string.IsNullOrEmpty(SelectedFolder))
                {
                    LoadFolderContent();
                }

                UpdateCalculatedFontSize();
            }
        }
    }

    /// <summary>
    /// Inverse of IsSimpleMode for UI binding convenience.
    /// True when in Extended mode, False when in Simple mode.
    /// </summary>
    public bool IsExtendedMode
    {
        get => !_isSimpleMode;
        set
        {
            if (value != IsExtendedMode)
            {
                IsSimpleMode = !value;
            }
        }
    }

    /// <summary>
    /// Controls visibility of the second ListBox in the UI.
    /// Visible only in Extended mode for displaying subdirectory files.
    /// </summary>
    public bool IsSubFolderVisible => IsExtendedMode;

    #endregion

    #region Folder Management

    /// <summary>
    /// Currently selected root folder path.
    /// Should contain "zalmy" subfolder for Simple mode and "kancional" subfolder for Extended mode.
    /// When changed, triggers automatic loading of folder content.
    /// </summary>
    private string _selectedFolder = string.Empty;
    public string SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            SetProperty(ref _selectedFolder, value);
            LoadFolderContent();
        }
    }

    #endregion

    #region Font Size and Layout Management

    /// <summary>
    /// Current actual width of the text display area.
    /// Updated by MainView when TextBox size changes.
    /// Used for automatic font size calculation.
    /// </summary>
    private double _textBoxActualWidth = 400;
    public double TextBoxActualWidth
    {
        get => _textBoxActualWidth;
        set
        {
            if (SetProperty(ref _textBoxActualWidth, value))
            {
                UpdateCalculatedFontSize();
            }
        }
    }

    /// <summary>
    /// Current actual height of the text display area.
    /// Updated by MainView when TextBox size changes.
    /// Used for automatic font size calculation.
    /// </summary>
    private double _textBoxActualHeight = 300;
    public double TextBoxActualHeight
    {
        get => _textBoxActualHeight;
        set
        {
            if (SetProperty(ref _textBoxActualHeight, value))
            {
                UpdateCalculatedFontSize();
            }
        }
    }

    /// <summary>
    /// Automatically calculated optimal font size for text display.
    /// Computed based on available space and current column/row constraints.
    /// Ensures text fits within the display area without scrolling.
    /// </summary>
    private double _calculatedFontSize = 20;
    public double CalculatedFontSize
    {
        get => _calculatedFontSize;
        set => SetProperty(ref _calculatedFontSize, value);
    }

    /// <summary>
    /// Calculated width for TextBox based on column settings and font metrics.
    /// Uses fixed character width estimation for Consolas font.
    /// Used for fixed-size layout mode.
    /// </summary>
    public double TextBoxCalculatedWidth
    {
        get
        {
            // Approximate character width in Consolas font size 20 is ~12px
            const double charWidthPixels = 12.0;
            const double paddingAndBorder = 20.0;
            return (IsSimpleMode ? GlobalSettings.MaxColumns : GlobalSettings.MaxColumnsExt) * charWidthPixels + paddingAndBorder;
        }
    }

    /// <summary>
    /// Calculated height for TextBox based on row settings and font metrics.
    /// Uses fixed line height estimation for Consolas font.
    /// Used for fixed-size layout mode.
    /// </summary>
    public double TextBoxCalculatedHeight
    {
        get
        {
            // Approximate line height in Consolas font size 20 is ~24px
            const double lineHeightPixels = 24.0; //24
            const double paddingAndBorder = 20.0;
            return (IsSimpleMode ? GlobalSettings.MaxRows : GlobalSettings.MaxRowsExt) * lineHeightPixels + paddingAndBorder;
        }
    }

    /// <summary>
    /// Maximum number of rows from current settings for UI binding.
    /// Reflects Simple or Extended mode settings as appropriate.
    /// </summary>
    public int MaxRowsFromSettings => IsSimpleMode ? GlobalSettings.MaxRows : GlobalSettings.MaxRowsExt; 
    
    /// <summary>
    /// Maximum text length for TextBox binding (rows × columns).
    /// Used to limit input in text controls.
    /// </summary>
    public int MaxLengthFromSettings => IsSimpleMode ? (GlobalSettings.MaxColumns * GlobalSettings.MaxRows) : (GlobalSettings.MaxColumnsExt * GlobalSettings.MaxRowsExt);

    #endregion

    #region Font Size Calculation

    /// <summary>
    /// Calculates optimal font size based on available display space and text constraints.
    /// Ensures all text fits within the TextBox without scrolling.
    /// Considers both width (columns) and height (rows) limitations.
    /// </summary>
    private void UpdateCalculatedFontSize()
    {
        if (TextBoxActualWidth <= 0 || TextBoxActualHeight <= 0) return;

        // Calculate available space after UI padding and margins
        var availableWidth = TextBoxActualWidth - 140; // 50 padding + 20 margin + buffer
        var availableHeight = TextBoxActualHeight - 60; // 20 margin + buffer

        // Get current mode's column and row constraints
        var maxColumns = IsSimpleMode ? GlobalSettings.MaxColumns : GlobalSettings.MaxColumnsExt;
        var maxRows = IsSimpleMode ? GlobalSettings.MaxRows : GlobalSettings.MaxRowsExt;

        if (maxColumns <= 0 || maxRows <= 0) return;

        // Calculate font size based on character and line spacing
        // For Consolas font: character width ≈ 0.6 * fontSize, line height ≈ 1.6 * fontSize
        var fontSizeByWidth = availableWidth / (maxColumns * 0.6);
        var fontSizeByHeight = availableHeight / (maxRows * 1.6);

        // Use smaller value to ensure all text fits
        var optimalFontSize = Math.Min(fontSizeByWidth, fontSizeByHeight);

        // Constrain font size to practical limits
        optimalFontSize = Math.Max(8, Math.Min(70, optimalFontSize));

        CalculatedFontSize = optimalFontSize;

        Debug.WriteLine($"TextBox: {TextBoxActualWidth:F0}x{TextBoxActualHeight:F0}, " +
                       $"Available: {availableWidth:F0}x{availableHeight:F0}, " +
                       $"Columns: {maxColumns}, Rows: {maxRows}, " +
                       $"FontSize: {optimalFontSize:F1}");
    }

    /// <summary>
    /// Updates TextBox size properties and recalculates font size.
    /// Called after settings changes to refresh display layout.
    /// Notifies UI of all size-related property changes.
    /// </summary>
    public void UpdateTextBoxSize()
    {
        OnPropertyChanged(nameof(TextBoxCalculatedWidth));
        OnPropertyChanged(nameof(TextBoxCalculatedHeight));
        OnPropertyChanged(nameof(MaxRowsFromSettings));
        OnPropertyChanged(nameof(MaxLengthFromSettings));

        // Recalculate optimal font size
        UpdateCalculatedFontSize();
    }

    #endregion

    #region Folder Content Loading

    /// <summary>
    /// Loads content of the selected folder based on current mode.
    /// Clears existing collections and populates them according to mode:
    /// - Simple mode: Loads files from zalmy subfolder
    /// - Extended mode: Loads subdirectories from kancional subfolder
    /// </summary>
    private void LoadFolderContent()
    {
        FilesInFolder.Clear();
        SubFolderFiles.Clear();
        SelectedFileContent = null;

        if (string.IsNullOrEmpty(SelectedFolder) || !Directory.Exists(SelectedFolder))
            return;

        if (IsSimpleMode)
        {
            LoadSimpleModeFiles();
        }
        else
        {
            LoadExtendedModeSubFolders();
        }
    }

    /// <summary>
    /// Loads files for Simple mode from the "zalmy" subfolder.
    /// Creates FileItem entries for files 001.txt through 996.txt.
    /// Marks each item as existing or missing based on file system state.
    /// </summary>
    private void LoadSimpleModeFiles()
    {
        var zalmyPath = Path.Combine(SelectedFolder, "zalmy");

        if (!Directory.Exists(zalmyPath))
        {
            Debug.WriteLine($"Zalmy folder not found: {zalmyPath}");
            return;
        }

        // Create entries for files 001.txt to 996.txt
        for (int i = 1; i <= 996; i++)
        {
            var fileName = $"{i:D3}.txt";
            var filePath = Path.Combine(zalmyPath, fileName);
            var exists = File.Exists(filePath);

            FilesInFolder.Add(new FileItem(fileName, filePath, exists));
        }
    }

    /// <summary>
    /// Loads subdirectories for Extended mode from the "kancional" subfolder.
    /// Creates FileItem entries for directories 001 through 900.
    /// Marks each directory as existing or missing based on file system state.
    /// </summary>
    private void LoadExtendedModeSubFolders()
    {
        var kancionalPath = Path.Combine(SelectedFolder, "kancional");

        if (!Directory.Exists(kancionalPath))
        {
            Debug.WriteLine($"Kancional folder not found: {kancionalPath}");
            return;
        }

        // Create entries for subdirectories 001 to 900
        for (int i = 1; i <= 900; i++)
        {
            var folderName = $"{i:D3}";
            var folderPath = Path.Combine(kancionalPath, folderName);
            var exists = Directory.Exists(folderPath);

            FilesInFolder.Add(new FileItem(folderName, folderPath, exists));
        }
    }

    /// <summary>
    /// Loads files within a selected subdirectory (Extended mode only).
    /// Populates SubFolderFiles with entries for files 1.txt through 9.txt.
    /// Called when user selects a subdirectory in Extended mode.
    /// </summary>
    /// <param name="subFolderPath">Path to the selected subdirectory</param>
    private void LoadSubFolderFiles(string? subFolderPath)
    {
        SubFolderFiles.Clear();

        if (string.IsNullOrEmpty(subFolderPath)) return;

        // Create entries for files 1.txt to 9.txt in subdirectory
        for (int i = 1; i <= 9; i++)
        {
            var fileName = $"{i}.txt";
            var filePath = Path.Combine(subFolderPath, fileName);
            var exists = File.Exists(filePath);

            SubFolderFiles.Add(new FileItem(fileName, filePath, exists));
        }
    }

    /// <summary>
    /// Asynchronously loads and displays content of the specified file.
    /// Uses ContentOperation for CP1250 encoding and proper formatting.
    /// Updates SelectedFileContent with formatted content for display.
    /// </summary>
    /// <param name="filePath">Path to file to load, or null to clear content</param>
    private async void LoadFileContentAsync(string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath))
        {
            // Get appropriate column/row limits for current mode
            var maxColumns = IsSimpleMode ? GlobalSettings.MaxColumns : GlobalSettings.MaxColumnsExt;
            var maxRows = IsSimpleMode ? GlobalSettings.MaxRows : GlobalSettings.MaxRowsExt;
            
            // Load and format file content
            SelectedFileContent = await ContentOperation.LoadDataAsync(filePath, maxColumns, maxRows);
            if (!string.IsNullOrEmpty(SelectedFileContent))
            {
                SelectedFileTitle = IsSimpleMode ? "Odpověď žalmu"  : "Kancionál";
            }
            else
            {
                SelectedFileTitle = Path.GetFileName(filePath) + " nenalezen";
            }
            
        }
        else
        {
            SelectedFileContent = null;
        }
    }

    #endregion

    #region Commands

    /// <summary>Command for selecting root folder via folder picker dialog</summary>
    public ICommand SelectFolderCommand => new AsyncRelayCommand(SelectFolderAsync);

    /// <summary>Command for editing the currently selected file</summary>
    private RelayCommand? _editCommand;
    public ICommand EditCommand => _editCommand ??= new RelayCommand(ToggleEditMode, CanEditFile);

    /// <summary>Command for creating a new file at the currently selected location</summary>
    private RelayCommand? _newCommand;
    public ICommand NewCommand => _newCommand ??= new RelayCommand(NewFile, CanNewFile);

    /// <summary>Command for deleting the currently selected file</summary>
    private RelayCommand? _deleteCommand;
    public ICommand DeleteCommand => _deleteCommand ??= new RelayCommand(DeleteFile, CanDeleteFile);

    /// <summary>Command for exporting all files to CSV format (Simple mode only)</summary>
    private RelayCommand? _exportCommand; 
    public ICommand ExportCommand => _exportCommand ??= new RelayCommand(ExportFile, CanExportFile);

    /// <summary>Command for opening the settings dialog</summary>
    public ICommand SettingsCommand => new RelayCommand(OpenSettings);

    #endregion

    #region Command Implementations

    /// <summary>
    /// Determines if CSV export is available.
    /// Export is only available in Simple mode and requires a selected folder.
    /// </summary>
    /// <returns>True if export can be performed</returns>
    private bool CanExportFile()
    {
        return IsSimpleMode && !string.IsNullOrWhiteSpace(SelectedFolder);
    }

    /// <summary>
    /// Handles folder selection via injected folder picker delegate.
    /// Updates SelectedFolder and refreshes export command availability.
    /// </summary>
    private async Task SelectFolderAsync()
    {
        var folder = await _pickFolderFunc();
        if (!string.IsNullOrWhiteSpace(folder))
        {
            SelectedFolder = folder;
            _exportCommand?.NotifyCanExecuteChanged();
            Debug.WriteLine("Selected folder (via SelectFolderCommand): " + folder);
        }
    }

    /// <summary>
    /// Initiates editing of the currently selected file.
    /// Opens EditDialogView for file modification.
    /// </summary>
    private void ToggleEditMode()
    {
        var currentFile = GetCurrentSelectedFilePath();
        if (!string.IsNullOrEmpty(currentFile))
        {
            OpenEditDialog(currentFile);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("No file selected for editing.");
        }
    }

    /// <summary>
    /// Gets the file path of the currently selected item.
    /// Returns path from SelectedFile (Simple mode) or SelectedSubFile (Extended mode).
    /// </summary>
    /// <returns>File path of selected item, or null if none selected</returns>
    private string? GetCurrentSelectedFilePath()
    {
        if (IsSimpleMode)
        {
            return SelectedFile?.FullPath;
        }
        else
        {
            return SelectedSubFile?.FullPath;
        }
    }

    /// <summary>
    /// Opens the edit dialog for the specified file.
    /// Handles dialog result and refreshes content after successful editing.
    /// Manages selection preservation in Extended mode.
    /// </summary>
    /// <param name="filePath">Path to file to edit</param>
    private async void OpenEditDialog(string filePath)
    {
        try
        {
            var editDialog = new DisplayEditorApp.Views.EditDialogView(filePath, IsSimpleMode);

            var mainWindow = App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow == null)
            {
                System.Diagnostics.Debug.WriteLine("Cannot show dialog: MainWindow is null");
                return;
            }

            var result = await editDialog.ShowDialog<bool?>(mainWindow);

            if (result == true)
            {
                if (IsSimpleMode)
                {
                    // In Simple mode, reload file content directly
                    LoadFileContentAsync(filePath);
                }
                else
                {
                    // In Extended mode, preserve selection and refresh file list
                    var selectedSubFileName = SelectedSubFile?.Name;

                    // Reload files in current subdirectory
                    var directoryPath = Path.GetDirectoryName(filePath);
                    LoadSubFolderFiles(directoryPath);

                    // Restore file selection
                    if (!string.IsNullOrEmpty(selectedSubFileName))
                    {
                        var subFileToSelect = SubFolderFiles.FirstOrDefault(f => f.Name == selectedSubFileName);
                        if (subFileToSelect != null)
                        {
                            SelectedSubFile = subFileToSelect;
                        }
                    }

                    // Reload content of edited file
                    LoadFileContentAsync(filePath);
                }

                System.Diagnostics.Debug.WriteLine($"File edited and saved: {filePath}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening edit dialog: {ex.Message}");
        }
    }

    #endregion

    #region CSV Export

    /// <summary>
    /// Exports all existing files to CSV format.
    /// Only available in Simple mode. Processes all files in zalmy folder,
    /// applies content spacing rules, and saves as UTF-8 CSV file.
    /// </summary>
    private async void ExportFile()
    {
        try
        {
            // Validate export prerequisites
            if (!IsSimpleMode)
            {
                Debug.WriteLine("Export is only available in Simple mode.");
                await ShowErrorDialog("Export Error", "Export is only available in Simple mode.");
                return;
            }

            // Get save location from user
            var csvPath = await _saveCsvFileFunc();
            if (string.IsNullOrEmpty(csvPath))
            {
                Debug.WriteLine("CSV export cancelled by user.");
                return;
            }

            // Initialize CSV data structure
            var csvLines = new List<string>();
            csvLines.Add("Number,Text"); // CSV header

            // Set up encoding for reading CP1250 files
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var windows1250Encoding = System.Text.Encoding.GetEncoding("windows-1250");

            // Process each file in the collection
            foreach (var fileItem in FilesInFolder)
            {
                // Skip non-existent files
                if (!fileItem.Exists)
                    continue;

                try
                {
                    // Extract file number from filename (e.g., "001.txt" -> "001")
                    var fileNumber = Path.GetFileNameWithoutExtension(fileItem.Name);
                    
                    // Load file content with proper encoding
                    string content;
                    if (!string.IsNullOrEmpty(fileItem.FullPath) && File.Exists(fileItem.FullPath))
                    {
                        var bytes = await File.ReadAllBytesAsync(fileItem.FullPath);
                        content = windows1250Encoding.GetString(bytes);
                    }
                    else
                    {
                        content = "";
                    }
                    
                    // Apply content spacing processing rules
                    var processedContent = ProcessContentSpacing(content);
                    
                    // Prepare content for CSV (escape quotes, remove newlines)
                    var cleanContent = string.IsNullOrEmpty(processedContent) ? "" : 
                        processedContent.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Replace("\"", "\"\"");
                    
                    // Add row to CSV
                    csvLines.Add($"{fileNumber},\"{cleanContent}\"");
                    
                    Debug.WriteLine($"Exported file {fileNumber}: {cleanContent.Length} characters");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error reading file {fileItem.Name}: {ex.Message}");
                    // Continue processing remaining files
                }
            }

            // Save CSV file with UTF-8 encoding
            await File.WriteAllLinesAsync(csvPath, csvLines, System.Text.Encoding.UTF8);

            Debug.WriteLine($"CSV file saved to: {csvPath} with {csvLines.Count - 1} records (UTF-8 encoding)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving CSV file: {ex.Message}");
            await ShowErrorDialog("Export Error", $"Failed to save CSV file: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes content spacing according to column constraints.
    /// Removes newlines, splits into virtual column-width chunks,
    /// and applies spacing rules for proper text flow.
    /// </summary>
    /// <param name="content">Raw file content to process</param>
    /// <returns>Processed content with proper spacing</returns>
    private string ProcessContentSpacing(string content)
    {
        if (string.IsNullOrEmpty(content))
            return "";

        var maxColumns = GlobalSettings.MaxColumns;
        
        // Remove all newlines and create continuous text
        var cleanContent = content.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
        
        var processedChunks = new List<string>();
        
        // Split content into virtual lines of maxColumns length
        for (int i = 0; i < cleanContent.Length; i += maxColumns)
        {
            var chunkLength = Math.Min(maxColumns, cleanContent.Length - i);
            var chunk = cleanContent.Substring(i, chunkLength);
            
            // Process each virtual line with spacing rules
            var processedChunk = ProcessVirtualLine(chunk, i + chunkLength < cleanContent.Length);
            
            processedChunks.Add(processedChunk);
        }
        
        return string.Join("", processedChunks);
    }

    /// <summary>
    /// Processes a single virtual line according to spacing rules.
    /// Handles trailing whitespace and inter-chunk spacing for proper text flow.
    /// </summary>
    /// <param name="virtualLine">Line content to process</param>
    /// <param name="hasNextContent">Whether more content follows this line</param>
    /// <returns>Processed line with appropriate spacing</returns>
    private string ProcessVirtualLine(string virtualLine, bool hasNextContent)
    {
        if (string.IsNullOrEmpty(virtualLine))
            return "";
        
        var maxColumns = GlobalSettings.MaxColumns;
        
        if (virtualLine.Length == maxColumns)
        {
            // Full-width line: add space if no trailing whitespace and more content follows
            if (!char.IsWhiteSpace(virtualLine[virtualLine.Length - 1]) && hasNextContent)
            {
                return virtualLine + " ";
            }
            else
            {
                // Trim trailing whitespace and add single space if more content follows
                var trimmed = virtualLine.TrimEnd();
                if (trimmed.Length < virtualLine.Length && hasNextContent)
                {
                    return trimmed + " ";
                }
                else
                {
                    return trimmed;
                }
            }
        }
        else
        {
            // Partial-width line: trim and add space if more content follows
            var trimmed = virtualLine.TrimEnd();
            if (hasNextContent && trimmed.Length > 0)
            {
                return trimmed + " ";
            }
            else
            {
                return trimmed;
            }
        }
    }

    #endregion

    #region File Operations

    /// <summary>
    /// Deletes the currently selected file after user confirmation.
    /// Handles various error conditions and updates UI after successful deletion.
    /// </summary>
    private async void DeleteFile()
    {
        var currentFile = GetCurrentSelectedFilePath();
        if (string.IsNullOrEmpty(currentFile))
        {
            Debug.WriteLine("No file selected for deletion.");
            return;
        }

        try
        {
            // Verify file exists before attempting deletion
            if (!File.Exists(currentFile))
            {
                Debug.WriteLine($"File does not exist: {currentFile}");
                return;
            }

            // Get user confirmation
            var fileName = Path.GetFileName(currentFile);
            var confirmMessage = $"Are you sure you want to delete file '{fileName}'?";

            var mainWindow = App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            var confirm = await ShowConfirmationDialog(mainWindow, "Delete File", confirmMessage);

            if (!confirm)
            {
                Debug.WriteLine("File deletion cancelled by user.");
                return;
            }

            // Perform deletion
            File.Delete(currentFile);

            Debug.WriteLine($"File deleted successfully: {currentFile}");

            // Update UI state
            RefreshAfterDelete();
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.WriteLine($"Access denied when deleting file: {ex.Message}");
            await ShowErrorDialog("Access Denied", $"Cannot delete file. Access denied: {ex.Message}");
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"IO error when deleting file: {ex.Message}");
            await ShowErrorDialog("IO Error", $"Cannot delete file. File may be in use: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unexpected error when deleting file: {ex.Message}");
            await ShowErrorDialog("Error", $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates UI state after successful file deletion.
    /// Clears content, refreshes file lists, and updates command availability.
    /// </summary>
    private void RefreshAfterDelete()
    {
        // Clear displayed content
        SelectedFileContent = null;

        // Refresh file lists while preserving selection context
        PreserveSelectionAndRefresh();

        // Update command availability
        _editCommand?.NotifyCanExecuteChanged();
        _deleteCommand?.NotifyCanExecuteChanged();
        _newCommand?.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Creates a new file at the currently selected location.
    /// Creates necessary directories and initializes file with minimal content.
    /// </summary>
    private async void NewFile()
    {
        string file = GetCurrentSelectedFilePath();
        
        if (string.IsNullOrEmpty(file))
        {
            Debug.WriteLine("No file path available.");
            return;
        }
       
        try
        {
            // Check if file already exists
            if (File.Exists(file))
            {
                Debug.WriteLine($"File already exists: {file}");
                return;
            }

            // Ensure target directory exists
            if (!EnsureDirectoryExistsForFile(file))
            {
                Debug.WriteLine($"Failed to create directory for file: {file}");
                await ShowErrorDialog("Error", "Cannot create directory for the file.");
                return;
            }

            // Create file with minimal content using ContentOperation
            var maxColumns = IsSimpleMode ? GlobalSettings.MaxColumns : GlobalSettings.MaxColumnsExt;
            var maxRows = IsSimpleMode ? GlobalSettings.MaxRows : GlobalSettings.MaxRowsExt;

            await ContentOperation.SaveDataAsync(file, " ", maxColumns, maxRows);

            Debug.WriteLine($"New file created: {file}");

            // Refresh UI while preserving selection
            PreserveSelectionAndRefresh();
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.WriteLine($"Access denied when creating file: {ex.Message}");
            await ShowErrorDialog("Access Denied", $"Cannot create file. Access denied: {ex.Message}");
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"IO error when creating file: {ex.Message}");
            await ShowErrorDialog("IO Error", $"Cannot create file: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unexpected error when creating file: {ex.Message}");
            await ShowErrorDialog("Error", $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Ensures that the directory structure exists for the specified file path.
    /// Creates parent directories as needed.
    /// </summary>
    /// <param name="filePath">File path requiring directory structure</param>
    /// <returns>True if directory exists or was created successfully</returns>
    private bool EnsureDirectoryExistsForFile(string filePath)
    {
        try
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            
            if (string.IsNullOrEmpty(directoryPath))
            {
                Debug.WriteLine($"Cannot determine directory path for file: {filePath}");
                return false;
            }

            if (!Directory.Exists(directoryPath))
            {
                // Create directory and all parent directories
                Directory.CreateDirectory(directoryPath);
                Debug.WriteLine($"Created directory: {directoryPath}");
            }
            else
            {
                Debug.WriteLine($"Directory already exists: {directoryPath}");
            }

            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.WriteLine($"Access denied creating directory for {filePath}: {ex.Message}");
            return false;
        }
        catch (DirectoryNotFoundException ex)
        {
            Debug.WriteLine($"Parent directory not found for {filePath}: {ex.Message}");
            return false;
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"IO error creating directory for {filePath}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unexpected error creating directory for {filePath}: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Command Availability Checks

    /// <summary>
    /// Determines if a file exists at the current selection.
    /// Checks appropriate selection based on current mode.
    /// </summary>
    /// <returns>True if selected file exists</returns>
    private bool IsFileExists()
    {
        if (IsSimpleMode)
        {
            return SelectedFile?.Exists == true;
        }
        else
        {
            return SelectedSubFile?.Exists == true;
        }
    }

    /// <summary>
    /// Determines if file deletion is available.
    /// Requires an existing file to be selected.
    /// </summary>
    /// <returns>True if delete operation is available</returns>
    private bool CanDeleteFile()
    {
        return IsFileExists();
    }

    /// <summary>
    /// Determines if new file creation is available.
    /// Requires a valid selection where no file currently exists.
    /// </summary>
    /// <returns>True if new file can be created</returns>
    private bool CanNewFile()
    {
        var currentFile = GetCurrentSelectedFilePath();
        if (string.IsNullOrEmpty(currentFile)) return false;
        return !File.Exists(currentFile);
    }

    /// <summary>
    /// Determines if file editing is available.
    /// Requires an existing file to be selected.
    /// </summary>
    /// <returns>True if edit operation is available</returns>
    private bool CanEditFile()
    {
        var currentFile = GetCurrentSelectedFilePath();
        if (string.IsNullOrEmpty(currentFile)) return false;
        return File.Exists(currentFile);
    }

    #endregion

    #region Settings Management

    /// <summary>
    /// Opens the settings dialog and handles result.
    /// Updates UI and reloads content if settings were changed.
    /// </summary>
    private async void OpenSettings()
    {
        try
        {
            var settingsDialog = new DisplayEditorApp.Views.SettingsView();

            var mainWindow = App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow == null)
            {
                System.Diagnostics.Debug.WriteLine("Cannot show settings dialog: MainWindow is null");
                return;
            }

            var result = await settingsDialog.ShowDialog<bool?>(mainWindow);

            if (result == true)
            {
                System.Diagnostics.Debug.WriteLine("Settings updated successfully");

                // Update display layout with new settings
                UpdateTextBoxSize();

                // Reload current file with new settings
                var currentFile = GetCurrentSelectedFilePath();
                if (!string.IsNullOrEmpty(currentFile))
                {
                    LoadFileContentAsync(currentFile);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening settings dialog: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads saved configuration from GlobalSettings.
    /// Restores last selected folder and mode preference.
    /// Called during ViewModel initialization.
    /// </summary>
    private void LoadSavedConfiguration()
    {
        try
        {
            // Restore saved folder path
            if (!string.IsNullOrWhiteSpace(GlobalSettings.LastSelectedFolder))
            {
                SelectedFolder = GlobalSettings.LastSelectedFolder;
            }

            // Restore saved mode preference
            IsSimpleMode = GlobalSettings.IsSimpleMode;

            Debug.WriteLine("Configuration loaded successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading configuration: {ex.Message}");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Shows a confirmation dialog to the user.
    /// Currently returns true by default - placeholder for actual dialog implementation.
    /// </summary>
    /// <param name="parent">Parent window for modal dialog</param>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Confirmation message</param>
    /// <returns>True if user confirms, False if cancelled</returns>
    private Task<bool> ShowConfirmationDialog(Window? parent, string title, string message)
    {
        try
        {
            Debug.WriteLine($"Confirmation dialog: {title} - {message}");
            return Task.FromResult(true); // TODO: Implement actual confirmation dialog
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Shows an error dialog to the user.
    /// Currently logs to debug output - placeholder for actual dialog implementation.
    /// </summary>
    /// <param name="title">Error dialog title</param>
    /// <param name="message">Error message</param>
    /// <returns>Completed task</returns>
    private Task ShowErrorDialog(string title, string message)
    {
        try
        {
            Debug.WriteLine($"Error dialog: {title} - {message}");
            return Task.CompletedTask; // TODO: Implement actual error dialog
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to show error dialog: {ex.Message}");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Refreshes folder content while preserving current selections.
    /// Saves selection state, reloads content, then restores selections.
    /// </summary>
    private void PreserveSelectionAndRefresh()
    {
        // Save current selection state
        var selectedFileName = SelectedFile?.Name;
        var selectedSubFileName = SelectedSubFile?.Name;
        
        // Reload content
        LoadFolderContent();
        
        // Restore selections
        RestoreSelection(selectedFileName, selectedSubFileName);
    }

    /// <summary>
    /// Restores selection state after content refresh.
    /// Attempts to reselect items by name in both primary and secondary collections.
    /// </summary>
    /// <param name="selectedFileName">Name of previously selected primary item</param>
    /// <param name="selectedSubFileName">Name of previously selected secondary item</param>
    private void RestoreSelection(string? selectedFileName, string? selectedSubFileName)
    {
        // Restore primary selection
        if (!string.IsNullOrEmpty(selectedFileName))
        {
            var fileToSelect = FilesInFolder.FirstOrDefault(f => f.Name == selectedFileName);
            if (fileToSelect != null)
            {
                SelectedFile = fileToSelect;
            }
        }

        // Restore secondary selection (Extended mode)
        if (!string.IsNullOrEmpty(selectedSubFileName))
        {
            var subFileToSelect = SubFolderFiles.FirstOrDefault(f => f.Name == selectedSubFileName);
            if (subFileToSelect != null)
            {
                SelectedSubFile = subFileToSelect;
            }
        }
    }

    #endregion
}

/// <summary>
/// Helper class for file information display.
/// Represents key-value pairs for file metadata.
/// Currently unused but available for future file information features.
/// </summary>
public class FileInfoItem
{
    /// <summary>Information label (e.g., "Size", "Date Modified")</summary>
    public string Key { get; set; }
    
    /// <summary>Information value (e.g., "1.2 KB", "2023-01-01")</summary>
    public string Value { get; set; }

    /// <summary>
    /// Creates a new file information item.
    /// </summary>
    /// <param name="key">Information label</param>
    /// <param name="value">Information value</param>
    public FileInfoItem(string key, string value)
    {
        Key = key;
        Value = value;
    }
}


