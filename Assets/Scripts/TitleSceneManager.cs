using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSceneManager : MonoBehaviour
{
    public Button gameStartButton;
    public Button saidanButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // ボタンにクリックイベントを追加
        gameStartButton.onClick.AddListener(StartGame);
        saidanButton.onClick.AddListener(SaidanGo);
    }

    void StartGame()
    {
        SceneManager.LoadScene("Match3Scene");
    }

    void SaidanGo()
    {
        SceneManager.LoadScene("SaidanMainScene");
    }

    // Update is called once per frame
    void Update()
    {

    }
}
