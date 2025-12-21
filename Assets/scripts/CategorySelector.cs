using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CategorySelector : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    void Start()
    {
        var gm = GameManager.Instance;

        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.ClearOptions();

        var opts = new List<TMP_Dropdown.OptionData>();
        foreach (var cat in gm.categories)
            opts.Add(new TMP_Dropdown.OptionData(cat.categoryName));

        dropdown.AddOptions(opts);

        dropdown.SetValueWithoutNotify(gm.state.selectedCategoryIndex);
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.AddListener(OnChange);
    }

    void OnChange(int value)
    {
        Debug.Log("Category changed to index: " + value);
        GameManager.Instance.SetCategory(value);
        dropdown.RefreshShownValue();
    }
}
