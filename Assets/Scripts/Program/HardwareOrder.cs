namespace Assets.Scripts.SpaceRace.Hardware
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Serialization;
    using Assets.Scripts.SpaceRace.Formulas;
    using Assets.Scripts.SpaceRace.Collections;
    using UnityEngine;
    using ModApi.Ui.Inspector;
    using ModApi.Math;
    using Assets.Scripts.SpaceRace.Hardware;
    using Assets.Scripts.Ui.Inspector;
    using Assets.Scripts.SpaceRace.Modifiers;
    using System.Linq;
    using ModApi;

    public class HardwareOrder
    {
        [XmlIgnore]
        public int ProjectId;
        [XmlIgnore]
        public string ProjectName;
        [XmlAttribute]
        public int ContractorId;
        [XmlAttribute]
        public string ContractorName;
        [XmlAttribute]
        public double CurrentProduction = 0;
        [XmlAttribute]
        public double TotalProduction = 0;
        [XmlAttribute]
        public string Description = string.Empty;
        [XmlAttribute]
        public bool RefurbishedHardwareCollected = false;
        public HardwareOccurenceList HardwareRequired = new HardwareOccurenceList();
        [XmlIgnore]
        public bool Rush = false;
        [XmlAttribute]
        public bool RushRequested = false;
        [XmlAttribute]
        public long Price = 0;
        [XmlIgnore]
        public int Priority;
        public double Markup => Price / TotalProduction;
        [XmlIgnore]
        public double TimeToCompletion = double.PositiveInfinity;
        [XmlIgnore]
        public TextModel Model;
        public bool IsOpen() 
        {
            return  CurrentProduction < TotalProduction;
        }
        public override string ToString()
        {
            return string.Format("{0}: {1} \n {2}", ContractorName, Units.GetMoneyString(Price), Description);
        }
        public string TTCString()
        {
            return Units.GetRelativeTimeString(TimeToCompletion);
        }
        public void UpdateTextModel()
        {
            if (Model != null)
            {
                Model.Label = Rush? ProjectName+" (Rush)":ProjectName;
                Model.Value = TTCString();
            }
        }
        public HardwareOrder NewOrder()
        {
            return new HardwareOrder()
            {
                ContractorId = ContractorId,
                ContractorName = ContractorName,
                TotalProduction = TotalProduction,
                Price = Price,
                HardwareRequired = new HardwareOccurenceList(HardwareRequired),
                Description = Description
            };
        }
        public HardwareOrder NewFreeOrder()
        {
            return new HardwareOrder()
            {
                ContractorId = ContractorId,
                ContractorName = ContractorName,
                TotalProduction = TotalProduction,
                Price = 0,
                HardwareRequired = new HardwareOccurenceList(),
                Description = Description
            };
        }

    }

}