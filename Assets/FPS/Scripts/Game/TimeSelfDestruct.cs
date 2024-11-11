using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// TimeSelfDestruct ������ ���� ������Ʈ�� ������ �ð��� ų
    /// </summary>
    public class TimeSelfDestruct : MonoBehaviour
    {
        #region Variables
        public float lifeTime = 1f;
        private float spawnTime;
        #endregion

        private void Start()
        {
            // ���� �ð��� ����
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

