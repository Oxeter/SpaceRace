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
    using SpaceRace.Collections;
    using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;

    public interface ISRPartFamily{}
    public class SRPartFamily : ISRPartFamily
    {
        [XmlAttribute]
        public int Id;
        [XmlAttribute]
        public string PrimaryMod;
        [XmlAttribute]
        public int Integrations = 0;
        [XmlAttribute]
        public string Name;
        [XmlAttribute]
        public bool AnyDeveloped = false;
        [XmlAttribute]
        public double StageProduction = 0.0;
        [XmlAttribute]
        public string DesignerCategory;
        public TechList Technologies = new TechList();
        public XElement Data = new XElement("Part");
    }

}