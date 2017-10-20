using System;
using System.Linq;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

using Match3Core;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Match3CoreDemo
{
    class GemControllerWrapper
    {
        private static Dictionary<GemType, BitmapImage> GemImages;

        private GemController instance;

        private Image image;
        public Image GemImage
        {
            get { return image; }
        }

        public GemControllerWrapper(GemController gemController)
        {
            if (null == GemImages)
            {
                PrepareGemImages();
            }

            instance = gemController;
            instance.OnAppear += OnAppear;
            instance.OnFadeout += OnFadeout;
            instance.OnTypeChanged += OnTypeChanged;
            instance.OnPositionChanged += OnPositionChanged;

            image = new Image();
            image.Width = 64;
            image.Height = 64;
            image.Stretch = Stretch.Fill;
            image.Source = GemImages[GemType.None];
            image.MouseLeftButtonDown += Image_MouseLeftButtonDown;
            image.MouseLeftButtonUp += Image_MouseLeftButtonUp;
        }

        public void Update()
        {
            if (null == instance)
            {
                return;
            }
            if (instance.NeighborChangedFlag != Direction.None)
            {
                instance.OnNeighborChanged();
            }
        }

        private void Image_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            instance.OnMovingStart();
        }

        private void Image_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            instance.MoveTo(Direction.Up);
        }

        private void PrepareGemImages()
        {
            GemImages = new Dictionary<GemType, BitmapImage>();
            GemType[] gemTypes = Enum.GetValues(typeof(GemType)).Cast<GemType>().ToArray();
            foreach (GemType gemtype in gemTypes)
            {
                BitmapImage bi = new BitmapImage(new Uri("pack://siteoforigin:,,,/Resources/" + gemtype.ToString() + ".png"));
                GemImages.Add(gemtype, bi);
            }
        }

        private void OnAppear(GemController sender)
        {
            Task.Delay(500).ContinueWith(_ =>
            {
                instance.OnAppearOver();
            });
        }

        private void OnFadeout(GemController sender)
        {
            image.Opacity = 0.0;
            Task.Delay(500).ContinueWith(_ =>
            {
                instance.OnFadeoutOver();
            });
        }

        private void OnTypeChanged(GemController sender, GemType type)
        {
            image.Source = GemImages[type];
        }

        private void OnPositionChanged(GemController sender, int x, int y, bool interpolate)
        {
            Canvas.SetLeft(image, 50 + x * 64);
            Canvas.SetTop(image, 50 + (64 * 6 - y * 64));
            if (interpolate)
            {
                instance.OnMovingStart();
                Task.Delay(500).ContinueWith(_ =>
                {
                    instance.OnMovingEnd();
                });
            }
        }
    }
}
