using System;
using System.Collections.Generic;
using System.Linq;

namespace Match3Core
{
    public class GemController
    {
        private static int ID = 0;
        private int id = 0;

        public object tag;

        #region events
        public delegate void SimpleGemEventDelegate(GemController sender);
        internal event SimpleGemEventDelegate OnReadyEvent;
        internal event SimpleGemEventDelegate OnNotReadyEvent;

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
        internal event EventWithSpecialType OnSpecialMatch;
        public event EventWithSpecialType OnSpecialTypeChanged;

        internal delegate void PossibleMoveDelegate(GemController sender, GemController[] participants, GemController key, Line direction);
        internal event PossibleMoveDelegate OnPossibleMoveFound;

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

        private Position position = new Position();
        public Position Position
        {
            get { return position; }
            private set { position = value; }
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

        private bool isReady = false;
        public bool IsReady
        {
            get { return isReady; }
            private set
            {
                if (isReady == value)
                {
                    return;
                }
                isReady = value;
                if (isReady)
                {
                    if (null != OnReadyEvent)
                    {
                        OnReadyEvent(this);
                    }
                }
                else
                {
                    if (null != OnNotReadyEvent)
                    {
                        OnNotReadyEvent(this);
                    }
                }
            }
        }
        #endregion

        #region neighbor
        private GemController leftNeighbor = null;
        private GemController rightNeighbor = null;
        private GemController upNeighbor = null;
        private GemController downNeighbor = null;

        private Direction neighborChangedFlag = Direction.None;
        private bool needToCheckNeighbor = false;

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

        private bool needToCheckPossibleMove = false;
        private bool needToCheckMatch = false;
        private Direction needToCheckNeighborPossibleMove = Direction.None;

        internal GemController()
        {
            id = ID++;
            possibleMoves.Add(PossibleMove.Role.Key, new List<PossibleMove>());
            possibleMoves.Add(PossibleMove.Role.Participant, new List<PossibleMove>());
        }

        internal void Init(bool beggining)
        {
            Logger.Instance.Message(this.ToString() + " was initialized");
            IsActive = true;
            if (beggining)
            {
                needToCheckNeighbor = true;
            }
            if (null != OnAppear)
            {
                OnAppear(this, beggining);
            }
        }

        internal void Deinit()
        {
            Clear();
            needToCheckPossibleMove = false;
            needToCheckMatch = false;
            needToCheckNeighborPossibleMove = Direction.None;
            neighborChangedFlag = Direction.None;
            SpecialType = GemSpecialType.Regular;
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
            position.x = x;
            position.y = y;
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

        public void Update()
        {
            if (CurrentState == State.Idle)
            {
                checkedForPossibleMove = false;
                if (needToCheckPossibleMove)
                {
                    CheckForPossibleMove();
                }
                if (needToCheckMatch)
                {
                    CheckForMatch();
                }
                if (needToCheckNeighbor && NeighborChangedFlag != Direction.None)
                {
                    CheckNeighbors();
                }
            }
        }

        internal void CheckNeighbors()
        {
            if ((neighborChangedFlag & Direction.Left) == Direction.Left)
            {
                CheckNeighbor(leftNeighbor, Direction.Left);
            }
            if ((neighborChangedFlag & Direction.Right) == Direction.Right)
            {
                CheckNeighbor(rightNeighbor, Direction.Right);
            }
            if ((neighborChangedFlag & Direction.Up) == Direction.Up)
            {
                CheckNeighbor(upNeighbor, Direction.Up);
            }
            if ((neighborChangedFlag & Direction.Down) == Direction.Down)
            {
                CheckNeighbor(downNeighbor, Direction.Down);
            }
            if (neighborChangedFlag != Direction.None)
            {
                return;
            }
            needToCheckNeighbor = false;
            needToCheckMatch = true;
        }

        private void CheckForMatch()
        {
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
                        else if (posMatch[i].IsMatch() && posMatch[j].IsMatch())
                        {
                            posMatch[i].Merge(posMatch[j]);
                        }
                    }
                }
            }

            bool isMatched = false;
            for (int i = possibleMatches.Count - 1; i >= 0; i--)
            {
                if (possibleMatches[i].IsMatch())
                {
                    if (currentSwipeAction != null && possibleMatches[i].MatchInitiator == null)
                    {
                        possibleMatches[i].MatchInitiator = this;
                    }
                    if (possibleMatches[i].Match())
                    {
                        isMatched = true;
                    }
                }
            }

            if (CurrentState == State.Idle && null != currentSwipeAction)
            {
                currentSwipeAction.SetGemSwipeResult(this, isMatched);
            }

            if (!isMatched)
            {
                needToCheckPossibleMove = true;
            }

            needToCheckMatch = false;
        }

