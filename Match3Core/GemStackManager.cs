using System.Collections.Generic;

namespace Match3Core
{
    internal class GemStackManager
    {
        private Queue<GemController> gemsStack = new Queue<GemController>();

        internal GemStackManager()
        {

        }

        internal void Push(GemController gem)
        {
            gem.SetPosition(666, 666);
            gem.Deinit();
            gemsStack.Enqueue(gem);
        }

        internal GemController Pop()
        {
            if (gemsStack.Count == 0)
            {
                return null;
            }
            return gemsStack.Dequeue();
        }

        internal void Deinit()
        {
            gemsStack.Clear();
        }
    }
}
