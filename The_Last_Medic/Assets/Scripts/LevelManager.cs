using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;
using System; // NavMeshAgent

public class LevelManager : MonoBehaviour
{
    public int numZombies;
    public int numAllies;
    public int playerScore;
    public float playerTime;
    public AudioSource attackSound;
    public AudioSource defendSound;
    public AudioSource allyDeathSound;




    public GameObject zombiesParent;
    public GameObject alliesParent;

    [Header("Player Root (gonna disable all scripts)")]
    public GameObject playerRoot;

    public TMP_Text alliesText;
    public TMP_Text zombiesText;
    public TMP_Text modeText;
    public TMP_Text timerText;
    public TMP_Text scoreText;

    [Header("Pause Panel")]
    public CanvasGroup endPanel;
    public TMP_Text endTitle;
    public TMP_Text endSubtitle;
    public Button retryButton;
    public Button nextLevelButton;
    public Button mainMenuButton;

    bool isCombatMode = false;
    bool gameEnded = false;
    bool playerVictory = false;

    // pause state
    bool isPaused = false;
    List<MonoBehaviour> cachedPlayerScripts = new List<MonoBehaviour>();
    List<NavMeshAgent> cachedAgents = new List<NavMeshAgent>();
    List<Animator> cachedAnimators = new List<Animator>();

    void Start()
    {
        // make sure gameplay runs at start
        playerTime = 600f;
        playerScore = 0;
        Time.timeScale = 1f;
        isPaused = false;
        AudioListener.pause = false;

        // init the starting count for all allies and zombies
        if (zombiesParent)
        {
            numZombies = zombiesParent.transform.childCount;
        }

        if (alliesParent)
        {
            numAllies = alliesParent.transform.childCount;
        }

        Debug.Log("# Allies: " + numAllies);
        Debug.Log("# Zombies: " + numZombies);

        // cache player scripts
        CachePlayerScripts();

        // cache agents & animators to pause/resume instantly
        CacheAgentsAndAnimators();

        // hide end/pause panel at start
        endPanel.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);
        nextLevelButton.gameObject.SetActive(false);
        mainMenuButton.gameObject.SetActive(false);

