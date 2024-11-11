using UI.Xml;
namespace Assets.Scripts.SpaceRace.History
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Assets.Scripts.SpaceRace.Projects;
    using Assets.Scripts.SpaceRace.Ui;
    using System.Xml.Serialization;
    using System.IO;
    using System;
    using System.Linq;
   
    using System.Xml.Linq;
    using ModApi.Common.Extensions;
    using System.Drawing;
    using System.Xml;
    using System.Diagnostics.Contracts;
    using UnityEngine.UIElements;
    using Assets.Scripts.Menu.ListView;
    using ModApi.Ui;
    using ModApi.State;
    using Unity.Mathematics;
    using Assets.Scripts.Career.Research;
    using ModApi.Craft;
    using ModApi.Craft.Parts.Modifiers;
    using ModApi.Craft.Parts;
    using Assets.Scripts.Flight;

    public class SpaceRaceContractsScript
    {
        private IProgramManager _pm;
        public SpaceRaceContractsData Data {get; private set;}
        
        public SpaceRaceContractsScript(IProgramManager pm, SpaceRaceContractsData data)
        {
            _pm = pm;
            Data = data;
        }

        public void OnContractCompleted(Career.Contracts.Contract contract)
        {
            switch (contract.Id)
            {
                case "clear-tracking-id":
                    FlightSceneScript.Instance.CraftNode.ContractTrackingId = null;
                    return;
                case "redstone":
                    Data.SRBMConfig = _pm.Data.LastLaunchConfig;
                    goto case "clear-tracking-id";
                case "redstone-repeat":
                    goto case "clear-tracking-id";
                case "mrbm":
                    Data.MRBMConfig = _pm.Data.LastLaunchConfig;
                    goto case "clear-tracking-id";
                case "mrbm-repeat":
                    goto case "clear-tracking-id";
                case "icbm":
                    Data.ICBMConfig = _pm.Data.LastLaunchConfig;
                    goto case "clear-tracking-id";
                case "icbm-repeat":
                    goto case "clear-tracking-id";
                case "icbm2":
                    Data.ICBM2Config = _pm.Data.LastLaunchConfig;
                    goto case "clear-tracking-id";
                case "icbm-2-repeat":
                    goto case "clear-tracking-id";
            }
        }

        public void OnFlightStart(ICraftNode craft)
        {
            PartData part = craft.CraftScript.Data.Assembly.Parts.FirstOrDefault(p => p.Payload?.PayloadId == "srbm");
            if (part != null)
            {
                SetSRBMPayload(craft);
                return;
            }
            part = craft.CraftScript.Data.Assembly.Parts.FirstOrDefault(p => p.Payload?.PayloadId == "mrbm");
            if (part != null)
            {
                SetMRBMPayload(craft);
                return;
            }
            part = craft.CraftScript.Data.Assembly.Parts.FirstOrDefault(p => p.Payload?.PayloadId == "icbm");
            if (part != null)
            {
                SetICBMPayload(craft);
                return;
            }
            part = craft.CraftScript.Data.Assembly.Parts.FirstOrDefault(p => p.Payload?.PayloadId == "icbm2");
            if (part != null)
            {
                SetAltICBMPayload(craft);
                return;
            }
            part = craft.CraftScript.Data.Assembly.Parts.FirstOrDefault(p => p.Payload?.PayloadId == "Docking-Target");
            if (part != null)
            {
               SetDockingTarget(craft); 
            }
        }
        private void SetSRBMPayload(ICraftNode craft)
        {
            Assets.Scripts.Career.Contracts.Contract contract = _pm.Career.Contracts.Active.FirstOrDefault(con => con.Id == "redstone-repeat");
            if (contract != null)
            {
                double sim = _pm.LastCraftConfig.Similarity(_pm.HM.GetCraftConfig(c => c.Id == Data.SRBMConfig));
                Debug.Log($"sim ={sim} { _pm.HM.GetCraftConfig(c => c.Id == Data.SRBMConfig)}");
                if (sim >= 0.65)
                {
                    craft.ContractTrackingId="SRBM";
                    Debug.Log("setting craft tracking ID to srbm.");
                    //contract.ResetStatus();
                    return;
                }
                else Debug.Log("srbm payload found. Not similar enough to srbm craft");
            }
        }

        private void SetMRBMPayload(ICraftNode craft)
        {
            Assets.Scripts.Career.Contracts.Contract contract = _pm.Career.Contracts.Active.FirstOrDefault(con => con.Id == "mrbm-repeat");
            if (contract != null)
            {
                double sim = _pm.LastCraftConfig.Similarity(_pm.HM.GetCraftConfig(c => c.Id == Data.MRBMConfig));
                Debug.Log($"sim ={sim} { _pm.HM.GetCraftConfig(c => c.Id == Data.MRBMConfig)}");
                if (sim >= 0.65)
                {
                    craft.ContractTrackingId="MRBM";
                    Debug.Log("setting craft tracking ID to mrbm.");
                    //contract.ResetStatus();
                    return;
                }
                else Debug.Log("mrbm payload found. Not similar enough to mrbm craft");
            }
        }


        private void SetICBMPayload(ICraftNode craft)
        {
            Debug.Log(_pm.LastCraftConfig.ToString());
            Assets.Scripts.Career.Contracts.Contract contract = _pm.Career.Contracts.Active.FirstOrDefault(con => con.Id == "icbm-repeat");
            if (contract != null)
            {
                double sim = _pm.LastCraftConfig.Similarity(_pm.HM.GetCraftConfig(c => c.Id == Data.ICBMConfig));
                Debug.Log($"sim ={sim} {_pm.HM.GetCraftConfig(c => c.Id == Data.ICBMConfig)}");
                if (sim >= 0.65)
                {
                    craft.ContractTrackingId="ICBM1";
                    Debug.Log("setting tracking id to icbm1");
                    //contract.ResetStatus();
                    return;
                }
                else Debug.Log("icbm payloads found. Not similar enough to icbm-1 craft");
            }
        }

        private void SetAltICBMPayload(ICraftNode craft)
        {
            Debug.Log(_pm.LastCraftConfig.ToString());
            Assets.Scripts.Career.Contracts.Contract contract = _pm.Career.Contracts.Active.FirstOrDefault(con => con.Id == "icbm2");
            if (contract != null)
            {
                double sim = _pm.LastCraftConfig.Similarity(_pm.HM.GetCraftConfig(c => c.Id == Data.ICBMConfig));
                if (sim <= 0.1)
                {
                    craft.ContractTrackingId="ICBM2";
                    Debug.Log("setting tracking id to icbm2");
                    return;
                }
                else Debug.Log("icbm payloads found. Too similar to icbm-1 craft");
                return;
            }
            contract = _pm.Career.Contracts.Active.FirstOrDefault(con => con.Id == "icbm2-repeat");
            if (contract != null)
            {
                double sim = _pm.LastCraftConfig.Similarity(_pm.HM.GetCraftConfig(c => c.Id == Data.ICBM2Config));
                Debug.Log($"sim ={sim} {_pm.HM.GetCraftConfig(c => c.Id == Data.ICBM2Config)}");
                if (sim >= 0.65)
                {
                    craft.ContractTrackingId="ICBM2";
                    Debug.Log("setting tracking id to icbm2");
                    return;
                }
                else Debug.Log("icbm payloads found. Not similar enough to icbm-2 craft");
            }

        }
        private void SetDockingTarget(ICraftNode craft)
        {
            Debug.Log("Part with rendevous payload found");
            craft.ContractTrackingId = "Docking-Target";
        }

    }

    public class SpaceRaceContractsData
    {
        [XmlAttribute]
        public int SRBMConfig = 0;
        [XmlAttribute]
        public int MRBMConfig = 0;
        [XmlAttribute]
        public int ICBMConfig = 0;
        [XmlAttribute]
        public int NumICBMIntegrated = 0;
        [XmlAttribute]
        public double LastICBMIntegration = 0;
        [XmlAttribute]
        public int ICBM2Config = 0;
        [XmlAttribute]
        public int NumICBM2Integrated = 0;
        [XmlAttribute]
        public double LastICBM2Integration = 0;
    }
}
