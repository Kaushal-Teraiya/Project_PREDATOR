using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Gameplay")]
    [SerializeField] private Toggle dismembermentToggle;
    [SerializeField] private InputActionReference toggleAction;

    [Header("Zombie Spawner")]
    [SerializeField] private ZombieSpawner zombieSpawner;
    [SerializeField] private GameObject tutorialPanel;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        dismembermentToggle.isOn = GameplaySettings.dismembermentEnabled;
        dismembermentToggle.onValueChanged.AddListener(OnDismemberToggleChanged);
    }

    private void Update()
    {
        if (toggleAction.action.WasPressedThisFrame())
        {
            dismembermentToggle.isOn = !dismembermentToggle.isOn;
        }
    }

    private void OnDismemberToggleChanged(bool enabled)
    {
        GameplaySettings.dismembermentEnabled = enabled;
    }
    public void ShowTutorialPanel()
    {
        tutorialPanel.SetActive(true);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    public void HideTutorialPanel()
    {
        tutorialPanel.SetActive(false);
    }
    // =========================
    // UI Button Functions
    // =========================

    public void SpawnZombies()
    {
        zombieSpawner.SpawnZombies();
    }

    public void GeneralTesting()
    {
        SceneManager.LoadScene("TestingArena");
    }

    public void CrawlerTesting()
    {
        SceneManager.LoadScene("WallCrawlerTest");
    }

    public void LightTesting()
    {
        SceneManager.LoadScene("LightTesting");
    }

    public void FlatGround()
    {
        SceneManager.LoadScene("FlatGround");
    }
}