        UpdateUI();
    }

    void CachePlayerScripts()
    {
        cachedPlayerScripts.Clear();
        if (!playerRoot) return;
        var scripts = playerRoot.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in scripts)
        {
            if (mb == null) continue;
            if (mb == this) continue; // don't disable LevelManager
            cachedPlayerScripts.Add(mb);
        }
    }

    void CacheAgentsAndAnimators()
    {
        cachedAgents.Clear();
        cachedAnimators.Clear();

        void CollectFrom(GameObject root)
        {
            if (!root) return;
            cachedAgents.AddRange(root.GetComponentsInChildren<NavMeshAgent>(true));
            cachedAnimators.AddRange(root.GetComponentsInChildren<Animator>(true));
        }

        CollectFrom(zombiesParent);
        CollectFrom(alliesParent);
        CollectFrom(playerRoot); // in case the player uses NavMesh/Animator too
    }

    public void MainMenu()
    {
        Debug.Log("Main Menu Called");
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(0);
    }

    public void RestartLevel()
    {
        Debug.Log("Restart Level Called");
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToNextScene()
    {
        Debug.Log("Next Scene Called");
        Time.timeScale = 1f;
        AudioListener.pause = false;
        int cur = SceneManager.GetActiveScene().buildIndex;
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

    // Play Ally Death sound
    if (allyDeathSound != null)
        allyDeathSound.Play();

    UpdateUI();
}

    void Update()
    {
        if (gameEnded)
        {
            return;
        }

        // Pause toggle
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetPaused(!isPaused);
        }

        if (isPaused)
        {
            // no gameplay updates while paused
            PauseGame();
            return;
        }

        if (playerTime > 1)
        {
            playerTime -= Time.deltaTime;
        }
        else
        {
            playerTime = 0;
        }
        UpdateUI();

        // toggle ally mode with E
        // we use both input systems so this should work
      if (Input.GetKeyDown(KeyCode.E))
{
    ToggleAllyMode();
}



        // lose if all allies dead or time ran out
        if (numAllies <= 0 || playerTime <= 0)
        {
            gameEnded = true;
            PauseGame();
        }

        // win if all zombies are gone
        if (numZombies <= 0)
        {
            gameEnded = true;
            playerVictory = true;
            PauseGame();
        }
    }

    void UpdateUI()
    {
        if (alliesText)
        {
            alliesText.text = "Allies Alive: " + Mathf.Max(0, numAllies);
        }

        if (zombiesText)
        {
            zombiesText.text = "Zombies Alive: " + Mathf.Max(0, numZombies);
        }

        if (modeText)
        {
            modeText.text = "Mode: " + (isCombatMode ? "Combat" : "Hidden");
        }

        int mins = Mathf.FloorToInt(playerTime / 60);
        int secs = Mathf.FloorToInt(playerTime % 60);

        timerText.text = "Time: " + string.Format("{0:00}:{1:00}", mins, secs);

        scoreText.text = "Score: " + playerScore; //+ string.Format("{0000}", playerScore);
    }

    public bool getOrder() {
        return isCombatMode;
    }

    void ToggleAllyMode()
    {
        isCombatMode = !isCombatMode;

        // HIDDEN means stand down, COMBAT means attack
        string order = isCombatMode ? "Attack" : "StandDown";

        if (alliesParent)
        {
            foreach (Transform ally in alliesParent.transform)
            {
                if (ally == alliesParent.transform) continue;

                var stats = ally.GetComponent<AllyStats>();
                if (!stats) continue;

                // skip dead allies
                if (stats.state == AllyStats.AllyState.DEAD) continue;

                // delegate state change to AllyStats (keeps your writing style)
                stats.ReceiveOrder(order);

                // optional visual cue
                var rend = ally.GetComponentInChildren<Renderer>();
                if (rend) rend.material.color = isCombatMode ? Color.red : Color.blue;

                
            }
        }


   //  Play Sound
    if (isCombatMode)
    {
        if (attackSound != null) attackSound.Play();
    }
    else
    {
        if (defendSound != null) defendSound.Play();
    }
        UpdateUI();
    }

    public void AddScore(int points)
    {
        playerScore += points;
    }

    public void PauseGame()
    {
        SetPaused(true);

        endTitle.text = !gameEnded    ? "Game Paused":
                        playerVictory ? "VICTORY" : 
                                        "DEFEAT";
        endSubtitle.text = !gameEnded     ? "" :
                           playerVictory  ? "You Killed All The Zombies!" :
                           numAllies <= 0 ? "All Allies were Killed!" :
                                            "You Ran Out of Time!";

        endPanel.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(true);
        nextLevelButton.gameObject.SetActive(true);
        mainMenuButton.gameObject.SetActive(true);

        // stop the world like in Dark Souls
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    // Pause System

    void SetPaused(bool pause)
    {
        isPaused = pause;

        Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = pause ? false : true;
        //Cursor.lockState = CursorLockMode.None;
        //Cursor.visible = true;

        // Time & audio
        Time.timeScale = pause ? 0f : 1f;
        AudioListener.pause = pause;

        // Disable/enable all player scripts
        SetPlayerScriptsEnabled(!pause);

        // Stop/resume NavMeshAgents + Animators
        ApplyAgentsPaused(pause);
        ApplyAnimatorsPaused(pause);
    }

    void SetPlayerScriptsEnabled(bool enabled)
    {
        // refresh cache if someone swapped playerRoot at runtime
        if (cachedPlayerScripts.Count == 0 && playerRoot)
            CachePlayerScripts();

        foreach (var script in cachedPlayerScripts)
        {
            if (!script) continue;
            script.enabled = enabled;
        }
    }

    void ApplyAgentsPaused(bool paused)
    {
        if (cachedAgents.Count == 0) CacheAgentsAndAnimators();

        for (int i = 0; i < cachedAgents.Count; i++)
        {
            var ag = cachedAgents[i];
            if (!ag) continue;
            ag.isStopped = paused;
        }
    }

    void ApplyAnimatorsPaused(bool paused)
    {
        if (cachedAnimators.Count == 0) CacheAgentsAndAnimators();

        for (int i = 0; i < cachedAnimators.Count; i++)
        {
            var an = cachedAnimators[i];
            if (!an) continue;
            an.speed = paused ? 0f : 1f;
        }
    }
}
