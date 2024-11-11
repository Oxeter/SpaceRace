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




    public class PartDevContractListViewModel : ListViewModel
    {  
        private List<PartDevBid> _offers;

        private PartDevContractDetails _details;
        //
        // Summary:
        //     Gets or sets the action to execute when the contract offer has been selected.
        //
        //
        // Value:
        //     The offer selected action.
        public Action<PartDevBid> OfferSelected { get; set; }
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
        
        public PartDevContractListViewModel(List<PartDevBid> offers, Action<PartDevBid> action)
        {
            _offers = offers;
            OfferSelected = action;
        }
        public override IEnumerator LoadItems()
        {
            _details = new PartDevContractDetails(base.ListView.ListViewDetails);
            ListViewItemScript selectedItem = null;
            foreach (PartDevBid offer in _offers)
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
            PartDevBid offer = selectedItem?.ItemModel as PartDevBid;
            OfferSelected?.Invoke(offer);
            base.ListView.Close();
        }

        public override void UpdateDetails(ListViewItemScript item, Action completeCallback)
        {
        if (item != null)
        {
            PartDevBid offer = item.ItemModel as PartDevBid;
            _details.UpdateDetails(offer);
        }

        completeCallback?.Invoke();
        }

    }
}
