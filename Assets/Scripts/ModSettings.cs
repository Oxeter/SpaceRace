namespace Assets.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Common;
    using ModApi.Settings.Core;

    /// <summary>
    /// The settings for the mod.
    /// </summary>
    /// <seealso cref="ModApi.Settings.Core.SettingsCategory{Assets.Scripts.ModSettings}" />
    public class ModSettings : SettingsCategory<ModSettings>
    {
        /// <summary>
        /// The mod settings instance.
        /// </summary>
        private static ModSettings _instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModSettings"/> class.
        /// </summary>
        public ModSettings() : base("SpaceRace")
        {
        }

        /// <summary>
        /// Gets the mod settings instance.
        /// </summary>
        /// <value>
        /// The mod settings instance.
        /// </value>
        public static ModSettings Instance => _instance ?? (_instance = Game.Instance.Settings.ModSettings.GetCategory<ModSettings>());

        ///// <summary>
        ///// Gets the FundingMultiplier value
        ///// </summary>
        ///// <value>
        ///// The FundingMultiplier value.
        ///// </value>
        public NumericSetting<float> FundingMultiplier { get; private set; }

        ///// <summary>
        ///// Gets the FundingMultiplier value
        ///// </summary>
        ///// <value>
        ///// The FundingMultiplier value.
        ///// </value>
        public NumericSetting<float> AblatorEffectiveness { get; private set; }
        /// <summary>
        /// Whether the help and tutorials appear
        /// </summary>
        public BoolSetting ShowHelp { get; private set; }
        /// <summary>
        /// Whether part failures occur
        /// </summary>
        public BoolSetting HardwareFailures { get; private set; }
        /// <summary>
        /// Whether space centers use a visible model (good for debugging)
        /// </summary>
        public BoolSetting VisibleSpaceCenters { get; private set; }
        /// <summary>
        /// Initializes the settings in the category.
        /// </summary>
        protected override void InitializeSettings()
        {
            this.ShowHelp = this.CreateBool("Show Help")
                .SetDescription("Whether the tutorial appears for new games and the help buttons appear in the interface.")
                .SetDefault(true);
            this.FundingMultiplier = this.CreateNumeric<float>("Funding Multiplier", 0.5f, 1.5f, 0.05f)
                .SetDescription("Adjusts the funding received.  Takes effect when a save is loaded.")
                .SetDisplayFormatter(x => x.ToString("F2"))
                .SetDefault(1.0f);
            this.AblatorEffectiveness = this.CreateNumeric<float>("Ablator Effectiveness", 0.05f, 0.75f, 0.01f)
                .SetDescription("Adjusts how effectively ablators remove heat as they ablate.  40% appears to match historical performace.")
                .SetDisplayFormatter(x => x.ToString("F2"))
                .SetDefault(0.4f);
            this.HardwareFailures = this.CreateBool("Hardware Failures")
                .SetDescription("Whether hardware can fail.  Keep this enabled for the historic experience.")
                .SetDefault(true);
            this.VisibleSpaceCenters = this.CreateBool("Visible Space Centers")
                .SetDescription("Space centers are usually invisible.  Enabling this might harm your immersion, but makes it easier to find and terminate misbahaving centers.  Does not alter existing centers and may require a reload to take full effect.")
                .SetDefault(false);  
        }

    }
}