namespace Assets.Scripts.SpaceRace.Hardware
{
    using System.Collections;
    using System.Collections.Generic;

    using UnityEngine;
    using System.Xml;
    using System.Xml.Serialization;
    using System.Xml.Linq;
    using System;
    using ModApi.Craft.Parts;
    using Assets.Scripts.Craft.Parts.Modifiers;
    using System.Linq;
    using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
    using Assets.Scripts.SpaceRace.Modifiers;
    using Assets.Scripts.SpaceRace.Collections;
    using Assets.Scripts.SpaceRace;


    public class HardwareDB
    {
        [XmlAttribute]
        public int NextId = 1;
        public List<SRPartFamily> Families = new List<SRPartFamily>();
        public List<SRPart> Parts = new List<SRPart>();
        public List<SRStage> Stages = new List<SRStage>();       
        public List<SRCraftConfig> CraftConfigs = new List<SRCraftConfig>();      
        public XElement PartsXml = new XElement("PartsXML");
    }
}
