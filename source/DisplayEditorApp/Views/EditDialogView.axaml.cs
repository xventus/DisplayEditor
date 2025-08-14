//
// vim: ts=4 et
// Copyright (c) 2025 Petr Vanek
// @author Petr Vanek, petr@fotoventus.cz

using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using DisplayEditorApp.Settings;
using DisplayEditorApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DisplayEditorApp.Views;

/// <summary>
/// File editing dialog with custom text formatting and validation.
/// Provides a rich text editor with undo/redo functionality, character validation,
/// and automatic text overflow handling according to column/row constraints.
/// Features real-time syntax highlighting for allowed/disallowed characters.
/// </summary>
public partial class EditDialogView : Window
{
    // State management flags to prevent recursive updates
    private bool _isUpdatingText = false;
    private bool _inUndoRedo = false;

    // Custom undo/redo system (TextBox default undo is disabled)
    private Stack<string> _undoStack = new();
    private Stack<string> _redoStack = new();

    /// <summary>
    /// Initializes the edit dialog with default settings.
    /// Sets up event handlers for keyboard input and data context changes.
    /// </summary>
    public EditDialogView()
    {
        InitializeComponent();
        this.DataContextChanged += OnDataContextChanged;
        this.KeyDown += OnWindowKeyDown;
        
        // Set minimum dialog size for usability
        MinWidth = 400;
        MinHeight = 250;
    }

    /// <summary>
    /// Handles global keyboard shortcuts for undo/redo and Enter key processing.
    /// Intercepts Ctrl+Z (undo), Ctrl+Y (redo), and Enter (line break) operations.
    /// </summary>
    /// <param name="sender">Window that received the key event</param>
    /// <param name="e">Key event arguments</param>
    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        var textBox = this.FindControl<TextBox>("EditableTextBox");
        if (textBox == null || !textBox.IsFocused) return;

