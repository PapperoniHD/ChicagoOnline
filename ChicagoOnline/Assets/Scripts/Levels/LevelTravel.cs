using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTravel : MonoBehaviour
{
    public void GoToLevel(int levelIndex)
    {
        SceneManager.LoadScene(levelIndex);
    }
}
