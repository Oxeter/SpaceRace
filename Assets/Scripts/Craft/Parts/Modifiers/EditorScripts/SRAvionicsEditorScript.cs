#if UNITY_EDITOR
namespace Assets.Scripts.SpaceRace.Modifiers.EditorScripts
{
    using ModApi.Craft.Parts.Editor;
    using ModApi.Craft.Parts.Modifiers;

    /// <summary>
    /// An editor only class used to associated part modifiers with game objects when defining parts.
    /// </summary>
    public sealed class SRAvionicsEditorScript : PartModifierEditorScript<SRAvionicsData>
    {
    }
}
#endif