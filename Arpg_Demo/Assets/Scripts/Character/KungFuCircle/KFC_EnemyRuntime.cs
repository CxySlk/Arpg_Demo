using cfg;

namespace Character
{
    public class KFC_EnemyRuntime
    {
        public readonly KFC_EnemyConfig config;
        public readonly float[] lastAtkTimes;
        public readonly int[] curUseTable;

        public KFC_EnemyRuntime(KFC_EnemyConfig config)
        {
            this.config = config;
            int count = config.AtkCDTable.Count;
            lastAtkTimes = new float[count];
            curUseTable = new int[count];

            for (int i = 0; i < count; i++)
            {
                lastAtkTimes[i] = float.MinValue;
                curUseTable[i] = config.UseTimesTable[i];
            }
        }
    }
}