using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using NexusFrame;
using NexusFrame.Extensions;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬 전환 시 화면 전환 효과를 관리하는 싱글톤 UI 컴포넌트.
/// <para>
/// - <see cref="Begin"/>/<see cref="End"/> 호출 쌍을 카운터(<see cref="TransitionCount"/>)로 추적하여
///   중복 요청이 발생해도 화면이 올바르게 전환되도록 보장한다.
/// - <see cref="TransitionEffectType"/>에 따라 <see cref="ITransitionEffect"/> 구현체를 선택적으로 적용한다.
/// - End 처리 중 새로운 Begin이 들어오면 End 완료 후 자동으로 해당 Begin 효과를 실행한다.
/// </para>
/// </summary>
public class TransitionUi : MonoPreload<TransitionUi>
{
    [SerializeField]
    private Graphic _backgroundOverlay = default;
    [SerializeField]
    private GameObject _root = default;
    public int TransitionCount { get; private set; }
    static public bool IsTransitioning => HasInstance && (Instance.TransitionCount > 0 || Instance._isPendingForEnd);

    private ITransitionEffect _activeEffect = default;
    private Dictionary<TransitionEffectType, ITransitionEffect> _transitionMap = new();
    private bool _isPendingForEnd = false;
    private System.Threading.CancellationToken _lifetimeToken;

    protected override void Awake()
    {
        base.Awake();
        _lifetimeToken = this.GetCancellationTokenOnDestroy();
        _root.SetActive(false);
        __addEffect(new InstantTransitionEffect());
        __addEffect(new FadeTransitionEffect());
        void __addEffect(ITransitionEffect effect) => _transitionMap.Add(effect.Type, effect);
    }

    public async UniTask Begin(TransitionEffectType type)
    {
        ++TransitionCount;
        if (2 <= TransitionCount) // 중복된 경우는 Count 만 증가
        {
            return;
        }

        _root.SetActiveSafe(true);
        _activeEffect = GetTransitionEffect(type);
        if (_isPendingForEnd)
        {
            // End 효과로 Pending 중에는 End 효과 종료시 자동으로 처리
            // Effect 전환이 발생하므로 별개 처리가 필요할 수 있음.
            return;
        }
        await _activeEffect.Begin(_backgroundOverlay, _lifetimeToken);
    }

    public async UniTask End()
    {
        --TransitionCount;
    if (1 <= TransitionCount)
        {
            return;
        }

        _isPendingForEnd = true;
        var activeEffectBackup = _activeEffect;
        _activeEffect = null;

        await activeEffectBackup.End(_lifetimeToken);
        if (HasInstance == false)
        {
            return;
        }

        if (_activeEffect != null) // End 처리 중에 추가된 transition
        {
            _activeEffect.Begin(_backgroundOverlay, _lifetimeToken).Forget();
        }
        else
        {
            _root.SetActiveSafe(false);
        }
        _isPendingForEnd = false;
    }

    private ITransitionEffect GetTransitionEffect(TransitionEffectType type)
        => _transitionMap[type];

    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}

