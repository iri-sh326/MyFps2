using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.AI
{
    /// <summary>
    /// 패트롤 Waypoint들을 관리하는 클래스
    /// </summary>
    public class PatrolPath : MonoBehaviour
    {
        #region Variables
        public List<Transform> wayPoints = new List<Transform>();

        // this path를 패트롤하는 Enemy들
        public List<EnemyController> enemiesToAssign = new List<EnemyController>();
        #endregion

        private void Start()
        {
            // 등록된 enemy에게 패트롤할 패스 지정
            foreach(var enemy in enemiesToAssign)
            {
                enemy.PatrolPath = this;
            }
        }

        // 특정 위치로부터 지정된 웨이포인트와의 거리 구하기
        public float GetDistanceToWaypoint(Vector3 origin, int wayPointIndex)
        {
            if(wayPointIndex < 0 || wayPointIndex >= wayPoints.Count || wayPoints[wayPointIndex] == null)
            {
                return -1f;
            }
            return (wayPoints[wayPointIndex].position - origin).magnitude;
        }

        // index로 지정된 WayPoint의 위치 반환
        public Vector3 GetPositionOfWayPoint(int index)
        {
            return Vector3.zero;
        }

        // 기즈모로 Path 그리기
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            for(int i = 0; i < wayPoints.Count; i++)
            {
                int nextIndex = i + 1;
                if (nextIndex == wayPoints.Count) return;
                if (nextIndex >= wayPoints.Count)
                {
                    nextIndex -= wayPoints.Count;
                }
                Vector3 position1 = wayPoints[nextIndex].position;
                Vector3 position2 = wayPoints[i].position;

                position1.y = position1.y + 1f;
                position2.y = position2.y + 1f;
                Gizmos.DrawLine(position2, position1);
                //Gizmos.DrawLine(wayPoints[i].position, wayPoints[nextIndex].position);
                //Gizmos.DrawSphere(wayPoints[i].position, 0.1f);
                Gizmos.DrawSphere(position2, 0.1f);
            }
        }
    }
}

