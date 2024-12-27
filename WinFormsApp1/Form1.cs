namespace WinFormsApp1;

public partial class Form1 : Form
{
    private const int CellSize = 30;
    private readonly List<DifficultyLevel> _difficultyLevels = new()
        {
            new DifficultyLevel("Легкий", 9, 9, 10),
            new DifficultyLevel("Средний", 16, 16, 40),
            new DifficultyLevel("Сложный", 30, 16, 99)
        };

    private readonly List<HighScore> _highScores = new();
    private Game _game;
    private Panel _gamePanel;
    private DataGridView _highScoresGridView;
    private System.Windows.Forms.Timer _timer;
    private Label _timerLabel;
    private Button _aboutButton;
    private bool _isPaused;

    public Form1()
    {
        InitializeComponent();
        SetupForm();
        LoadHighScores();
        DisplayHighScores(); 
    }

    private void SetupForm()
    {
        this.Text = "Сапёр";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Width = 30 * _difficultyLevels[2].Width + 40;
        this.Height = 30 * _difficultyLevels[2].Height + 100 + 300;

        var difficultySelector = new ComboBox
        {
            DataSource = _difficultyLevels,
            DisplayMember = "Name",
            Dock = DockStyle.Top
        };

        difficultySelector.SelectedIndexChanged += (s, e) => StartNewGame((DifficultyLevel)difficultySelector.SelectedItem);

        _gamePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

        _highScoresGridView = new DataGridView
        {
            Dock = DockStyle.Bottom,
            Height = 300,
            ReadOnly = true,
            AllowUserToAddRows = false,
            ColumnCount = 2
        };
        _highScoresGridView.Columns[0].HeaderText = "Игрок";
        _highScoresGridView.Columns[1].HeaderText = "Время (сек)";

        _timerLabel = new Label
        {
            Text = "Время: 00:00",
            Dock = DockStyle.Top,
            Font = new Font("Arial", 12, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(_timerLabel);

        _aboutButton = new Button
        {
            Text = "Об авторе",
            Dock = DockStyle.Top,
            Height = 50
        };

        _aboutButton.Click += AboutButton_Click;

        _timer = new System.Windows.Forms.Timer();
        _timer.Interval = 1000;
        _timer.Tick += TimerTickHandler;
        _timer.Start();

        Controls.Add(_gamePanel);
        Controls.Add(_timerLabel);
        Controls.Add(difficultySelector);
        Controls.Add(_aboutButton);
        Controls.Add(_highScoresGridView);

        var footerLabel = new Label
        {
            Text = "Создатель: О738Б Вячеслав Комоватов",
            Dock = DockStyle.Bottom,
            Font = new Font("Arial", 10, FontStyle.Italic),
            TextAlign = ContentAlignment.MiddleCenter
        };

        Controls.Add(footerLabel);

        StartNewGame(_difficultyLevels.First());
    }

    private void AboutButton_Click(object sender, EventArgs e)
    {
        if (!_isPaused)
        {
            _isPaused = true;
            _timer.Stop();
            _game.Pause();
        }

        MessageBox.Show(
            "Автор: Вячеслав Комоватов\nСтудент О738Б, 2024 год\nКурсовая работа \"Игра Сапёр\"",
            "Об авторе",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );

        if (_isPaused)
        {
            _isPaused = false;
            _game.Resume();
            _timer.Start();
        }
    }

    private void StartNewGame(DifficultyLevel level)
    {
        _game = new Game(level);
        _game.GameOver += GameOverHandler;
        _game.GameWon += GameWonHandler;
        RenderGameField();
        ResetTimer();
        _game.Resume();
        _timer.Start();
    }

    private void RenderGameField()
    {
        _gamePanel.Controls.Clear();
        _gamePanel.Width = _game.Level.Width * CellSize;
        _gamePanel.Height = _game.Level.Height * CellSize;

        for (int y = 0; y < _game.Level.Height; y++)
        {
            for (int x = 0; x < _game.Level.Width; x++)
            {
                var cell = new Button
                {
                    Width = CellSize,
                    Height = CellSize,
                    Location = new Point(x * CellSize, y * CellSize),
                    Tag = (x, y)
                };

                cell.MouseUp += CellClickHandler;
                _gamePanel.Controls.Add(cell);
            }
        }
    }

    private void TimerTickHandler(object sender, EventArgs e)
    {
        var elapsedTime = _game.ElapsedTime;
        _timerLabel.Text = $"Время: {elapsedTime:mm\\:ss}";
    }

    private void ResetTimer()
    {
        _timerLabel.Text = "Время: 00:00";
        _timer.Stop();
    }

    private void DisplayHighScores()
    {
        var sortedHighScores = _highScores.OrderBy(h => h.Time).ToList();

        _highScoresGridView.Rows.Clear();
        foreach (var score in sortedHighScores)
        {
            _highScoresGridView.Rows.Add(score.PlayerName, score.Time.TotalSeconds.ToString("F2"));
        }
    }

    private void CellClickHandler(object sender, MouseEventArgs e)
    {
        if (sender is not Button cell || _game == null) return;

        var (x, y) = ((int, int))cell.Tag;
        var gameCell = _game.GetCell(x, y);
        if (gameCell == null) return;

        switch (e.Button)
        {
            case MouseButtons.Left:
                _game.RevealCell(x, y);
                break;
            case MouseButtons.Right:
                _game.ToggleFlag(x, y);
                break;
        }

        UpdateGameField();
    }

    private void UpdateGameField()
    {
        foreach (Control control in _gamePanel.Controls)
        {
            if (control is not Button cell) continue;
            var (x, y) = ((int, int))cell.Tag;
            var gameCell = _game.GetCell(x, y);

            if (gameCell.IsRevealed)
            {
                if (gameCell.IsMine)
                {
                    cell.BackgroundImage = Image.FromFile("mine.jpg");
                    cell.BackgroundImageLayout = ImageLayout.Stretch;
                }
                else
                {
                    cell.Text = gameCell.NeighboringMines > 0 ? gameCell.NeighboringMines.ToString() : string.Empty;
                    cell.BackColor = Color.LightGray;
                }
            }
            else if (gameCell.IsFlagged)
            {
                cell.Text = "F";
                cell.BackColor = Color.Yellow;
            }
            else
            {
                cell.Text = string.Empty;
                cell.BackColor = default;
                cell.BackgroundImage = null;
            }
        }
    }

    private void GameOverHandler()
    {
        _timer.Stop();
        foreach (var cell in _gamePanel.Controls.OfType<Button>())
        {
            var (x, y) = ((int, int))cell.Tag;
            if (_game.GetCell(x, y)?.IsMine == true)
            {
                cell.BackgroundImage = Image.FromFile("mine.jpg");
                cell.BackgroundImageLayout = ImageLayout.Stretch;
            }
        }

        MessageBox.Show("Вы проиграли!", "Игра окончена", MessageBoxButtons.OK, MessageBoxIcon.Information);
        StartNewGame(_game.Level);
    }

    private void GameWonHandler()
    {
        _timer.Stop();
        var playerName = Prompt.ShowDialog("Введите ваше имя:", "Победа!");
        if (!string.IsNullOrWhiteSpace(playerName))
        {
            var elapsedTime = _game.ElapsedTime;
            _highScores.Add(new HighScore(playerName, elapsedTime));
            SaveHighScores();
        }
        MessageBox.Show("Поздравляем, вы выиграли!", "Победа", MessageBoxButtons.OK, MessageBoxIcon.Information);
        DisplayHighScores();
        StartNewGame(_game.Level);
    }

    private void LoadHighScores()
    {
        if (!File.Exists("highscores.txt")) return;

        foreach (var line in File.ReadAllLines("highscores.txt"))
        {
            var parts = line.Split(',');
            if (parts.Length == 2 && TimeSpan.TryParse(parts[1], out TimeSpan time))
            {
                _highScores.Add(new HighScore(parts[0], time));
            }
        }
    }

    private void SaveHighScores()
    {
        var lines = _highScores.Select(h => $"{h.PlayerName},{h.Time}");
        File.WriteAllLines("highscores.txt", lines);
    }
}


public class Game
{
    public DifficultyLevel Level { get; }
    public event Action GameOver;
    public event Action GameWon;

    private readonly Cell[,] _field;
    private int _remainingCells;
    private DateTime _startTime;
    private DateTime _lastPauseTime;
    private TimeSpan _pausedTime = TimeSpan.Zero;

    public Game(DifficultyLevel level)
    {
        Level = level;
        _field = new Cell[level.Width, level.Height];
        InitializeField();
        _startTime = DateTime.Now;
    }

    public TimeSpan ElapsedTime => DateTime.Now - _startTime - _pausedTime;

    public void Pause()
    {
        _lastPauseTime = DateTime.Now;
    }

    public void Resume()
    {
        if (_lastPauseTime != default)
        {
            _pausedTime += DateTime.Now - _lastPauseTime;
            _lastPauseTime = default;
        }
    }

    public void RevealCell(int x, int y)
    {
        if (!IsInBounds(x, y) || _field[x, y].IsRevealed || _field[x, y].IsFlagged) return;

        var cell = _field[x, y];
        cell.IsRevealed = true;

        if (cell.IsMine)
        {
            GameOver?.Invoke();
            return;
        }

        _remainingCells--;

        if (_remainingCells == 0)
        {
            GameWon?.Invoke();
            return;
        }

        if (cell.NeighboringMines == 0)
        {
            foreach (var (nx, ny) in GetNeighbors(x, y))
            {
                RevealCell(nx, ny);
            }
        }
    }

    public void ToggleFlag(int x, int y)
    {
        if (!IsInBounds(x, y) || _field[x, y].IsRevealed) return;
        _field[x, y].IsFlagged = !_field[x, y].IsFlagged;
    }

    public Cell GetCell(int x, int y)
    {
        if (!IsInBounds(x, y)) return null;
        return _field[x, y];
    }

    private void InitializeField()
    {
        var random = new Random();
        _remainingCells = Level.Width * Level.Height - Level.Mines;

        // Инициализация массива
        for (int x = 0; x < Level.Width; x++)
        {
            for (int y = 0; y < Level.Height; y++)
            {
                _field[x, y] = new Cell();
            }
        }

        // Расстановка мин
        var mineCount = 0;
        while (mineCount < Level.Mines)
        {
            int x = random.Next(Level.Width);
            int y = random.Next(Level.Height);

            if (!_field[x, y].IsMine)
            {
                _field[x, y].IsMine = true;
                mineCount++;
            }
        }

        // Подсчет соседних мин
        for (int x = 0; x < Level.Width; x++)
        {
            for (int y = 0; y < Level.Height; y++)
            {
                _field[x, y].NeighboringMines = GetNeighbors(x, y).Count(n =>
                {
                    var neighbor = _field[n.Item1, n.Item2];
                    return neighbor != null && neighbor.IsMine;
                });
            }
        }
    }


    private bool IsInBounds(int x, int y) => x >= 0 && y >= 0 && x < Level.Width && y < Level.Height;

    private IEnumerable<(int, int)> GetNeighbors(int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (IsInBounds(nx, ny)) yield return (nx, ny);
            }
        }
    }
}

