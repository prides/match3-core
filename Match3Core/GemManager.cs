using System.Collections.Generic;

namespace Match3Core
{
    public class GemManager
    {
        public delegate void EventDelegateWithGem(GemManager sender, GemController instance);
        public event EventDelegateWithGem OnGemCreated;
        public event EventDelegateWithGem OnGemMatch;

        public delegate void EventDelegateWithBool(GemManager sender, bool value);
        public event EventDelegateWithBool OnReadyStateChanged;

        private bool isReady = false;
        public bool IsReady
        {
            get { return isReady; }
            private set
            {
                isReady = value;
                if (null != OnReadyStateChanged)
                {
                    OnReadyStateChanged(this, value);
                }
            }
        }

        private int rowCount;
        private int columnCount;
        private TileType[][] tiles;
        private GemStackManager gemStackManager;
        private GemController[][] gems;
        private List<PossibleMatch> possibleMatches = new List<PossibleMatch>();
        private List<PossibleMatch> matchedMatches = new List<PossibleMatch>();

        public GemManager(int rowCount, int columnCount, TileType[][] tiles = null)
        {
            Logger.Instance.Message("GemManager was created");
            this.rowCount = rowCount;
            this.columnCount = columnCount;
            if (tiles == null)
            {
                this.tiles = new TileType[columnCount][];
                for (int i = 0; i < this.tiles.Length; i++)
                {
                    this.tiles[i] = new TileType[rowCount];
                    for (int j = 0; j < this.tiles[i].Length; j++)
                    {
                        this.tiles[i][j] = TileType.Regular;
                    }
                }
            }
            else
            {
                this.tiles = tiles;
            }
            gemStackManager = new GemStackManager();
        }

        public void Init()
        {
            gems = new GemController[columnCount][];
            for (int x = 0; x < columnCount; x++)
            {
                gems[x] = new GemController[rowCount];
                for (int y = 0; y < rowCount; y++)
                {
                    if (tiles[x][y] != TileType.None)
                    {
                        gemNotReadyCount++;
                        gems[x][y] = CreateGem(x, y);
                    }
                }
            }
            for (int x = 0; x < columnCount; x++)
            {
                for (int y = 0; y < rowCount; y++)
                {
                    if (null != gems[x][y])
                    {
                        SetGemNeighbor(gems[x][y]);
                    }
                }
            }
            for (int x = 0; x < columnCount; x++)
            {
                for (int y = 0; y < rowCount; y++)
                {
                    if (null != gems[x][y])
                    {
                        gems[x][y].CheckNeighbors();
                    }
                }
            }
            Logger.Instance.Message("GemManager was initialized");
        }

        public void Deinit()
        {
            if (null != gems)
            {
                for (int x = 0; x < columnCount; x++)
                {
                    for (int y = 0; y < rowCount; y++)
                    {
                        if (null != gems[x][y])
                        {
                            gems[x][y].OnReadyEvent -= OnGemReady;
                            gems[x][y].OnNotReadyEvent -= OnGemNotReady;
                            gems[x][y].OnPossibleMatchAddedEvent -= OnPossibleMatchAdded;
                            gems[x][y].OnMovingToEvent -= OnGemMove;
                            gems[x][y].OnDissapear -= OnGemDissapear;
                            gems[x][y].OnSpecialMatch -= OnSpecialGemMatch;
                            gems[x][y].OnPossibleMoveFound -= OnPossibleMoveFound;
                        }
                    }
                }
            }
            gemStackManager.Deinit();
            Logger.Instance.Message("GemManager was deinitialized");
        }

