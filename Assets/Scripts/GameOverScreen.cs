using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

public class GameOverScreen : MonoBehaviour
{
    public void Setup()
    {
        gameObject.SetActive(true);
    }
    
    public void RestartButton()
    {
        SceneManager.LoadScene("GameplayScene");
        Time.timeScale = 1;
    }
}
