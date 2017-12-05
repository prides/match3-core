using Match3Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;

namespace Match3CoreDemo
{
    class PossibleMoveWrapper
    {
        public delegate void PossibleMoveWrapperEvent(PossibleMoveWrapper sender);
        public event PossibleMoveWrapperEvent OnOverEvent;

        private PossibleMove instance;
        private Polygon winShape;
        public Polygon WinShape
        {
            get { return winShape; }
        }

        public PossibleMoveWrapper(PossibleMove instance)
        {
            this.instance = instance;
            instance.OnOver += Instance_OnOver;
            instance.tag = this;
            InitShape();
        }

        private void InitShape()
        {
            winShape = new Polygon();
            winShape.Points = GetShapePoints();
            winShape.Stroke = Brushes.Gray;
            winShape.StrokeThickness = 2;
            winShape.Width = 16;
            winShape.Height = 16;
            winShape.Stretch = Stretch.Fill;
            winShape.Fill = Brushes.OrangeRed;
            int angle = GetShapeAngle();
            winShape.RenderTransform = new RotateTransform(angle, 8, 8);
            winShape.IsHitTestVisible = false;

            Canvas.SetLeft(winShape, 50 + instance.Key.Position.x * 32 + 8);
            Canvas.SetTop(winShape, 50 + (32 * 6 - instance.Key.Position.y * 32) + 8);
        }

        private PointCollection GetShapePoints()
        {
            PointCollection result = new PointCollection();
            result.Add(new Point(0.0, 1.0));
            result.Add(new Point(0.5, 0.0));
            result.Add(new Point(1.0, 1.0));
            return result;
        }

        private int GetShapeAngle()
        {
            Direction direction = DirectionHelper.GetDirectionByPosition(instance.Key.Position, instance.MatchablePosition);
            switch (direction)
            {
                case Direction.Up:
                    return 0;
                case Direction.Right:
                    return 90;
                case Direction.Down:
                    return 180;
                case Direction.Left:
                    return 270;
                default:
                    return 0;
            }
        }

        private void Instance_OnOver(PossibleMove sender)
        {
            if (OnOverEvent != null)
            {
                OnOverEvent(this);
            }
        }
    }
}
