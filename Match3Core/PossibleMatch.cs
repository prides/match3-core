using System.Collections.Generic;

namespace Match3Core
{
    public class PossibleMatch
    {
        public const int MATCH_COUNT = 3;

        private static int ID = 0;

        private int id = 0;

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

        private bool isMatched = false;
        public bool IsMatched
        {
            get { return isMatched; }
            private set { isMatched = value; }
        }

        private bool isOver = false;
        public bool IsOver
        {
            get { return isOver; }
        }

        private bool isContainer = false;
        public bool IsContainer
        {
            get { return isContainer; }
            private set { isContainer = value; }
        }

        public PossibleMatch(GemType type, bool isContainer = false)
        {
            this.isContainer = isContainer;
            id = ID++;
            Logger.Instance.Message(ToString() + " was created with type " + type);
            matchType = type;
        }

        public bool AddGem(GemController gem)
        {
            if (!IsPossibleToAdd(gem))
            {
                return false;
            }
            if (matchedGems.Count == 1 && !isContainer)
            {
                if (matchedGems[0].Position.x == gem.Position.x)
                {
                    MatchDirection = Line.Vertical;
                }
                else if (matchedGems[0].Position.y == gem.Position.y)
                {
                    MatchDirection = Line.Horizontal;
                }
            }
            Logger.Instance.Message(ToString() + ": " + gem.ToString() + " was added");
            matchedGems.Add(gem);
            if (!IsContainer)
            {
                gem.PossibleMatches.Add(this);
            }
            return true;
        }

        public void RemoveGem(GemController gem)
        {
            Logger.Instance.Message(ToString() + ": " + gem.ToString() + " was removed");
            if (MatchInitiator == gem)
            {
                MatchInitiator = null;
            }
            matchedGems.Remove(gem);
            if (!IsContainer)
            {
                gem.PossibleMatches.Remove(this);
            }
            if (matchedGems.Count == 1)
            {
                Over();
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
            other.Over();
        }

        public void Over()
        {
            Logger.Instance.Message(ToString() + " was cleared");
            MatchInitiator = null;
            isOver = true;
            MatchDirection = Line.None;
            if (!IsContainer)
            {
                foreach (GemController gem in matchedGems)
                {
                    gem.PossibleMatches.Remove(this);
                }
            }
            matchedGems.Clear();
            if (null != OnOver)
            {
                OnOver(this);
            }
        }

        public bool IsMatch()
        {
            return matchedGems.Count >= MATCH_COUNT;
        }

        public bool IsMatch(GemController additionalGem)
        {
            if (IsPossibleToAdd(additionalGem))
            {
                return matchedGems.Count >= MATCH_COUNT - 1;
            }
            else
            {
                return IsMatch();
            }
        }

        public bool IsPossibleToAdd(GemController addableGem)
        {
            if (addableGem.CurrentGemType.HasSameFlags(matchType))
            {
                if (matchedGems.Count == 0)
                {
                    return true;
                }
                else if (MatchDirection == Line.Horizontal && addableGem.Position.y != matchedGems[0].Position.y
                    || MatchDirection == Line.Vertical && addableGem.Position.x != matchedGems[0].Position.x
                    || matchedGems.Contains(addableGem))
                {
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Match()
        {
            if (!IsMatch())
            {
                return false;
            }
            if (IsMatched)
            {
                return true;
            }
            IsMatched = true;
            Logger.Instance.Message(ToString() + " matched");
            if (null != OnMatch)
            {
                OnMatch(this, matchedGems.ToArray());
            }
            return true;
        }

        private void CheckForPossibleMove()
        {

        }

        public override string ToString()
        {
            return "PossibleMatch[" + id + "," + matchDirection + "]";
        }
    }
}