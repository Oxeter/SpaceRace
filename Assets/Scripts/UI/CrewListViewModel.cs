namespace Assets.Scripts.SpaceRace.Ui
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Assets.Scripts.State;
    using ModApi.Craft;
    using ModApi.Math;
    using ModApi.Services.Purchasing;
    using ModApi.State;
    using ModApi.Ui;
    using UnityEngine;
    using Assets.Scripts.Menu.ListView;
    using Assets.Scripts.SpaceRace.Projects;
    using Assets.Scripts.SpaceRace.Hardware;
    using Assets.Scripts.Career.Contracts;
    using Assets.Scripts.Menu.ListView.Career;
    using System.Windows.Forms;
    using Assets.Scripts.SpaceRace.Collections;

    public class CrewListViewModel : ListViewModel
    {  
        private IProgramManager _pm;
        private List<Contract> _contracts;
        private ContractsDetails _details;
        //
        // Summary:
        //     Gets or sets the action to execute when the craft has been selected.
        //
        //
        // Value:
        //     The offer selected action.
        public Action<SRCraftConfig> CraftSelected { get; set; }
        //
        // Summary:
        //     Gets or sets the primary button text.
        //
        // Value:
        //     The primary button text.
        public string PrimaryButtonText { get; set; } = "SEND CREW";


        //
        // Summary:
        //     Gets or sets the title.
        //
        // Value:
        //     The title.
        public string Title { get; set; } = "CONTRACT ROSTERS";
        
        public CrewListViewModel(IProgramManager pm)
        {
            _contracts = new List<Contract>(pm.Career.Contracts.Active);
            _contracts.Add(pm.Career.Contracts.Completed.FirstOrDefault(con => con.Id == "extra crew"));
            _pm = pm;
        }
        private string Subtitle(Contract contract)
        {
            if (_pm.Data.ContractCrewsProvided.Contains(contract.ContractNumber))
            {
                return "Crew already deployed";
            }
            int num = _pm.RequiredCrew(contract).TotalOccurences();
            if (num > 0)
            {
                return "Crew: " + num.ToString();
            }
            return "Contract does not require crew";
        }
        public override IEnumerator LoadItems()
        {
            _details = new ContractsDetails(base.ListView.ListViewDetails, null);
            ListViewItemScript selectedItem = null;
            foreach (Contract contract in _contracts)
            {
                ListViewItemScript item = ListView.CreateItem(contract.Name, Subtitle(contract), contract);
                if (_pm.Data.ContractCrewsProvided.Contains(contract.ContractNumber) || _pm.RequiredCrew(contract).TotalOccurences() ==0)
                {
                    item.StatusIcon = ListViewItemScript.StatusIconType.Locked;
                }
            }
            yield return new WaitForEndOfFrame();
            base.ListView.SelectedItem = selectedItem;
        }
        public override void OnListViewInitialized(ListViewScript listView)
        {
            base.OnListViewInitialized(listView);
            listView.Title = Title;
            listView.PrimaryButtonText = PrimaryButtonText;
            listView.DisplayType = ListViewScript.ListViewDisplayType.SmallDialog;
        }
        public override void OnPrimaryButtonClicked(ListViewItemScript selectedItem)
        {
            Contract contract = selectedItem?.ItemModel as Contract;
            ListView.Close();
            _pm.SendCrew(contract);
        }


        public override void UpdateDetails(ListViewItemScript item, Action completeCallback)
        {
            if (item != null)
            {
                Contract contract = item.ItemModel as Contract;
                _details.UpdateDetails(contract);
                if (_pm.Data.ContractCrewsProvided.Contains(contract.ContractNumber) || _pm.RequiredCrew(contract).TotalOccurences() == 0)
                {
                    ListView.PrimaryButtonText = "CLOSE";
                }
                else 
                {
                    ListView.PrimaryButtonText = "SEND CREW";
                }
            }
            completeCallback?.Invoke();
        }

    }
}
