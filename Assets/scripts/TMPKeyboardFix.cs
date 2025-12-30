using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class ForceKeyboard : MonoBehaviour, IPointerClickHandler
{
    private TMP_InputField inputField;
    private TouchScreenKeyboard keyboard;

    void Start()
    {
        inputField = GetComponent<TMP_InputField>();
    }

    void Update()
    {
        if (inputField == null) return;
        if (keyboard == null) return;

        // If there is no EventSystem, don't crash
        if (EventSystem.current == null) return;

        bool isThisFieldSelected = (EventSystem.current.currentSelectedGameObject == gameObject);

        // visible is safer than status on some platforms
        if (isThisFieldSelected && TouchScreenKeyboard.visible)
        {
            inputField.text = keyboard.text;
        }

        // When keyboard is not visible, just clear it (DON'T read keyboard.status)
        if (!TouchScreenKeyboard.visible)
        {
            keyboard = null;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inputField == null) return;
        if (EventSystem.current == null) return;
        if (!TouchScreenKeyboard.isSupported) return;

        EventSystem.current.SetSelectedGameObject(gameObject);
        inputField.ActivateInputField();
        keyboard = TouchScreenKeyboard.Open(inputField.text, TouchScreenKeyboardType.Default);
    }
}


