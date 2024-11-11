using System.Collections.Generic;
using Assets.Scripts.Menu;
using Assets.Scripts.Menu.ListView.Career;
using Assets.Scripts.Ui;
using Assets.Scripts.Ui.Settings;
using Assets.Scripts.Ui.Sharing.PhotoLibrary;
using Assets.Scripts.Ui.Sharing.Upload;
using Assets.Scripts.Ui.Sharing.Upload.BugReport;
using Assets.Scripts.Ui.Sharing.Upload.Craft;
using Assets.Scripts.Web;
using ModApi;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Ui;
using UI.Xml;

namespace Assets.Scripts.Design
{
    //
    // Summary:
    //     Script for the menu panel.
    public class MenuPanelScript : DesignerFlyoutPanelScript
    {
        //
        // Summary:
        //     Initializes the flyout panel.
        //
        // Parameters:
        //   designerUi:
        //     The designer UI script reference.
        public override void Initialize(DesignerUiScript designerUi)
        {
            base.Initialize(designerUi);
            List<XmlElement> elementsByClass = base.xmlLayout.GetElementsByClass("career-mode");
            foreach (XmlElement item in elementsByClass)
            {
                item.SetActive(Game.IsCareer);
            }

            List<XmlElement> elementsByClass2 = base.xmlLayout.GetElementsByClass("sandbox-mode");
            foreach (XmlElement item2 in elementsByClass2)
            {
                item2.SetActive(!Game.IsCareer);
            }

            bool isFreemiumEnabled = GameMenuScript.IsFreemiumEnabled;
            List<XmlElement> elementsByClass3 = base.xmlLayout.GetElementsByClass("freemium-only");
            foreach (XmlElement item3 in elementsByClass3)
            {
                item3.SetActive(isFreemiumEnabled);
            }
        }

        //
        // Summary:
        //     Checks if the tutorial is running and if it is then displays an error message
        //     to the user.
        //
        // Returns:
        //     True if the tutorial is not running, false otherwise.
        private bool EnsureNoTutorial()
        {
            if (base.DesignerUi.Designer.IsTutorialRunning)
            {
                ModApi.Ui.MessageDialogScript messageDialogScript = Game.Instance.UserInterface.CreateMessageDialog();
                messageDialogScript.MessageText = "This button is disabled while the tutorial is in progress.";
                return false;
            }

            return true;
        }

        //
        // Summary:
        //     Called when the bug report button is clicked.
        private void OnBugReportButtonClicked()
        {
            base.DesignerUi.Designer.SaveCraft(CraftDesigns.EditorCraftId);
            UploadBugReportViewModel viewModel = new UploadBugReportViewModel();
            UploadContentDialogScript.Create(base.DesignerUi.Transform, viewModel);
        }

        //
        // Summary:
        //     Called when the career button is clicked.
        private void OnCareerButtonClicked()
        {
            if (!EnsureNoTutorial())
            {
                return;
            }

            base.Flyout.Close();
            UserInterface userInterface = Game.Instance.UserInterface as UserInterface;
            CareerDialogScript dialog = userInterface.CreateCareerDialog();
            dialog.Closed += delegate
            {
                if (dialog.RequiresSceneReload)
                {
                    base.DesignerUi.Designer.Exit("Design");
                }
            };
        }

        //
        // Summary:
        //     Called when the download craft button is clicked.
        private void OnDownloadCraftButtonClicked()
        {
            if (EnsureNoTutorial())
            {
                base.Flyout.Close();
                WebUtility.OpenUrl(string.Format($"{Game.SimpleRocketsWebsiteUrl}/Crafts/Game?mobile={0}", Device.IsMobileBuild));
            }
        }

        //
        // Summary:
        //     Called when exit button clicked.
        private void OnExitButtonClicked()
        {
            base.DesignerUi.Designer.Exit();
        }

        //
        // Summary:
        //     Called when load craft button clicked.
        private void OnLoadCraftButtonClicked()
        {
            if (EnsureNoTutorial())
            {
                base.Flyout.Close();
                base.DesignerUi.ToggleFlyout(base.DesignerUi.Flyouts.LoadCraft);
            }
        }

        //
        // Summary:
        //     Called when the new craft button is clicked.
        private void OnNewCraftButtonClicked()
        {
            if (!EnsureNoTutorial())
            {
                return;
            }

            base.Flyout.Close();
            if (Game.Instance.GameState.Validator.IsCareerMode)
            {
                base.DesignerUi.Designer.CreateNewCraft(CrafConfigurationType.Rocket, delegate (ICraftScript craftScript)
                {
                    CraftData data = craftScript.Data;
                    if (data.Assembly.Parts.Count == 1)
                    {
                        PartData partData = data.Assembly.Parts[0];
                        data.Assembly.Parts[0].OnDesignerPullout(partData.Name, data.Assembly, skipStartPartScale: false);
                    }
                });
                return;
            }

            ModApi.Ui.MessageDialogScript messageDialogScript = base.DesignerUi.Designer.UserInterface.CreateMessageDialog(MessageDialogType.ThreeButtons);
            messageDialogScript.MiddleButtonText = "AIRPLANE";
            messageDialogScript.OkayButtonText = "ROCKET";
            messageDialogScript.MessageText = "Do want to create a rocket or an airplane? This can be changed later by updating the command pod's configuration type.";
            messageDialogScript.MiddleClicked += delegate (ModApi.Ui.MessageDialogScript dialog)
            {
                base.DesignerUi.Designer.CreateNewCraft(CrafConfigurationType.Plane);
                dialog.Close();
            };
            messageDialogScript.OkayClicked += delegate (ModApi.Ui.MessageDialogScript dialog)
            {
                base.DesignerUi.Designer.CreateNewCraft(CrafConfigurationType.Rocket);
                dialog.Close();
            };
        }

        //
        // Summary:
        //     Called when the photo library button has been clicked.
        private void OnPhotoLibraryButtonClicked()
        {
            PhotoLibraryDialogScript.Create(Game.Instance.UserInterface.Transform);
        }

        //
        // Summary:
        //     Called when the purchase button is clicked.
        private void OnPurchaseButtonClicked()
        {
            Game.Instance.InAppPurchases.CreatePurchaseDialog(null);
        }

        //
        // Summary:
        //     Called when save craft button is clicked.
        private void OnSaveCraftButtonClicked()
        {
            if (EnsureNoTutorial())
            {
                base.Flyout.Close();
                base.DesignerUi.Designer.DialogSave();
            }
        }

        //
        // Summary:
        //     Called when the settings button is clicked.
        private void OnSettingsButtonClicked()
        {
            SettingsDialogScript.Create();
        }

        //
        // Summary:
        //     Called when the share craft button is clicked.
        private void OnShareCraftButtonClicked()
        {
            if (EnsureNoTutorial())
            {
                UploadCraftViewModel viewModel = new UploadCraftViewModel(base.DesignerUi.Designer.CraftScript, base.DesignerUi.Designer);
                UploadContentDialogScript.Create(base.DesignerUi.Transform, viewModel);
                base.Flyout.Close();
            }
        }

        //
        // Summary:
        //     Called when the tech tree button is clicked.
        private void OnTechTreeButtonClicked()
        {
            if (EnsureNoTutorial())
            {
                base.DesignerUi.Designer.Exit("TechTree");
            }
        }
        private void OnSpaceCenterButtonClicked()
        {
            
        }
    }
}