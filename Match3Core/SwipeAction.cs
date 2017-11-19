using System;
using System.Collections.Generic;

namespace Match3Core
{
    public class SwipeAction
    {
        private class SwipeInfo
        {
            internal GemController gem = null;
            internal bool isOver = false;
            internal bool isMatched = false;

            internal SwipeInfo(GemController gem, bool isOver, bool isMatched)
            {
                this.gem = gem;
                this.isOver = isOver;
                this.isMatched = isMatched;
            }
        }

        public delegate void SwipeActionEventDelegate(SwipeAction sender, GemController gem1, GemController gem2, bool result);
        public event SwipeActionEventDelegate OnSwipeActionOver;

        private List<SwipeInfo> swipingGems = new List<SwipeInfo>();

        public SwipeAction(GemController gem1, GemController gem2)
        {
            Logger.Instance.Message("SwipeAction was created with " + gem1.ToString() + " and " + gem2.ToString());
            swipingGems.Add(new SwipeInfo(gem1, false, false));
            swipingGems.Add(new SwipeInfo(gem2, false, false));
        }

        public void SetGemSwipeResult(GemController gem, bool isMatched)
        {
            Logger.Instance.Message(gem.ToString() + " is " + (isMatched ? "matched" : "not matched"));
            for (int i = 0; i < swipingGems.Count; i++)
            {
                if (swipingGems[i].gem == gem)
                {
                    swipingGems[i].isMatched = isMatched;
                    swipingGems[i].isOver = true;
                    break;
                }
            }

            CheckGemsSwipeEnded();
        }

        private void CheckGemsSwipeEnded()
        {
            bool isOver = true;
            bool isMatched = false;
            foreach (SwipeInfo si in swipingGems)
            {
                if (si.isMatched)
                {
                    isMatched = true;
                }
                if (!si.isOver)
                {
                    isOver = false;
                }
            }
            if (isOver && OnSwipeActionOver != null)
            {
                Logger.Instance.Message("swipe is over and is " + (isMatched ? "matched" : "not matched"));
                OnSwipeActionOver(this, swipingGems[0].gem, swipingGems[1].gem, isMatched);
            }
        }
    }
}