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




    public class StageDevContractListViewModel : ListViewModel
    {  
        private List<StageDevBid> _offers;

        private StageDevContractDetails _details;
        //
        // Summary:
        //     Gets or sets the action to execute when the contract offer has been selected.
        //
        //
        // Value:
        //     The offer selected action.
        public Action<StageDevBid> OfferSelected { get; set; }
        //
        // Summary:
        //     Gets or sets the primary button text.
        //
        // Value:
        //     The primary button text.
        public string PrimaryButtonText { get; set; } = "AWARD CONTRACT";


        //
        // Summary:
        //     Gets or sets the title.
        //
        // Value:
        //     The title.
        public string Title { get; set; } = "DEVELOPMENT BIDS";
        
        public StageDevContractListViewModel(List<StageDevBid> offers, Action<StageDevBid> action)
        {
            _offers = offers;
            OfferSelected = action;
        }
        public override IEnumerator LoadItems()
        {
            _details = new StageDevContractDetails(base.ListView.ListViewDetails);
            ListViewItemScript selectedItem = null;
            foreach (StageDevBid offer in _offers)
            {
                string subtitle = String.Format("Cost: {0}/Day.  Estimated total: {1} over {2}", Units.GetMoneyString(offer.DevCostPerDay), Units.GetMoneyString(offer.TotalProjectedCost), Units.GetRelativeTimeString(offer.ProjectedDevelopmentTime));
                ListView.CreateItem(offer.Contractor.Data.Name, subtitle, offer, offer.Sprite, ListViewScript.SpriteLoadLocation.Resources);
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
            StageDevBid offer = selectedItem?.ItemModel as StageDevBid;
            OfferSelected?.Invoke(offer);
            base.ListView.Close();
        }

        public override void UpdateDetails(ListViewItemScript item, Action completeCallback)
        {
        if (item != null)
        {
            StageDevBid offer = item.ItemModel as StageDevBid;
            _details.UpdateDetails(offer);
        }

        completeCallback?.Invoke();
        }

    }
}