        public void CheckMatchedMatches()
        {
            if (matchedMatches.Count > 0)
            {
                foreach (PossibleMatch pm in matchedMatches)
                {
                    if (pm.IsOver)
                    {
                        continue;
                    }
                    Logger.Instance.Message(pm.ToString() + " was matched");

                    List<GemController> matchedGems = new List<GemController>(pm.MatchedGems);
                    if (matchedGems.Count > 3 && !pm.IsContainer)
                    {
                        GemSpecialType type = GemSpecialType.Regular;
                        if (matchedGems.Count > 4 && (pm.MatchDirection == Line.Horizontal || pm.MatchDirection == Line.Vertical))
                        {
                            type = GemSpecialType.HitType;
                        }
                        else if (pm.MatchDirection == Line.Cross)
                        {
                            type = GemSpecialType.Bomb;
                        }
                        else if (pm.MatchDirection == Line.Vertical)
                        {
                            type = GemSpecialType.Horizontal;
                        }
                        else if (pm.MatchDirection == Line.Horizontal)
                        {
                            type = GemSpecialType.Vertical;
                        }
                        GemController gem = pm.MatchInitiator;
                        if (gem == null)
                        {
                            gem = matchedGems[Randomizer.Range(0, matchedGems.Count)];
                        }
                        gem.SpecialType = type;
                        matchedGems.Remove(gem);
                    }

                    foreach (GemController gem in matchedGems)
                    {
                        MatchGem(gem);
                    }
                    pm.Over();
                }
                matchedMatches.Clear();
            }
            if (dissapearedGems.Count > 0)
            {
                RefillColums();
            }
        }

        private void RefillColums()
        {
            List<int> needToCheckColums = new List<int>();
            List<GemController> gemsUpdateNeighbors = new List<GemController>();
            lock (dissapearedGemsLocker)
            {
                foreach (GemController gem in dissapearedGems)
                {
                    if (!needToCheckColums.Contains(gem.Position.x))
                    {
                        needToCheckColums.Add(gem.Position.x);
                    }
                    gems[gem.Position.x][gem.Position.y] = null;
                    gemStackManager.Push(gem);
                }
                dissapearedGems.Clear();
            }
            foreach (int x in needToCheckColums)
            {
                Logger.Instance.Message("Check column " + x);
                int posToY = 0;
                int posFromY = 0;
                string message = "";
                for (int i = 0; i < rowCount; i++)
                {
                    message += (gems[x][i] != null ? gems[x][i].ToString() : "null") + (i == rowCount - 1 ? ", " : "");
                }
                Logger.Instance.Message("column " + x + " gems: " + message);
                while (posFromY < rowCount)
                {
                    while (posToY < rowCount && (gems[x][posToY] != null || tiles[x][posToY] == TileType.None))
                    {
                        posToY++;
                    }
                    if (posToY >= rowCount)
                    {
                        break;
                    }
                    posFromY = posToY + 1;
                    while (posFromY < rowCount && (gems[x][posFromY] == null || tiles[x][posToY] == TileType.None))
                    {
                        posFromY++;
                    }
                    if (posFromY >= rowCount)
                    {
                        break;
                    }

                    gems[x][posFromY].SetPosition(x, posToY, true);
                    gemsUpdateNeighbors.Add(gems[x][posFromY]);
                    SwitchGems(x, posToY, x, posFromY);
                }
            }
            foreach (int x in needToCheckColums)
            {
                int posFromY = 0;
                while (posFromY < rowCount && (gems[x][posFromY] != null || tiles[x][posFromY] == TileType.None))
                {
                    posFromY++;
                }
                int posYdiff = rowCount - posFromY;
                while (posFromY < rowCount)
                {
                    if (tiles[x][posFromY] == TileType.None)
                    {
                        continue;
                    }
                    GemController gem = CreateGem(x, posFromY, false);
                    gems[x][posFromY] = gem;
                    gem.SetPosition(x, posFromY + posYdiff, false);
                    gem.SetPosition(x, posFromY, true);
                    posFromY++;
                    gemsUpdateNeighbors.Add(gem);
                }
            }
            foreach (GemController gem in gemsUpdateNeighbors)
            {
                SetGemNeighbor(gem);
            }
        }

