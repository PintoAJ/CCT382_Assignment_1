using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public int numZombies;
    public int numAllies;
    public int playerScore;
    public float playerTime;

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToNextScene()
    {
        int cur = SceneManager.GetActiveScene().buildIndex;
        int next = cur + 1;
        int total = SceneManager.sceneCountInBuildSettings;
        if (next >= total)
        {
            next = 0;
        }
        SceneManager.LoadScene(next);
    }

    public void ZombieDown()
    {
        numZombies--;
    }

    public void AllyDown()
    {
        numAllies--;
    }

    public void Update()
    {
        playerTime -= Time.deltaTime;

        if (numAllies <= 0)
        {
            // player lose screen; all allies died
            // two options: main menu and retry level
        }

        if (playerTime <= 0)
        {
            // player lose screen; rab out of time
            // two options: main menu and retry level
        }

        if (numZombies <= 0)
        {
            // you win!
            //display score at end fo each level. 
        }
    }
}