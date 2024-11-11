#if UNITY_EDITOR
using ModApi.Craft.Parts.Editor;
using ModApi.Craft.Parts.Modifiers;
namespace Assets.Scripts.SpaceRace.Modifiers
{


    /// <summary>
    /// An editor only class used to associated part modifiers with game objects when defining parts.
    /// </summary>
    public sealed class SRFuelCellEditorScript : PartModifierEditorScript<SRFuelCellData>
    {
    }
}
#endif