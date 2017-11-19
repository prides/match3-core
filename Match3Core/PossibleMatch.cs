using System.Collections.Generic;

namespace Match3Core
{
    public class PossibleMatch
    {
        private static int ID = 0;

        private int id = 0;
        public enum Line
        {
            None = 0,
            Horizontal = 1,
            Vertical = 2,
            Cross = 3
        }

        public delegate void GemsMatchedEventDelegate(PossibleMatch sender, GemController[] matchedGems);
        public event GemsMatchedEventDelegate OnMatch;

        public delegate void SimplePossibleMatchDelegate(PossibleMatch sender);
        public event SimplePossibleMatchDelegate OnOver;

        private GemController matchInitiator;
        public GemController MatchInitiator
        {
            get { return matchInitiator; }
            set { matchInitiator = value; }
        }

        private GemType matchType = 0;
        public GemType MatchType
        {
            get { return matchType; }
        }

        private Line matchDirection = Line.None;
        public Line MatchDirection
        {
            get { return matchDirection; }
            private set
            {
                matchDirection = value;
                Logger.Instance.Message(ToString() + " direction was changed to " + value);
            }
        }

        private List<GemController> matchedGems = new List<GemController>();
        public List<GemController> MatchedGems
        {
            get { return matchedGems; }
        }

        private bool isOver = false;
        public bool IsOver
        {
            get { return isOver; }
        }

        public PossibleMatch(GemType type)
        {
            id = ID++;
            Logger.Instance.Message(ToString() + " was created with type " + type);
            matchType = type;
        }

        public bool AddGem(GemController gem)
        {
            if (gem.CurrentGemType.HasSameFlags(matchType))
            {
                if (MatchDirection == Line.Horizontal && gem.CurrentX != matchedGems[0].CurrentX)
                {
                    return false;
                }
                if (MatchDirection == Line.Vertical && gem.CurrentY != matchedGems[0].CurrentY)
                {
                    return false;
                }
                if (matchedGems.Contains(gem))
                {
                    return false;
                }
                if (matchedGems.Count == 1)
                {
                    if (matchedGems[0].CurrentX == gem.CurrentX)
                    {
                        MatchDirection = Line.Horizontal;
                    }
                    else if (matchedGems[0].CurrentY == gem.CurrentY)
                    {
                        MatchDirection = Line.Vertical;
                    }
                }
                Logger.Instance.Message(ToString() + ": " + gem.ToString() + " was added");
                matchedGems.Add(gem);
                gem.PossibleMatches.Add(this);
                return true;
            }
            return false;
        }

        public void RemoveGem(GemController gem)
        {
            Logger.Instance.Message(ToString() + ": " + gem.ToString() + " was removed");
            if (MatchInitiator == gem)
            {
                MatchInitiator = null;
            }
            matchedGems.Remove(gem);
            gem.PossibleMatches.Remove(this);
            if (matchedGems.Count == 1)
            {
                Clear();
            }
        }

        public void Merge(PossibleMatch other)
        {
            Logger.Instance.Message(ToString() + " was merged with " + other.ToString());
            this.MatchDirection |= other.MatchDirection;
            foreach (GemController gem in other.matchedGems)
            {
                AddGem(gem);
            }
            other.Clear();
        }

        public void Clear()
        {
            Logger.Instance.Message(ToString() + " was cleared");
            MatchInitiator = null;
            isOver = true;
            MatchDirection = Line.None;
            foreach (GemController gem in matchedGems)
            {
                gem.PossibleMatches.Remove(this);
            }
            matchedGems.Clear();
            if (null != OnOver)
            {
                OnOver(this);
            }
        }

        public bool IsMatched()
        {
            return matchedGems.Count >= 3;
        }

        public bool CheckMatch()
        {
            if (matchedGems.Count >= 3)
            {
                Logger.Instance.Message(ToString() + " matched");
                if (null != OnMatch)
                {
                    OnMatch(this, matchedGems.ToArray());
                }
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return "PossibleMatch[" + id + "," + matchDirection + "]";
        }
    }
}