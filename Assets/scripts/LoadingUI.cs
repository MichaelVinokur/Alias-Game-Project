using System.Collections;
using TMPro;
using UnityEngine;

public class LoadingUI : MonoBehaviour
{
    public GameObject badConnectionPanel;
    public TMP_Text joinErrorText;

    public void ShowError(string msg, float seconds = 1.5f)
    {
        StopAllCoroutines();
        StartCoroutine(Show(msg, seconds));
    }

    private IEnumerator Show(string msg, float seconds)
    {
        if (joinErrorText) joinErrorText.text = msg;
        if (badConnectionPanel) badConnectionPanel.SetActive(true);

        yield return new WaitForSeconds(seconds);

        if (badConnectionPanel) badConnectionPanel.SetActive(false);
    }
}
