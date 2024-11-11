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

    public class HistoryManager
    {
        private IProgramManager _pm;
        private readonly DateTime _careerStart = new DateTime(1950,1,1,8,0,0);


        public IProgramManager ProgramManager {get {return _pm;}}
        public HistoryManager(IProgramManager pm, List<HistoricalEvent> pastevents)
        {
            _pm = pm;
            PastEvents = pastevents;
            foreach (HistoricalEvent pevent in PastEvents.Where(p => p.Data.Split(':')[0]=="techNode"))
            {
                pm.Career.TechTree.GetNode(pevent.Data.Split(':')[1]).Researched = true;
                Debug.Log($"Technode {pevent.Data.Split(':')[1]} Unlocked by past event");
            }
            //Game.Instance.GameState.Career.Contracts.ContractCompleted += OnContractCompleted;
            RegisterEvents(Enumerable.Range(1950, 100).Select(i => new YearlyReport(i){HistoryManager=this}));
        }
        public List<HistoricalEvent> PastEvents {get; private set;}
        public List<HistoricalEvent> Events {get; private set;} = new List<HistoricalEvent>();
        public void RegisterEvent(HistoricalEvent hevent)
        { 
            if (PastEvents.Any(even => even.Id == hevent.Id))
            {
                hevent.Occured = true;
            }
            if (hevent.Repeatable || !hevent.Occured)
            {
                hevent.HistoryManager= this;
                Events.Add(hevent);
            }
        }
        public void RegisterEvents(IEnumerable<HistoricalEvent> hevents)
        {
            //Debug.Log($"Registering {hevents.Count()} events");
            foreach (HistoricalEvent hevent in hevents)
            {
                RegisterEvent(hevent);
            }
        }
        public void Fire(string eventId)
        {
            HistoricalEvent hevent = Events.Find(ev => ev.Id == eventId);
            if (hevent == null) throw new ArgumentException("Historical event with id "+eventId+" not in database");
            Fire(hevent);
        }
        public void Fire(HistoricalEvent hevent)
        {
            Debug.Log("Trying to fire event " + hevent.Id);
            if (hevent.Occured && !hevent.Repeatable) 
            {
                throw new ArgumentException("Trying to fire nonrepeatable event that already occured");
            }
            bool blocked = hevent.BlockedBy != null && PastEvents.Any(ev => ev.Occured && ev.Id == hevent.BlockedBy);
            if (!blocked)
            {
                hevent.OnOccurence();
            }
            if (!hevent.Occured)
            {
                double seconds = Game.InFlightScene? Game.Instance.FlightScene.FlightState.Time : Game.Instance.GameState.GetCurrentTime();
                HistoricalEvent pevent = hevent.PastEvent(hevent.Visible && !blocked, _careerStart.AddSeconds(seconds));
                pevent.Occured = !blocked;
                PastEvents.Add(pevent);
                if (pevent.Visible)
                {
                    Game.Instance.FlightScene.TimeManager.RequestPauseChange(true,false);
                    _pm.SetWarp(false);
                    OpenHistoryListView(pevent);
                }
            }
            hevent.Occured = true;
        }

        public void OnContractCompleted(Assets.Scripts.Career.Contracts.Contract contract)
        {
            if (Game.Instance.GameState.Type == GameStateType.Simulation) return;
            Debug.Log($"HistoryManager OnContractCompleted {contract.Id}");
            foreach (HistoricalEvent hevent in Events.Where(ev => ev.CompletedContractId != null))
            {
                Debug.Log($"{hevent.Id} Occured = {hevent.Occured} Repeatable ={hevent.Repeatable}");
            }
            foreach (HistoricalEvent hevent in Events.Where(ev => ev.CompletedContractId == contract.Id && (!ev.Occured || ev.Repeatable)))
            {
                Fire(hevent);
            }
        }

        public void FireEventsByDate(double time)
        {
            if (Game.Instance.GameState.Type == GameStateType.Simulation) return;
            DateTime date = _careerStart.AddSeconds(time);
            //Debug.Log("Checking events on " + date.ToLongDateString());
            foreach (HistoricalEvent hevent in Events.Where(ev => !ev.Occured && ev.OccurenceDate <= date && !ev.Repeatable))
            {
                Fire(hevent);
            }
        }

        public void FireEventsByContractsAccepted(Career.Contracts.Contract contract)
        {
            //Debug.Log("Firing events from contract id " + contract.Id);
            foreach (HistoricalEvent hevent in Events.Where(ev => ev.AcceptedContractId == contract.Id &&  (!ev.Occured || ev.Repeatable)))
            {
                Fire(hevent);
            }
        }

        public void OpenHistoryListView(HistoricalEvent hevent)
        {
            HistoryListViewModel model = new HistoryListViewModel(PastEvents.Where(ev=> ev.Visible).ToList(), hevent);
            Game.Instance.UserInterface.CreateListView(model);
        }
        public void AdjustTechCosts(double time)
        {
            DateTime dateTime = _careerStart.AddSeconds(time);
            int year = dateTime.Year;
            AdjustTechsByYear(year);
        }
        public void AdjustTechsByYear(int year)
        {
            foreach (TechNode node in ProgramManager.Career.TechTree.AllNodes.Where(node => !node.Researched && node.Id.Length >= 4))
            {
                if (int.TryParse(node.Id[..4], out int techyear))
                {
                    Debug.Log($"Adjusting tech {node.Id}");
                    node.Cost = techyear - year;
                    if (node.Cost <= 0)
                    {
                        node.Researched = true;
                    }
                }
            }
        }
    }
}
