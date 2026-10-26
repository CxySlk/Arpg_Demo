using System.Collections.Generic;
using GAS;
using UnityEngine;

namespace Bullet
{
    public class BulletData
    {
        public LayerMask colliderLayerMask;
        public LayerMask targetLayerMask;
        public List<GameplayEffect> effects = new();
        public float maxSpeed = 10;
        public float accelerate = 10;
        public float maxDuration = 3;
    }
}