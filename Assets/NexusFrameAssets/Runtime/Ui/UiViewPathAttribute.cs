namespace NexusFrame
{
    /// <summary>
    /// <see cref="MainViewLayer.ShowView{T}"/> 가 프리팹을 로드할 때 사용하는 Resources 경로를 지정한다.
    /// <para>
    /// <see cref="IUiView"/> 를 구현하는 모든 View 클래스에 선언해야 한다.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [UiViewPath("Ui/ExplorationHud")]
    /// public class ExplorationHud : UiViewBase { ... }
    /// </code>
    /// </example>
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false)]
    public sealed class UiViewPathAttribute : System.Attribute
    {
        /// <summary>Resources 폴더 기준 상대 경로.</summary>
        public string Path { get; }

        public UiViewPathAttribute(string path) => Path = path;
    }
}
