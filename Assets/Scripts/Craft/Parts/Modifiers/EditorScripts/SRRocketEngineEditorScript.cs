#if UNITY_EDITOR
namespace Assets.Scripts.SpaceRace.Modifiers.EditorScripts
{
    using ModApi.Craft.Parts.Editor;
    using Assets.Scripts.SpaceRace.Modifiers;

    /// <summary>
    /// An editor only class used to associated part modifiers with game objects when defining parts.
    /// </summary>
    public sealed class SRRocketEngineEditorScript : PartModifierEditorScript<SRRocketEngineData>
    {
    }
}
#endif