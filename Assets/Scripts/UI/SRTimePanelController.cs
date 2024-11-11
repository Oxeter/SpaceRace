
using Assets.Scripts.Flight.Sim;
using ModApi.Flight;
using ModApi.Flight.UI;
using TMPro;
using UI.Xml;
using UnityEngine;
using Assets.Scripts.Flight.UI;
using Assets.Scripts.Flight;
using System;
using ModApi.Craft;
using System.Linq;
using Assets.Scripts.SpaceRace.Projects;

namespace Assets.Scripts.SpaceRace.UI{

    //
    // Summary:
    //     A historical controller for the time panel.
    public class SRTimePanelController : FlightPanelController
    {
        private IProgramManager _pm;
        /// <summary>
        /// The start date of the spacerace career
        /// </summary>
        private readonly DateTime _careerStart = new DateTime(1950,1,1,8,0,0);
        //
        // Summary:
        //     The display time in days
        private bool _displayTimeInDays = false;

        /// <summary>
        /// Whether the active craft is the space center.
        /// </summary>
        private bool _craftIsSpaceCenter = false;
        //
        // Summary:
        //     The flight scene UI
        private IFlightSceneUI _flightSceneUi;

        //
        // Summary:
        //     The mission time
        private long _missionTime;

        //
        // Summary:
        //     The mode fast
        private XmlElement _modeFast;

        //
        // Summary:
        //     The mode normal
        private XmlElement _modeNormal;

        //
        // Summary:
        //     The mode slow
        private XmlElement _modeSlow;

        //
        // Summary:
        //     The mode warp
        private XmlElement _modeWarp;

        //
        // Summary:
        //     The time manager
        private TimeManager _timeManager;

        //
        // Summary:
        //     The time multiplier
        private double _timeMultiplier;

        //
        // Summary:
        //     The time text
        private TextMeshProUGUI _timeText;

        //
        // Summary:
        //     The warp button text
        private TextMeshProUGUI _warpButtonText;

        //
        // Summary:
        //     The warp panel
        private XmlElement _warpPanel;

        //
        // Summary:
        //     Initializes the panel.
        //
        // Parameters:
        //   flightSceneUiController:
        //     The flight scene UI controller.
        public override void Initialize(FlightSceneUiController flightSceneUiController)
        {
            base.Initialize(flightSceneUiController);
            _timeManager = FlightSceneScript.Instance.TimeManager as TimeManager;
            _flightSceneUi = FlightSceneScript.Instance.FlightSceneUI;
            _pm = ProgramManager.Instance;
            FlightSceneScript.Instance.CraftChanged += CraftNodeChanged;
            _craftIsSpaceCenter = SRManager.IsSpaceCenter(FlightSceneScript.Instance.CraftNode);
        }

        //
        // Summary:
        //     This function will be called whenever the layout is rebuilt - if you have any
        //     setup code which needs to be executed after the layout is rebuilt, override this
        //     function and implement it here.
        //
        // Parameters:
        //   parseResult:
        //     The parse result.
        public override void LayoutRebuilt(ParseXmlResult parseResult)
        {
            _warpPanel = base.xmlLayout.GetElementById("warp-panel");
            _warpPanel.Hide();
            _warpButtonText = base.xmlLayout.GetElementById<TextMeshProUGUI>("warp-text");
            _timeText = base.xmlLayout.GetElementById<TextMeshProUGUI>("time-text");
            _modeSlow = base.xmlLayout.GetElementById("mode-slow");
            _modeNormal = base.xmlLayout.GetElementById("mode-normal");
            _modeFast = base.xmlLayout.GetElementById("mode-fast");
            _modeWarp = base.xmlLayout.GetElementById("mode-warp");
        }

        //
        // Summary:
        //     Updates the panel.
        //
        // Parameters:
        //   craftNode:
        //     The craft node.
        public override void UpdatePanel(CraftNode craftNode)
        {
            SetMode(_timeManager.CurrentMode);
            UpdateMissionTime();
        }
        public void CraftNodeChanged(ICraftNode craftNode)
        {
            _craftIsSpaceCenter = SRManager.IsSpaceCenter(craftNode);
        }
        //
        // Summary:
        //     Called when decrease warp is clicked.
        private void OnDecreaseWarpClicked()
        {
            _timeManager.DecreaseTimeMultiplier();
        }

