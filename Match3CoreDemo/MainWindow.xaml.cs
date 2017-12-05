using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Collections.Generic;
using System.ComponentModel;

using Match3Core;

namespace Match3CoreDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>メインウィンドウが使用可能かどうか</summary>
        private bool mIsAvailable = false;

        private bool mIsClosed = false;
        /// <summary>ウィンドウが閉じられたかどうか</summary>
        public bool IsClosed { get { return mIsClosed; } }

        /// <summary>停止中かどうか</summary>
        private bool mIsPaused = false;

        private double mFps = 0;
        /// <summary>表示するフレームレート</summary>
        public double FPS { set { mFps = value; } get { return mFps; } }

        private GemManager manager;

        private int rowCount = 7;
        private int columnCount = 7;

        private List<GemControllerWrapper> gemControllers = new List<GemControllerWrapper>();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            #region ウィンドウスタイル設定
            //// タイトル
            this.Title = "Title";//Properties.Resources.Str_Title;

            //// メニュー
            this.ExitMenuItem.Header = "Exit";//Properties.Resources.Command_Exit;
            this.PauseMenuItem.Header = "Pause";//Properties.Resources.Command_Pause;

            //// キー処理
            this.KeyUp += new KeyEventHandler(MainWindow_KeyUp);

            //// アイコン
            //System.IO.MemoryStream memStream = new System.IO.MemoryStream();
            //(Properties.Resources.Ico_App as System.Drawing.Icon).Save(memStream);
            //this.Icon = BitmapFrame.Create(memStream);

            //// 透明化
            //this.AllowsTransparency = true;
            //this.Background = Brushes.Transparent;
            //this.WindowStyle = System.Windows.WindowStyle.None;

            //// サイズ設定
            //this.WindowState = System.Windows.WindowState.Maximized;
            #endregion

            // Load後の処理
            this.Loaded += (s, e) =>
            {
                mIsAvailable = true;
            };

            // 閉じた後の処理
            this.Closing += (s, e) =>
            {
                mIsClosed = true;
                mIsAvailable = false;
                //if (_notifyIconManager != null) _notifyIconManager.Dispose();
            };

            //---------------------------------------------MATCH3CORE---------------------------------------------
            Logger.Instance.OnDebugMessage += OnDebugMessage;
            Logger.Instance.OnWarningMessage += OnWarningMessage;
            Logger.Instance.OnErrorMessage += OnErrorMessage;

            manager = new GemManager(rowCount, columnCount);
            manager.OnGemCreated += OnGemCreated;
            manager.OnPossibleMoveCreate += OnPossibleMoveCreate;
            manager.Init();
            //---------------------------------------------MATCH3CORE---------------------------------------------
        }

        //---------------------------------------------MATCH3CORE---------------------------------------------
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(typeof(MainWindow));

        private void OnDebugMessage(string message)
        {
#if DEBUG
            //Console.WriteLine("[Debug]: " + message);
            _logger.Info("[Debug]: " + message);
#endif
        }

        private void OnWarningMessage(string message)
        {
            //Console.WriteLine("[Warning]: " + message);
            _logger.Warn("[Warning]: " + message);
        }

        private void OnErrorMessage(string message)
        {
            //Console.WriteLine("[Error]: " + message);
            _logger.Error("[Error]: " + message);
        }

        private void OnGemCreated(GemController gemController)
        {
            GemControllerWrapper wrapper = new GemControllerWrapper(gemController);
            DrawCanvas.Children.Add(wrapper.GemImage);
            DrawCanvas.Children.Add(wrapper.SpecialImage);
            wrapper.GemImage.MouseDown += Image_MouseDown;
            wrapper.OnOverEvent += OnGemWrapperOver;
            gemControllers.Add(wrapper);
        }

        private void OnPossibleMoveCreate(PossibleMove possibleMove)
        {
            PossibleMoveWrapper pmw = new PossibleMoveWrapper(possibleMove);
            DrawCanvas.Children.Add(pmw.WinShape);
            pmw.OnOverEvent += Pmw_OnOverEvent;
        }

        private void Pmw_OnOverEvent(PossibleMoveWrapper sender)
        {
            DrawCanvas.Children.Remove(sender.WinShape);
        }

        private void OnGemWrapperOver(GemControllerWrapper sender)
        {
            Dispatcher.Invoke(() =>
            {
                DrawCanvas.Children.Remove(sender.GemImage);
                DrawCanvas.Children.Remove(sender.SpecialImage);
                sender.GemImage.MouseDown -= Image_MouseDown;
                sender.OnOverEvent -= OnGemWrapperOver;
                gemControllers.Remove(sender);
            });
        }

        //---------------------------------------------MATCH3CORE---------------------------------------------

        /// <summary>更新処理</summary>
        public void Update(float deltaTime)
        {
            if (!mIsAvailable) return;

            if (!mIsPaused)
            {
                //---------------------------------------------MATCH3CORE---------------------------------------------
                foreach (GemControllerWrapper gem in gemControllers)
                {
                    gem.Update();
                }
                //...更新 ...
                manager.CheckMatchedMatches();
                //---------------------------------------------MATCH3CORE---------------------------------------------
            }
        }

        public void Draw()
        {
            if (!mIsAvailable) return;

            label_fps.Content = "FPS:" + mFps.ToString();
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MenuItem_Pause_Click(object sender, RoutedEventArgs e)
        {
            mIsPaused = !mIsPaused;
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.P:
                    mIsPaused = !mIsPaused; break;

                case Key.Escape:
                    this.Close(); break;

                case Key.S:
                    manager.ShuffleGems(); break;
            }
        }

        private void OnSizeChanged()
        {

        }

        private const int WmExitSizeMove = 0x232;

        private IntPtr HwndMessageHook(IntPtr wnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WmExitSizeMove:
                    this.OnSizeChanged();
                    handled = true;
                    break;
            }
            return IntPtr.Zero;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            if (helper.Handle != null)
            {
                var source = HwndSource.FromHwnd(helper.Handle);
                if (source != null)
                    source.AddHook(HwndMessageHook);
            }
        }

        private void OnPropertyChanged(String propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private GemControllerWrapper selectedGem = null;
        private Point mouseDownPosition;
        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image)
            {
                Image img_sender = sender as Image;
                if (img_sender.Tag != null && img_sender.Tag is GemControllerWrapper)
                {
                    selectedGem = img_sender.Tag as GemControllerWrapper;
                    mouseDownPosition = e.GetPosition(this);
                }
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (selectedGem != null)
            {
                Point mousePos = e.GetPosition(this);
                Direction direction = CalculateDirection(mousePos, mouseDownPosition);
                if (!selectedGem.GetConstrains().HasFlag(direction))
                {
                    selectedGem.MoveTo(direction);
                }
                selectedGem = null;
            }
        }

        private Direction CalculateDirection(Point a, Point b)
        {
//#if DEBUG
//            Console.WriteLine("a: " + a.ToString() + ", b: " + b.ToString());
//#endif
            Direction result;
            double mouseDiffX = a.X - b.X;
            double mouseDiffY = a.Y - b.Y;
            if (Math.Abs(mouseDiffX) > Math.Abs(mouseDiffY))
            {
                result = mouseDiffX > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                result = mouseDiffY > 0 ? Direction.Down : Direction.Up;
            }
//#if DEBUG
//            Console.WriteLine("Direction: " + result);
//#endif
            return result;
        }
    }
}