        private void CheckNeighbor(GemController neighbor, Direction neighborDirection)
        {
            if (null == neighbor)
            {
                neighborChangedFlag &= (~neighborDirection);
                return;
            }
            if (!neighbor.IsActive)
            {
                return;
            }
            if (!neighbor.CurrentGemType.HasSameFlags(this.CurrentGemType))
            {
                neighborChangedFlag &= (~neighborDirection);
                return;
            }
            PossibleMatch addedPossibleMatch = null;
            foreach (PossibleMatch possibleMatch in neighbor.PossibleMatches)
            {
                if (possibleMatch.AddGem(this))
                {
                    addedPossibleMatch = possibleMatch;
                }
            }
            if (addedPossibleMatch == null)
            {
                addedPossibleMatch = new PossibleMatch(this.CurrentGemType.GetSameFlags(neighbor.CurrentGemType));
                addedPossibleMatch.AddGem(this);
                Logger.Instance.Message(this.ToString() + " adding neighbor " + neighbor.ToString() + " to " + addedPossibleMatch.ToString());
                addedPossibleMatch.AddGem(neighbor);
                if (null != OnPossibleMatchAddedEvent)
                {
                    OnPossibleMatchAddedEvent(this, addedPossibleMatch);
                }
            }
            neighborChangedFlag &= (~neighborDirection);
        }

        private void Clear()
        {
            IsActive = false;
            for (int i = possibleMatches.Count - 1; i >= 0; i--)
            {
                possibleMatches[i].RemoveGem(this);
            }
            for (int i = possibleMoves[PossibleMove.Role.Key].Count - 1; i >= 0; i--)
            {
                possibleMoves[PossibleMove.Role.Key][i].RemoveKey(this);
            }
            for (int i = possibleMoves[PossibleMove.Role.Participant].Count - 1; i >= 0; i--)
            {
                possibleMoves[PossibleMove.Role.Participant][i].RemoveParticipant(this);
            }
        }

        public void MoveTo(Direction direction)
        {
            if (!IsActive)
            {
                return;
            }
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
            IsReady = false;
        }

        public void OnMovingEnd()
        {
            Logger.Instance.Message(this.ToString() + " moving finished");
            IsActive = true;
            CurrentState = State.Idle;
            needToCheckNeighbor = true;
            needToCheckNeighborPossibleMove = Direction.Left | Direction.Right | Direction.Up | Direction.Down;
            IsReady = true;
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
            IsReady = false;
            if (SpecialType != GemSpecialType.Regular)
            {
                if (null != OnSpecialMatch)
                {
                    OnSpecialMatch(this, SpecialType);
                }
            }
        }

        public void OnAppearOver()
        {
            Logger.Instance.Message(this.ToString() + " OnAppearOver");
            //IsActive = true;
            IsReady = true;
        }

        public void OnFadeoutOver()
        {
            Logger.Instance.Message(this.ToString() + " OnFadeoutOver");
            if (null != OnDissapear)
            {
                OnDissapear(this);
            }
        }

        internal PossibleMatch GetPossibleMatchByDirection(Line direction)
        {
            foreach (PossibleMatch pm in possibleMatches)
            {
                if (pm.MatchDirection == direction)
                {
                    return pm;
                }
            }
            return null;
        }