        private void OnSpecialGemMatch(GemController sender, GemSpecialType type)
        {
            Logger.Instance.Message("Special " + sender.ToString() + " matched");
            List<GemController> hitGems = new List<GemController>();
            int bombsize = type == GemSpecialType.DoubleBomb ? 5 : 3;
            switch(type)
            {
                case GemSpecialType.Horizontal:
                    hitGems.AddRange(GetGemsByRowIndex(sender.Position.y));
                    break;
                case GemSpecialType.Vertical:
                    hitGems.AddRange(GetGemsByColumnIndex(sender.Position.x));
                    break;
                case GemSpecialType.Vertical | GemSpecialType.Horizontal:
                    hitGems.AddRange(GetGemsByRowIndex(sender.Position.y));
                    hitGems.AddRange(GetGemsByColumnIndex(sender.Position.x));
                    break;
                case GemSpecialType.Vertical | GemSpecialType.Bomb:
                    if (sender.Position.x - 1 >= 0)
                    {
                        hitGems.AddRange(GetGemsByColumnIndex(sender.Position.x - 1));
                    }
                    hitGems.AddRange(GetGemsByColumnIndex(sender.Position.x));
                    if (sender.Position.x + 1 < columnCount)
                    {
                        hitGems.AddRange(GetGemsByColumnIndex(sender.Position.x + 1));
                    }
                    break;
                case GemSpecialType.Horizontal | GemSpecialType.Bomb:
                    if (sender.Position.y - 1 >= 0)
                    {
                        hitGems.AddRange(GetGemsByRowIndex(sender.Position.y - 1));
                    }
                    hitGems.AddRange(GetGemsByRowIndex(sender.Position.y));
                    if (sender.Position.y + 1 < rowCount)
                    {
                        hitGems.AddRange(GetGemsByRowIndex(sender.Position.y + 1));
                    }
                    break;
                case GemSpecialType.DoubleBomb:
                case GemSpecialType.Bomb:
                    for (int x = sender.Position.x - (bombsize / 2); x <= sender.Position.x + (bombsize / 2); x++)
                    {
                        for (int y = sender.Position.y - (bombsize / 2); y <= sender.Position.y + (bombsize / 2); y++)
                        {
                            if (y < 0 || x < 0 || y >= rowCount || x >= columnCount)
                            {
                                continue;
                            }
                            GemController gem = gems[x][y];
                            if (gem == null)
                            {
                                continue;
                            }
                            hitGems.Add(gem);
                        }
                    }
                    break;
                case GemSpecialType.HitType:
                    break;
                default:
                    Logger.Instance.Error("unknown special type " + type + " of " + sender.ToString());
                    break;
            }
            foreach (GemController gem in hitGems)
            {
                if (gem == sender)
                {
                    continue;
                }
                MatchGem(gem);
            }
        }

        private int gemNotReadyCount = 0;
        private void OnGemReady(GemController sender)
        {
            gemNotReadyCount--;
            if (gemNotReadyCount == 0)
            {
                IsReady = true;
                Logger.Instance.Warning("Game is ready");
            }
            else if (gemNotReadyCount < 0)
            {
                Logger.Instance.Error("gem ready counter is less than 0");
            }
        }

        private void OnGemNotReady(GemController sender)
        {
            if (gemNotReadyCount == 0)
            {
                IsReady = false;
                Logger.Instance.Warning("Game is not ready");
            }
            gemNotReadyCount++;
        }

        private void OnPossibleMatchAdded(GemController sender, PossibleMatch possibleMatch)
        {
            if (null == possibleMatch)
            {
                Logger.Instance.Error("possibleMatch is null");
                return;
            }
            Logger.Instance.Message(possibleMatch.ToString() + " was added to possibleMatches");
            possibleMatch.OnMatch += OnGemsMatch;
            possibleMatch.OnOver += OnPossibleMatchOver;
            possibleMatches.Add(possibleMatch);
        }

