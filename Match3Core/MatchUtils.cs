namespace Match3Core
{
    public class MatchUtils
    {
        public static int CalculateHash(int[] values)
        {
            int hash = 17;
            foreach (int value in values)
            {
                hash = hash * 31 + value;
            }
            return hash;
        }
    }
}
