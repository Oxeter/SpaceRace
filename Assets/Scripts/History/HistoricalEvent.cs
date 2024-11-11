using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Security.Cryptography;
using Assets.Scripts.SpaceRace.Projects;
using Assets.Scripts.SpaceRace.Hardware;
using ModApi.Math;
using System.Linq;
using Assets.Scripts.Flight;
using Assets.Scripts.Career.Research;
using Assets.Scripts.SpaceRace.Formulas;
namespace Assets.Scripts.SpaceRace.History
{
    public class HistoricalEvent
    {
        [XmlIgnore]
        public HistoryManager HistoryManager;
        
        /// <summary>
        /// Whether the event has occured.
        /// </summary>
        [XmlAttribute]
        public bool Occured = false;
        
        /// <summary>
        /// Whether the event will open and appear in the history list.
        /// </summary>
        [XmlAttribute]
        public bool Visible = true;
        
        /// <summary>
        /// Unqiue identifier for the event.
        /// </summary>
        [XmlAttribute]
        public string Id;
        
        /// <summary>
        /// The player-facing name of the event.
        /// </summary>
        [XmlAttribute]
        public string Name;
        
        /// <summary>
        /// The text that appears in the listview details for the event. Formatted for textmeshpro?
        /// </summary>
        public string Description;
        /// <summary>
        /// Can the event fire more than once?  Repeats will not generate and show a past event, but will otherwise perform their action.
        /// </summary>
        [XmlAttribute]
        public bool Repeatable = false;
        
        /// <summary>
        /// Resouce path to the image
        /// </summary>
        [XmlAttribute]
        public string ImagePath;
        
        /// <summary>
        /// Event fires when this contract is complete.
        /// </summary>
        [XmlAttribute]
        public string CompletedContractId;
        
        /// <summary>
        /// Event fires when this contract is accepted.
        /// </summary>
        [XmlAttribute]
        public string AcceptedContractId;
        
        /// <summary>
        /// Event fires at this date.
        /// </summary>
        public DateTime FixedOccurenceDate;
        /// <summary>
        /// Event fires a fixed time after the event with this Id
        /// </summary>
        [XmlAttribute]
        public string FollowsEvent;
        /// <summary>
        /// Delay in seconds between the followed event and this one
        /// </summary>
        [XmlAttribute]
        public double FollowDelay = 0.0;
        /// <summary>
        /// Event will fire with no effect if this event fires first
        /// </summary>
        [XmlAttribute]
        public string BlockedBy;
        [XmlAttribute]
        public string Data = string.Empty;
        /// <summary>
        /// Method to invoke when this event occurs.
        /// </summary>
        [XmlIgnore]
        public Action OnOccurence;
        [XmlIgnore]
        public DateTime OccurenceDate => FollowsEvent != null && HistoryManager.PastEvents.Any(ev => ev.Id == FollowsEvent)? HistoryManager.PastEvents.First(ev => ev.Id == FollowsEvent).FixedOccurenceDate.AddSeconds(FollowDelay) : FixedOccurenceDate;
 
        public HistoricalEvent(){}

        public virtual HistoricalEvent PastEvent(bool visible, DateTime date, string description= null)
        {
            return new HistoricalEvent
            {
                Occured = true, 
                Visible = visible,
                Id = Id,
                Name = Name,
                ImagePath = ImagePath,
                FixedOccurenceDate = date,
                Description = description ?? Description,
                Data = Data,
            };
        }
    }

    public class StaticHistoricalEvent : HistoricalEvent
    {
        [XmlAttribute]
        public List<string> EndFundingIds;
        [XmlAttribute]
        public string FundingName;
        [XmlAttribute]
        public double FundingEfficiency;
        [XmlAttribute]
        public List<string> FailsContractIds;
        [XmlAttribute]
        public string CompletesContractId;
        [XmlAttribute]
        public string CompletesTechId;
        [XmlAttribute]
        public int ManufactureQuantity =0;
        [XmlAttribute]
        public double ManufactureDaysPer = 1.0;

