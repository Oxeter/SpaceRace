namespace Assets.Scripts.SpaceRace.Ui
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Assets.Scripts.State;
    using ModApi.Craft;
    using ModApi.Math;
    using System.IO;
    using ModApi.State;
    using ModApi.Ui;
    using UnityEngine;
    using Assets.Scripts.Menu.ListView;
    using Assets.Scripts.SpaceRace.Projects;
    using Assets.Scripts.SpaceRace.History;

    public class HistoryListViewModel : ListViewModel
    {  
        private IEnumerable<HistoricalEvent> _pastEvents;
        private HistoricalEvent _initialSelection;

        private HistoricalEventDetails _details;
        //
        // Summary:
        //     Gets or sets the action to execute when the contract offer has been selected.
        //
        //
        // Value:
        //     The offer selected action.
        public Action<HistoricalEvent> EventSelected { get; set; }
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
        public string Title { get; set; } = "EVENTS";
        
        public HistoryListViewModel(IEnumerable<HistoricalEvent> pastEvents, HistoricalEvent selectedEvent)
        {
            _pastEvents = pastEvents;
            _initialSelection = selectedEvent;
            DoubleClickIsPrimaryClick = false;
            NoItemsFoundMessage = "No items found, go make history!";
        }
        public override IEnumerator LoadItems()
        {
            _details = new HistoricalEventDetails(base.ListView.ListViewDetails);
            foreach (HistoricalEvent hevent in _pastEvents)
            {
                string subtitle = hevent.FixedOccurenceDate.ToLongDateString();
                ListViewItemScript item = ListView.CreateItem(hevent.Name, subtitle, hevent, null, ListViewScript.SpriteLoadLocation.Resources);
                if (hevent == _initialSelection) base.ListView.SelectedItem = item;
            }
            yield return new WaitForEndOfFrame();
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
            HistoricalEvent selectedEvent = selectedItem?.ItemModel as HistoricalEvent;
            EventSelected?.Invoke(selectedEvent);
            base.ListView.Close();
        }

        public override void UpdateDetails(ListViewItemScript item, Action completeCallback)
        {
        if (item != null)
        {
            HistoricalEvent selectedEvent = item.ItemModel as HistoricalEvent;
            _details.UpdateDetails(selectedEvent);
        }

        completeCallback?.Invoke();
        }

    }

    public class HistoricalEventDetails
    {
        private DetailsImageScript _image;
        private DetailsTextScript _text;
        public HistoricalEventDetails(ListViewDetailsScript listViewDetails)
        {
            _image = listViewDetails.Widgets.AddImage();
            _text = listViewDetails.Widgets.AddText("Text");
        }

        public void UpdateDetails(HistoricalEvent hevent)
        {
            if (hevent.ImagePath != null && File.Exists(Path.Combine(CareerState.CheckOverridePath(Game.Instance.GameState.Career.Contracts.ResourcesPath, "Images/Events/"), hevent.ImagePath)))
            {
                _image.ImagePath = Path.Combine(CareerState.CheckOverridePath(Game.Instance.GameState.Career.Contracts.ResourcesPath, "Images/Events/"), hevent.ImagePath);
                _image.Visible = true;
            }
            else 
            {
                _image.Visible = false;
            }
            _text.Text = hevent.Description;
        }
    }
}
