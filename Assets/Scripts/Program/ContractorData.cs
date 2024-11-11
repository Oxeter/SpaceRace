namespace Assets.Scripts.SpaceRace.Projects
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Serialization;
    using Assets.Scripts.SpaceRace.Formulas;
    using Assets.Scripts.SpaceRace.Collections;
    public class ContractorData
    {
        [XmlAttribute]
        public int Id = 0;
        [XmlAttribute]
        public string Name;
        [XmlAttribute]
        public bool Visible = false;
        [XmlAttribute]
        public bool Temporary = false;
        [XmlAttribute]
        public bool Rush = false;
        [XmlAttribute]
        public List<string> HardwareTypes = new List<string>();
        public TechList DevelopedTechs = new TechList();
        [XmlAttribute]
        public bool Active = false;
        [XmlAttribute]
        public double Inventory = 0.0;
        [XmlAttribute]
        public double AverageRecentInventory = 0.0;
        [XmlAttribute]
        public double AverageRecentRush = 0.0;
        /// <summary>
        /// Production per day
        /// </summary>
        [XmlAttribute]
        public double BaseProductionRate = 0.0;
  

        [XmlAttribute]
        public string Sprite = null;
        [XmlIgnore]
        public double InventoryLimit => Formulas.SRFormulas.InventoryLimit(BaseProductionRate);
        public HardwareOccurenceList RecoveredHardware = new HardwareOccurenceList();
        public HardwareOccurenceList RefurbishedHardware = new HardwareOccurenceList();
        [XmlElement]
        public ContractorAttitude Attitude = new ContractorAttitude
        {
            Patience = UnityEngine.Random.value,
            Competitiveness = UnityEngine.Random.value,
            Competence = UnityEngine.Random.value,
            Optimism = UnityEngine.Random.value,
            Size = UnityEngine.Random.value
        };
    }

    public class ContractorAttitude
    {
        [XmlAttribute]
        public float Patience;
        [XmlAttribute]
        public float Competitiveness;
        [XmlAttribute]
        public float Competence;
        [XmlAttribute]
        public float Optimism;
        [XmlAttribute]
        public float Size;
    }
}