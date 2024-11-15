using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// ���ӿ� �����ϴ� Actor
    ///
    /// </summary>
    public class Actor : MonoBehaviour
    {
        #region Variables
        // �Ҽ�
        public int affiliation;
        // ������
        public Transform aimPoint;

        private ActorManager actorManager;
        #endregion

        private void Start()
        {
            // Actor ����Ʈ�� �߰�(���)
            actorManager = GameObject.FindObjectOfType<ActorManager>();
            // ����Ʈ�� ���ԵǾ� �ִ��� üũ
            if (actorManager.Actors.Contains(this))
            {
                actorManager.Actors.Add(this);
            }
        }

        private void OnDestroy()
        {
            // Actor ����Ʈ���� ����
            if(actorManager != null)
            {
                actorManager.Actors.Remove(this);
            }
        }
    }

}

