using UnityEngine;

namespace NexusFrame.Extensions
{
    /// <summary>
    /// <see cref="GameObject"/> 확장 메서드 모음.
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// 현재 활성 상태와 다를 때만 <see cref="GameObject.SetActive"/>를 호출합니다.
        /// </summary>
        /// <param name="gameObject">대상 오브젝트.</param>
        /// <param name="value">설정할 활성 상태.</param>
        public static void SetActiveSafe(this GameObject gameObject, bool value)
        {
            if (gameObject.activeSelf == value)
            {
                return;
            }
            gameObject.SetActive(value);
        }
    }
}
