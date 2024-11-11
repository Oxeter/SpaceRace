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

    public class PreviousCraftViewModel : ListViewModel
    {  
        private IProgramManager _pm;
        private List<SRCraftConfig> _craft;
        private SRCraftConfigDetails _details;
        //
        // Summary:
        //     Gets or sets the action to execute when the contract offer has been selected.
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
        public string PrimaryButtonText { get; set; } = "BEGIN INTEGRATION";


        //
        // Summary:
        //     Gets or sets the title.
        //
        // Value:
        //     The title.
        public string Title { get; set; } = "PREVIOUS CRAFT";
        
        public PreviousCraftViewModel(List<SRCraftConfig> craft, Action<SRCraftConfig> action, IProgramManager pm)
        {
            _craft = craft;
            CraftSelected = action;
            _pm = pm;
        }
        public override IEnumerator LoadItems()
        {
            _details = new SRCraftConfigDetails(base.ListView.ListViewDetails, _pm);
            ListViewItemScript selectedItem = null;
            foreach (SRCraftConfig config in _craft)
            {
                string subtitle = string.Format("Estimated cost: {0}", Units.GetMoneyString(_pm.CostCalculator.GetIntegrationCost(config)));
                ListView.CreateItem(config.Name, subtitle, config, null, ListViewScript.SpriteLoadLocation.Resources);
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
            SRCraftConfig config = selectedItem?.ItemModel as SRCraftConfig;
            CraftSelected?.Invoke(config);
            base.ListView.Close();
        }

        public override void OnDeleteButtonClicked(ListViewItemScript selectedItem)
        {
            SRCraftConfig config = selectedItem?.ItemModel as SRCraftConfig;
            config.Active = false;
        }

        public override void UpdateDetails(ListViewItemScript item, Action completeCallback)
        {
        if (item != null)
        {
            SRCraftConfig config = item.ItemModel as SRCraftConfig;
            _details.UpdateDetails(config);
        }

        completeCallback?.Invoke();
        }

    }
}
