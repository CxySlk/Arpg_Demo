using LLKFrame;

namespace Character
{
    public class CharacterEventKey
    {
        /// <summary>敌人死亡</summary>
        public static readonly EventKey<EnemyController> EnemyDeathEventKey = "Enemy.Death";
        /// <summary>玩家死亡</summary>
        public static readonly EventKey<PlayerController> PlayerDeathEventKey = "Player.Death";
        /// <summary>怪物全部死亡</summary>
        public static readonly  EventKey EnemyAllDeathEventKey = "Enemy.AllDeath";
    }
}