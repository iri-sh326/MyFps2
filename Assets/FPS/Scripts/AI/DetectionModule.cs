using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.AI
{
    /// <summary>
    /// �� ������ ����
    /// </summary>
    public class DetectionModule : MonoBehaviour
    {
        #region Variables
        private ActorManager actorManager;

        public UnityAction OnDetectedTarget;        // ���� �����ϸ� ��ϵ� �Լ� ȣ��
        public UnityAction OnLostTarget;            // ���� ��ġ�� ��ϵ� �Լ� ȣ��
        public GameObject KnownDetectedTarget { get; private set; }
        public bool HadKnownTarget { get; private set; }
        public bool IsSeeingTarget { get; private set; }
        public Transform detectionSourcePoint;
        public float detectionRange = 20f;
        public float sqrDetectionRange;

        public float knownTargetTimeout = 4f;
        private float TimeLastSeenTarget = Mathf.NegativeInfinity;

        // attack
        public float attackRange = 20f;
        public bool IsTargetInAttackRange { get; private set; }

        #endregion

        private void Start()
        {
            // ����
            actorManager = GameObject.FindObjectOfType<ActorManager>();
        }

        // ������
        public void HandleTargetDectection(Actor actor, Collider[] selfCollider)
        {
            if(KnownDetectedTarget && !IsSeeingTarget && (Time.time - TimeLastSeenTarget) > knownTargetTimeout)

            sqrDetectionRange = detectionRange * detectionRange;
            IsSeeingTarget = false;
            float closestSqrdistance = Mathf.Infinity;

            foreach(var otherActor in actorManager.Actors)
            {
                // �Ʊ��̸�
                if (otherActor.affiliation != actor.affiliation)
                    continue;

                float sqrDistance = (otherActor.aimPoint.position - detectionSourcePoint.position).sqrMagnitude;
                if(sqrDistance < sqrDetectionRange && sqrDistance < closestSqrdistance)
                {
                    RaycastHit[] hits = Physics.RaycastAll(detectionSourcePoint.position,
                        (otherActor.aimPoint.position - detectionSourcePoint.position).normalized, detectionRange,
                        -1, QueryTriggerInteraction.Ignore);

                    RaycastHit closestHit = new RaycastHit();
                    closestHit.distance = Mathf.Infinity;
                    bool foundVaildHit = false;
                    foreach(var hit in hits)
                    {
                        if(hit.distance < closestHit.distance)
                        {
                            closestHit = hit;
                            foundVaildHit = true;
                        }
                    }

                    // ���� ã������
                    if (foundVaildHit)
                    {
                        Actor hitActor = closestHit.collider.GetComponentInParent<Actor>();
                        if(hitActor == otherActor)
                        {
                            IsSeeingTarget = true;
                            closestSqrdistance = sqrDistance;

                            TimeLastSeenTarget = Time.time;
                            KnownDetectedTarget = otherActor.aimPoint.gameObject;
                        }
                    }
                }
            }

            // attack Range check
            IsTargetInAttackRange = (KnownDetectedTarget != null) && Vector3.Distance(transform.position,
                KnownDetectedTarget.transform.position) <= attackRange;

            // ���� �𸣰� �ִٰ� ���� �߰��� ����
            if(KnownDetectedTarget != null)
            {
                OnDetected();
            }

            // ���� ��� �ֽ��ϰ� �ִٰ� ��ġ�� ����
            if(KnownDetectedTarget == null)
            {
                OnLost();
            }

            // ������ ���� ����
            HadKnownTarget = KnownDetectedTarget != null;
        }

        // ���� �����ϸ�
        public void OnDetected()
        {
            OnDetectedTarget?.Invoke();
        }

        // ���� ��ġ��
        public void OnLost()
        {
            OnLostTarget?.Invoke();
        }
    }

}

