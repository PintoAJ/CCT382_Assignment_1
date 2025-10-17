using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public int numZombies;
    public int numAllies;
    public int playerScore;
    public float playerTime;

    public GameObject zombiesParent;
    public GameObject alliesParent;

    public Text alliesText;
    public Text zombiesText;
    public Text modeText;
    public Text scoreText;

    public CanvasGroup endPanel;
    public Text endTitle;
    public Button retryButton;
    public Button nextButton;
    public Button menuButton;

    bool isCombatMode = false;
    bool gameEnded = false;

    void Start()
    {
        // init the starting count for all allies and zombies

        // for some reason the get GetComponentsInChildren includes the parrent so we -1
        if (zombiesParent)
        {
            numZombies = zombiesParent.GetComponentsInChildren<Transform>().Length - 1;
        }

        if (alliesParent) 
        {
            numAllies = alliesParent.GetComponentsInChildren<Transform>().Length - 1;
        }
 
        // hide end panel at start
        if (endPanel)
        {
            endPanel.alpha = 0;
            endPanel.interactable = false;
            endPanel.blocksRaycasts = false;
        }

        // hook up the button listeners
        retryButton.onClick.AddListener(RestartLevel);
        nextButton.onClick.AddListener(GoToNextScene);
        menuButton.onClick.AddListener(() => SceneManager.LoadScene(0));

        UpdateUI();
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToNextScene()
    {
        int cur = SceneManager.GetActiveScene().buildIndex;
        // I didnt know how to loop back to start so they said use mod
        // this should work
        int next = (cur + 1) % SceneManager.sceneCountInBuildSettings;
        SceneManager.LoadScene(next);
    }

    public void ZombieDown()
    {
        numZombies--;
        UpdateUI();
    }

    public void AllyDown()
    {
        numAllies--;
        UpdateUI();
    }

    void Update()
    {
        if (gameEnded)
        {
            return;
        }

        playerTime -= Time.deltaTime;
        UpdateUI();

        // toggle ally mode with E
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleAllyMode();
        }

        // lose if all allies dead or time ran out
        if (numAllies <= 0 || playerTime <= 0)
        {
            StartCoroutine(EndGame(false));
        }

        // win if all zombies are gone
        if (numZombies <= 0)
        {
            StartCoroutine(EndGame(true));
        }
    }

    void UpdateUI()
    {
        // show how many allies are still alive
        if (alliesText)
        {
            alliesText.text = "Allies Alive: " + Mathf.Max(0, numAllies);
        }

        // show how many zombies are still alive
        if (zombiesText)
        {
            zombiesText.text = "Zombies Alive: " + Mathf.Max(0, numZombies);
        }

        // show ally mode
        if (modeText)
        {
            modeText.text = "Mode: " + (isCombatMode ? "Combat" : "Hidden");
        }
    }

    void ToggleAllyMode()
    {
        isCombatMode = !isCombatMode;
        string order = isCombatMode ? "Attack" : "StandDown";

        // send new order to all allies
        if (alliesParent)
        {
            foreach (Transform ally in alliesParent.transform)
            {
                if (ally == alliesParent.transform)
                {
                    continue;
                }

                var allyStats = ally.GetComponent("AllyStats");
                if (allyStats != null)
                {
                    allyStats.SendMessage("ReceiveOrder", order, SendMessageOptions.DontRequireReceiver);
                }

                // change color depending on mode
                var rend = ally.GetComponentInChildren<Renderer>();
                if (rend)
                {
                    rend.material.color = isCombatMode ? Color.red : Color.blue;
                }
            }
        }

        UpdateUI();
    }

    IEnumerator EndGame(bool win)
    {
        gameEnded = true;
        playerScore = Mathf.RoundToInt(playerTime * 10f);

        endTitle.text = win ? "VICTORY" : "DEFEAT";
        scoreText.text = "Score: " + playerScore;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime;
            if (endPanel)
            {
                endPanel.alpha = Mathf.Lerp(0f, 0.95f, t);
                endPanel.interactable = true;
                endPanel.blocksRaycasts = true;
            }
            yield return null;
        }

        // stop the world like in Dark Souls
        Time.timeScale = 0f;
    }
}