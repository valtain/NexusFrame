using UnityEngine;

namespace NexusFrame
{
    /// <summary>
    /// Normal UI 레이어. 역할 미확정 — 확장 진입점만 제공한다.
    /// </summary>
    public class NormalLayer : MonoBehaviour
    {
        [field: SerializeField] public Canvas Canvas { get; private set; }
    }
}
