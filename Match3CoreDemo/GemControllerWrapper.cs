using System;
using System.Linq;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

using Match3Core;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Reflection;
using System.IO;

namespace Match3CoreDemo
{
    class GemControllerWrapper
    {
        public delegate void GemControllerEventDelegate(GemControllerWrapper sender);
        public event GemControllerEventDelegate OnOverEvent;

        private static Dictionary<GemType, BitmapImage> GemImages;
        private static Dictionary<GemSpecialType, BitmapImage> SpecialTypeImages;

        private GemController instance;

        private Image image;
        public Image GemImage
        {
            get { return image; }
        }

        private Image specialImage;
        public Image SpecialImage
        {
            get { return specialImage; }
        }

        public GemControllerWrapper(GemController gemController)
        {
            if (null == GemImages)
            {
                PrepareGemImages();
            }

            if (null == SpecialTypeImages)
            {
                PrepareSpecialTypeImages();
            }

            instance = gemController;
            instance.OnAppear += OnAppear;
            instance.OnFadeout += OnFadeout;
            instance.OnTypeChanged += OnTypeChanged;
            instance.OnPositionChanged += OnPositionChanged;
            instance.OnSpecialTypeChanged += OnSpecialTypeChanged;

            image = new Image();
            image.Width = 32;
            image.Height = 32;
            image.Stretch = Stretch.Fill;
            image.Source = GemImages[GemType.None];
            image.Tag = this;

            specialImage = new Image();
            specialImage.Width = 32;
            specialImage.Height = 32;
            specialImage.Stretch = Stretch.Fill;
            specialImage.Source = SpecialTypeImages[GemSpecialType.Regular];
            specialImage.Tag = this;
            specialImage.IsHitTestVisible = false;
        }

        public void Update()
        {
            if (null == instance)
            {
                return;
            }
            instance.Update();
        }

        public void OnMovingStart()
        {
            instance.OnMovingStart();
        }

        public void MoveTo(Direction direction)
        {
            instance.MoveTo(direction);
        }

        public Direction GetConstrains()
        {
            return instance.Constrains;
        }

        private void PrepareGemImages()
        {
            GemImages = new Dictionary<GemType, BitmapImage>();
            GemType[] gemTypes = Enum.GetValues(typeof(GemType)).Cast<GemType>().Where(v => v != GemType.All).ToArray();
            foreach (GemType gemtype in gemTypes)
            {
                string filename = Environment.CurrentDirectory + @"\Resources\" + gemtype.ToString() + ".png";
                if (File.Exists(filename))
                {
                    BitmapImage bi = new BitmapImage(new Uri(filename));
                    GemImages.Add(gemtype, bi);
                }
            }
        }

        private void PrepareSpecialTypeImages()
        {
            SpecialTypeImages = new Dictionary<GemSpecialType, BitmapImage>();
            GemSpecialType[] gemSpecialTypes = Enum.GetValues(typeof(GemSpecialType)).Cast<GemSpecialType>().ToArray();
            foreach (GemSpecialType gemspecialtype in gemSpecialTypes)
            {
                string filename = Environment.CurrentDirectory + @"\Resources\" + gemspecialtype.ToString() + ".png";
                if (File.Exists(filename))
                {
                    BitmapImage bi = new BitmapImage(new Uri(filename));
                    SpecialTypeImages.Add(gemspecialtype, bi);
                }
            }
        }

        private void OnAppear(GemController sender, bool animated)
        {
            image.Opacity = 1.0;
            specialImage.Opacity = 1.0;
            Task.Delay(500).ContinueWith(_ =>
            {
                instance.OnAppearOver();
            });
        }

        private void OnFadeout(GemController sender)
        {
            image.Opacity = 0.0;
            specialImage.Opacity = 0.0;
            Task.Delay(500).ContinueWith(_ =>
            {
                instance.OnFadeoutOver();
            });
        }

        private void OnTypeChanged(GemController sender, GemType type)
        {
            if (GemImages.ContainsKey(type))
            {
                image.Source = GemImages[type];
            }
        }

        private void OnPositionChanged(GemController sender, int x, int y, bool interpolate)
        {
            Canvas.SetLeft(image, 50 + x * 32);
            Canvas.SetTop(image, 50 + (32 * 6 - y * 32));
            Canvas.SetLeft(specialImage, 50 + x * 32);
            Canvas.SetTop(specialImage, 50 + (32 * 6 - y * 32));
            if (interpolate)
            {
                Task.Delay(500).ContinueWith(__ =>
                {
                    instance.OnMovingEnd();
                });
            }
            //else
            //{
            //    instance.OnMovingEnd();
            //}
        }

        private void OnSpecialTypeChanged(GemController sender, GemSpecialType specialType)
        {
            if (SpecialTypeImages.ContainsKey(specialType))
            {
                specialImage.Source = SpecialTypeImages[specialType];
            }
        }
    }
}
