using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Design;
using ModApi.Design.PartProperties;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Attributes;
using UnityEngine;
using Assets.Scripts.Craft.Parts.Modifiers;
using HarmonyLib;

namespace Assets.Scripts.SpaceRace.Modifiers
{
    [Serializable]
    [DesignerPartModifier("Fuel Cell")]
    [PartModifierTypeId("SpaceRace.SRFuelCell")]
    public class SRFuelCellData : PartModifierData<SRFuelCellScript>
    {
        [SerializeField]
        [DesignerPropertySlider(0.25f, 4f, 76, Label = "Size", Tooltip = "Defines the scale of the generator.")]
        private float _scale = 1f;

        [SerializeField]
        [DesignerPropertySlider(0f, 1f, 76, Label = "Efficiency", Tooltip = "Defines the efficiency of the fuel cell.  This will be zero until fuel cell technology is developed.", TechTreeIdForMaxValue = "Fuelcell.Efficiency")]
        private float _efficiency = 0f;

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            base.OnDesignerInitialization(d);
            d.OnPropertyChanged(() => _scale, delegate
            {
                d.Manager.RefreshUI();
                UpdateScale();
                Symmetry.SynchronizePartModifiers(base.Script.PartScript);
                base.Script.PartScript.CraftScript.SetStructureChanged();
            });
            d.OnPropertyChanged(() => _efficiency, delegate
            {
                d.Manager.RefreshUI();
                UpdateScale();
                Symmetry.SynchronizePartModifiers(base.Script.PartScript);
                base.Script.PartScript.CraftScript.SetStructureChanged();
            });
        }

        private void UpdateScale()
        {
            GeneratorData modifier = Part.GetModifier<GeneratorData>();
            Traverse.Create(modifier).Field("_efficiency").SetValue(_efficiency);
            modifier.Scale = _scale;
            modifier.Script.UpdateScale();
        }

    }
}