using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI; // NavMeshAgent

public class LevelManager : MonoBehaviour
{
    public int numZombies;
    public int numAllies;
    public int playerScore;
    public float playerTime;

    public GameObject zombiesParent;
    public GameObject alliesParent;

    [Header("Player Root (gonna disable all scripts)")]
    public GameObject playerRoot;

    public TMP_Text alliesText;
    public TMP_Text zombiesText;
    public TMP_Text modeText;
    public TMP_Text scoreText;

    [Header("End Panel")]
    public CanvasGroup endPanel;
    public TMP_Text endTitle;
    public Button retryButton;
    public Button nextButton;
    public Button menuButton;

    [Header("Pause Panel")]
    public CanvasGroup pausePanel;
    public Button pauseResumeButton;
    public Button pauseRetryButton;
    public Button pauseSkipButton;
    public Button pauseMenuButton;

    bool isCombatMode = false;
    bool gameEnded = false;

    // pause state
    bool isPaused = false;
    List<MonoBehaviour> cachedPlayerScripts = new List<MonoBehaviour>();
    List<NavMeshAgent> cachedAgents = new List<NavMeshAgent>();
    List<Animator> cachedAnimators = new List<Animator>();

    void Start()
    {
        // make sure gameplay runs at start
        playerTime = 600f;
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

        // hide end panel at start
        if (endPanel)
        {
            endPanel.alpha = 0;
            endPanel.interactable = false;
            endPanel.blocksRaycasts = false;
        }

        // hide pause panel at start
        if (pausePanel)
        {
            pausePanel.alpha = 0f;
            pausePanel.interactable = false;
            pausePanel.blocksRaycasts = false;
        }

        // hook up the END panel button listeners (game over)
        if (retryButton) retryButton.onClick.AddListener(RestartLevel);
        if (nextButton) nextButton.onClick.AddListener(GoToNextScene);
        if (menuButton) menuButton.onClick.AddListener(() => SceneManager.LoadScene(0));

        // hook up the PAUSE panel button listeners
        if (pauseResumeButton) pauseResumeButton.onClick.AddListener(() => SetPaused(false));
        if (pauseRetryButton) pauseRetryButton.onClick.AddListener(RestartLevel);
        if (pauseSkipButton) pauseSkipButton.onClick.AddListener(GoToNextScene);
        if (pauseMenuButton) pauseMenuButton.onClick.AddListener(() => SceneManager.LoadScene(0));

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

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToNextScene()
    {
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
            return;
        }

        playerTime -= Time.deltaTime;
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

        UpdateUI();
    }

    IEnumerator EndGame(bool win)
    {
        gameEnded = true;
        playerScore = Mathf.RoundToInt(playerTime * 10f);

        if (endTitle) endTitle.text = win ? "VICTORY" : "DEFEAT";
        if (scoreText) scoreText.text = "Score: " + playerScore;

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
        AudioListener.pause = true;
    }

    // Pause System

    void SetPaused(bool pause)
    {
        if (gameEnded) return; // end screen owns the timescale

        isPaused = pause;

        // UI
        if (pausePanel)
        {
            pausePanel.alpha = pause ? 1f : 0f;
            pausePanel.interactable = pause;
            pausePanel.blocksRaycasts = pause;
        }

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
