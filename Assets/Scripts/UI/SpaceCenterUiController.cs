using UI.Xml;

namespace Assets.Scripts.SpaceRace.UI
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Assets.Scripts.State;
    using Assets.Scripts.Menu.ListView.Career;
    using Assets.Scripts.Flight.Sim;
    using Assets.Scripts.Flight.UI;
    using UnityEngine;
    using Assets.Scripts.Menu.ListView;
    using ModApi.Planet;
    using ModApi.CelestialData;
    using Assets.Scripts.Ui;
    using ModApi.Flight;
    using ModApi.Scripts.State;
    using Assets.Packages.DevConsole;
    using ModApi.Ui;
    using System.Windows.Forms;
    using TMPro;
    using Assets.Scripts.SpaceRace.Projects;
    using ModApi.Math;
    using ModApi.Craft;
    using ModApi.Craft.Parts;
    using Assets.Scripts.Flight;
    using Assets.Scripts.Flight.GameView;
    using Assets.Scripts.Flight.ScaledSpace;
    using UnityEngine.UI;
    using System.Text;
    using Unity.Mathematics;
    using Assets.Scripts.SpaceRace.Ui;

    public class SpaceCenterUiController : XmlLayoutController
    {
        private IProgramManager _pm;
        private bool _active;
        private Dictionary<string,FlightPanelController> _panels = new Dictionary<string,FlightPanelController>();

        //
        // Summary:
        //     The active crafts button
        private XmlElement _activeCraftsButton;

        //
        // Summary:
        //     The events crafts button
        private XmlElement _eventsButton;
        //
        // Summary:
        //     The number of available tech nodes.
        private int _availableTechNodes;

        //
        // Summary:
        //     The bottom panel.
        private XmlElement _bottomPanel;

        //
        // Summary:
        //     The button text career
        private TextMeshProUGUI _buttonTextCareer;
        //
        // Summary:
        //     The button text launch
        private TextMeshProUGUI _buttonTextLaunch;
        //
        // Summary:
        //     The next space center button
        private XmlElement _nextSpaceCenterButton;
        //
        // Summary:
        //     The previous space center button
        private XmlElement _previousSpaceCenterButton;
        //
        // Summary:
        //     The button text career initial color
        private Color _buttonTextCareerInitialColor;

        //
        // Summary:
        //     The button text tech tree
        private TextMeshProUGUI _buttonTextTechTree;


        //
        // Summary:
        //     The company name text
        private XmlElement _companyNameText = null;

        //
        // Summary:
        //     The file menu
        private XmlElement _fileMenu = null;

        private XmlElement _instrumentPanel = null;

        //
        // Summary:
        //     The rocket text
        private TextMeshProUGUI _rocketText = null;

        //
        // Summary:
        //     The text active crafts
        private TextMeshProUGUI _textActiveCrafts;

        //
        // Summary:
        //     The text active jobs
        private TextMeshProUGUI _textActiveJobs;

        //
        // Summary:
        //     The text money
        private TextMeshProUGUI _textMoney;

        //
        // Summary:
        //     The text tech points
        private TextMeshProUGUI _textBalance;

        //
        // Summary:
        //     The top panel.
        private XmlElement _topPanel;
        //
        // Summary:
        //     Gets or sets a value indicating whether the top panel is visible.
        //
        // Value:
        //     true if the top panel is visible; otherwise, false.
        public bool TopPanelVisible
        {
            get
            {
                return _topPanel.Visible;
            }
            set
            {
                if (value)
                {
                    _topPanel.Show();
                }
                else
                {
                    _topPanel.Hide();
                }
            }
        }

        public bool Visible
        {
            get
            {
                return gameObject.activeInHierarchy;
            }
            set
            {
                //Game.Instance.UserInterface.InspectorPanelsVisible = value;
                gameObject.SetActive(value);
            }
        }

        protected virtual void Start()
        {
            FlightSceneScript instance = FlightSceneScript.Instance;
            //ViewManagerScript viewManager = FlightSceneScript.Instance.ViewManager;
            //instance.ActiveCommandPodChanged += OnActiveCommandPodChanged;
            //instance.ActiveCommandPodStateChanged += OnActiveCommandPodStateChanged;
            instance.CraftChanged += OnCraftChanged;
        }
        public override void LayoutRebuilt(ParseXmlResult parseResult)
        {
            base.LayoutRebuilt(parseResult);
            _pm = ProgramManager.Instance;
            _pm.SpaceCenterUi= this;
            _topPanel = xmlLayout.GetElementById("top-panel");
            _bottomPanel = xmlLayout.GetElementById("bottom-panel");
            _companyNameText = xmlLayout.GetElementById("company-text");
            UpdateCompanyName();
            _rocketText = xmlLayout.GetElementById<TextMeshProUGUI>("rocket-text");
            _rocketText.SetText(ScaleLowerCase("Space Center"));
            _previousSpaceCenterButton = xmlLayout.GetElementById("previous-space-center-button");
            _nextSpaceCenterButton = xmlLayout.GetElementById("next-space-center-button");
            _activeCraftsButton = xmlLayout.GetElementById("active-crafts-button");
            _eventsButton = xmlLayout.GetElementById("events-button");
            _textBalance = xmlLayout.GetElementById<TextMeshProUGUI>("balance-text");
            _textActiveCrafts = xmlLayout.GetElementById<TextMeshProUGUI>("active-crafts-text");
            _textActiveJobs = xmlLayout.GetElementById<TextMeshProUGUI>("active-jobs-text");
            _textMoney = xmlLayout.GetElementById<TextMeshProUGUI>("money-text");
            _buttonTextTechTree = xmlLayout.GetElementById<TextMeshProUGUI>("tech-tree-button-text");
            _buttonTextCareer = xmlLayout.GetElementById<TextMeshProUGUI>("career-button-text");
            _buttonTextCareerInitialColor = ((Graphic)_buttonTextCareer).color;
            _buttonTextLaunch = xmlLayout.GetElementById<TextMeshProUGUI>("launch-button-text");
            UpdateStats();
            _panels.Clear();
            AddPanel<ViewPanelController>("view");
            AddPanel<NavPanelController>("nav");
            AddPanel<EvaPanelController>("eva");
            AddPanel<StagingPanelController>("staging");
            AddPanel<ActivationPanelController>("activation");
            AddPanel<SRTimePanelController>("time");
            AddPanel<FuelTransferPanelController>("fuel");
            AddPanel<InputSliderPanelController>("input");
            OnCraftChanged(Game.Instance.FlightScene.CraftNode);
        }
        //
        // Summary:
        //     Highlights the text.
        //
        // Parameters:
        //   text:
        //     The text.
        public void HighlightText(TextMeshProUGUI text)
        {
            float t = (1f + Mathf.Sin(Time.time * 4f)) * 0.5f;
            ((Graphic)text).color = Color.Lerp("#00b7ed".ToColor(), "#abb4be".ToColor(), t);
        }

        protected virtual void Update()
        {

        }

        //
        // Summary:
        //     Adds the panel with the specified name.
        //
        // Parameters:
        //   name:
        //     The name.
        //
        // Returns:
        //     The flight panel controller.
        private FlightPanelController AddPanel<T>(string name) where T: FlightPanelController
        {
            T match = gameObject.GetComponentInParent<FlightSceneUiController>().gameObject.GetComponentInChildren<T>();
            //Debug.Log(match == null? "Did not find script component "+typeof(T).Name :"Found script component " +typeof(T).Name);
            _panels[name] = match;
            return match;
        }

        //
        // Summary:
        //     Adds the panel.
        //
        // Parameters:
        //   panel:
        //     The panel.
        public void OnCraftChanged(ICraftNode craftNode)
        {
            _active = SRManager.IsSpaceCenter(craftNode);
            _topPanel.SetActive(_active);
            _bottomPanel.SetActive(_active);
            _panels["nav"].Active = !_active;
            //_panels["eva"].Active = !_active;
            _panels["activation"].Active = !_active;
            _panels["fuel"].Active = !_active;
            _panels["input"].Active = !_active;
            //_panels["staging"].Active = !_active; doesnt seem to acomplish anything
            //_instrumentPanel.Visible = !_active; not currently assigning successfully
            if (_active)
            {
                Game.Instance.FlightScene.FlightSceneUI.SetNavSphereVisibility(!_active, false);
                //_panels["staging"].Hide();
                UpdateStats();
            }
            else
            {
                //_panels["staging"].Show();
                Game.Instance.FlightScene.FlightSceneUI.RestoreNavSphereVisibility();
            }

        }
        public void OnBuildButtonClicked()
        {
            Game.Instance.FlightScene.ExitFlightScene(true, FlightSceneExitReason.SaveAndExit, "Design");
        }

        public void OnActiveCraftsButtonClicked()
        {
            SimpleActiveCraftsViewModel activeCraftsViewModel = new SimpleActiveCraftsViewModel();
            activeCraftsViewModel.Closed += OnActiveCraftsListViewClosed;
            ListViewScript script  = Game.Instance.UserInterface.CreateListView(activeCraftsViewModel) as ListViewScript;
        }

        private void OnActiveCraftsListViewClosed(ListViewModel model)
        {
            model.Closed -= OnActiveCraftsListViewClosed;
            UpdateStats();
        }

        public void OnEventsButtonClicked()
        {
            Game.Instance.UserInterface.CreateListView(new HistoryListViewModel(_pm.History.PastEvents.Where(ev => ev.Visible), null));
        }
        public void OnTechTreeButtonClicked()
        {
            Game.Instance.FlightScene.ExitFlightScene(true, FlightSceneExitReason.SaveAndExit, "TechTree");
        }

        public void OnContractsButtonClicked()
        {
            _pm.UpdateAllRates(true);
            UserInterface userInterface = Game.Instance.UserInterface as UserInterface;
            CareerDialogScript dialog = userInterface.CreateCareerDialog(true);
        }
        public void OnProgramButtonClicked()
        {
            _pm.ToggleProgramPanel();
        }
        public void OnLaunchButtonClicked()
        {
            Game.Instance.UserInterface.CreateListView(new LaunchListViewModel(_pm.Integrations.Values.ToList<ILaunchable>(), _pm.HM.Craft.Values.ToList<ILaunchable>(), _pm));
        }
        public void OnPreviousSpaceCenterButtonClicked()
        {
            _pm.PreviousSpaceCenter();
        }
        public void OnNextSpaceCenterButtonClicked()
        {
            _pm.NextSpaceCenter();
        }
        //
        // Summary:
        //     Called when the menu button is clicked.
        //
        // Parameters:
        //   button:
        //     The button.
        public void OnMenuButtonClicked(XmlElement button)
        {
            ModApi.Ui.MessageDialogScript messageDialogScript = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.OkayCancel);
            messageDialogScript.MessageText = "Exit to Menu?";
            messageDialogScript.OkayButtonText = "Exit";
            messageDialogScript.OkayClicked += delegate (ModApi.Ui.MessageDialogScript d)
            {
                d.Close();
                Game.Instance.FlightScene.ExitFlightScene(true, FlightSceneExitReason.SaveAndExit, "Menu");
            };
            
        }
        private static string ScaleLowerCase(string s, float scale = 0.75f)
        {
            //Debug.Log("string builder on " +s);
            StringBuilder stringBuilder = new StringBuilder();
            bool flag = false;
            if (s != null)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    if (!flag && char.IsLower(s[i]))
                    {
                        flag = true;
                        stringBuilder.Append($"<size={(int)(scale * 100f)}%>");
                    }
                    else if (flag && !char.IsLower(s[i]))
                    {
                        flag = false;
                        stringBuilder.Append("</size>");
                    }

                    stringBuilder.Append(s[i]);
                }
            }

            return stringBuilder.ToString();
        }
        //
        // Summary:
        //     Updates the name of the company.
        private void UpdateCompanyName()
        {
            if (_companyNameText == null)
            Debug.Log("companyNameText element not set");
            else
            _companyNameText.SetText(ScaleLowerCase(Game.Instance.GameState.CompanyName));
        }
        //
        // Summary:
        //     Updates the stats.
        public void UpdateStats()
        {
            if (!_active) return;
            _activeCraftsButton.gameObject.SetActive(value: true);
            int num = Game.Instance.FlightScene.FlightState.CraftNodes.Where((ICraftNode x) => x.HasCommandPod).Count();
            _activeCraftsButton.SetActive(num > 1);
            _eventsButton.gameObject.SetActive(value: true);
            int num2 = _pm.History.PastEvents.Count();
            _eventsButton.SetActive(num2 > 0);
            GameState gameState = Game.Instance.GameState;
            if (gameState.Career != null)
            {
                int num3 = (int)gameState.Career.TechTree.GetItemValue("MaxActiveCrafts").ValueAsFloat;
                _textBalance.text = _pm.BalancePerDayString;
                _textActiveCrafts.text = $"{num} / {num3}";
                _textActiveJobs.text = $"{gameState.Career.Contracts.Active.Count} / {gameState.Career.Contracts.Active.Count + gameState.Career.Contracts.Generated.Count}";
                _textMoney.text = Units.GetMoneyString(gameState.Career.Money) ?? "";
                if ((Game.Instance.GameState.Career?.Contracts.NumContractsNotSeen ?? 0) > 0)
                {
                    HighlightText(_buttonTextCareer);
                }
                else
                {
                    ((Graphic)_buttonTextCareer).color = _buttonTextCareerInitialColor;
                }

                if (_availableTechNodes > 0)
                {
                    HighlightText(_buttonTextTechTree);
                }
                if (_pm.HighlightLaunchButton)
                {
                    HighlightText(_buttonTextLaunch);
                }
                else
                {
                    ((Graphic)_buttonTextLaunch).color = _buttonTextCareerInitialColor;
                }
            }
        }
    }
}

