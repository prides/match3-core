using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Match3Core
{
    public enum GemSpecialType
    {
        Regular     = 0,    //0000000
        Horizontal  = 1,    //0000001
        Vertical    = 2,    //0000010
        Bomb        = 4,    //0000100
        HitType     = 8,    //0001000
        DoubleBomb  = 16,   //0010000
    }
}