public class Cell
{
    public bool IsMine { get; set; }
    public bool IsRevealed { get; set; }
    public bool IsFlagged { get; set; }
    public int NeighboringMines { get; set; }
}

public class DifficultyLevel
{
    public string Name { get; }
    public int Width { get; }
    public int Height { get; }
    public int Mines { get; }

    public DifficultyLevel(string name, int width, int height, int mines)
    {
        Name = name;
        Width = width;
        Height = height;
        Mines = mines;
    }
}

public class HighScore
{
    public string PlayerName { get; set; }
    public TimeSpan Time { get; set; }

    public HighScore(string playerName, TimeSpan time)
    {
        PlayerName = playerName;
        Time = time;
    }
}

public static class Prompt
{
    public static string ShowDialog(string text, string caption)
    {
        var prompt = new Form
        {
            Width = 400,
            Height = 200,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = caption,
            StartPosition = FormStartPosition.CenterScreen
        };

        var textLabel = new Label { Left = 20, Top = 20, Text = text, AutoSize = true };
        var textBox = new TextBox { Left = 20, Top = 50, Width = 340 };
        var confirmation = new Button { Text = "OK", Left = 270, Width = 90, Top = 90, DialogResult = DialogResult.OK };

        prompt.Controls.Add(textLabel);
        prompt.Controls.Add(textBox);
        prompt.Controls.Add(confirmation);
        prompt.AcceptButton = confirmation;

        return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
    }
}