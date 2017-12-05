using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Match3Core
{
    public struct Position
    {
        public int x;
        public int y;

        public Position(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return x + "," + y;
        }
    }
}