        // Handle Ctrl+Z (Undo) and Ctrl+Y (Redo)
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (e.Key == Key.Z)
            {
                e.Handled = true;
                PerformUndo(textBox);
                return;
            }
            else if (e.Key == Key.Y)
            {
                e.Handled = true;
                PerformRedo(textBox);
                return;
            }
        }

        // Handle Enter key for custom line break behavior
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            HandleEnterKey(textBox);
        }
    }

    /// <summary>
    /// Performs undo operation by restoring previous text state.
    /// Manages undo/redo stacks and updates both TextBox and ViewModel.
    /// </summary>
    /// <param name="textBox">TextBox control to operate on</param>
    private async void PerformUndo(TextBox textBox)
    {
        // Need at least 2 items in stack (current + previous)
        if (_undoStack.Count < 2)
        {
            System.Diagnostics.Debug.WriteLine("PerformUndo: Nothing to undo");
            return;
        }

        // Set flags to prevent recursive updates
        _inUndoRedo = true;
        _isUpdatingText = true;

        // Move current state to redo stack and restore previous state
        var currentText = _undoStack.Pop();
        var previousText = _undoStack.Peek();
        _redoStack.Push(currentText);

        System.Diagnostics.Debug.WriteLine($"PerformUndo: Switched from '{currentText}' to '{previousText}'");

        // Update TextBox and maintain caret position
        textBox.Text = previousText;
        textBox.CaretIndex = Math.Min(previousText.Length, textBox.CaretIndex);

        // Synchronize with ViewModel
        if (DataContext is EditDialogViewModel viewModel)
        {
            viewModel.FileContent = previousText;
        }

        // Update visual representation
        UpdateColoredText(previousText);

        // Reset flags asynchronously to prevent timing issues
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _isUpdatingText = false;
            _inUndoRedo = false;
        });

        System.Diagnostics.Debug.WriteLine("Undo completed");
    }

    /// <summary>
    /// Performs redo operation by restoring next text state.
    /// Manages undo/redo stacks and updates both TextBox and ViewModel.
    /// </summary>
    /// <param name="textBox">TextBox control to operate on</param>
    private async void PerformRedo(TextBox textBox)
    {
        // Check if there's anything to redo
        if (_redoStack.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("PerformRedo: Nothing to redo");
            return;
        }

        // Set flags to prevent recursive updates
        _inUndoRedo = true;
        _isUpdatingText = true;

        // Restore next state from redo stack
        var nextText = _redoStack.Pop();
        _undoStack.Push(nextText);

        System.Diagnostics.Debug.WriteLine($"PerformRedo: Redo to '{nextText}'");

        // Update TextBox and maintain caret position
        textBox.Text = nextText;
        textBox.CaretIndex = Math.Min(nextText.Length, textBox.CaretIndex);

        // Synchronize with ViewModel
        if (DataContext is EditDialogViewModel viewModel)
        {
            viewModel.FileContent = nextText;
        }

        // Update visual representation
        UpdateColoredText(nextText);

        // Reset flags asynchronously to prevent timing issues
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _isUpdatingText = false;
            _inUndoRedo = false;
        });

        System.Diagnostics.Debug.WriteLine("Redo completed");
    }

    /// <summary>
    /// Handles DataContext changes to update visual representation.
    /// Called when EditDialogViewModel is set or changed.
    /// </summary>
    /// <param name="sender">Object that triggered the event</param>
    /// <param name="e">Event arguments</param>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is EditDialogViewModel viewModel)
        {
            UpdateColoredText(viewModel.FileContent);
        }
    }

    /// <summary>
    /// Initializes the edit dialog with specific file and mode settings.
    /// Creates EditDialogViewModel and sets up file loading and event handling.
    /// </summary>
    /// <param name="filePath">Path to the file being edited</param>
    /// <param name="simple">True for Simple mode, False for Extended mode</param>
    public EditDialogView(string filePath, bool simple) : this()
    {
        var viewModel = new EditDialogViewModel(filePath, simple);
        DataContext = viewModel;

        // Handle dialog close requests from ViewModel
        viewModel.RequestClose += (result) =>
        {
            Close(result);
        };

        // Set up content loading when dialog is fully loaded
        this.Loaded += (sender, e) =>
        {
            // Asynchronously load file content
            viewModel.LoadContentAsync();

            var textBox = this.FindControl<TextBox>("EditableTextBox");
            System.Diagnostics.Debug.WriteLine($"LOADED EVENT: TextBox.Text = '{textBox?.Text}'");

            if (textBox != null)
            {
                // Disable default TextBox undo system (we use custom implementation)
                textBox.IsUndoEnabled = false;

                // Ensure TextBox content matches ViewModel
                if (viewModel.FileContent != textBox.Text)
                {
                    textBox.Text = viewModel.FileContent;
                }

                // Initialize undo system with initial content
                _undoStack.Clear();
                _redoStack.Clear();
                _undoStack.Push(textBox.Text ?? "");

                System.Diagnostics.Debug.WriteLine($"Initial undo stack: '{_undoStack.Peek()}'");
            }

            // Update visual representation
            UpdateColoredText(viewModel.FileContent);
        };
    }

    /// <summary>
    /// Updates the colored text display based on character validation rules.
    /// Shows allowed characters in green (Lime) and disallowed characters in red.
    /// Lines exceeding row limits are highlighted in red.
    /// </summary>
    /// <param name="content">Text content to process and display</param>
    private void UpdateColoredText(string content)
    {
        var textBlock = this.FindControl<TextBlock>("ColoredTextBlock");
        if (textBlock == null) return;

        textBlock.Inlines.Clear();

        // Handle empty content
        if (string.IsNullOrEmpty(content))
        {
            textBlock.Inlines.Add(new Run("(EMPTY)") { Foreground = Brushes.Gray });
            return;
        }

        // Process each line for character validation and row limit checking
        var lines = content.Split('\n');
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];

            if (lineIndex > 0)
            {
                textBlock.Inlines.Add(new LineBreak());
            }

            ProcessLineCharacters(textBlock, line, lineIndex);
        }
    }

    /// <summary>
    /// Processes individual characters in a line for color coding.
    /// Applies different colors based on character validation and row limits.
    /// </summary>
    /// <param name="textBlock">TextBlock to add colored runs to</param>
    /// <param name="line">Line of text to process</param>
    /// <param name="lineIndex">Zero-based line index for row limit checking</param>
    private void ProcessLineCharacters(TextBlock textBlock, string line, int lineIndex)
    {
        if (DataContext is EditDialogViewModel viewModel)
        {
            // Lines beyond MaxRows limit are displayed in red
            if (lineIndex >= viewModel.MaxRows)
            {
                var displayLine = string.IsNullOrEmpty(line) ? " " : line;
                textBlock.Inlines.Add(new Run(displayLine) { Foreground = Brushes.Red });
                return;
            }

            // Empty lines within limits are displayed in green
            if (string.IsNullOrEmpty(line))
            {
                textBlock.Inlines.Add(new Run("") { Foreground = Brushes.Lime });
                return;
            }

            // Process each character for validation coloring
            var currentRun = new System.Text.StringBuilder();
            IBrush currentColor = null;

            for (int charIndex = 0; charIndex < line.Length; charIndex++)
            {
                var character = line[charIndex];
                var charColor = GetCharacterColor(character);

                // Start new run when color changes
                if (currentColor != charColor)
                {
                    if (currentRun.Length > 0)
                    {
                        textBlock.Inlines.Add(new Run(currentRun.ToString()) { Foreground = currentColor });
                        currentRun.Clear();
                    }
                    currentColor = charColor;
                }

                currentRun.Append(character);
            }

            // Add final run if any characters remain
            if (currentRun.Length > 0)
            {
                textBlock.Inlines.Add(new Run(currentRun.ToString()) { Foreground = currentColor });
            }
        }
    }

    /// <summary>
    /// Determines the color for a character based on validation rules.
    /// Uses GlobalSettings.AllowedCharacters for validation.
    /// </summary>
    /// <param name="character">Character to validate</param>
    /// <returns>Lime brush for allowed characters, Red brush for disallowed</returns>
    private IBrush GetCharacterColor(char character)
    {
        // Green for allowed characters
        if (GlobalSettings.IsCharacterAllowed(character))
        {
            return Brushes.Lime;
        }

        // Red for disallowed characters
        return Brushes.Red;
    }

    /// <summary>
    /// Handles Enter key press by splitting the current line at cursor position.
    /// Creates a new line and maintains proper cursor positioning.
    /// Updates undo stack and visual representation.
    /// </summary>
    /// <param name="textBox">TextBox control to operate on</param>
    private void HandleEnterKey(TextBox textBox)
    {
        var text = textBox.Text ?? string.Empty;
        var caretIndex = textBox.CaretIndex;

        var lines = text.Split('\n').ToList();
        var currentLineIndex = GetCurrentLineIndex(text, caretIndex);

        var currentLineStart = GetLineStartPosition(text, currentLineIndex);
        var positionInLine = caretIndex - currentLineStart;

        var currentLine = currentLineIndex < lines.Count ? lines[currentLineIndex] : string.Empty;

        // Split current line at cursor position
        var beforeCursor = currentLine.Substring(0, Math.Min(positionInLine, currentLine.Length));
        var afterCursor = currentLine.Substring(Math.Min(positionInLine, currentLine.Length));

        lines[currentLineIndex] = beforeCursor;
        lines.Insert(currentLineIndex + 1, afterCursor);

        // Set flag before text change to prevent recursive validation
        _isUpdatingText = true;
        textBox.Text = string.Join('\n', lines);
        textBox.CaretIndex = currentLineStart + beforeCursor.Length + 1;
        
        // Update ViewModel with new content
        if (DataContext is EditDialogViewModel viewModel)
        {
            viewModel.FileContent = textBox.Text;
        }
        
        // Update undo stack with new state
        var validatedText = textBox.Text ?? "";
        if (_undoStack.Count == 0 || _undoStack.Peek() != validatedText)
        {
            _undoStack.Push(validatedText);
            _redoStack.Clear();
        }
        
        UpdateColoredText(validatedText);
        
        // Reset flag with slight delay to prevent timing issues
        Dispatcher.UIThread.Post(() => {
            _isUpdatingText = false;
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// Validates and formats text according to column/row constraints.
    /// Handles text overflow by wrapping content to next lines.
    /// Maintains cursor position during formatting operations.
    /// </summary>
    /// <param name="textBox">TextBox control to validate and format</param>
    private void ValidateAndFormatText(TextBox textBox)
    {
        var text = textBox.Text ?? string.Empty;
        var caretIndex = textBox.CaretIndex;

        if (DataContext is not EditDialogViewModel viewModel)
            return;

        int maxRows = viewModel.MaxRows;
        int maxColumns = viewModel.MaxColumns;
        
        var lines = text.Split('\n').ToList();
        var newCaretIndex = caretIndex; // Track new cursor position
        var characterPosition = 0; // Current character position in entire text
        var caretAdjustment = 0; // How many characters we added/removed before cursor
        
        // Process each line - handle text overflow to next lines
        for (int i = 0; i < lines.Count; i++)
        {
            var currentLine = lines[i];
            var lineStartPosition = characterPosition;
            
            // If line exceeds MaxColumns
            while (currentLine.Length > maxColumns)
            {
                // Trim to MaxColumns
                var keepPart = currentLine.Substring(0, maxColumns);
                var overflowPart = currentLine.Substring(maxColumns);
                
                lines[i] = keepPart;
                
                // Check if cursor was in this part of the line
                if (caretIndex >= lineStartPosition && caretIndex <= lineStartPosition + currentLine.Length)
                {
                    var positionInOriginalLine = caretIndex - lineStartPosition;
                    
                    if (positionInOriginalLine > maxColumns)
                    {
                        // Cursor is in overflowing part - move it to next line
                        var newLinePosition = positionInOriginalLine - maxColumns;
                        
                        if (i + 1 < lines.Count)
                        {
                            // Cursor will be at start of next line + offset in overflowing part
                            newCaretIndex = lineStartPosition + maxColumns + 1 + newLinePosition; // +1 for \n
                        }
                        else
                        {
                            // Adding new line
                            newCaretIndex = lineStartPosition + maxColumns + 1 + newLinePosition; // +1 for \n
                        }
                    }
                }
                
                // Transfer overflow text to next line
                if (i + 1 < lines.Count)
                {
                    // Append to existing line
                    lines[i + 1] = overflowPart + lines[i + 1];
                    currentLine = lines[i + 1]; // Continue processing next line
                    i++; // Move to next line
                    
                    // Update position for next line
                    characterPosition = lineStartPosition + maxColumns + 1; // +1 for \n
                }
                else
                {
                    // Add new line at end
                    lines.Add(overflowPart);
                    caretAdjustment += 1; // Added \n
                    break; // End while loop
                }
            }
            
            // If line wasn't changed, continue normally
            if (currentLine.Length <= maxColumns)
            {
                characterPosition += currentLine.Length + 1; // +1 for \n
            }
        }
        
        // Ensure minimum number of lines (MaxRows)
        while (lines.Count < maxRows)
        {
            lines.Add(string.Empty);
        }
        
        var newText = string.Join('\n', lines);
        
        // Apply changes if text was modified
        if (newText != text)
        {
            _isUpdatingText = true;
            textBox.Text = newText;
            
            // Use recalculated cursor position
            textBox.CaretIndex = Math.Min(Math.Max(0, newCaretIndex), newText.Length);
            _isUpdatingText = false;
        }

        // Update ViewModel with current content
        viewModel.FileContent = textBox.Text ?? string.Empty;
    }

    /// <summary>
    /// Gets the line index where the cursor is currently positioned.
    /// </summary>
    /// <param name="text">Full text content</param>
    /// <param name="caretIndex">Current cursor position</param>
    /// <returns>Zero-based line index</returns>
    private int GetCurrentLineIndex(string text, int caretIndex)
    {
        var textBeforeCaret = text.Substring(0, Math.Min(caretIndex, text.Length));
        return textBeforeCaret.Count(c => c == '\n');
    }

    /// <summary>
    /// Gets the character position where a specific line starts.
    /// </summary>
    /// <param name="text">Full text content</param>
    /// <param name="lineIndex">Zero-based line index</param>
    /// <returns>Character position of line start</returns>
    private int GetLineStartPosition(string text, int lineIndex)
    {
        var lines = text.Split('\n');
        var position = 0;

        for (int i = 0; i < lineIndex && i < lines.Length; i++)
        {
            position += lines[i].Length + 1;
        }

        return position;
    }

    /// <summary>
    /// Handles text changes in the main TextBox control.
    /// Triggers validation, formatting, and visual updates.
    /// Updates undo stack when content changes.
    /// </summary>
    /// <param name="sender">TextBox that triggered the event</param>
    /// <param name="e">Text change event arguments</param>
    private void OnTextBoxTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        // Skip validation if we're currently updating text or in undo/redo operation
        if (_isUpdatingText || _inUndoRedo)
            return;

        // Validate and format the text content
        ValidateAndFormatText(textBox);
        var validatedText = textBox.Text ?? "";

        // Update undo stack when content changes
        if (_undoStack.Count == 0 || _undoStack.Peek() != validatedText)
        {
            _undoStack.Push(validatedText);
            _redoStack.Clear();
        }

        // Update visual representation
        UpdateColoredText(validatedText);

        // Synchronize with ViewModel
        if (DataContext is EditDialogViewModel viewModel)
        {
            viewModel.FileContent = validatedText;
        }
    }
}