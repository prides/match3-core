using System;
using System.Collections;
using System.Collections.Generic;

namespace Match3Core
{
    public class GemController
    {
        #region events
        public delegate void SimpleGemEventDelegate(GemController sender);
        public event SimpleGemEventDelegate OnReadyEvent;

        public delegate void PossibleMatchDelegate(GemController sender, PossibleMatch possibleMatch);
        public event PossibleMatchDelegate OnPossibleMatchAddedEvent;

        public delegate void EventWithDirectionDelegate(GemController sender, Direction direction);
        public event EventWithDirectionDelegate OnMovingToEvent;

        public delegate void EventWithTypeDelegate(GemController sender, GemType type);
        public event EventWithTypeDelegate OnTypeChanged;

        public delegate void EventWithPosition(GemController sender, int x, int y, bool interpolate);
        public event EventWithPosition OnPositionChanged;

        public event SimpleGemEventDelegate OnAppear;
        public event SimpleGemEventDelegate OnFadeout;
        #endregion

        #region properties
        private GemType currentGemType = 0;
        public GemType CurrentGemType
        {
            get { return currentGemType; }
            private set { currentGemType = value; }
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
        }

        private List<PossibleMatch> possibleMatches = new List<PossibleMatch>();
        public List<PossibleMatch> PossibleMatches
        {
            get { return possibleMatches; }
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

        private SwipeAction currentSwipeAction = null;
        public SwipeAction CurrentSwipeAction
        {
            get { return currentSwipeAction; }
            set { currentSwipeAction = value; }
        }
        #endregion

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
        }

        public void Init()
        {
            isActive = true;
            if (null != OnAppear)
            {
                OnAppear(this);
            }
        }

        public void SetGemType(GemType type)
        {
            currentGemType = type;
            if (null != OnTypeChanged)
            {
                OnTypeChanged(this, type);
            }
        }

        public void SetPosition(int x, int y, bool interpolate = false)
        {
            CurrentX = x;
            CurrentY = y;
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

        public void OnNeighborChanged()
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
                    isMatched = true;
                }
            }

            if (currentState == State.Idle && null != currentSwipeAction)
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
                possibleMatch.AddGem(neighbor);
                if (null != OnPossibleMatchAddedEvent)
                {
                    OnPossibleMatchAddedEvent(this, possibleMatch);
                }
            }
        }

        private void Clear()
        {
            isActive = false;
            for (int i = possibleMatches.Count - 1; i >= 0; i--)
            {
                possibleMatches[i].RemoveGem(this);
            }
        }

        public void MoveTo(Direction direction)
        {
            if (null != OnMovingToEvent)
            {
                OnMovingToEvent(this, direction);
            }
        }

        public void OnMovingStart()
        {
            Clear();
            currentState = State.Moving;
        }

        public void OnMovingEnd()
        {
            isActive = true;
            currentState = State.Idle;
            if (null != OnReadyEvent)
            {
                OnReadyEvent(this);
            }
        }

        public void OnMatch()
        {
            //Clear();
            currentState = State.Matched;
            isActive = false;
            possibleMatches.Clear();
            if (null != OnFadeout)
            {
                OnFadeout(this);
            }
        }

        public void OnAppearOver()
        {
            isActive = true;
            if (null != OnReadyEvent)
            {
                OnReadyEvent(this);
            }
        }

        public void OnFadeoutOver()
        {

        }
    }
}