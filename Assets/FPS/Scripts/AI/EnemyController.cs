using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.AI
{
    public class EnemyController : MonoBehaviour
    {
        #region Variables
        private Health health;

        // death
        public GameObject deathVfxPrefab;
        public Transform deathVfxSpawnPosition;
        #endregion

        private void Start()
        {
            // 참조
            health = GetComponent<Health>();
            health.OnDamaged += OnDamaged;
            health.OnDie += OnDie;
        }

        private void OnDamaged(float damage, GameObject damageSource)
        {

        }

        private void OnDie()
        {
            // 폭발 효과
            GameObject effectGo = Instantiate(deathVfxPrefab, deathVfxSpawnPosition.position, Quaternion.identity);
            Destroy(effectGo, 5f);

            // Enemy 킬
            Destroy(gameObject);
        }
    }
}

