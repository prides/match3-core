using System.Collections.Generic;

namespace Match3Core
{
    public class GemManager
    {
        public delegate void GemCreatedEventDelegate(GemController instance);
        public event GemCreatedEventDelegate OnGemCreated;

        private int rowCount = 7;
        private int columnCount = 7;
        private TileType[][] tiles =
            new TileType[][] {
                new TileType[] { TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular},
                new TileType[] { TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular},
                new TileType[] { TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular},
                new TileType[] { TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular},
                new TileType[] { TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular},
                new TileType[] { TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular},
                new TileType[] { TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular, TileType.Regular},
            };
        private GemController[][] gems;
        private List<PossibleMatch> possibleMatches = new List<PossibleMatch>();
        private List<PossibleMatch> matchedMatches = new List<PossibleMatch>();

        public GemManager(int rowCount, int columnCount)
        {
            Logger.Instance.Message("GemManager was created");
            this.rowCount = rowCount;
            this.columnCount = columnCount;
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
                        gems[x][y] = CreateGem(x, y);
                    }
                }
            }
            Logger.Instance.Message("GemManager was initialized");
        }

        public void DeInit()
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
                            gems[x][y].OnPossibleMatchAddedEvent -= OnPossibleMatchAdded;
                            gems[x][y].OnMovingToEvent -= OnGemMove;
                            gems[x][y].OnDissapear -= OnGemDissapear;
                            gems[x][y].OnSpecialMatch -= OnSpecialGemMatch;
                        }
                    }
                }
            }
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
                    if (matchedGems.Count > 3)
                    {
                        GemSpecialType type = GemSpecialType.Regular;
                        if (matchedGems.Count > 4 && (pm.MatchDirection == PossibleMatch.Line.Horizontal || pm.MatchDirection == PossibleMatch.Line.Vertical))
                        {
                            type = GemSpecialType.HitType;
                        }
                        else if (pm.MatchDirection == PossibleMatch.Line.Cross)
                        {
                            type = GemSpecialType.Bomb;
                        }
                        else if (pm.MatchDirection == PossibleMatch.Line.Horizontal)
                        {
                            type = GemSpecialType.Horizontal;
                        }
                        else if (pm.MatchDirection == PossibleMatch.Line.Vertical)
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
                        //gem.PossibleMatches.Remove(pm);
                        RemoveGemNeighbor(gem);
                        gem.OnMatch();
                    }
                    pm.Clear();
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
            lock (dissapearedGemsLocker)
            {
                foreach (GemController gem in dissapearedGems)
                {
                    if (!needToCheckColums.Contains(gem.CurrentX))
                    {
                        needToCheckColums.Add(gem.CurrentX);
                    }
                }
                dissapearedGems.Clear();
            }
            foreach (int x in needToCheckColums)
            {
                Logger.Instance.Message("Check column " + x);
                int posToY = 0;
                int posFromY = 0;
                while (posFromY < rowCount)
                {
                    while (posToY < rowCount && ((gems[x][posToY] == null || gems[x][posToY].CurrentState != GemController.State.Matched) || tiles[x][posToY] == TileType.None))
                    {
                        posToY++;
                    }
                    if (posToY >= rowCount)
                    {
                        break;
                    }
                    posFromY = posToY + 1;
                    while (posFromY < rowCount && ((gems[x][posFromY] == null || gems[x][posFromY].CurrentState == GemController.State.Matched) || tiles[x][posToY] == TileType.None))
                    {
                        posFromY++;
                    }
                    if (posFromY >= rowCount)
                    {
                        break;
                    }

                    RemoveGemNeighbor(gems[x][posFromY]);
                    gems[x][posFromY].SetPosition(x, posToY, true);
                    SwitchGems(x, posToY, x, posFromY);
                }
                posFromY = 0;
                while (posFromY < rowCount && ((gems[x][posFromY] == null || gems[x][posFromY].CurrentState != GemController.State.Matched) || tiles[x][posToY] == TileType.None))
                {
                    posFromY++;
                }
                int posYdiff = rowCount - posFromY;
                while (posFromY < rowCount)
                {
                    gems[x][posFromY] = CreateGem(x, posFromY, false);
                    gems[x][posFromY].SetPosition(x, posFromY + posYdiff, false);
                    gems[x][posFromY].SetPosition(x, posFromY, true);
                    posFromY++;
                }
            }
        }

        private void OnSpecialGemMatch(GemController sender, GemSpecialType type)
        {
            Logger.Instance.Message("Special " + sender.ToString() + " matched");
            List<GemController> hitGems = new List<GemController>();
            switch(type)
            {
                case GemSpecialType.Horizontal:
                    for (int x = 0; x < columnCount; x++)
                    {
                        if (x == sender.CurrentX)
                        {
                            continue;
                        }
                        GemController gem = gems[x][sender.CurrentY];
                        if (gem == null)
                        {
                            continue;
                        }
                        hitGems.Add(gem);
                    }
                    break;
                case GemSpecialType.Vertical:
                    for (int y = 0; y < rowCount; y++)
                    {
                        if (y == sender.CurrentY)
                        {
                            continue;
                        }
                        GemController gem = gems[sender.CurrentX][y];
                        if (gem == null)
                        {
                            continue;
                        }
                        hitGems.Add(gem);
                    }
                    break;
                case GemSpecialType.Bomb:
                    for (int x = sender.CurrentX - 1; x <= sender.CurrentX + 1; x++)
                    {
                        for (int y = sender.CurrentY - 1; y <= sender.CurrentY + 1; y++)
                        {
                            if (y == sender.CurrentY && x == sender.CurrentX)
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
                    for (int x = 0; x < columnCount; x++)
                    {
                        for (int y = 0; y < rowCount; y++)
                        {
                            if (y == sender.CurrentY && x == sender.CurrentX)
                            {
                                continue;
                            }
                            GemController gem = gems[x][y];
                            if (gem == null)
                            {
                                continue;
                            }
                            if (gem.CurrentGemType.HasSameFlags(sender.CurrentGemType))
                            {
                                hitGems.Add(gem);
                            }
                        }
                    }
                    break;
                default:
                    Logger.Instance.Error("unknown special type " + type + " of " + sender.ToString());
                    break;
            }
            foreach (GemController gem in hitGems)
            {
                RemoveGemNeighbor(gem);
                gem.OnMatch();
            }
        }

        private void OnGemReady(GemController sender)
        {
            SetGemNeighbor(sender);
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
            int x = gem.CurrentX;
            int y = gem.CurrentY;

            GemController leftNeighbor = x - 1 >= 0 ? gems[x - 1][y] : null;
            if (null != leftNeighbor && leftNeighbor.CurrentState != GemController.State.Idle)
            {
                leftNeighbor = null;
            }
            GemController rightNeighbor = x + 1 < columnCount ? gems[x + 1][y] : null;
            if (null != rightNeighbor && rightNeighbor.CurrentState != GemController.State.Idle)
            {
                rightNeighbor = null;
            }
            GemController upNeighbor = y + 1 < rowCount ? gems[x][y + 1] : null;
            if (null != upNeighbor && upNeighbor.CurrentState != GemController.State.Idle)
            {
                upNeighbor = null;
            }
            GemController downNeighbor = y - 1 >= 0 ? gems[x][y - 1] : null;
            if (null != downNeighbor && downNeighbor.CurrentState != GemController.State.Idle)
            {
                downNeighbor = null;
            }

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
            GemController gem = new GemController();
            if (null != OnGemCreated)
            {
                OnGemCreated(gem);
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
            GemController ym2Neighbor = y - 2 >= 0 ? gems[x][y - 1] : null;
            if ((ym1Neighbor != null && ym2Neighbor != null) && (ym1Neighbor.CurrentGemType.HasSameFlags(ym2Neighbor.CurrentGemType)))
            {
                unacceptableType.AddFlag(ym1Neighbor.CurrentGemType.GetSameFlags(ym2Neighbor.CurrentGemType), out unacceptableType);
            }

            GemType type = (~unacceptableType).Random();

            gem.SetGemType(type);
            gem.OnReadyEvent += OnGemReady;
            gem.OnPossibleMatchAddedEvent += OnPossibleMatchAdded;
            gem.OnMovingToEvent += OnGemMove;
            gem.OnDissapear += OnGemDissapear;
            gem.OnSpecialMatch += OnSpecialGemMatch;
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
            int x1 = gem.CurrentX;
            int y1 = gem.CurrentY;
            int x2 = neighbor.CurrentX;
            int y2 = neighbor.CurrentY;

            RemoveGemNeighbor(gem);
            RemoveGemNeighbor(neighbor);
            SwitchGems(x1, y1, x2, y2);
            gem.SetPosition(x2, y2, true);
            neighbor.SetPosition(x1, y1, true);
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
            int x1 = gem1.CurrentX;
            int y1 = gem1.CurrentY;
            int x2 = gem2.CurrentX;
            int y2 = gem2.CurrentY;
            SwitchGems(x1, y1, x2, y2);
            gem1.SetPosition(x2, y2, true);
            gem2.SetPosition(x1, y1, true);
        }

        private void SwitchGems(int x1, int y1, int x2, int y2)
        {
            Logger.Instance.Message("Switching gems " + gems[x1][y1].ToString() + " and " + gems[x2][y2].ToString());
            GemController tmpgem = gems[x1][y1];
            gems[x1][y1] = gems[x2][y2];
            gems[x2][y2] = tmpgem;
        }
    }
}