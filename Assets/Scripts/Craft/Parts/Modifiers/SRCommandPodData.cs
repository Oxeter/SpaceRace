namespace Assets.Scripts.SpaceRace.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("SRCommandPod")]
    [PartModifierTypeId("SpaceRace.SRCommandPod")]
    public class SRCommandPodData : PartModifierData<SRCommandPodScript>
    {
    }
}