using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
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
            manager = new GemManager(rowCount, columnCount);
            manager.OnGemCreated += OnGemCreated;
            manager.Init();
            //---------------------------------------------MATCH3CORE---------------------------------------------
        }

        //---------------------------------------------MATCH3CORE---------------------------------------------
        private void OnGemCreated(GemController gemController)
        {
            GemControllerWrapper wrapper = new GemControllerWrapper(gemController);
            DrawCanvas.Children.Add(wrapper.GemImage);
            gemControllers.Add(wrapper);
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
    }
}
