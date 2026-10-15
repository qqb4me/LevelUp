using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClickerController : MonoBehaviour
{
    [Header("Coin Settings")]
    [SerializeField] private long _coins = 0;
    [SerializeField] private long _coinsPerClick = 10;
    [SerializeField] private float _coinsPerSecond = 0f;

    [Header("Upgrade Settings")]
    [SerializeField] private float _baseUpgradeCost = 500f;
    [SerializeField] private float _upgradeCostMultiplier = 1.15f;
    [SerializeField] private float _upgradeCoinsPerSecondIncrease = 1f;
    private float _currentUpgradeCost;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _coinsPerSecondText;
    [SerializeField] private TextMeshProUGUI _upgradeCostText;
    [SerializeField] private TextMeshProUGUI _upgradeRewardText;
    [SerializeField] private TextMeshProUGUI _progressBarText;
    [SerializeField] private Image _progressBar;
    [SerializeField] private RectTransform _barBackground;
    [SerializeField] private Button _upgradeButton;

    [Header("Progress Bar Settings")]
    [SerializeField] private float _progressBarMaxValue = 1500f;
    [SerializeField] private bool _capCoinsAtMaxValue = false;

    [Header("Save Settings")]
    [SerializeField] private bool _enableAutoSave = true;
    [SerializeField] private bool _resetOnStart = false;
    private float _autoSaveTimer = 0f;
    private const float AUTO_SAVE_INTERVAL = 5f;
    private float _progressBarFullWidth;

    private void Awake()
    {
        _currentUpgradeCost = _baseUpgradeCost;
        
        // Sync progress bar rect to BAR background for correct visual scaling
        if (_progressBar != null && _barBackground != null)
        {
            var progressRect = _progressBar.rectTransform;
            progressRect.anchorMin = Vector2.zero;
            progressRect.anchorMax = Vector2.one;
            progressRect.offsetMin = Vector2.zero;
            progressRect.offsetMax = Vector2.zero;
            progressRect.pivot = new Vector2(0f, 0.5f);
            _progressBarFullWidth = progressRect.rect.width;
        }
        
        if (_upgradeButton == null)
        {
            Debug.LogError("[ClickerController] _upgradeButton is NOT assigned in Inspector! Button clicks won't update interactable state.");
        }
        
        if (_resetOnStart)
        {
            ResetGame();
        }
        else
        {
            LoadGame();
        }
        
        UpdateUI();
        UpdateUpgradeButtonState();
    }

    private void Update()
    {
        if (_enableAutoSave)
        {
            _autoSaveTimer += Time.deltaTime;
            if (_autoSaveTimer >= AUTO_SAVE_INTERVAL)
            {
                SaveGame();
                _autoSaveTimer = 0f;
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && _enableAutoSave)
            SaveGame();
    }

    private void OnApplicationQuit()
    {
        if (_enableAutoSave)
            SaveGame();
    }

    public void AddCoinsFromClick()
    {
        AddCoins(_coinsPerClick);
    }

    public void AddCoinsPerSecond()
    {
        if (_coinsPerSecond > 0)
        {
            AddCoins(Mathf.FloorToInt(_coinsPerSecond));
        }
    }

    private void AddCoins(long amount)
    {
        long previousCoins = _coins;
        long newCoins = _coins + amount;
        if (_capCoinsAtMaxValue)
            newCoins = (long)Mathf.Min(newCoins, _progressBarMaxValue);
        _coins = newCoins;
        TryShowInterstitialAd(previousCoins, _coins);
        UpdateUI();
        UpdateUpgradeButtonState();
    }

    public void TryUpgrade()
    {
        Debug.Log($"[TryUpgrade] Coins: {_coins}, Cost: {_currentUpgradeCost}, CanAfford: {_coins >= _currentUpgradeCost}");
        
        if (_coins >= _currentUpgradeCost)
        {
            _coins -= Mathf.FloorToInt(_currentUpgradeCost);
            _coinsPerSecond += _upgradeCoinsPerSecondIncrease;
            _currentUpgradeCost = Mathf.Floor(_currentUpgradeCost * _upgradeCostMultiplier);
            UpdateUI();
            UpdateUpgradeButtonState();
            if (_enableAutoSave) SaveGame();
            Debug.Log($"[TryUpgrade] Success! New coins: {_coins}, New CPS: {_coinsPerSecond}, Next cost: {_currentUpgradeCost}");
        }
        else
        {
            Debug.Log("[TryUpgrade] Not enough coins");
        }
    }

    private void UpdateUI()
    {
        if (_coinsPerSecondText != null)
            _coinsPerSecondText.text = FormatNumber((long)_coinsPerSecond);

        if (_upgradeCostText != null)
            _upgradeCostText.text = FormatNumber((long)_currentUpgradeCost);

        if (_upgradeRewardText != null)
            _upgradeRewardText.text = "+" + FormatNumber((long)_upgradeCoinsPerSecondIncrease);

        if (_progressBarText != null)
            _progressBarText.text = $"{FormatNumber(_coins)} / {FormatNumber((long)_progressBarMaxValue)}";

        if (_progressBar != null && _barBackground != null)
        {
            float progress = Mathf.Clamp01(_coins / _progressBarMaxValue);

            if (_progressBar.type == Image.Type.Filled)
            {
                _progressBar.fillAmount = progress;
            }
            else
            {
                RectTransform progressRect = _progressBar.rectTransform;
                progressRect.offsetMin = Vector2.zero;
                progressRect.offsetMax = new Vector2(
                    -_progressBarFullWidth * (1f - progress),
                    0f);
            }
        }
    }

    private void UpdateUpgradeButtonState()
    {
        if (_upgradeButton != null)
        {
            bool canAfford = _coins >= _currentUpgradeCost;
            _upgradeButton.interactable = canAfford;
        }
        else
        {
            Debug.LogWarning("[ClickerController] _upgradeButton is not assigned in Inspector!");
        }
    }

    private string FormatNumber(long number)
    {
        if (number >= 1000000)
            return $"{number / 1000000f:F1}M";
        if (number >= 1000)
            return $"{number / 1000f:F1}K";
        return number.ToString();
    }

    private void TryShowInterstitialAd(long previousCoins, long currentCoins)
    {
        if (previousCoins >= currentCoins)
            return;

        long halfProgressThreshold = Mathf.CeilToInt(_progressBarMaxValue * 0.5f);
        if (previousCoins < halfProgressThreshold && currentCoins >= halfProgressThreshold)
        {
#if !UNITY_EDITOR && InterstitialAdv_yg && YandexGamesPlatform_yg
            YG.YG2.InterstitialAdvShow();
#endif
        }
    }

    private void SaveGame()
    {
        PlayerPrefs.SetString("Clicker_Coins", _coins.ToString());
        PlayerPrefs.SetFloat("Clicker_CoinsPerSecond", _coinsPerSecond);
        PlayerPrefs.SetFloat("Clicker_UpgradeCost", _currentUpgradeCost);
        PlayerPrefs.Save();
    }

    private void LoadGame()
    {
        if (PlayerPrefs.HasKey("Clicker_Coins"))
            _coins = long.Parse(PlayerPrefs.GetString("Clicker_Coins"));
        
        if (PlayerPrefs.HasKey("Clicker_CoinsPerSecond"))
            _coinsPerSecond = PlayerPrefs.GetFloat("Clicker_CoinsPerSecond");
        
        if (PlayerPrefs.HasKey("Clicker_UpgradeCost"))
            _currentUpgradeCost = PlayerPrefs.GetFloat("Clicker_UpgradeCost");
        else
            _currentUpgradeCost = _baseUpgradeCost;
    }

    public void ResetGame()
    {
        _coins = 0;
        _coinsPerSecond = 0;
        _currentUpgradeCost = _baseUpgradeCost;
        PlayerPrefs.DeleteKey("Clicker_Coins");
        PlayerPrefs.DeleteKey("Clicker_CoinsPerSecond");
        PlayerPrefs.DeleteKey("Clicker_UpgradeCost");
        PlayerPrefs.Save();
        UpdateUI();
        UpdateUpgradeButtonState();
    }

    public void SetAutoSaveEnabled(bool enabled)
    {
        _enableAutoSave = enabled;
    }
}