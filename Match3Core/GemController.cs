using System;
using System.Collections;
using System.Collections.Generic;

namespace Match3Core
{
    public class GemController
    {
        private static int ID = 0;
        private int id = 0;

        #region events
        public delegate void SimpleGemEventDelegate(GemController sender);
        internal event SimpleGemEventDelegate OnReadyEvent;

        public delegate void PossibleMatchDelegate(GemController sender, PossibleMatch possibleMatch);
        internal event PossibleMatchDelegate OnPossibleMatchAddedEvent;

        public delegate void EventWithDirectionDelegate(GemController sender, Direction direction);
        internal event EventWithDirectionDelegate OnMovingToEvent;

        public delegate void EventWithTypeDelegate(GemController sender, GemType type);
        public event EventWithTypeDelegate OnTypeChanged;

        public delegate void EventWithPosition(GemController sender, int x, int y, bool interpolate);
        public event EventWithPosition OnPositionChanged;

        public delegate void EventWithBoolean(GemController sender, bool value);
        public event EventWithBoolean OnAppear;

        public delegate void EventWithSpecialType(GemController sender, GemSpecialType type);
        public event EventWithSpecialType OnSpecialMatch;
        public event EventWithSpecialType OnSpecialTypeChanged;

        public event SimpleGemEventDelegate OnFadeout;
        public event SimpleGemEventDelegate OnDissapear;
        #endregion

        #region properties
        public enum State
        {
            Idle,
            Moving,
            Matched
        }

        private State currentState;
        public State CurrentState
        {
            get { return currentState; }
            private set
            {
                Logger.Instance.Message(this.ToString() + " current state was changed to " + value);
                currentState = value;
            }
        }

        private GemType currentGemType = 0;
        public GemType CurrentGemType
        {
            get { return currentGemType; }
            private set { currentGemType = value; }
        }

        private GemSpecialType specialType;
        public GemSpecialType SpecialType
        {
            get { return specialType; }
            set
            {
                specialType = value;
                if (null != OnSpecialTypeChanged)
                {
                    OnSpecialTypeChanged(this, specialType);
                }
            }
        }

        private int currentX = 0;
        public int CurrentX
        {
            get { return currentX; }
            private set { currentX = value; }
        }

        private int currentY = 0;
        public int CurrentY
        {
            get { return currentY; }
            private set { currentY = value; }
        }

        private bool isActive = false;
        public bool IsActive
        {
            get { return isActive; }
            private set
            {
                isActive = value;
                Logger.Instance.Message(this.ToString() + " isActive set to " + value);
            }
        }

        private List<PossibleMatch> possibleMatches = new List<PossibleMatch>();
        internal List<PossibleMatch> PossibleMatches
        {
            get { return possibleMatches; }
        }

        private SwipeAction currentSwipeAction = null;
        internal SwipeAction CurrentSwipeAction
        {
            get { return currentSwipeAction; }
            set { currentSwipeAction = value; }
        }

        public Direction Constrains
        {
            get
            {
                return (LeftNeighbor == null ? Direction.Left : Direction.None)
                    | (RightNeighbor == null ? Direction.Right : Direction.None)
                    | (UpNeighbor == null ? Direction.Up : Direction.None)
                    | (DownNeighbor == null ? Direction.Down : Direction.None);
            }
        }
        #endregion

        #region neighbor
        private GemController leftNeighbor = null;
        private GemController rightNeighbor = null;
        private GemController upNeighbor = null;
        private GemController downNeighbor = null;

        private Direction neighborChangedFlag = Direction.None;
        public Direction NeighborChangedFlag
        {
            get { return neighborChangedFlag; }
        }

        public GemController LeftNeighbor
        {
            get { return leftNeighbor; }
            set
            {
                leftNeighbor = value;
                neighborChangedFlag |= Direction.Left;
            }
        }

        public GemController RightNeighbor
        {
            get { return rightNeighbor; }
            set
            {
                rightNeighbor = value;
                neighborChangedFlag |= Direction.Right;
            }
        }

        public GemController UpNeighbor
        {
            get { return upNeighbor; }
            set
            {
                upNeighbor = value;
                neighborChangedFlag |= Direction.Up;
            }
        }

        public GemController DownNeighbor
        {
            get { return downNeighbor; }
            set
            {
                downNeighbor = value;
                neighborChangedFlag |= Direction.Down;
            }
        }
        #endregion

        internal GemController()
        {
            id = ID++;
        }

        internal void Init(bool animated)
        {
            Logger.Instance.Message(this.ToString() + " was initialized");
            IsActive = true;
            if (null != OnAppear)
            {
                OnAppear(this, animated);
            }
        }

        internal void SetGemType(GemType type)
        {
            Logger.Instance.Message(this.ToString() + " type was set to " + type);
            currentGemType = type;
            if (null != OnTypeChanged)
            {
                OnTypeChanged(this, type);
            }
        }

        internal void SetPosition(int x, int y, bool interpolate = false)
        {
            Logger.Instance.Message(this.ToString() + " position was set to " + x + "," + y);
            CurrentX = x;
            CurrentY = y;
            if (interpolate)
            {
                OnMovingStart();
            }
            if (null != OnPositionChanged)
            {
                OnPositionChanged(this, x, y, interpolate);
            }
        }

