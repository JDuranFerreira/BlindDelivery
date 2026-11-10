using System;

namespace Template.Core.Save
{
    /// <summary>Validates document keys. Keys become file names, so path punctuation is rejected.</summary>
    public static class SaveKey
    {
        public static void Validate(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("A save key is required.", nameof(key));
            }

            foreach (char c in key)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.')
                {
                    throw new ArgumentException(
                        $"Save key '{key}' contains '{c}'. Keys are file names: letters, digits, '_', '-' and '.' only.",
                        nameof(key));
                }
            }
        }
    }
}