using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    
    [Header("Game Settings")]
    [SerializeField] private int totalCoins = 0;
    [SerializeField] private int score = 0;
    
    private static GameManager _instance;
    
    // Singleton pattern
    public static GameManager Instance
    {
        get { return _instance; }
    }
    
    private void Awake()
    {
        // Ensure there's only one GameManager
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Count total coins in the level for potential completion tracking
        Coin[] coins = FindObjectsByType<Coin>(FindObjectsSortMode.None);
        totalCoins = coins.Length;
    }
    
    private void Start()
    {
        // Initialize score display
        UpdateScoreDisplay();
    }
    
    public void AddScore(int points)
    {
        score += points;
        UpdateScoreDisplay();
        
        // Check if all coins collected
        if (score >= totalCoins)
        {
            Debug.Log("All coins collected!");
            // Trigger level completion logic here
        }
    }
    
    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = "Coins: " + score;
        }
    }
}