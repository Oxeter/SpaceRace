namespace Assets.Scripts.SpaceRace.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Assets.Scripts.Craft.Parts.Modifiers;
    using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Propulsion;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class SRFuelTankScript : PartModifierScript<SRFuelTankData>
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();
            Data.Part.GetModifier<FuselageData>().Script.MeshesUpdated += OnMeshChanged;
            Data.Part.GetModifier<FuelTankData>().FuelTypeChanged += OnFuelTypeChanged;
        }

        public void OnFuelTypeChanged(FuelTankData fuelTank)
        {
            Data.ContructionByFuelType(fuelTank);
        }
        

        void OnMeshChanged(FuselageScript fuselageScript)
        {
            Data.Diameter = Mathf.Round(10f*Math.Max(fuselageScript.Data.TopScale.x, fuselageScript.Data.BottomScale.x))/10f;
        }
    }
}