namespace Assets.Scripts.SpaceRace.Collections
{
    using System.Collections.Generic;
    using Assets.Scripts.SpaceRace.Hardware;
    using ModApi.Craft.Parts;
    using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
    using System;
    using System.Drawing;
    using ModApi.Math;

    public class HardwareFamilyPair
    {
        public HardwareFamilyPair()
        {
            PartId = 0;
            FamilyId = 0;
        }
        public HardwareFamilyPair(int partId, int familyId)
        {
            PartId = partId;
            FamilyId = familyId;
        }
        public int PartId;
        public int FamilyId;
    }
    public class TypeTotal
    {
        public string Type;
        public int Total = 0;
    } 

    public class EfficiencyFactor
    {
        public string Text;
        public float Penalty;
        public string ToolTip = string.Empty;
        public EfficiencyFactor(){}
        public EfficiencyFactor(string text, float penalty)
        {
            Text = text;
            Penalty = penalty;
        }
        public EfficiencyFactor(string text, float penalty, string tooltip)
        {
            Text = text;
            Penalty = penalty;
            ToolTip = tooltip;
        }
        public override string ToString()
        {
            return String.Format("{0}: {1:P0}", Text, Penalty);
        }
        public string PenaltyString()
        {
            return Units.GetPercentageString(Penalty);
        }
    }
}