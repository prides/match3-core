namespace Match3Core
{
    public enum GemType
    {
        None        = 0,    //0000000
        Close       = 1,    //0000001
        Distance    = 2,    //0000010
        Mass        = 4,    //0000100
        Heal        = 8,    //0001000
        Defence     = 16,   //0010000
        Money       = 32,   //0100000
        HitType     = 64,   //1000000
        //All         = 127    //1111111
    }
}