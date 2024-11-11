using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// �߻�ü ǥ����
    /// </summary>
    public class ProjectileStandard : ProjectileBase
    {
        #region Variables
        private ProjectileBase projectileBase;
        private float maxLifeTime = 5f;

        // �̵�
        public float speed = 20f;
        public float gravityDown = 0f;
        public Transform root;
        public Transform tip;

        private Vector3 velocity;
        private Vector3 lastRootPosition;
        private float shotTime;

        // �浹
        public float radius = 0.01f;                       // �浹 �˻��ϴ� ��ü�� �ݰ�

        public LayerMask hittableLayers = -1;               // Hit�� ������ Layer
        private List<Collider> ignoredColliders;            // Hit ������ �����ϴ� �浹ü ����Ʈ

        // �浹 ����
        public GameObject impactVfxPrefab;                        // Ÿ�� ȿ�� ����Ʈ
        [SerializeField] private float impactVfxLifeTime = 5f;
        private float impactVfxSpawnOffset = 0.1f;

        public AudioClip impactSfxClip;                    // Ÿ����

        // ������
        public float damage = 20f;
        #endregion

        private void OnEnable()
        {
            projectileBase = GetComponent<ProjectileBase>();
            projectileBase.OnShoot += OnShoot;

            Destroy(gameObject, maxLifeTime);
        }

        // shoot �� ����
        new void OnShoot()
        {
            velocity = transform.forward * speed;
            transform.position += projectileBase.InheritedMuzzleVelocity * Time.deltaTime;

            lastRootPosition = root.position;

            // �浹 ���� ����Ʈ ���� - projectile�� �߻��ϴ� �ڽ��� �浹ü�� �����ͼ� ���
            ignoredColliders = new List<Collider>();
            Collider[] ownerColliders = projectileBase.Owner.GetComponentsInChildren<Collider>();
            ignoredColliders.AddRange(ownerColliders);

            // projectile�� ���� �հ� ���ư��� ���� ����
            PlayerWeaponsManager weaponsManager = projectileBase.Owner.GetComponent<PlayerWeaponsManager>();
            if (weaponsManager)
            {
                Vector3 cameraToMuzzle = projectileBase.InitialPosition - weaponsManager.weaponCamera.transform.position;
                if (Physics.Raycast(weaponsManager.weaponCamera.transform.position, cameraToMuzzle.normalized, out RaycastHit hit,
                    cameraToMuzzle.magnitude, hittableLayers, QueryTriggerInteraction.Collide))
                {
                    if (IsHitValid(hit))
                    {
                        OnHit(hit.point, hit.normal, hit.collider);
                    }

                }
            }
        }

        private void Update()
        {
            // �̵�
            transform.position += velocity * Time.deltaTime;

            // �߷�
            if(gravityDown > 0f)
            {
                velocity += Vector3.down * gravityDown * Time.deltaTime;
            }

            // �浹
            RaycastHit closestHit = new RaycastHit();
            closestHit.distance = Mathf.Infinity;
            bool foundHit = false;                      // hit�� �浹ü�� ã�Ҵ��� ����

            // Sphere Cast
            Vector3 displacementSinceLastFrame = tip.position - lastRootPosition;
            RaycastHit[] hits = Physics.SphereCastAll(lastRootPosition, radius, displacementSinceLastFrame.normalized,
                displacementSinceLastFrame.magnitude, hittableLayers, QueryTriggerInteraction.Collide);

            foreach(var hit in hits)
            {
                if(IsHitValid(hit) && hit.distance < closestHit.distance)
                {
                    foundHit = true;
                    closestHit = hit;
                }
            }

            // hit�� �浹ü�� ã�Ҵ�
            if (foundHit)
            {
                if(closestHit.distance <= 0f)
                {
                    closestHit.point = root.position;
                    closestHit.normal = -transform.forward;
                }
                OnHit(closestHit.point, closestHit.normal, closestHit.collider);
            }

            lastRootPosition = root.position;
        }

        // ��ȿ�� hit ���� ����
        bool IsHitValid(RaycastHit hit)
        {
            // IgnoreHitDetection ������Ʈ�� ���� �ݶ��̴� ����
            if (hit.collider.GetComponent<IgnoreHitDetection>())
            {
                return false;
            }

            // ignoredColliders�� ���Ե� �ݶ��̴� ����
            if(ignoredColliders != null && ignoredColliders.Contains(hit.collider))
            {
                return false;
            }

            // trigger collider���� Damageable�� ����� ��
            if (hit.collider.isTrigger && hit.collider.GetComponent<Damageable>() == null)
            {
                return false;
            }

            return true;
        }

        // Hit ����, ������, Vfx, Sfx, ...etc
        void OnHit(Vector3 point, Vector3 normal, Collider collider)
        {

            // Vfx
            if (impactVfxPrefab)
            {
                GameObject impactObject = Instantiate(impactVfxPrefab, point + (normal * impactVfxSpawnOffset), Quaternion.LookRotation(normal));
                if(impactVfxLifeTime > 0f)
                {
                    Destroy(impactObject, impactVfxLifeTime);
                }
            }

            // Sfx
            if (impactSfxClip)
            {
                // �浹 ��ġ�� ���ӿ�����Ʈ�� �����ϰ� AudioSource ������Ʈ�� �߰��ؼ� ������ Ŭ���� �÷����Ѵ�
                AudioUtility.CreateSfx(impactSfxClip, point + (normal * impactVfxSpawnOffset), 1f, 3f);
            }
            



            // �߻�ü ų
            Destroy(gameObject);
        }
    }
}