        private void OnPossibleMatchOver(PossibleMatch sender)
        {
            Logger.Instance.Message(sender.ToString() + " is over");
            sender.OnMatch -= OnGemsMatch;
            sender.OnOver -= OnPossibleMatchOver;
            possibleMatches.Remove(sender);
        }

        private void OnGemsMatch(PossibleMatch sender, GemController[] matchGems)
        {
            Logger.Instance.Message(sender.ToString() + " was added to matchedMatches");
            if (!matchedMatches.Contains(sender))
            {
                matchedMatches.Add(sender);
            }
        }

        private void SetGemNeighbor(GemController gem)
        {
            if (gem == null)
            {
                return;
            }
            int x = gem.Position.x;
            int y = gem.Position.y;

            GemController leftNeighbor = x - 1 >= 0 ? gems[x - 1][y] : null;
            GemController rightNeighbor = x + 1 < columnCount ? gems[x + 1][y] : null;
            GemController upNeighbor = y + 1 < rowCount ? gems[x][y + 1] : null;
            GemController downNeighbor = y - 1 >= 0 ? gems[x][y - 1] : null;

            gem.LeftNeighbor = leftNeighbor;
            gem.RightNeighbor = rightNeighbor;
            gem.UpNeighbor = upNeighbor;
            gem.DownNeighbor = downNeighbor;

            if (leftNeighbor != null)
            {
                leftNeighbor.RightNeighbor = gem;
            }
            if (rightNeighbor != null)
            {
                rightNeighbor.LeftNeighbor = gem;
            }
            if (upNeighbor != null)
            {
                upNeighbor.DownNeighbor = gem;
            }
            if (downNeighbor != null)
            {
                downNeighbor.UpNeighbor = gem;
            }
        }

        private void RemoveGemNeighbor(GemController gem)
        {
            gem.LeftNeighbor = null;
            gem.RightNeighbor = null;
            gem.UpNeighbor = null;
            gem.DownNeighbor = null;

            if (gem.LeftNeighbor != null)
            {
                gem.LeftNeighbor.RightNeighbor = null;
            }
            if (gem.RightNeighbor != null)
            {
                gem.RightNeighbor.LeftNeighbor = null;
            }
            if (gem.UpNeighbor != null)
            {
                gem.UpNeighbor.DownNeighbor = null;
            }
            if (gem.DownNeighbor != null)
            {
                gem.DownNeighbor.UpNeighbor = null;
            }
        }

        private GemController CreateGem(int x, int y, bool beggining = true)
        {
            GemController gem = gemStackManager.Pop();
            if (gem == null)
            {
                gem = new GemController();
                gem.OnReadyEvent += OnGemReady;
                gem.OnNotReadyEvent += OnGemNotReady;
                gem.OnPossibleMatchAddedEvent += OnPossibleMatchAdded;
                gem.OnMovingToEvent += OnGemMove;
                gem.OnDissapear += OnGemDissapear;
                gem.OnSpecialMatch += OnSpecialGemMatch;
                gem.OnPossibleMoveFound += OnPossibleMoveFound;
                if (null != OnGemCreated)
                {
                    OnGemCreated(this, gem);
                }
            }
            gem.SetPosition(x, y);

            GemType unacceptableType = GemType.None;
            GemController xm1Neighbor = x - 1 >= 0 ? gems[x - 1][y] : null;
            GemController xm2Neighbor = x - 2 >= 0 ? gems[x - 2][y] : null;
            if ((xm1Neighbor != null && xm2Neighbor != null) && (xm1Neighbor.CurrentGemType.HasSameFlags(xm2Neighbor.CurrentGemType)))
            {
                unacceptableType.AddFlag(xm1Neighbor.CurrentGemType.GetSameFlags(xm2Neighbor.CurrentGemType), out unacceptableType);
            }

            GemController ym1Neighbor = y - 1 >= 0 ? gems[x][y - 1] : null;
            GemController ym2Neighbor = y - 2 >= 0 ? gems[x][y - 2] : null;
            if ((ym1Neighbor != null && ym2Neighbor != null) && (ym1Neighbor.CurrentGemType.HasSameFlags(ym2Neighbor.CurrentGemType)))
            {
                unacceptableType.AddFlag(ym1Neighbor.CurrentGemType.GetSameFlags(ym2Neighbor.CurrentGemType), out unacceptableType);
            }

            GemType type = (~unacceptableType).Random();

            gem.SetGemType(type);
            gem.Init(beggining);
            return gem;
        }

