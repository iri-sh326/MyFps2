using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Unity.FPS.AI
{
    /// <summary>
    /// 렌더러 데이터: 메티리얼 정보 저장
    /// </summary>

    [System.Serializable]
    public struct RendererIndexData
    {
        public Renderer renderer;
        public int materialIdx;

        public RendererIndexData(Renderer _renderer, int index)
        {
            renderer = _renderer;
            materialIdx = index;
        }
    }
    public class EnemyController : MonoBehaviour
    {
        #region Variables
        private Health health;

        // death
        public GameObject deathVfxPrefab;
        public Transform deathVfxSpawnPosition;

        // damage
        public UnityAction Damaged;

        // Sfx
        public AudioClip damageSfx;

        // Vfx
        public Material bodyMaterial;           // 데미지를 줄 메터리얼
        [GradientUsage(true)]
        public Gradient OnHitBodyGradient;      // 데미지를 컬러 그라디언트 효과로 표현
        private List<RendererIndexData> bodyRenderer = new List<RendererIndexData>();       // body Material을 가지고 있는 렌더러 데이터 리스트
        private MaterialPropertyBlock bodyFlashMaterialProperty;

        [SerializeField] private float flashOnHitDuration = 0.5f;
        float lastTimeDamaged = float.NegativeInfinity;
        bool wasDamagedThisFrame = false;

        // Patrol
        public NavMeshAgent Agent { get; private set; }
        public PatrolPath PatrolPath { get; set; }
        private int pathDestinationIndex;
        private float pathReachingRadius = 1f;          // 도착판정
        #endregion

        private void Start()
        {
            // 참조
            health = GetComponent<Health>();

            health.OnDamaged += OnDamaged;
            health.OnDie += OnDie;

            // body Material을 가지고 있는 렌더러 정보 리스트 만들기
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in renderers)
            {
                for(int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    if (renderer.sharedMaterials[i] == bodyMaterial)
                    {
                        bodyRenderer.Add(new RendererIndexData(renderer, i));
                    }
                }
            }

            //
            bodyFlashMaterialProperty = new MaterialPropertyBlock();
        }

        private void Update()
        {
            // 데미지 효과
            Color currentColor = OnHitBodyGradient.Evaluate((Time.time - lastTimeDamaged) / flashOnHitDuration);
            bodyFlashMaterialProperty.SetColor("_EmissionColor", currentColor);
            foreach(var data in bodyRenderer)
            {
                data.renderer.SetPropertyBlock(bodyFlashMaterialProperty, data.materialIdx);
            }



            //
            wasDamagedThisFrame = false;
        }

        private void OnDamaged(float damage, GameObject damageSource)
        {
            if(damageSource && damageSource.GetComponent<EnemyController>() == null)
            {
                // 등록된 함수 호출
                Damaged?.Invoke();

                // 데미지를 준 시간
                lastTimeDamaged = Time.time;

                // Sfx
                if (damageSfx && wasDamagedThisFrame == false)
                {
                    AudioUtility.CreateSfx(damageSfx, this.transform.position, 0f);

                }
                wasDamagedThisFrame = true;
             
            }

        }

        private void OnDie()
        {
            // 폭발 효과
            GameObject effectGo = Instantiate(deathVfxPrefab, deathVfxSpawnPosition.position, Quaternion.identity);
            Destroy(effectGo, 5f);

            // Enemy 킬
            Destroy(gameObject);
        }

        // 패트롤이 유효한지?
        private bool IsPathVaild()
        {
            return PatrolPath && PatrolPath.wayPoints.Count > 0;
        }

        // 가장 가까운 WayPoint
        private void SetPathDestinationToClosestWayPoint()
        {
            if(IsPathVaild() == false)
            {
                pathDestinationIndex = 0;
                return;
            }

            int closestWayPointIndex = 0;

            for(int i = 0; i< PatrolPath.wayPoints.Count; i++)
            {
                float distance = PatrolPath.GetDistanceToWaypoint(transform.position, i);
                float closestDistance = PatrolPath.GetDistanceToWaypoint(transform.position, closestWayPointIndex);
                if(distance < closestDistance)
                {
                    closestWayPointIndex = i;
                }
            }
            pathDestinationIndex = closestWayPointIndex;
        }

        // 목표 지점의 위치 값 얻어오기
        public Vector3 GetDestinationOnPath()
        {
            if (IsPathVaild() == false)
            {
                return this.transform.position;
            }

            return PatrolPath.GetPositionOfWayPoint(pathDestinationIndex);
        }

        // 목표 지점 설정 - Nav 시스템 이용
        public void SetNavDestination(Vector3 destination)
        {
            if (Agent)
            {
                Agent.SetDestination(destination);
            }
        }

        // 도착 판정 후 다음 목표지점 설정
        public void UpdatePathDestination(bool inverseOrder = false)
        {
            if (IsPathVaild() == false)
                return;

            // 도착 판정
            float distance = (transform.position - GetDestinationOnPath()).magnitude;
            if(distance <= pathReachingRadius)
            {
                pathDestinationIndex = inverseOrder ? (pathDestinationIndex - 1) : (pathDestinationIndex + 1);
                if(pathDestinationIndex < 0)
                {
                    pathDestinationIndex += PatrolPath.wayPoints.Count;
                }
                if(pathDestinationIndex >= PatrolPath.wayPoints.Count)
                {
                    pathDestinationIndex -= PatrolPath.wayPoints.Count;
                }

            }
        }
    }
}

