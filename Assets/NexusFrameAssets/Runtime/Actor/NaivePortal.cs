using UnityEngine;

namespace NexusFrame
{
    [RequireComponent(typeof(Collider))]
    public class NaivePortal : MonoBehaviour
    {
        [SerializeField] private PlaySessionType _sessionType = PlaySessionType.None;
        [SerializeField] private PlaySessionSwitch _sessionSwitch = PlaySessionSwitch.Replace;
        [SerializeField] private GameStageDesc _stageDesc = default;
        [SerializeField] private TransitionEffectType _transitionType = TransitionEffectType.Fade;

        private bool _isPortalInUse = false;
        private void OnTriggerEnter(Collider other)
        {
            if (_isPortalInUse)
            {
                return;
            }

            //[TODO] PC 와의 충돌일 때만 동작하도록 추가 필요
            Debug.Assert(_sessionType != PlaySessionType.None, "Invalid session type in portal.");
            Debug.Log($"[NaivePortal] Triggered by {other.name}, launching {_sessionType} to {_stageDesc.AssetPath} with {_transitionType}.");

            GamePlaySystem.Instance.LaunchSession(
                _sessionType,
                _sessionSwitch,
                _stageDesc,
                _transitionType);

            _isPortalInUse = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (_isPortalInUse)
            {
                _isPortalInUse = false;
            }
        }
    }
}
