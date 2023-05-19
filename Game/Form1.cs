using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Game
{
    public partial class MainForm : Form
    {
        private const int BallSize = 50;
        private int ballCount = 0;
        private int TimerInterval = 1000;
        private const int PenaltyThreshold = 5;
        private List<Player> players;

        private int score;
        private int penalty;
        private Random random;
        private Timer timer;

        public MainForm()
        {
            InitializeComponent();

            random = new Random();
            timer = new Timer();
            timer.Interval = TimerInterval;
            timer.Tick += Timer_Tick;
            players = new List<Player>();

            score = 0;
            penalty = 0;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadPlayers();
            timer.Start();
        }

        private void MainForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                CheckHit(e.Location);
            }
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            DrawScoreAndPenalty(e.Graphics);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            GenerateBall();
            penalty++;
            if (penalty >= PenaltyThreshold)
            {
                GameOver();
            }
            Refresh();
        }

        private void Ball_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Ball ball = (Ball)sender;
                Controls.Remove(ball);
                score += ball.score;
                penalty--;
            }
        }

        private void GenerateBall()
        {
            int x, y;
            bool overlap;

            do
            {
                x = random.Next(0, ClientSize.Width - BallSize);
                y = random.Next(0, ClientSize.Height - BallSize);

                Rectangle playerInfoArea = new Rectangle(10, 60, ClientSize.Width, 20 * players.Count);
                Rectangle ballBounds = new Rectangle(x, y, BallSize, BallSize);

                if (playerInfoArea.IntersectsWith(ballBounds))
                {
                    overlap = true;
                    continue;
                }

                overlap = false;

                foreach (Control control in Controls)
                {
                    if (control is Ball existingBall)
                    {
                        if (existingBall.Bounds.IntersectsWith(ballBounds))
                        {
                            overlap = true;
                            break;
                        }
                    }
                }
            }
            while (overlap);

            Color[] colors = { Color.Red, Color.Green, Color.Blue, Color.Yellow };
            int[] scores = { 1, 2, 3, 4 };

            int index = random.Next(colors.Length);

            Color color = colors[index];
            int ballScore = scores[index];

            Ball ball = new Ball(x, y, BallSize, color, ballScore);
            ball.MouseClick += Ball_MouseClick;
            Controls.Add(ball);

            ballCount++;

            if (ballCount % 10 == 0)
            {
                if(TimerInterval > 200)
                {
                    DecreaseTimerInterval();
                }
            }
        }

        private void DecreaseTimerInterval()
        {
            TimerInterval -= 100;
        }

        private void CheckHit(Point clickLocation)
        {
            foreach (Control control in Controls)
            {
                if (control is Ball ball)
                {
                    if (ball.Bounds.Contains(clickLocation))
                    {
                        Rectangle playerInfoArea = new Rectangle(10, 60, ClientSize.Width, 20 * players.Count);
                        Rectangle ballBounds = ball.Bounds;

                        if (!playerInfoArea.IntersectsWith(ballBounds))
                        {
                            Controls.Remove(ball);
                            score++;
                            penalty--;
                            break;
                        }
                    }
                }
            }
        }

        private void DrawScoreAndPenalty(Graphics graphics)
        {
            string scoreText = "Рахунок: " + score;
            string penaltyText = "Пропущених: " + (penalty + 1);

            graphics.DrawString(scoreText, Font, Brushes.Black, 10, 10);
            graphics.DrawString(penaltyText, Font, Brushes.Black, 10, 30);

            int y = 60;

            foreach (Player player in players)
            {
                string playerInfo = "Гравець: " + player.Name + " - Рахунок: " + player.Score;
                graphics.DrawString(playerInfo, Font, Brushes.Black, 10, y);
                y += 20;
            }

            foreach (Control control in Controls)
            {
                if (control is Ball ball)
                {
                    string ballScoreText = $"{ball.score}";
                    graphics.DrawString(ballScoreText, Font, Brushes.Black, ball.Left + 20, ball.Top - 20);
                }
            }
        }

        private string PromptPlayerName()
        {
            string playerName = "";

            using (InputDialog dialog = new InputDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    playerName = dialog.PlayerName;
                }
            }

            return playerName;
        }

        private void GameOver()
        {
            timer.Stop();

            string playerName = PromptPlayerName();

            if (!string.IsNullOrEmpty(playerName))
            {
                Player player = new Player { Name = playerName, Score = score };
                players.Add(player);
                SavePlayers();
            }

            DialogResult result = MessageBox.Show("Хочеш зіграти ще раз?", "Гру закінчено", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                ResetGame();
            }
            else
            {
                Close();
            }
        }

        private void ResetGame()
        {
            score = 0;
            penalty = 0;

            for (int i = Controls.Count - 1; i >= 0; i--)
            {
                if (Controls[i] is Ball ball)
                {
                    Controls.RemoveAt(i);
                    ball.Dispose();
                }
            }

            timer.Start();

            Refresh();
        }

        private void SavePlayers()
        {
            using (StreamWriter writer = new StreamWriter("players.txt"))
            {
                foreach (Player player in players)
                {
                    writer.WriteLine(player.Name + "," + player.Score);
                }
            }
        }

        private void LoadPlayers()
        {
            players = new List<Player>();

            try
            {
                using (StreamReader reader = new StreamReader("players.txt"))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split(',');
                        if (parts.Length == 2)
                        {
                            string name = parts[0];
                            int score;
                            if (int.TryParse(parts[1], out score))
                            {
                                Player player = new Player { Name = name, Score = score };
                                players.Add(player);
                            }
                        }
                    }
                }

                players.Sort((x, y) => y.Score.CompareTo(x.Score));

                players = players.Take(5).ToList();
            }
            catch (FileNotFoundException)
            {
                players = new List<Player>();
            }
        }

        public class Ball : Control
        {
            private Color color;
            public int score;

            public Ball(int x, int y, int size, Color color, int score)
            {
                this.color = color;
                Location = new Point(x, y);
                Size = new Size(size, size);
                this.score = score;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.FillEllipse(new SolidBrush(color), ClientRectangle);
            }
        }

        public class Player
        {
            public string Name { get; set; }
            public int Score { get; set; }
        }

        public class InputDialog : Form
        {
            private TextBox textBox;
            private Button okButton;

            public string PlayerName { get; private set; }

            public InputDialog()
            {
                InitializeComponent();
            }

            private void InitializeComponent()
            {
                textBox = new TextBox();
                okButton = new Button();

                SuspendLayout();

                textBox.Location = new Point(12, 12);
                textBox.Name = "textBox";
                textBox.Size = new Size(200, 20);
                Controls.Add(textBox);

                okButton.Location = new Point(75, 50);
                okButton.Name = "okButton";
                okButton.Size = new Size(75, 23);
                okButton.Text = "OK";
                okButton.Click += OkButton_Click;
                Controls.Add(okButton);

                AcceptButton = okButton;
                AutoScaleDimensions = new SizeF(6F, 13F);
                AutoScaleMode = AutoScaleMode.Font;
                ClientSize = new Size(224, 85);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                StartPosition = FormStartPosition.CenterScreen;
                Text = "Введіть ім'я гравця";

                ResumeLayout(false);
                PerformLayout();
            }

            private void OkButton_Click(object sender, EventArgs e)
            {
                PlayerName = textBox.Text.Trim();
                DialogResult = DialogResult.OK;
            }
        }
    }
}