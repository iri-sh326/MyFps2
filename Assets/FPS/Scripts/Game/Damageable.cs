using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 데미지를 입는 충돌체(hit box)에 부착되어 데미지를 관리하는 클래스
    /// </summary>
    public class Damageable : MonoBehaviour
    {
        #region Variables
        private Health health;

        // 데미지 계수
        [SerializeField] private float damageMultiplier = 1f;

        // 자신이 입힌 데미지 계수
        [SerializeField] private float sensibilityToSelfDamage = 0.5f;
        #endregion

        private void Awake()
        {
            health = GetComponent<Health>();
            if(health == null)  // GetComponent를 했는데 못 찾았다  // 부모에게서 컴포넌트 가지고 오기
            {
                health = GetComponentInParent<Health>();
            }
        }

        public void InflictDamage(float damage, bool isExplosionDamage, GameObject damageSource)    // isExplosionDamage : 폭발에 의한 데미지 여부
        {
            if (health == null) return;

            // 실제 데미지 값
            var totalDamage = damage;

            // 폭발 데미지 체크 - 폭발 데미지일 때는 damageMultiplier를 계산하지 않는다
            if(isExplosionDamage == false)
            {
                totalDamage *= damageMultiplier;
            }

            // 자신이 입힌 데미지면
            if(health.gameObject == damageSource)
            {
                totalDamage *= sensibilityToSelfDamage;
            }



            // 데미지 입히기
            health.TakeDamage(totalDamage, damageSource);
        }
    }
}

