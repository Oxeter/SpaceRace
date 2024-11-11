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
        using Assets.Scripts.Craft.Parts.Modifiers;
    using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;

    [Serializable]
    [DesignerPartModifier("SRAdjust")]
    [PartModifierTypeId("SpaceRace.SRAdjust")]
    public class SRAdjustData : PartModifierData<SRAdjustScript>
    {
        public override float MassDry => MassAdjustment();

        private float MassAdjustment()
        {
            ParachuteData chute = Part.GetModifier<ParachuteData>();
            if (chute != null)
            {
                return -0.9F * chute.MassDry;
            }

            DockingPortData dock = Part.GetModifier<DockingPortData>();
            if (dock != null)
            {
                return -0.8F * dock.MassDry;
            }

            if (Part.GetModifier<DetacherData>() != null)
            { 
                return -0.5F * Part.GetModifier<FuselageData>().MassDry;
            }


            return 0f;

        }
    }
}