        private List<GemController> dissapearedGems = new List<GemController>();
        private object dissapearedGemsLocker = new object();
        private void OnGemDissapear(GemController gem)
        {
            lock (dissapearedGemsLocker)
            {
                dissapearedGems.Add(gem);
            }
        }

        public void OnGemMove(GemController gem, Direction direction)
        {
            GemController neighbor = gem.GetNeighbor(direction);
            if (neighbor == null)
            {
                return;
            }
            if (!neighbor.IsActive || !gem.IsActive)
            {
                return;
            }
            if (gem.SpecialType != GemSpecialType.Regular && neighbor.SpecialType != GemSpecialType.Regular ||
                gem.SpecialType == GemSpecialType.HitType || neighbor.SpecialType == GemSpecialType.HitType)
            {
                DoSpecialSwipe(gem, neighbor);
                return;
            }
            int x1 = gem.Position.x;
            int y1 = gem.Position.y;
            int x2 = neighbor.Position.x;
            int y2 = neighbor.Position.y;

            SwitchGems(x1, y1, x2, y2);
            gem.SetPosition(x2, y2, true);
            neighbor.SetPosition(x1, y1, true);
            SetGemNeighbor(gem);
            SetGemNeighbor(neighbor);
            SwipeAction swipeAction = new SwipeAction(gem, neighbor);
            swipeAction.OnSwipeActionOver += OnSwipeOver;
            gem.CurrentSwipeAction = swipeAction;
            neighbor.CurrentSwipeAction = swipeAction;
        }

        private void OnSwipeOver(SwipeAction sender, GemController gem1, GemController gem2, bool isMatched)
        {
            if (gem1.CurrentSwipeAction == sender)
            {
                gem1.CurrentSwipeAction = null;
            }
            if (gem2.CurrentSwipeAction == sender)
            {
                gem2.CurrentSwipeAction = null;
            }
            if (isMatched)
            {
                return;
            }
            int x1 = gem1.Position.x;
            int y1 = gem1.Position.y;
            int x2 = gem2.Position.x;
            int y2 = gem2.Position.y;
            SwitchGems(x1, y1, x2, y2);
            gem1.SetPosition(x2, y2, true);
            gem2.SetPosition(x1, y1, true);
            SetGemNeighbor(gem1);
            SetGemNeighbor(gem2);
        }

        private void SwitchGems(int x1, int y1, int x2, int y2)
        {
            Logger.Instance.Message("Switching gems " + (gems[x1][y1] != null ? gems[x1][y1].ToString() : "null") + " and " + (gems[x2][y2] != null ? gems[x2][y2].ToString() : "null"));
            GemController tmpgem = gems[x1][y1];
            gems[x1][y1] = gems[x2][y2];
            gems[x2][y2] = tmpgem;
        }

