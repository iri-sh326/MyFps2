using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.AI
{
    /// <summary>
    /// 적 디텍팅 구현
    /// </summary>
    public class DetectionModule : MonoBehaviour
    {
        #region Variables
        private ActorManager actorManager;

        public UnityAction OnDetectedTarget;        // 적을 감지하면 등록된 함수 호출
        public UnityAction OnLostTarget;            // 적을 놓치면 등록된 함수 호출
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
            // 참조
            actorManager = GameObject.FindObjectOfType<ActorManager>();
        }

        // 디텍팅
        public void HandleTargetDectection(Actor actor, Collider[] selfCollider)
        {
            if(KnownDetectedTarget && !IsSeeingTarget && (Time.time - TimeLastSeenTarget) > knownTargetTimeout)

            sqrDetectionRange = detectionRange * detectionRange;
            IsSeeingTarget = false;
            float closestSqrdistance = Mathf.Infinity;

            foreach(var otherActor in actorManager.Actors)
            {
                // 아군이면
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

                    // 적을 찾았으면
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

            // 적을 모르고 있다가 적을 발견한 순간
            if(KnownDetectedTarget != null)
            {
                OnDetected();
            }

            // 적을 계속 주시하고 있다가 놓치는 순간
            if(KnownDetectedTarget == null)
            {
                OnLost();
            }

            // 디텍팅 상태 저장
            HadKnownTarget = KnownDetectedTarget != null;
        }

        // 적을 감지하면
        public void OnDetected()
        {
            OnDetectedTarget?.Invoke();
        }

        // 적을 놓치면
        public void OnLost()
        {
            OnLostTarget?.Invoke();
        }
    }

}

