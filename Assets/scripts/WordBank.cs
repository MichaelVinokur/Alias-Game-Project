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
            .Select(w => w.Trim())
            .Where(w => !string.IsNullOrEmpty(w))
            .Distinct()
            .ToList();
    }
}
