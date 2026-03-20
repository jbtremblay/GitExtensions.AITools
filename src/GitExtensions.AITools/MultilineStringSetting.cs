using GitExtensions.Extensibility.Settings;

namespace GitExtensions.AITools;

internal sealed class MultilineStringSetting : ISetting
{
    public MultilineStringSetting(string name, string caption, string defaultValue, bool useDefaultValueIfBlank = false)
    {
        Name = name;
        Caption = caption;
        DefaultValue = defaultValue;
        UseDefaultValueIfBlank = useDefaultValueIfBlank;
    }

    public string Name { get; }
    public string Caption { get; }
    public string DefaultValue { get; }
    public bool UseDefaultValueIfBlank { get; }

    public ISettingControlBinding CreateControlBinding()
    {
        return new MultilineTextBoxBinding(this);
    }

    public string? this[SettingsSource settings]
    {
        get => settings.GetString(Name, null);
        set => settings.SetString(Name, value);
    }

    public string ValueOrDefault(SettingsSource settings)
    {
        return this[settings] ?? DefaultValue;
    }

    private sealed class MultilineTextBoxBinding : SettingControlBinding<MultilineStringSetting, TextBox>
    {
        public MultilineTextBoxBinding(MultilineStringSetting setting)
            : base(setting, null)
        {
        }

        public override TextBox CreateControl()
        {
            return new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Height = 160,
                WordWrap = true,
            };
        }

        public override void LoadSetting(SettingsSource settings, TextBox control)
        {
            string? settingVal = settings.SettingLevel == SettingLevel.Effective
                ? Setting.ValueOrDefault(settings)
                : Setting[settings];

            if (settingVal is null && Setting.UseDefaultValueIfBlank)
            {
                settingVal = Setting.ValueOrDefault(settings);
            }

            control.Text = settingVal?.Replace(Environment.NewLine, "\n").Replace("\n", Environment.NewLine) ?? "";
        }

        public override void SaveSetting(SettingsSource settings, TextBox control)
        {
            string controlValue = control.Text;
            if (settings.SettingLevel == SettingLevel.Effective)
            {
                if (Setting.ValueOrDefault(settings) == controlValue)
                {
                    return;
                }
            }

            // Store null instead of empty to clear the setting at this level,
            // allowing the cascade (Effective → Local → Distributed → Global) to work correctly.
            Setting[settings] = string.IsNullOrWhiteSpace(controlValue) ? null : controlValue;
        }
    }
}