        public StaticHistoricalEvent()
        {
            OnOccurence = () => 
            {
                if (CompletesContractId != null)
                {
                    Assets.Scripts.Career.Contracts.Contract contract = HistoryManager.ProgramManager.Career.Contracts.Active.FirstOrDefault(con => con.Id == CompletesContractId);
                    if (contract != null)
                    {
                        contract.Status = Career.Contracts.ContractStatus.Complete;
                        HistoryManager.ProgramManager.Career.Contracts.CloseContract(contract, Game.Instance.GameState.LoadFlightStateData());
                        Debug.Log(contract.Name +" completed by event");
                    }
                }
                if (FailsContractIds != null)
                {
                    foreach(Career.Contracts.Contract contract in HistoryManager.ProgramManager.Career.Contracts.Active.Where(con => FailsContractIds.Contains(con.Id)))
                    {
                        contract.Status = Career.Contracts.ContractStatus.Rejected;
                        HistoryManager.ProgramManager.Career.Contracts.CloseContract(contract, Game.Instance.GameState.LoadFlightStateData());
                        Debug.Log(contract.Name +" rejected by event");
                    }
                }
                if (CompletesTechId != null)
                {
                    TechNode node = HistoryManager.ProgramManager.Career.TechTree.GetNode(CompletesTechId);
                    Data = $"techNode:{CompletesTechId}";
                    if (node != null)
                    {   
                        node.Researched = true;
                    }

                }
                if (FundingName != null)
                {
                    FundingScript script = HistoryManager.ProgramManager.Funding.Values.FirstOrDefault(fun => fun.Data.Name == FundingName);
                    if (script != null && FundingEfficiency >= 0.0)
                    {
                        script.Data.Efficiency *= FundingEfficiency;
                    }
                    else Debug.Log($"Event {Id} either references unknown funding {FundingName} or has missing or negative efficiency factor");
                }
                if (ManufactureQuantity != 0)
                {
                    SRCraftConfig config = HistoryManager.ProgramManager.LastCraftConfig;
                    HistoryManager.ProgramManager.NewExternalCraftOrder(config,"External - "+config.Name, ManufactureQuantity, ManufactureDaysPer);
                    Description = $"Even though testing has not concluded, the Defense Department has approved an order for {ManufactureQuantity} of the new {config.Name}.  This will improve efficiency of integration and spur contractors to increase production capacity for the required hardware.  Every {SRFormulas.ExternalTestInterval}{GetOrdinalSuffix(SRFormulas.ExternalTestInterval)} missle will be test flown and the data will improve reliability of components.  More orders will be placed with the completion of additional test contracts.";
                    Name = $"{config.Name} Enters Mass Production";
                }
                
            };
        }
        private static string GetOrdinalSuffix(int num)
        {
            string number = num.ToString();
            if (number.EndsWith("11")) return "th";
            if (number.EndsWith("12")) return "th";
            if (number.EndsWith("13")) return "th";
            if (number.EndsWith("1")) return "st";
            if (number.EndsWith("2")) return "nd";
            if (number.EndsWith("3")) return "rd";
            return "th";
        }

    }



    public class YearlyReport : HistoricalEvent
    {
        [XmlAttribute]
        public int Year = 1949;
        [XmlAttribute]
        public long CashOnHand = 0;
        [XmlAttribute]
        public int ContractsCompletedCount = 0;
        [XmlAttribute]
        public int Flights = 0;
        public YearlyReport(){}
        public YearlyReport(int year)
        {
            Id = $"YearlyReport{year}";
            Name =$"{year} Yearly Report";
            FixedOccurenceDate = new DateTime(year + 1, 1, 1);
            Year = year;
            OnOccurence = UpdateCapacities;
        }
        public void UpdateCapacities()
        {
            HistoryManager.AdjustTechsByYear(Year +1);
            Description = HistoryManager.ProgramManager.UpdateCapacities();
            Debug.Log("Description is "+Description);
        }

        public override HistoricalEvent PastEvent(bool visible, DateTime date, string description= null)
        {
            CashOnHand = HistoryManager.ProgramManager.Career.Money;
            Flights = HistoryManager.ProgramManager.Career.Contracts.Flight.NumLaunches;
            ContractsCompletedCount = HistoryManager.ProgramManager.Career.Contracts.Completed.Count;
            string lastyeardata = HistoryManager.PastEvents.Find(ev => ev.Id == $"YearlyReport{Year -1}")?.Data ?? "0:0:0";
            return new HistoricalEvent()
            {
                Occured = true, 
                Id = Id,
                Visible = true,
                Name = Name,
                ImagePath = ImagePath,
                FixedOccurenceDate = date,
                Description = $"Money = {Units.GetMoneyString(CashOnHand)}\n"+
                $"Change from last year = {Units.GetMoneyString(CashOnHand- long.Parse(lastyeardata.Split(":")[0]))}\n"+
                $"Flights this year = {Flights - int.Parse(lastyeardata.Split(":")[1])}\n"+
                $"Contracts completed = {ContractsCompletedCount - int.Parse(lastyeardata.Split(":")[2])}\n"+
                Description,
                Data = $"{CashOnHand}:{Flights}:{ContractsCompletedCount}"
            };
        }
    }
    
}