        private bool checkedForPossibleMove = false;
        internal void CheckForPossibleMove()
        {
            if (checkedForPossibleMove)
            {
                return;
            }
            checkedForPossibleMove = true;
            Dictionary<GemType, Dictionary<Line, List<GemController>>> matchedNeighbors = new Dictionary<GemType, Dictionary<Line, List<GemController>>>();
            Direction[] directions = Enum.GetValues(typeof(Direction)).Cast<Direction>().Where(v => v != Direction.None).ToArray();
            List<GemController> availableNeighbors = new List<GemController>();
            foreach (Direction dir in directions)
            {
                GemController neighbor = GetNeighbor(dir);
                if (neighbor != null && neighbor.CurrentGemType != CurrentGemType && neighbor.IsActive)
                {
                    availableNeighbors.Add(neighbor);
                }
            }
            if (availableNeighbors.Count <= 1)
            {
                needToCheckPossibleMove = false;
                return;
            }
            for (int i = 0; i < availableNeighbors.Count; i++)
            {
                for (int j = i + 1; j < availableNeighbors.Count; j++)
                {
                    if (availableNeighbors[i].CurrentGemType == availableNeighbors[j].CurrentGemType)
                    {
                        if (!matchedNeighbors.ContainsKey(availableNeighbors[i].CurrentGemType))
                        {
                            matchedNeighbors[availableNeighbors[i].CurrentGemType] = new Dictionary<Line, List<GemController>>();
                        }
                        Line gemLine = DirectionHelper.GetLineByDirection(GetDirectionByNeighbor(availableNeighbors[i]));
                        if (!matchedNeighbors[availableNeighbors[i].CurrentGemType].ContainsKey(gemLine))
                        {
                            matchedNeighbors[availableNeighbors[i].CurrentGemType][gemLine] = new List<GemController>();
                        }
                        if (!matchedNeighbors[availableNeighbors[i].CurrentGemType][gemLine].Contains(availableNeighbors[i]))
                        {
                            matchedNeighbors[availableNeighbors[i].CurrentGemType][gemLine].Add(availableNeighbors[i]);
                        }
                        Line gemgemLine = DirectionHelper.GetLineByDirection(GetDirectionByNeighbor(availableNeighbors[j]));
                        if (!matchedNeighbors[availableNeighbors[j].CurrentGemType].ContainsKey(gemgemLine))
                        {
                            matchedNeighbors[availableNeighbors[j].CurrentGemType][gemgemLine] = new List<GemController>();
                        }
                        if (!matchedNeighbors[availableNeighbors[j].CurrentGemType][gemgemLine].Contains(availableNeighbors[j]))
                        {
                            matchedNeighbors[availableNeighbors[j].CurrentGemType][gemgemLine].Add(availableNeighbors[j]);
                        }
                    }
                }
            }
            foreach (GemType type in matchedNeighbors.Keys)
            {
                foreach (Line line in matchedNeighbors[type].Keys)
                {
                    Line oppositeLine = line == Line.Horizontal ? Line.Vertical : Line.Horizontal;
                    if (matchedNeighbors[type][line].Count > 1)
                    {
                        if (matchedNeighbors[type].ContainsKey(oppositeLine))
                        {
                            foreach (GemController keygem in matchedNeighbors[type][oppositeLine])
                            {
                                PossibleMoveFound(matchedNeighbors[type][line].ToArray(), keygem, line);
                            }
                        }
                        for (int i = 0; i < matchedNeighbors[type][line].Count; i++)
                        {
                            int otherindex = i == 0 ? 1 : 0;
                            if (matchedNeighbors[type][line][i].GetPossibleMatchByDirection(line) != null)
                            {
                                PossibleMoveFound(new GemController[] { matchedNeighbors[type][line][i] }, matchedNeighbors[type][line][otherindex], line);
                            }
                        }
                    }
                    else if (matchedNeighbors[type][line].Count == 1)
                    {
                        if (matchedNeighbors[type].ContainsKey(oppositeLine))
                        {
                            if (matchedNeighbors[type][line][0].GetPossibleMatchByDirection(line) != null)
                            {
                                foreach (GemController keygem in matchedNeighbors[type][oppositeLine])
                                {
                                    PossibleMoveFound(matchedNeighbors[type][line].ToArray(), keygem, line);
                                }
                            }
                        }
                    }
                }
            }
            needToCheckPossibleMove = false;
            if (needToCheckNeighborPossibleMove != Direction.None)
            {
                Direction[] dirs = Enum.GetValues(typeof(Direction)).Cast<Direction>().Where(d => d != Direction.None).ToArray();
                foreach (Direction dir in dirs)
                {
                    CheckNeighborPossibleMove(dir);
                }
            }
        }

        private void CheckNeighborPossibleMove(Direction direction)
        {
            GemController neighbor = GetNeighbor(direction);
            if ((needToCheckNeighborPossibleMove & direction) == direction && neighbor != null && neighbor.IsActive)
            {
                bool havesamepossiblematch = false;
                foreach (PossibleMatch pm in neighbor.PossibleMatches)
                {
                    if (PossibleMatches.Contains(pm))
                    {
                        havesamepossiblematch = true;
                    }
                }
                if (havesamepossiblematch)
                {
                    neighbor.GetNeighbor(direction)?.CheckForPossibleMove();
                }
                else
                {
                    neighbor.CheckForPossibleMove();
                }
            }
            needToCheckNeighborPossibleMove &= (~direction);
        }

        private void PossibleMoveFound(GemController[] participantsNeighbors, GemController key, Line direction)
        {
            if (null != OnPossibleMoveFound)
            {
                OnPossibleMoveFound(this, participantsNeighbors, key, direction);
            }
        }

        private Direction GetDirectionByNeighbor(GemController neighbor)
        {
            if (neighbor == null)
            {
                return Direction.None;
            }
            return DirectionHelper.GetDirectionByPosition(Position, neighbor.Position);
        }

        #region PossibleMoves
        private Dictionary<PossibleMove.Role, List<PossibleMove>> possibleMoves = new Dictionary<PossibleMove.Role, List<PossibleMove>>();
        public Dictionary<PossibleMove.Role, List<PossibleMove>> PossibleMoves
        {
            get { return possibleMoves; }
        }
        internal void AddPossibleMove(PossibleMove possibleMove, PossibleMove.Role role)
        {
            possibleMoves[role].Add(possibleMove);
        }

        internal void RemovePossibleMove(PossibleMove possibleMove, PossibleMove.Role role)
        {
            possibleMoves[role].Remove(possibleMove);
        }
        #endregion

        public override string ToString()
        {
            return "Gem[" + id + "," + Position.x + "," + Position.y + "," + CurrentGemType + "," + SpecialType + "," + CurrentState + "]";
        }
    }
}