        //
        // Summary:
        //     Called when [fast forward clicked].
        private void OnFastForwardClicked()
        {
            _timeManager.SetFastForwardMode();
        }

        //
        // Summary:
        //     Called when [increase warp clicked].
        private void OnIncreaseWarpClicked()
        {
            if (_timeManager.CanIncreaseTimeMultiplier(out var failRason))
            {
                _timeManager.IncreaseTimeMultiplier();
            }
            else
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(failRason);
            }
        }

        //
        // Summary:
        //     Called when [play clicked].
        private void OnPlayClicked()
        {
            _timeManager.SetNormalSpeedMode();
            _pm.SetWarp(false);
        }

        //
        // Summary:
        //     Called when [slow motion clicked].
        private void OnSlowMotionClicked()
        {
            _timeManager.SetSlowMotionMode();
            _pm.SetWarp(false);
        }

        //
        // Summary:
        //     Called when [time clicked].
        private void OnTimeClicked()
        {
            _displayTimeInDays = !_displayTimeInDays;
            _missionTime = -1L;
        }

        //
        // Summary:
        //     Called when [warp mode clicked].
        private void OnWarpModeClicked()
        {
            _pm.SetWarp(!_pm.IsWarp);
        }

        //
        // Summary:
        //     Selects the button.
        //
        // Parameters:
        //   button:
        //     The button.
        //
        //   select:
        //     if set to true [select].
        private void SelectButton(XmlElement button, bool select)
        {
            if (select)
            {
                if (!button.HasClass("time-button-selected"))
                {
                    button.AddClass("time-button-selected");
                }
            }
            else if (button.HasClass("time-button-selected"))
            {
                button.RemoveClass("time-button-selected");
            }
        }

        //
        // Summary:
        //     Sets the mode.
        //
        // Parameters:
        //   mode:
        //     The mode.
        private void SetMode(ITimeMultiplierMode mode)
        {
            if (_timeMultiplier != mode.TimeMultiplier)
            {
                _timeMultiplier = mode.TimeMultiplier;
                SelectButton(_modeWarp, _pm.IsWarp);
                SelectButton(_modeNormal, mode.TimeMultiplier == 1.0);
                SelectButton(_modeSlow, mode.TimeMultiplier < 1.0);
                SelectButton(_modeFast, mode.TimeMultiplier > 1.0);
                if (mode.TimeMultiplier != 1.0)
                {
                    _warpPanel.Show();
                }
                else
                {
                    _warpPanel.Hide();
                }

                SetWarpModeButtonText();
            }
        }

        //
        // Summary:
        //     Sets the warp mode button text.
        private void SetWarpModeButtonText()
        {
            string text;
            if (_timeMultiplier >= 1.0)
            {
                text = $"{(int)_timeMultiplier:n0}<size=60%>x</size>";
            }
            else if (_timeMultiplier > 0.0)
            {
                text = $"1/{(int)(1.0 / _timeMultiplier):n0}<size=60%>x</size>";
            }
            else if (_timeMultiplier == 0.0)
            {
                text = "Paused";
            }
            else
            {
                Debug.LogError($"Unsupported time multiplier: {_timeMultiplier}");
                text = "N/A";
            }

            _warpButtonText.text = text;
        }

        //
        // Summary:
        //     Updates the mission time.
        private void UpdateMissionTime()
        {
            double now = FlightSceneScript.Instance.FlightState.Time;
            if (_missionTime == (long)now)
            {
                return;
            }
            _missionTime = (long)now;
            if (_displayTimeInDays || _craftIsSpaceCenter)
            {
                DateTime dt = new DateTime(1950,1,1,13,0,0).AddSeconds(now);
                _timeText.text = $"{dt.ToShortDateString()} {dt.ToLongTimeString()}";
            }
            else
            {
                long num = (long)(now - FlightSceneScript.Instance.CraftNode.InitialCraftNodeData.First().LaunchTime);
                int num3 = (int)(num / 86400);
                string days = num3 >0? $"{num3}d " : string.Empty;
                int num4 = (int)(num % 86400);
                int num5 = num4 / 3600;
                int num6 = (num4 - num5 * 60 * 60) / 60;
                int num7 = num4 % 60;
                _timeText.text = $"T+ {days} {num5:00}:{num6:00}:{num7:00}";
            }
        }
    }
}