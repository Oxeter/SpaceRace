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

    public class StagesViewModel : ListViewModel
    {  
        private List<SRStage> _stages;
        private IProgramManager _pm;

        private StageDetails _details;
        //
        // Summary:
        //     Gets or sets the action to execute when the contract offer has been selected.
        //
        //
        // Value:
        //     The offer selected action.
        public Action<SRStage> StageSelected { get; set; }
        //
        // Summary:
        //     Gets or sets the primary button text.
        //
        // Value:
        //     The primary button text.
        public string PrimaryButtonText { get; set; } = "CLOSE";


        //
        // Summary:
        //     Gets or sets the title.
        //
        // Value:
        //     The title.
        public string Title { get; set; } = "DEVELOPED STAGES";
        
        public StagesViewModel(List<SRStage> stages, Action<SRStage> action, IProgramManager pm)
        {
            _stages = stages;
            StageSelected = action;
            _pm = pm;
        }
        public override IEnumerator LoadItems()
        {
            _details = new StageDetails(base.ListView.ListViewDetails);
            ListViewItemScript selectedItem = null;
            foreach (SRStage stage in _stages)
            {
                string subtitle = stage.Developed? String.Format("Cost: {0}", Units.GetMoneyString(stage.Price)): "Under development";
                ListView.CreateItem(stage.Name, subtitle, stage, null, ListViewScript.SpriteLoadLocation.Resources);
            }

            yield return new WaitForEndOfFrame();
            base.ListView.SelectedItem = selectedItem;
        }
        public override void OnListViewInitialized(ListViewScript listView)
        {
            base.OnListViewInitialized(listView);
            listView.Title = Title;
            listView.PrimaryButtonText = PrimaryButtonText;
            listView.CanDelete = false;
            listView.DisplayType = ListViewScript.ListViewDisplayType.SmallDialog;
        }
        public override void OnPrimaryButtonClicked(ListViewItemScript selectedItem)
        {
            SRStage stage = selectedItem?.ItemModel as SRStage;
            StageSelected?.Invoke(stage);
            base.ListView.Close();
        }

        public override void UpdateDetails(ListViewItemScript item, Action completeCallback)
        {
            if (item != null)
            {
                SRStage stage = item.ItemModel as SRStage;
                _details.UpdateDetails(stage, _pm);
            }
        completeCallback?.Invoke();
        }

    }

    public class StageDetails
    {
        private DetailsPropertyScript _contractor;
        private DetailsPropertyScript _productionTime;
        private DetailsTextScript _partFamilies;
        public StageDetails(ListViewDetailsScript listViewDetails)
        {
            _contractor = listViewDetails.Widgets.AddProperty("Contractor");
            _productionTime = listViewDetails.Widgets.AddProperty("Days/Unit");
            listViewDetails.Widgets.AddSpacer();
            listViewDetails.Widgets.AddText("Parts");   
            _partFamilies = listViewDetails.Widgets.AddText("");  
        }

        public void UpdateDetails(SRStage stage, IProgramManager pm)
        {
            _contractor.ValueText = pm.Contractor(stage.ContractorId).Data.Name;
            _productionTime.ValueText = (stage.ProductionRequired / pm.Contractor(stage.ContractorId).Data.BaseProductionRate).ToString();
            _partFamilies.Text = string.Join("\n", stage.PartFamilies.Select(hop => pm.HM.Families[hop.Id].Name+" x"+hop.Occurences.ToString()));
        }
    }
}
