using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Match3CoreDemo
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        #region frame rate 関係の変数
        /// <summary>次回処理をしたい時刻</summary>
        private long mNextTick;
        /// <summary>前回処理をした時刻</summary>
        private long mLastCountTick;
        /// <summary>前回FPSを計算した時刻</summary>
        private long mLastFpsTick;

        /// <summary>フレームレート計算用の時刻</summary>
        private long mCurrentTick;
        /// <summary>フレームレート計算用のフレーム数</summary>
        private int mFrameCount = 0;
        /// <summary>現在のフレームレート</summary>
        private double mFrameRate;
        /// <summary>理想フレームレート</summary>
        private const double mIdealFrameRate = 60;
        #endregion

        /// <summary>メインウィンドウ</summary>
        private static MainWindow mWin;

        /// <summary>二重起動禁止用mutex</summary>
        private static System.Threading.Mutex mMutex;

        /// <summary>アプリケーション初期化処理</summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 二重起動禁止
            mMutex = new System.Threading.Mutex(false, Application.ResourceAssembly.FullName);
            if (!mMutex.WaitOne(0, false))
            {
                mMutex.Close();
                mMutex = null;
                this.Shutdown();
            }

            // 開始
            Start();
        }

        /// <summary>終了処理</summary>
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (mMutex != null)
            {
                mMutex.ReleaseMutex();
                mMutex.Close();
            }
        }

        /// <summary>メイン処理を開始する</summary>
        private void Start()
        {
            // 窓の表示
            mWin = new MainWindow();
            mWin.Show();

            // メインループ
            while (!mWin.IsClosed)
            {
                mCurrentTick = Environment.TickCount;
                double diffms = Math.Floor(1000.0 / mIdealFrameRate);
                if (mNextTick == 0)
                {
                    mNextTick = mCurrentTick + (long)diffms;
                }

                if (mCurrentTick < mNextTick)
                {
                    // 待ち
                }
                else
                {
                    // 処理
                    if (mLastCountTick != 0)
                    {
                        mWin.Update((mCurrentTick - mLastCountTick) / 1000.0f);
                    }

                    if (Environment.TickCount >= mNextTick + diffms)
                    {
                        // フレームスキップ
                    }
                    else
                    {
                        // 描画
                        mWin.Draw();
                    }

                    mFrameCount++;
                    mLastCountTick = mCurrentTick;
                    while (mCurrentTick >= mNextTick)
                    {
                        mNextTick += (long)diffms;
                    }
                }

                // frame rate 計算
                if (mCurrentTick - mLastFpsTick >= 1000)
                {
                    mFrameRate = mFrameCount * 1000 / (double)(mCurrentTick - mLastFpsTick);
                    mWin.FPS = mFrameRate;
                    mFrameCount = 0;
                    mLastFpsTick = mCurrentTick;
                }

                // UIメッセージ処理
                DoEvents();
            }
        }

        private void DoEvents()
        {
            // 新しくネスト化されたメッセージ ポンプを作成
            DispatcherFrame frame = new DispatcherFrame();

            // DispatcherFrame (= 実行ループ) を終了させるコールバック
            DispatcherOperationCallback exitFrameCallback = (f) =>
            {
                // ネスト化されたメッセージ ループを抜ける
                ((DispatcherFrame)f).Continue = false;
                return null;
            };

            // 非同期で実行する
            // 優先度を Background にしているので、このコールバックは
            // ほかに処理するメッセージがなくなったら実行される
            DispatcherOperation exitOperation = Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background, exitFrameCallback, frame);

            // 実行ループを開始する
            Dispatcher.PushFrame(frame);

            // コールバックが終了していない場合は中断
            if (exitOperation.Status != DispatcherOperationStatus.Completed)
            {
                exitOperation.Abort();
            }
        }
    }
}
