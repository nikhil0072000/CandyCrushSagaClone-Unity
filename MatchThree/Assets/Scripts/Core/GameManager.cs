using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Tools;

namespace Core
{
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
        [SerializeField] private int _maxAllowedMove = 40;
        [SerializeField] private int _targetScore = 50000;
        [SerializeField] private int _baseTargetScore = 50000;
        [SerializeField] private int _scoreStep = 500;
        [SerializeField] private int _baseMoves = 20;
        [SerializeField] private int _movesDropPerLevel = 2;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _moveText;
        [SerializeField] private TextMeshProUGUI _targetScoreText;
        [SerializeField] private Vector2Int _dimensions;
        [SerializeField] private GameObject LevelcompletePanel;
        [SerializeField] private GameObject LevelfailPanel;

        private int _score;
        private MatchableGrid _grid;
        private bool _levelFinished;
        private GameObject _resultPanel;
        private TextMeshProUGUI _resultText;
        private Button _primaryButton;
        private Button _secondaryButton;

        [ContextMenu("ClearAndPopulate")]
        private void ClearAndPopulate()
        {
            _grid.ClearGrid();
            _grid.PopulateGrid();
        }

        [ContextMenu("Populate")]
        private void Populate()
        {
            _grid.PopulateGrid();
        }

        [ContextMenu("ClearGrid")]
        private void ClearGrid()
        {
            _grid.ClearGrid();
        }

        [ContextMenu("Test")]
        private void Testt()
        {
            MatchableFX fxx = MatchableFXPool.Instance.GetObject();
            fxx.transform.position = _grid.GetItemAt(0, 0).transform.position;
            fxx.PlayColorExplode(_grid.GetItemAt(9, 9).transform);
        }

        protected override void Awake()
        {
            base.Awake();
            _grid = (MatchableGrid)MatchableGrid.Instance;
            if (_grid != null)
            {
                _grid.InitializeGrid(_dimensions);
                _grid.PopulateGrid();
            }

            RefreshUi();
        }

        private void Start()
        {
            int levelNumber = GetLevelNumberFromSceneName();
            ConfigureLevel(CalculateTargetScore(levelNumber), CalculateMaxMoves(levelNumber));
        }

        public void ConfigureLevel(int targetScore, int maxMoves)
        {
            _targetScore = targetScore;
            _maxAllowedMove = maxMoves;
            _score = 0;
            _levelFinished = false;
            RefreshUi();
        }

        public void IncreaseScore(int value)
        {
            if (_levelFinished)
                return;

            _score += value;
            RefreshUi();
            CheckLevelState();
        }

        public bool CanMoveMatchables()
        {
            if (_levelFinished)
                return false;

            return _maxAllowedMove > 0;
        }

        public void DecreaseMove()
        {
            if (_levelFinished)
                return;

            _maxAllowedMove--;
            RefreshUi();
            CheckLevelState();
        }

        public int GetScore()
        {
            return _score;
        }

        public int GetTargetScore()
        {
            return _targetScore;
        }

        public int GetMovesRemaining()
        {
            return _maxAllowedMove;
        }

        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void RefreshUi()
        {
            if (_scoreText != null)
                _scoreText.text = _score.ToString("D5");

            if (_moveText != null)
                _moveText.text = _maxAllowedMove.ToString();

            if (_targetScoreText != null)
                _targetScoreText.text = _targetScore.ToString();
        }

        private void CheckLevelState()
        {
            if (_levelFinished)
                return;

            if (_score >= _targetScore)
            {
                CompleteLevel();
            }
            else if (_maxAllowedMove <= 0)
            {
                FailLevel();
            }
        }

        private void CompleteLevel()
        {
            _levelFinished = true;
            Debug.Log("Level completed");
            LevelcompletePanel.SetActive(true);

            //ShowResultPanel(true);
        }

        private void FailLevel()
        {
            _levelFinished = true;
            Debug.Log("Level fail");
            LevelfailPanel.SetActive(true);
            //ShowResultPanel(false);
        }

        private void ShowResultPanel(bool completed)
        {
            CreateResultUi();
            _resultPanel.SetActive(true);
            _resultText.text = completed ? "Level Completed!" : "Level Failed!";

            _primaryButton.gameObject.SetActive(completed);
            _secondaryButton.gameObject.SetActive(!completed);

            _primaryButton.onClick.RemoveAllListeners();
            _secondaryButton.onClick.RemoveAllListeners();

            if (completed)
            {
                _primaryButton.onClick.AddListener(LoadNextLevel);
                _primaryButton.GetComponentInChildren<TextMeshProUGUI>().text = "Next Level";
            }
            else
            {
                _secondaryButton.onClick.AddListener(RestartLevel);
                _secondaryButton.GetComponentInChildren<TextMeshProUGUI>().text = "Try Again";
            }
        }

        private void CreateResultUi()
        {
            if (_resultPanel != null)
                return;

            GameObject canvasObject = new GameObject("LevelResultCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            _resultPanel = new GameObject("LevelResultPanel");
            _resultPanel.transform.SetParent(canvasObject.transform, false);
            Image panelImage = _resultPanel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.8f);
            RectTransform panelRect = _resultPanel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(420f, 260f);
            panelRect.anchoredPosition = Vector2.zero;

            GameObject titleObject = new GameObject("Title");
            titleObject.transform.SetParent(_resultPanel.transform, false);
            _resultText = titleObject.AddComponent<TextMeshProUGUI>();
            _resultText.alignment = TextAlignmentOptions.Center;
            _resultText.fontSize = 30;
            _resultText.color = Color.white;
            _resultText.text = "Level Result";
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(360f, 80f);
            titleRect.anchoredPosition = new Vector2(0f, 60f);

            _primaryButton = CreateButton("Next Level", new Vector2(0f, -20f), Color.green, _resultPanel.transform);
            _secondaryButton = CreateButton("Try Again", new Vector2(0f, -20f), Color.red, _resultPanel.transform);

            _primaryButton.gameObject.SetActive(false);
            _secondaryButton.gameObject.SetActive(false);
            _resultPanel.SetActive(false);
        }

        private Button CreateButton(string label, Vector2 position, Color color, Transform parent)
        {
            GameObject buttonObject = new GameObject(label.Replace(" ", string.Empty) + "Button");
            buttonObject.transform.SetParent(parent, false);

            Button button = buttonObject.AddComponent<Button>();
            Image image = buttonObject.AddComponent<Image>();
            image.color = color;

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(180f, 54f);
            buttonRect.anchoredPosition = position;
            button.targetGraphic = image;

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            TextMeshProUGUI labelText = labelObject.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;
            labelText.fontSize = 20;

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;

            return button;
        }

        private int CalculateTargetScore(int levelNumber)
        {
            return _baseTargetScore + (levelNumber - 1) * _scoreStep;
        }

        private int CalculateMaxMoves(int levelNumber)
        {
            return Mathf.Max(4, _baseMoves - (levelNumber - 1) * _movesDropPerLevel);
        }

        private int GetLevelNumberFromSceneName()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName.StartsWith("Level", StringComparison.OrdinalIgnoreCase) && int.TryParse(sceneName.Substring("Level".Length), out int parsedLevel))
                return parsedLevel;

            return 1;
        }

       public void LoadNextLevel()
        {
            int nextLevel = GetLevelNumberFromSceneName() + 1;
            string nextSceneName = "Level" + nextLevel;
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogWarning("No next level scene found. Please add Level" + nextLevel + " to the build settings.");
            }
        }
    }
}


