using TMPro;
using UnityEngine;

public class CategorySelector : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    void Start()
    {
        dropdown.ClearOptions();

        var gm = GameManager.Instance;

        foreach (var cat in gm.categories)
            dropdown.options.Add(new TMP_Dropdown.OptionData(cat.categoryName));

        dropdown.onValueChanged.AddListener(OnChange);

        gm.SetCategory(dropdown.value);
    }

    void OnChange(int value)
    {
        GameManager.Instance.SetCategory(value);
    }
}
