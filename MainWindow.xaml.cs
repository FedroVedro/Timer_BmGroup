using System;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;          // PathFigure, ArcSegment, PathGeometry, SweepDirection
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ModernTimerWidget
{
    public partial class MainWindow : Window
    {
        // инициализируем тут, чтобы избежать предупреждения CS8618
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private SoundPlayer? currentSoundPlayer = null;

        private TimeSpan totalTime = TimeSpan.Zero;
        private TimeSpan remainingTime = TimeSpan.Zero;
        private bool isRunning = false;
        private bool isDraggingDial = false;

        public MainWindow()
        {
            InitializeComponent();
            InitTimer();

            // Перетаскивание окна по любой области
            this.MouseLeftButtonDown += (_, e) => DragMove();
        }

        private void InitTimer()
        {
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += Timer_Tick;
            UpdateDisplay();
        }

        // Теперь signature соответствует EventHandler (object? sender)
        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (remainingTime.TotalMilliseconds > 0)
            {
                remainingTime -= TimeSpan.FromMilliseconds(10);
                if (remainingTime.TotalMilliseconds < 0)
                    remainingTime = TimeSpan.Zero;

                UpdateDisplay();
                double frac = (totalTime.TotalMilliseconds - remainingTime.TotalMilliseconds) / totalTime.TotalMilliseconds;
                UpdateArc(frac * 360);
            }
            else
            {
                timer.Stop();
                isRunning = false;
                StartPauseButton.Content = "▶";
                ProgressArc.Visibility = Visibility.Hidden;

                // Показываем панель уведомления
                TimeUpPanel.Visibility = Visibility.Visible;

                // Воспроизводим звук
                try
                {
                    currentSoundPlayer = new SoundPlayer("finish.wav");
                    currentSoundPlayer.Load();
                    currentSoundPlayer.PlayLooping(); // Используем PlayLooping, чтобы звук играл до нажатия крестика
                }
                catch (Exception ex)
                {
                    // на случай, если файл не найден или не поддерживается
                    MessageBox.Show($"Не удалось воспроизвести звук: {ex.Message}", 
                                    "Ошибка звука", 
                                    MessageBoxButton.OK, 
                                    MessageBoxImage.Warning);
                }
            }
        }

        private void UpdateDisplay()
        {
            int mm = remainingTime.Minutes + remainingTime.Hours * 60;
            TimerDisplay.Text = $"{mm:D2}:{remainingTime.Seconds:D2}:{remainingTime.Milliseconds:D3}";
        }

        private void SetTimer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && int.TryParse(fe.Tag?.ToString(), out int min))
            {
                totalTime = TimeSpan.FromMinutes(min);
                remainingTime = totalTime;
                isRunning = false;
                timer.Stop();
                StartPauseButton.Content = "▶";

                // Скрываем панель уведомления и останавливаем звук при установке нового таймера
                TimeUpPanel.Visibility = Visibility.Collapsed;
                StopSound();

                UpdateArc(360);
                UpdateDisplay();
                ProgressArc.Visibility = Visibility.Visible;
            }
        }

        private void StartPause_Click(object sender, RoutedEventArgs e)
        {
            if (totalTime.TotalMilliseconds == 0) return;

            // Если время истекло и показана панель уведомления, сначала скрываем её
            if (TimeUpPanel.Visibility == Visibility.Visible)
            {
                TimeUpPanel.Visibility = Visibility.Collapsed;
                StopSound();
                return;
            }

            if (isRunning)
            {
                timer.Stop();
                isRunning = false;
                StartPauseButton.Content = "▶";
            }
            else
            {
                timer.Start();
                isRunning = true;
                StartPauseButton.Content = "⏸";
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            isRunning = false;
            remainingTime = totalTime;
            StartPauseButton.Content = "▶";
            
            // Скрываем панель уведомления и останавливаем звук
            TimeUpPanel.Visibility = Visibility.Collapsed;
            StopSound();
            
            UpdateDisplay();
            UpdateArc(360);
        }

        private void StopSound_Click(object sender, RoutedEventArgs e)
        {
            // Останавливаем звук и скрываем панель
            StopSound();
            TimeUpPanel.Visibility = Visibility.Collapsed;
        }

        private void StopSound()
        {
            if (currentSoundPlayer != null)
            {
                currentSoundPlayer.Stop();
                currentSoundPlayer.Dispose();
                currentSoundPlayer = null;
            }
        }

        private void DialCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDraggingDial = true;
            SetTimeByPosition(e.GetPosition(DialCanvas));
        }

        private void DialCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingDial && e.LeftButton == MouseButtonState.Pressed)
                SetTimeByPosition(e.GetPosition(DialCanvas));
        }

        private void DialCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDraggingDial = false;
        }

        private void SetTimeByPosition(Point p)
        {
            double cx = DialCanvas.ActualWidth / 2;
            double cy = DialCanvas.ActualHeight / 2;
            double dx = p.X - cx;
            double dy = p.Y - cy;
            double angle = Math.Atan2(dy, dx) * 180 / Math.PI + 90;
            if (angle < 0) angle += 360;

            double secs = angle / 360 * totalTime.TotalSeconds;
            totalTime = TimeSpan.FromSeconds(secs);
            remainingTime = totalTime;
            isRunning = false;
            timer.Stop();
            StartPauseButton.Content = "▶";

            // Скрываем панель уведомления и останавливаем звук при изменении времени через циферблат
            TimeUpPanel.Visibility = Visibility.Collapsed;
            StopSound();

            UpdateDisplay();
            UpdateArc(angle);
            ProgressArc.Visibility = Visibility.Visible;
        }

        private void UpdateArc(double angle)
        {
            double radius = DialCanvas.ActualWidth / 2 - 4;
            double cx = DialCanvas.ActualWidth / 2;
            double cy = DialCanvas.ActualHeight / 2;
            double rad = (angle - 90) * Math.PI / 180;
            double ex = cx + radius * Math.Cos(rad);
            double ey = cy + radius * Math.Sin(rad);
            bool large = angle > 180;

            var fig = new PathFigure
            {
                StartPoint = new Point(cx, cy - radius),
                IsClosed = false
            };
            fig.Segments.Add(new ArcSegment
            {
                Point = new Point(ex, ey),
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = large
            });

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            ProgressArc.Data = geo;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Останавливаем звук перед закрытием
            StopSound();
            this.Close();
        }
    }
}