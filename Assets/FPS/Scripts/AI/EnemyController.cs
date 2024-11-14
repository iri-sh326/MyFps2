using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Unity.FPS.AI
{
    /// <summary>
    /// ������ ������: ��Ƽ���� ���� ����
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
        public Material bodyMaterial;           // �������� �� ���͸���
        [GradientUsage(true)]
        public Gradient OnHitBodyGradient;      // �������� �÷� �׶���Ʈ ȿ���� ǥ��
        private List<RendererIndexData> bodyRenderer = new List<RendererIndexData>();       // body Material�� ������ �ִ� ������ ������ ����Ʈ
        private MaterialPropertyBlock bodyFlashMaterialProperty;

        [SerializeField] private float flashOnHitDuration = 0.5f;
        float lastTimeDamaged = float.NegativeInfinity;
        bool wasDamagedThisFrame = false;

        // Patrol
        public NavMeshAgent Agent { get; private set; }
        public PatrolPath PatrolPath { get; set; }
        private int pathDestinationIndex;
        private float pathReachingRadius = 1f;          // ��������
        #endregion

        private void Start()
        {
            // ����
            health = GetComponent<Health>();

            health.OnDamaged += OnDamaged;
            health.OnDie += OnDie;

            // body Material�� ������ �ִ� ������ ���� ����Ʈ �����
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
            // ������ ȿ��
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
                // ��ϵ� �Լ� ȣ��
                Damaged?.Invoke();

                // �������� �� �ð�
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
            // ���� ȿ��
            GameObject effectGo = Instantiate(deathVfxPrefab, deathVfxSpawnPosition.position, Quaternion.identity);
            Destroy(effectGo, 5f);

            // Enemy ų
            Destroy(gameObject);
        }

        // ��Ʈ���� ��ȿ����?
        private bool IsPathVaild()
        {
            return PatrolPath && PatrolPath.wayPoints.Count > 0;
        }

        // ���� ����� WayPoint
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

        // ��ǥ ������ ��ġ �� ������
        public Vector3 GetDestinationOnPath()
        {
            if (IsPathVaild() == false)
            {
                return this.transform.position;
            }

            return PatrolPath.GetPositionOfWayPoint(pathDestinationIndex);
        }

        // ��ǥ ���� ���� - Nav �ý��� �̿�
        public void SetNavDestination(Vector3 destination)
        {
            if (Agent)
            {
                Agent.SetDestination(destination);
            }
        }

        // ���� ���� �� ���� ��ǥ���� ����
        public void UpdatePathDestination(bool inverseOrder = false)
        {
            if (IsPathVaild() == false)
                return;

            // ���� ����
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

