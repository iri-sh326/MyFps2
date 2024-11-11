using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// 발사체 표준형
    /// </summary>
    public class ProjectileStandard : ProjectileBase
    {
        #region Variables
        private ProjectileBase projectileBase;
        private float maxLifeTime = 5f;

        // 이동
        public float speed = 20f;
        public float gravityDown = 0f;
        public Transform root;
        public Transform tip;

        private Vector3 velocity;
        private Vector3 lastRootPosition;
        private float shotTime;

        // 충돌
        public float radius = 0.01f;                       // 충돌 검사하는 구체의 반경

        public LayerMask hittableLayers = -1;               // Hit가 가능한 Layer
        private List<Collider> ignoredColliders;            // Hit 판정시 무시하는 충돌체 리스트

        // 충돌 연출
        public GameObject impactVfxPrefab;                        // 타격 효과 이펙트
        [SerializeField] private float impactVfxLifeTime = 5f;
        private float impactVfxSpawnOffset = 0.1f;

        public AudioClip impactSfxClip;                    // 타격음

        // 데미지
        public float damage = 20f;
        #endregion

        private void OnEnable()
        {
            projectileBase = GetComponent<ProjectileBase>();
            projectileBase.OnShoot += OnShoot;

            Destroy(gameObject, maxLifeTime);
        }

        // shoot 값 설정
        new void OnShoot()
        {
            velocity = transform.forward * speed;
            transform.position += projectileBase.InheritedMuzzleVelocity * Time.deltaTime;

            lastRootPosition = root.position;

            // 충돌 무시 리스트 생성 - projectile을 발사하는 자신의 충돌체를 가져와서 등록
            ignoredColliders = new List<Collider>();
            Collider[] ownerColliders = projectileBase.Owner.GetComponentsInChildren<Collider>();
            ignoredColliders.AddRange(ownerColliders);

            // projectile이 벽을 뚫고 날아가는 버그 수정
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
            // 이동
            transform.position += velocity * Time.deltaTime;

            // 중력
            if(gravityDown > 0f)
            {
                velocity += Vector3.down * gravityDown * Time.deltaTime;
            }

            // 충돌
            RaycastHit closestHit = new RaycastHit();
            closestHit.distance = Mathf.Infinity;
            bool foundHit = false;                      // hit한 충돌체를 찾았는지 여부

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

            // hit한 충돌체를 찾았다
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

        // 유효한 hit 인지 판정
        bool IsHitValid(RaycastHit hit)
        {
            // IgnoreHitDetection 컴포넌트를 가진 콜라이더 무시
            if (hit.collider.GetComponent<IgnoreHitDetection>())
            {
                return false;
            }

            // ignoredColliders에 포함된 콜라이더 무시
            if(ignoredColliders != null && ignoredColliders.Contains(hit.collider))
            {
                return false;
            }

            // trigger collider에게 Damageable이 없어야 함
            if (hit.collider.isTrigger && hit.collider.GetComponent<Damageable>() == null)
            {
                return false;
            }

            return true;
        }

        // Hit 구현, 데미지, Vfx, Sfx, ...etc
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
                // 충돌 위치에 게임오브젝트를 생성하고 AudioSource 컴포넌트를 추가해서 지정된 클립을 플레이한다
                AudioUtility.CreateSfx(impactSfxClip, point + (normal * impactVfxSpawnOffset), 1f, 3f);
            }
            



            // 발사체 킬
            Destroy(gameObject);
        }
    }
}

