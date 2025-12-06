using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class WordController : MonoBehaviour
{
    public TMP_Text wordText;
    public TMP_Text timerText;
    public TMP_Text scoreText;

    public Button plusButton;
    public Button minusButton;

    public float roundTime = 60f;

    private float timeLeft;
    private int score = 0;

    private List<string> words = new List<string> { "Apple", "Banana", "Cat", "Dog" }; // fix later

    void Start()
    {
        timeLeft = roundTime;
        score = 0;
        UpdateScoreText();
        NextWord();

        plusButton.onClick.AddListener(OnPlusClicked);
        minusButton.onClick.AddListener(OnMinusClicked);
    }

    void Update()
    {
        if (timeLeft > 0f)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft < 0f) timeLeft = 0f;

            timerText.text = Mathf.CeilToInt(timeLeft).ToString();
        }
        else
        {
            plusButton.interactable = false;
            minusButton.interactable = false;
            SceneManager.LoadScene("MainMenu"); // goes back to the main squad page.
        }
    }

    void OnPlusClicked()
    {
        score++;
        UpdateScoreText();
        NextWord();
    }
    void OnMinusClicked()
    {
        score--;
        UpdateScoreText();
        NextWord();
    }

    void UpdateScoreText()
    {
        scoreText.text = score.ToString();
    }

    void NextWord()
    {
        if (words.Count == 0)
        {
            wordText.text = "-";
            return;
        }

        int index = Random.Range(0, words.Count);
        wordText.text = words[index];
    }
}
