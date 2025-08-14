using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DisplayEditorApp.Helpers;

public static class EncodingHelper
{
                                    // Static constructor for registering encoding providers
    static EncodingHelper()
    {
        // Register encoding provider for CP1250
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    // Windows-1250 (CP1250) encoding
    public static Encoding CP1250 => Encoding.GetEncoding(1250);

    /// <summary>
    /// Asynchronously reads a file with CP1250 encoding and returns as UTF-8 string
    /// </summary>
    public static async Task<string> ReadFileCP1250Async(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        try
        {
            // Read bytes from file
            byte[] bytes = await File.ReadAllBytesAsync(filePath);

            // Decode from CP1250 to UTF-8 string
            return CP1250.GetString(bytes);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading file {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Synchronously reads a file with CP1250 encoding and returns as UTF-8 string
    /// </summary>
    public static string ReadFileCP1250(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        try
        {
            // Read bytes from file
            byte[] bytes = File.ReadAllBytes(filePath);

            // Decode from CP1250 to UTF-8 string
            return CP1250.GetString(bytes);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading file {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Asynchronously saves a string to file with CP1250 encoding
    /// </summary>
    public static async Task WriteFileCP1250Async(string filePath, string content)
    {
        try
        {
            // Convert UTF-8 string to CP1250 bytes
            byte[] bytes = CP1250.GetBytes(content);

            // Save bytes to file
            await File.WriteAllBytesAsync(filePath, bytes);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error saving file {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Synchronously saves a string to file with CP1250 encoding
    /// </summary>
    public static void WriteFileCP1250(string filePath, string content)
    {
        try
        {
            // Convert UTF-8 string to CP1250 bytes
            byte[] bytes = CP1250.GetBytes(content);

            // Save bytes to file
            File.WriteAllBytes(filePath, bytes);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error saving file {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Converts text from CP1250 to UTF-8 (for display)
    /// </summary>
    public static string ConvertCP1250ToUTF8(byte[] cp1250Bytes)
    {
        return CP1250.GetString(cp1250Bytes);
    }

    /// <summary>
    /// Converts text from UTF-8 to CP1250 (for saving)
    /// </summary>
    public static byte[] ConvertUTF8ToCP1250(string utf8Text)
    {
        return CP1250.GetBytes(utf8Text);
    }

    /// <summary>
    /// Detects and fixes some common encoding issues
    /// </summary>
    public static string FixEncodingIssues(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Fix some common encoding issues
        return text
            .Replace("Ã¡", "á")  // á
            .Replace("Ã¤", "ä")  // ä
            .Replace("Ã©", "é")  // é
            .Replace("Ã­", "í")  // í
            .Replace("Ã³", "ó")  // ó
            .Replace("Ãº", "ú")  // ú
            .Replace("Ã½", "ý")  // ý
            .Replace("Ä", "č")   // č
            .Replace("Ä", "ď")   // ď
            .Replace("Ä", "ě")   // ě
            .Replace("Å", "ň")   // ň
            .Replace("Å", "ř")   // ř
            .Replace("Å¡", "š")  // š
            .Replace("Å¥", "ť")  // ť
            .Replace("Å¯", "ů")  // ů
            .Replace("Å¾", "ž"); // ž
    }
}