#if UNITY_EDITOR
namespace Assets.Scripts.SpaceRace.Modifiers.EditorScripts
{
    using ModApi.Craft.Parts.Editor;
    using ModApi.Craft.Parts.Modifiers;
    using Assets.Scripts.Craft.Parts.Modifiers;
    using Assets.Scripts.Craft.Parts.Modifiers.EditorScripts;

    /// <summary>
    /// An editor only class used to associated part modifiers with game objects when defining parts.
    /// </summary>
    public sealed class SRAdjustEditorScript : PartModifierEditorScript<SRAdjustData>
    {
    }
}
#endif