        private void DoSpecialSwipe(GemController gem, GemController neighbor)
        {
            if (gem == null || neighbor == null)
            {
                Logger.Instance.Error("DoSpecialSwipe(): gem or neighbor is null");
                return;
            }
            if (neighbor.SpecialType == GemSpecialType.HitType && gem.SpecialType == GemSpecialType.HitType)
            {
                BreakByType(GemType.All);
            }
            else if (gem.SpecialType == GemSpecialType.HitType)
            {
                if (neighbor.SpecialType != GemSpecialType.Regular)
                {
                    SetSpecialTypeByType(neighbor.SpecialType, neighbor.CurrentGemType);
                }
                BreakByType(neighbor.CurrentGemType);
                MatchGem(gem);
            }
            else if (neighbor.SpecialType == GemSpecialType.HitType)
            {
                if (gem.SpecialType != GemSpecialType.Regular)
                {
                    SetSpecialTypeByType(gem.SpecialType, gem.CurrentGemType);
                }
                BreakByType(gem.CurrentGemType);
                MatchGem(neighbor);
            }
            else
            {
                GemSpecialType newSpecialType = GemSpecialType.Regular;
                if (neighbor.SpecialType == gem.SpecialType)
                {
                    switch(neighbor.SpecialType)
                    {
                        case GemSpecialType.Bomb:
                            newSpecialType = GemSpecialType.DoubleBomb;
                            break;
                        case GemSpecialType.Horizontal:
                        case GemSpecialType.Vertical:
                            newSpecialType = GemSpecialType.Horizontal | GemSpecialType.Vertical;
                            break;
                    }
                }
                else
                {
                    newSpecialType = neighbor.SpecialType | gem.SpecialType;
                }
                neighbor.SpecialType = newSpecialType;
                gem.SpecialType = GemSpecialType.Regular;
                MatchGem(gem);
                MatchGem(neighbor);
            }
        }

        private void SetSpecialTypeByType(GemSpecialType specialType, GemType type)
        {
            if (null == gems)
            {
                Logger.Instance.Error("SetSpecialTypeByType(): gems not inited");
            }
            for (int x = 0; x < columnCount; x++)
            {
                for (int y = 0; y < rowCount; y++)
                {
                    if (null != gems[x][y] && gems[x][y].CurrentGemType.HasSameFlags(type))
                    {
                        gems[x][y].SpecialType = specialType;
                    }
                }
            }
        }

        private void BreakByType(GemType type)
        {
            if (null == gems)
            {
                Logger.Instance.Error("BreakByType(): gems not inited");
            }
            PossibleMatch pm = new PossibleMatch(type, true);
            for (int x = 0; x < columnCount; x++)
            {
                for (int y = 0; y < rowCount; y++)
                {
                    if (null != gems[x][y] && gems[x][y].CurrentGemType.HasSameFlags(type))
                    {
                        pm.AddGem(gems[x][y]);
                    }
                }
            }
            matchedMatches.Add(pm);
        }

        private List<GemController> GetGemsByRowIndex(int rowIndex)
        {
            List<GemController> result = new List<GemController>();
            for (int x = 0; x < columnCount; x++)
            {
                GemController gem = gems[x][rowIndex];
                if (gem == null)
                {
                    continue;
                }
                result.Add(gem);
            }
            return result;
        }

        private List<GemController> GetGemsByColumnIndex(int columnIndex)
        {
            List<GemController> result = new List<GemController>();
            for (int y = 0; y < rowCount; y++)
            {
                GemController gem = gems[columnIndex][y];
                if (gem == null)
                {
                    continue;
                }
                result.Add(gem);
            }
            return result;
        }

        private void MatchGem(GemController gem)
        {
            if (gem == null || gem.CurrentState == GemController.State.Matched)
            {
                return;
            }
            RemoveGemNeighbor(gem);
            gem.OnMatch();
            if (null != OnGemMatch)
            {
                OnGemMatch(this, gem);
            }
        }