        public GemController GetNeighbor(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    return this.LeftNeighbor;
                case Direction.Right:
                    return this.RightNeighbor;
                case Direction.Up:
                    return this.UpNeighbor;
                case Direction.Down:
                    return this.DownNeighbor;
                default:
                    return null;
            }
        }

        public void CheckNeighbor()
        {
            if (NeighborChangedFlag != Direction.None)
            {
                OnNeighborChanged();
            }
        }

        private void OnNeighborChanged()
        {
            if ((neighborChangedFlag & Direction.Left) == Direction.Left)
            {
                CheckNeighbor(leftNeighbor);
            }
            if ((neighborChangedFlag & Direction.Right) == Direction.Right)
            {
                CheckNeighbor(rightNeighbor);
            }
            if ((neighborChangedFlag & Direction.Up) == Direction.Up)
            {
                CheckNeighbor(upNeighbor);
            }
            if ((neighborChangedFlag & Direction.Down) == Direction.Down)
            {
                CheckNeighbor(downNeighbor);
            }

            if (possibleMatches.Count > 1)
            {
                List<PossibleMatch> posMatch = new List<PossibleMatch>(possibleMatches);
                for (int i = 0; i < posMatch.Count; i++)
                {
                    for (int j = 0; j < posMatch.Count; j++)
                    {
                        if (posMatch[i] == posMatch[j])
                        {
                            continue;
                        }
                        if (posMatch[i].MatchDirection == posMatch[j].MatchDirection)
                        {
                            posMatch[i].Merge(posMatch[j]);
                        }
                        else if (posMatch[i].IsMatched() && posMatch[j].IsMatched())
                        {
                            posMatch[i].Merge(posMatch[j]);
                        }
                    }
                }
            }

            bool isMatched = false;
            for (int i = possibleMatches.Count - 1; i >= 0; i--)
            {
                if (possibleMatches[i].CheckMatch())
                {
                    if (currentSwipeAction != null)
                    {
                        possibleMatches[i].MatchInitiator = this;
                    }
                    isMatched = true;
                }
            }

            if (CurrentState == State.Idle && null != currentSwipeAction)
            {
                currentSwipeAction.SetGemSwipeResult(this, isMatched);
            }

            neighborChangedFlag = Direction.None;
        }

        private void CheckNeighbor(GemController neighbor)
        {
            if (null == neighbor)
            {
                return;
            }
            if (!neighbor.CurrentGemType.HasSameFlags(this.CurrentGemType))
            {
                return;
            }
            bool gemAdded = false;
            foreach (PossibleMatch possibleMatch in neighbor.PossibleMatches)
            {
                if (possibleMatch.AddGem(this))
                {
                    gemAdded = true;
                }
            }
            if (!gemAdded)
            {
                PossibleMatch possibleMatch = new PossibleMatch(this.CurrentGemType.GetSameFlags(neighbor.CurrentGemType));
                possibleMatch.AddGem(this);
                Logger.Instance.Message(this.ToString() + " adding neighbor " + neighbor.ToString() + " to " + possibleMatch.ToString());
                possibleMatch.AddGem(neighbor);
                if (null != OnPossibleMatchAddedEvent)
                {
                    OnPossibleMatchAddedEvent(this, possibleMatch);
                }
            }
        }

        private void Clear()
        {
            IsActive = false;
            for (int i = possibleMatches.Count - 1; i >= 0; i--)
            {
                possibleMatches[i].RemoveGem(this);
            }
        }

        public void MoveTo(Direction direction)
        {
            Logger.Instance.Message(this.ToString() + " moving to " + direction);
            if (null != OnMovingToEvent)
            {
                OnMovingToEvent(this, direction);
            }
        }

        public void OnMovingStart()
        {
            Logger.Instance.Message(this.ToString() + " moving start");
            Clear();
            CurrentState = State.Moving;
        }

        public void OnMovingEnd()
        {
            Logger.Instance.Message(this.ToString() + " moving finished");
            IsActive = true;
            CurrentState = State.Idle;
            if (null != OnReadyEvent)
            {
                OnReadyEvent(this);
            }
        }

        internal void OnMatch()
        {
            if (CurrentState == State.Matched)
            {
                return;
            }
            Logger.Instance.Message(this.ToString() + " matched");
            Clear();
            CurrentState = State.Matched;
            IsActive = false;
            possibleMatches.Clear();
            if (null != OnFadeout)
            {
                OnFadeout(this);
            }
            if (null != OnSpecialMatch)
            {
                OnSpecialMatch(this, SpecialType);
            }
        }

        public void OnAppearOver()
        {
            Logger.Instance.Message(this.ToString() + " OnAppearOver");
            IsActive = true;
            if (null != OnReadyEvent)
            {
                OnReadyEvent(this);
            }
        }

        public void OnFadeoutOver()
        {
            Logger.Instance.Message(this.ToString() + " OnFadeoutOver");
            if (null != OnDissapear)
            {
                OnDissapear(this);
            }
        }

        public override string ToString()
        {
            return "Gem[" + id + "," + CurrentX + "," + CurrentY + "," + CurrentGemType + "," + SpecialType + "," + CurrentState + "]";
        }
    }
}