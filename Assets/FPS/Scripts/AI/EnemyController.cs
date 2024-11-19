using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
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
        private float pathReachingRadius = 1f;          // ��������\

        // Detection
        private Actor actor;
        private Collider[] selfColliders;
        public DetectionModule DetectionModule { get; private set; }

        public GameObject knownDetectedTarget => DetectionModule.KnownDetectedTarget;
        public bool IsSeeingTarget => DetectionModule.IsSeeingTarget;
        public bool HadKnownTarget => DetectionModule.HadKnownTarget;

        public Material eyeColorMaterial;
        [ColorUsage(true, true)] public Color defaultEyeColor;
        [ColorUsage(true, true)] public Color attackEyeColor;

        // eye Material�� ������ �ִ� ������ ������
        private RendererIndexData eyeRendererData;
        private MaterialPropertyBlock eyeColorMaterialPropertyBlock;

        public UnityAction OnDetectedTarget;
        public UnityAction OnLostTarget;

        // Attack
        public UnityAction OnAttack;

        private float orientSpeed = 10f;
        public bool IsTargetAttackRange => DetectionModule.IsTargetInAttackRange;

        public bool swapToNextWeapon = false;
        public float delayAfterWeaponSwap = 0f;
        private float lastTimeWeaponSwapped = Mathf.NegativeInfinity;

        public int currentWeaponIndex;
        private WeaponController currentWeapon;
        private WeaponController[] weapons;

        // enemyManager
        private EnemyManager enemyManager;
        #endregion

        private void Start()
        {
            // ����
            enemyManager = GameObject.FindObjectOfType<EnemyManager>();
            enemyManager.RegisterEnemy(this);

            Agent = GetComponent<NavMeshAgent>();
            actor = GetComponent<Actor>();
            selfColliders = GetComponentsInChildren<Collider>();

            var detectionModules = GetComponentInChildren<DetectionModule>();
            DetectionModule = detectionModules;
            DetectionModule.OnDetectedTarget += OnDetected;
            DetectionModule.OnLostTarget += OnLost;

            health = GetComponent<Health>();
            health.OnDamaged += OnDamaged;
            health.OnDie += OnDie;

            // ���� �ʱ�ȭ
            FindAndInitializeAllWeapons();
            var weapon = GetCurrentWeapon();
            weapon.ShowWeapon(true);

            // body Material�� ������ �ִ� ������ ���� ����Ʈ �����
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in renderers)
            {
                for(int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    // body
                    if (renderer.sharedMaterials[i] == bodyMaterial)
                    {
                        bodyRenderer.Add(new RendererIndexData(renderer, i));
                    }

                    // eye
                    if (renderer.sharedMaterials[i] == eyeColorMaterial)
                    {
                        eyeRendererData = new RendererIndexData(renderer, i);
                    }
                }
            }

            // body
            bodyFlashMaterialProperty = new MaterialPropertyBlock();

            // eye
            if(eyeRendererData.renderer != null)
            {
                eyeColorMaterialPropertyBlock = new MaterialPropertyBlock();
                eyeColorMaterialPropertyBlock.SetColor("_EmissionColor", defaultEyeColor);
                eyeRendererData.renderer.SetPropertyBlock(eyeColorMaterialPropertyBlock, eyeRendererData.materialIdx);
            }
        }

        private void Update()
        {
            // ���ؼ�
            DetectionModule.HandleTargetDectection(actor, selfColliders);

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
            // enemyManager ����Ʈ���� ����
            enemyManager.RemoveEnemy(this);

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

        //
        public void OrientToward(Vector3 lookPosition)
        {
            Vector3 lookDirect = Vector3.ProjectOnPlane(lookPosition - transform.position, Vector3.up);
            if(lookDirect.sqrMagnitude != 0)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirect);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, orientSpeed * Time.deltaTime);
            }
        }

        // �� ������ ȣ��Ǵ� �Լ�
        private void OnDetected()
        {
            OnDetectedTarget?.Invoke();

            if (eyeRendererData.renderer)
            {
                eyeColorMaterialPropertyBlock.SetColor("_EmissionColor", defaultEyeColor);
                eyeRendererData.renderer.SetPropertyBlock(eyeColorMaterialPropertyBlock,
                    eyeRendererData.materialIdx);
            }
        }

        // ���� ��ġ�� ȣ��Ǵ� �Լ�
        private void OnLost()
        {
            OnLostTarget?.Invoke();

            if (eyeRendererData.renderer)
            {
                eyeColorMaterialPropertyBlock.SetColor("_EmissionColor", defaultEyeColor);
                eyeRendererData.renderer.SetPropertyBlock(eyeColorMaterialPropertyBlock,
                    eyeRendererData.materialIdx);
                
            }
        }

        // ������ �ִ� ���� ã�� �ʱ�ȭ
        private void FindAndInitializeAllWeapons()
        {
            if(weapons == null)
            {
                weapons = this.GetComponentsInChildren<WeaponController>();

                for(int i = 0; i < weapons.Length; i++)
                {
                    weapons[i].Owner = this.gameObject;
                }
            }
        }

        // ������ �ε����� �ش��ϴ� ���⸦ current�� ����
        private void SetCurrentWeapon(int index)
        {
            currentWeaponIndex = index;
            currentWeapon = weapons[currentWeaponIndex];
            if (swapToNextWeapon)
            {
                lastTimeWeaponSwapped = Time.time;
            }
            else
            {
                lastTimeWeaponSwapped = Mathf.NegativeInfinity;
            }
        }

        // ���� current weapon ã��
        public WeaponController GetCurrentWeapon()
        {
            FindAndInitializeAllWeapons();
            if(currentWeapon == null)
            {
                SetCurrentWeapon(0);
            }
            return currentWeapon;
        }

        // ������ �ѱ��� ������
        public void OrientWeaponsToward(Vector3 lookPosition)
        {
            for(int i = 0;i < weapons.Length;i++)
            {
                Vector3 weaponForward = (lookPosition - weapons[i].transform.position).normalized;
                weapons[i].transform.forward = weaponForward;
            }
        }

        // ����
        public bool TryAttack(Vector3 targetPosition)
        {
            // ���� ��ü�� ������ �ð����� ���� �Ҵ�
            if(lastTimeWeaponSwapped + delayAfterWeaponSwap >= Time.time)
            {
                return false;
            }

            // ���� Shoot
            bool didFire = GetCurrentWeapon().HandleShootInputs(false, true, false);

            if (didFire)
            {
                OnAttack?.Invoke();

                // �߻縦 �� �� �� ������ ���� ����� ��ü
                if(swapToNextWeapon == true && weapons.Length > 1)
                {
                    int nextWeaponIndex = (currentWeaponIndex + 1)% weapons.Length;
                    SetCurrentWeapon(nextWeaponIndex);
                }
            }

            return true;
        }
    }
}

