using Unity.Cinemachine;
using UnityEngine;

[System.Serializable]
public class CameraRelativeInputFrameResolver
{
    // --- 설정 ---
    [SerializeField] private float _blendTime = 2f;
    public float BlendTime => _blendTime;

    // --- 상수 ---
    // 모든 인스턴스가 동일한 값 → static readonly
    private static readonly Quaternion UpsideDown = Quaternion.AngleAxis(180f, Vector3.right);
    private const float AxisValidThreshold = 0.001f;

    // --- 캐싱 ---
    private float _timeInHemisphere;
    private bool _inTopHemisphere = true;

    /// <summary>
    /// 카메라 frame에서 플레이어 입력 좌표계를 계산하고
    /// 반구 전환 구간을 블렌딩해서 반환
    /// </summary>
    /// <param name="frame">카메라의 현재 회전</param>
    /// <param name="playerUp">플레이어의 업벡터</param>
    /// <param name="inputDirectionChanged">입력 방향 변경 여부</param>
    /// <param name="deltaTime">프레임 경과 시간</param>
    /// <returns>입력 좌표계로 사용할 쿼터니언</returns>
    public Quaternion Update(Quaternion frame, Vector3 playerUp, bool inputDirectionChanged, float deltaTime)
    {
        var up   = frame * Vector3.up;
        var axis = Vector3.Cross(up, playerUp);

        // 플레이어가 frame 기준으로 기울어지지 않은 경우 early-out
        if (axis.sqrMagnitude < AxisValidThreshold && Vector3.Dot(up, playerUp) >= 0f)
            return frame;

        var frameTop    = ComputeFrameTop(frame, playerUp, up, axis);
        var frameBottom = ComputeFrameBottom(frame, playerUp, up, axis);

        UpdateHemisphere(up, playerUp, inputDirectionChanged, deltaTime);

        return GetBlendedResult(frameTop, frameBottom);
    }

    public void Reset()
    {
        _timeInHemisphere = 0f;
        _inTopHemisphere  = true;
    }

    private Quaternion ComputeFrameTop(Quaternion frame, Vector3 playerUp, Vector3 up, Vector3 axis)
    {
        // top hemisphere용: cam up → player up 방향으로 틸트 보정
        var angle = UnityVectorExtensions.SignedAngle(up, playerUp, axis);
        return Quaternion.AngleAxis(angle, axis) * frame;
    }

    private Quaternion ComputeFrameBottom(Quaternion frame, Vector3 playerUp, Vector3 up, Vector3 axis)
    {
        // bottom hemisphere용: upsidedown 기준으로 반대편 보정
        var frameBottom = frame * UpsideDown;
        var axisBottom  = Vector3.Cross(frameBottom * Vector3.up, playerUp);

        if (axisBottom.sqrMagnitude <= AxisValidThreshold)
            return frameBottom;

        var angle = UnityVectorExtensions.SignedAngle(up, playerUp, axis);
        return Quaternion.AngleAxis(180f - angle, axisBottom) * frameBottom;
    }

    private void UpdateHemisphere(Vector3 up, Vector3 playerUp, bool inputDirectionChanged, float deltaTime)
    {
        _timeInHemisphere += deltaTime;

        bool inTopHemisphere = Vector3.Dot(up, playerUp) >= 0f;
        if (inTopHemisphere != _inTopHemisphere)
        {
            _inTopHemisphere  = inTopHemisphere;
            // 전환 직후: 남은 블렌딩 시간을 이어받아 역방향으로 블렌딩
            _timeInHemisphere = Mathf.Max(0f, BlendTime - _timeInHemisphere);
        }

        // 입력 방향이 바뀌면 타이머 강제 만료 → 즉시 현재 반구 frame 사용
        if (inputDirectionChanged)
            _timeInHemisphere = BlendTime;
    }

    private Quaternion GetBlendedResult(Quaternion frameTop, Quaternion frameBottom)
    {
        if (_timeInHemisphere >= BlendTime)
            return _inTopHemisphere ? frameTop : frameBottom;

        float t = _timeInHemisphere / BlendTime;

        return _inTopHemisphere
            ? Quaternion.Slerp(frameBottom, frameTop,    t)
            : Quaternion.Slerp(frameTop,    frameBottom, t);
    }
}