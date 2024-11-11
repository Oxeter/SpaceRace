namespace Assets.Scripts.SpaceRace.Projects
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using System.IO;
    using UnityEngine;

    using ModApi.Craft.Parts;
    using ModApi.Common.Extensions;
    using ModApi.Flight;
    using ModApi.Scenes.Events;
    using ModApi.Flight.Events;
    using Assets.Scripts.SpaceRace.Hardware;
    using ModApi.Ui.Inspector;
    using Assets.Scripts.SpaceRace.Collections;
    using Assets.Scripts.SpaceRace.Modifiers;
    using Assets.Scripts.SpaceRace.Formulas;
    using Assets.Scripts.Craft.Parts;
    using ModApi.Craft.Propulsion;
    using Assets.Scripts.State;

    public class VerboseCraftConfiguration
    {
        public Dictionary<SRStage, Dictionary<int,SRPartFamily>> Stages = new Dictionary<SRStage, Dictionary<int,SRPartFamily>>();
        public Dictionary<IPartScript, SRPart> Parts = new Dictionary<IPartScript, SRPart>();
        public List<HardwareOrder> Orders = new List<HardwareOrder>();
        public Dictionary<FuelType, double> Fuels = new Dictionary<FuelType, double>();
        public long UndevelopedPartPrice = 0;
        public long FuelPrice => (long)Fuels.Sum(f => f.Key.Density * f.Key.Price * f.Value);
        public List<CrewMember> Crew = new List<CrewMember>();
        public float MassWet = 0F;
        public float MassDry = 0F;
        public float Height = 0F;
        public float Width = 0F;
    }
}