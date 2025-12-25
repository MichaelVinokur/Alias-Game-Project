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
        // בדיקה קריטית: האם השדה הזה הוא האובייקט שנבחר כרגע ב-EventSystem?
        bool isThisFieldSelected = EventSystem.current.currentSelectedGameObject == gameObject;

        if (isThisFieldSelected && keyboard != null && TouchScreenKeyboard.visible)
        {
            inputField.text = keyboard.text;
        }

        // אם המקלדת נסגרה, ננקה את המשתנה כדי שלא "ייזכר" לפעם הבאה
        if (keyboard != null && !TouchScreenKeyboard.visible && keyboard.status != TouchScreenKeyboard.Status.Visible)
        {
            keyboard = null;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inputField != null)
        {
            // מסמנים למערכת שזה השדה הפעיל
            EventSystem.current.SetSelectedGameObject(gameObject);

            inputField.ActivateInputField();
            // פותחים את המקלדת עם הטקסט הקיים בשדה הספציפי הזה
            keyboard = TouchScreenKeyboard.Open(inputField.text, TouchScreenKeyboardType.Default);
        }
    }
}


