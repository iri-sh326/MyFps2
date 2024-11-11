using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// ÃæÀü¿ë ¹ß»çÃ¼ ¹ß»çÇÒ ¶§ ¹ß»çÃ¼¯M ¼Ó¼º°ªÀ» ¼³Á¤
    /// </summary>
    public class ProjectileChargeParameter : MonoBehaviour
    {
        #region Variables
        private ProjectileBase projectileBase;

        public MinMaxFloat Damage;
        public MinMaxFloat Speed;
        public MinMaxFloat GravityDown;
        public MinMaxFloat Radius;
        #endregion


        private void OnEnable() // È°¼ºÈ­ µÉ ¶§
        {
            // ÂüÁ¶
            projectileBase = GetComponent<ProjectileBase>();
            projectileBase.OnShoot += OnShoot;
        }

        // ¹ß»çÃ¼ ¹ß»ç½Ã projectileBaseÀÇ OnShoot µ¨¸®°ÔÀÌÆ® ÇÔ¼ö¿¡¼­ È£Ãâ
        // ¹ß»çÀÇ ¼Ó¼º°ªÀ» Charge°ª¿¡ µû¶ó ¼³Á¤
        void OnShoot()
        {
            // ÃæÀü·®¿¡ µû¶ó ¹ß»çÃ¼ ¼Ó¼º°ª ¼³Á¤
            ProjectileStandard projectileStandard = GetComponent<ProjectileStandard>();
            projectileStandard.damage = Damage.GetValueFromRatio(projectileBase.InitialCharge);
            projectileStandard.speed = Speed.GetValueFromRatio(projectileBase.InitialCharge);
            projectileStandard.gravityDown = GravityDown.GetValueFromRatio(projectileBase.InitialCharge);
            projectileStandard.radius = Radius.GetValueFromRatio(projectileBase.InitialCharge);
        }
    }

}

