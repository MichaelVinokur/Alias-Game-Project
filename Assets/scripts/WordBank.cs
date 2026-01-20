using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WordBank
{
    public static List<string> LoadCategory(string fileNameNoExt)
    {
        TextAsset ta = Resources.Load<TextAsset>($"Words/{fileNameNoExt}");
        if (ta == null)
        {
            Debug.LogError($"WordBank: Missing file Resources/Words/{fileNameNoExt}.txt");
            return new List<string>();
        }

        return ta.text
    .Split('\n')
    .Select(w =>
    {
        if (string.IsNullOrEmpty(w)) return "";

        // normalize line
        w = w.Replace("\r", "").Trim();

        // remove common invisible / direction chars
        string[] bad = {
            "\u200F", // RLM
            "\u200E", // LRM
            "\u202A", // LRE
            "\u202B", // RLE
            "\u202C", // PDF
            "\u202D", // LRO
            "\u202E", // RLO
            "\u2066", // LRI
            "\u2067", // RLI
            "\u2068", // FSI
            "\u2069", // PDI
            "\uFEFF", // BOM
            "\u200B", // ZWSP
            "\u200C", // ZWNJ
            "\u200D", // ZWJ
            "\u00A0"  // NBSP (non-breaking space)
        };

        foreach (var ch in bad)
            w = w.Replace(ch, "");

        // collapse multiple spaces
        while (w.Contains("  "))
            w = w.Replace("  ", " ");

        return w.Trim();
    })
    .Where(w => !string.IsNullOrEmpty(w))
    .Distinct()
    .ToList();
    }
}
