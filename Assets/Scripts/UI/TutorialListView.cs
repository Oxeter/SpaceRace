namespace Assets.Scripts.SpaceRace.UI
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

    using Assets.Scripts.SpaceRace.History;
    using Assets.Scripts.SpaceRace.Projects;

    public class TutorialListViewModel : ListViewModel
    {  
        private IProgramManager _pm;
        private List<TutorialMessage> _messages;
        private TutorialMessageDetails _details;
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
        public string Title { get; set; } = "TUTORIAL";
        
        public TutorialListViewModel(IProgramManager pm, List<TutorialMessage> messages)
        {
            _pm = pm;
            _messages = new List<TutorialMessage>(messages.Where(m => m.Index <= pm.CurrentTutorialMessage));
            DoubleClickIsPrimaryClick = false;
        }
        public TutorialListViewModel(List<TutorialMessage> messages)
        {
            _messages = new List<TutorialMessage>(messages);
        }
        public override IEnumerator LoadItems()
        {
            ListView.CanDelete = _pm.CurrentTutorialMessage < SRTutorialMessages.MaxMessage;
            _details = new TutorialMessageDetails(base.ListView.ListViewDetails);
            for (int i = _messages.Count() -1 ; i >= 0; i--)
            {
                ListViewItemScript item = ListView.CreateItem(_messages[i].Title, _messages[i].Subtitle, _messages[i], null, ListViewScript.SpriteLoadLocation.Resources);
                if (i == _messages.Count() -1) base.ListView.SelectedItem = item;
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
            base.ListView.Close();
        }


        public override void OnDeleteButtonClicked(ListViewItemScript selectedItem)
        {
            _pm.CurrentTutorialMessage = SRTutorialMessages.MaxMessage;
            base.ListView.Close();
        }

        public override void UpdateDetails(ListViewItemScript item, Action completeCallback)
        {
        if (item != null)
        {
            TutorialMessage selectedMessage = item.ItemModel as TutorialMessage;
            _details.UpdateDetails(selectedMessage);
        }

        completeCallback?.Invoke();
        }

    }

    public class TutorialMessageDetails
    {
        private DetailsImageScript _image;
        private DetailsTextScript _text;
        public TutorialMessageDetails(ListViewDetailsScript listViewDetails)
        {
            _image = listViewDetails.Widgets.AddImage();
            _text = listViewDetails.Widgets.AddText("Text");
        }

        public void UpdateDetails(TutorialMessage message)
        {
            if (File.Exists(Path.Combine(CareerState.CheckOverridePath(Game.Instance.GameState.Career.Contracts.ResourcesPath, "Images/Tutorial/"), $"Tutorial{message.Index}.png")))
            {
                _image.ImagePath = Path.Combine(CareerState.CheckOverridePath(Game.Instance.GameState.Career.Contracts.ResourcesPath, "Images/Tutorial/"), $"Tutorial{message.Index}.png");
                _image.Visible = true;
            }
            else 
            {
                _image.Visible = false;
            }
            _text.Text = message.Text;
        }
    }
}
