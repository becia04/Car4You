using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Car4You.Helpers
{
    public static class FileHelper
    {
        public static string NormalizeFileName(string name)
        {
            string normalized = name.ToLower();
            normalized = Regex.Replace(normalized.Normalize(NormalizationForm.FormD), @"[\u0300-\u036f]", ""); // Usunięcie akcentów
            normalized = normalized.Replace(" ", "_"); // Zamiana spacji na _
            normalized = Regex.Replace(normalized, @"[^a-z0-9_]", ""); // Usunięcie niedozwolonych znaków
            return normalized;
        }
    }
}
