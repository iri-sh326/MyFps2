using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// TimeSelfDestruct 부착한 게임 오브젝트는 지정된 시간에 킬
    /// </summary>
    public class TimeSelfDestruct : MonoBehaviour
    {
        #region Variables
        public float lifeTime = 1f;
        private float spawnTime;
        #endregion

        private void Start()
        {
            // 생성 시간을 저장
            spawnTime = Time.time;
        }

        private void Update()
        {
            if(spawnTime + lifeTime <= Time.time)
            {
                Destroy(gameObject);
            }
        }
    }
}