        public void ShuffleGems()
        {
            for (int i = columnCount * rowCount - 1; i >= 0; i--)
            {
                int i0 = i / rowCount;
                int i1 = i % rowCount;

                if (tiles[i0][i1] == TileType.None)
                {
                    continue;
                }

                GemType unacceptableType = GemType.None;
                GemController xm1Neighbor = i0 + 1 < columnCount ? gems[i0 + 1][i1] : null;
                GemController xm2Neighbor = i0 + 2 < columnCount ? gems[i0 + 2][i1] : null;
                if ((xm1Neighbor != null && xm2Neighbor != null) && (xm1Neighbor.CurrentGemType.HasSameFlags(xm2Neighbor.CurrentGemType)))
                {
                    unacceptableType.AddFlag(xm1Neighbor.CurrentGemType.GetSameFlags(xm2Neighbor.CurrentGemType), out unacceptableType);
                }

                GemController ym1Neighbor = i1 + 1 < rowCount ? gems[i0][i1 + 1] : null;
                GemController ym2Neighbor = i1 + 2 < rowCount ? gems[i0][i1 + 2] : null;
                if ((ym1Neighbor != null && ym2Neighbor != null) && (ym1Neighbor.CurrentGemType.HasSameFlags(ym2Neighbor.CurrentGemType)))
                {
                    unacceptableType.AddFlag(ym1Neighbor.CurrentGemType.GetSameFlags(ym2Neighbor.CurrentGemType), out unacceptableType);
                }

                int j = Randomizer.Range(0, i + 1);
                int j0 = j / rowCount;
                int j1 = j % rowCount;

                while (gems[j0][j1] != null && unacceptableType.HasSameFlags(gems[j0][j1].CurrentGemType) || tiles[j0][j1] == TileType.None)
                {
                    j = Randomizer.Range(0, i + 1);
                    j0 = j / rowCount;
                    j1 = j % rowCount;

                    if ((i == 0 || i == 1) && gems[j0][j1] != null && unacceptableType.HasSameFlags(gems[j0][j1].CurrentGemType))
                    {
                        i = rowCount;
                        continue;
                    }
                }

                GemController temp = gems[i0][i1];
                gems[i0][i1] = gems[j0][j1];
                gems[j0][j1] = temp;
                if (gems[i0][i1] != null)
                {
                    gems[i0][i1].SetPosition(i0, i1, true);
                }
            }

            for (int x = 0; x < columnCount; x++)
            {
                for (int y = 0; y < rowCount; y++)
                {
                    if (null != gems[x][y])
                    {
                        SetGemNeighbor(gems[x][y]);
                    }
                }
            }
        }

        #region PossibleMoves
        public delegate void PossibleMoveEvent(PossibleMove possibleMove);
        public event PossibleMoveEvent OnPossibleMoveCreate;

        private List<PossibleMove> possibleMoves = new List<PossibleMove>();
        public List<PossibleMove> PossibleMoves
        {
            get { return possibleMoves; }
        }
        private void OnPossibleMoveFound(GemController sender, GemController[] participants, GemController key, Line direction)
        {
            foreach (PossibleMove pm in PossibleMoves)
            {
                if (pm.GetHashCode() == MatchUtils.CalculateHash(new int[] { key.Position.x, key.Position.y, sender.Position.x, sender.Position.y }))
                {
                    return;
                }
            }
            List<GemController> participantsList = new List<GemController>();
            foreach (GemController participant in participants)
            {
                PossibleMatch participantPossibleMatch = participant.GetPossibleMatchByDirection(direction);
                if (participantPossibleMatch != null && !participantPossibleMatch.IsMatch())
                {
                    participantsList.AddRange(participantPossibleMatch.MatchedGems);
                }
                else
                {
                    participantsList.Add(participant);
                }
            }
            PossibleMove possibleMove = new PossibleMove(participantsList, key, sender.Position, direction);
            possibleMove.OnOver += PossibleMove_OnOver;
            possibleMoves.Add(possibleMove);

            if (OnPossibleMoveCreate != null)
            {
                OnPossibleMoveCreate(possibleMove);
            }
        }

        private void PossibleMove_OnOver(PossibleMove sender)
        {
            possibleMoves.Remove(sender);
        }
        #endregion
    }
}