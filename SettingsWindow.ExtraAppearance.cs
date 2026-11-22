using System.Windows;

namespace GhostClip;

// Appearance-only settings kept separate from clipboard and update logic.
public partial class SettingsWindow
{
    public bool ShowGhost { get; private set; }
    public bool ShowTitle { get; private set; }
    public string GhostColour { get; private set; } = WidgetAppearanceOptions.GhostColour;
    public string TitleColour { get; private set; } = WidgetAppearanceOptions.TitleColour;
    public string PrivateOffButtonColour { get; private set; } = WidgetAppearanceOptions.PrivateOffColour;
    public string StatusOffButtonColour { get; private set; } = WidgetAppearanceOptions.StatusOffColour;
    public string SettingsButtonColour { get; private set; } = WidgetAppearanceOptions.SettingsButtonColour;
    public string SettingsIconColour { get; private set; } = WidgetAppearanceOptions.SettingsIconColour;
    public string ExitButtonColour { get; private set; } = WidgetAppearanceOptions.ExitButtonColour;
    public string ExitIconColour { get; private set; } = WidgetAppearanceOptions.ExitIconColour;

    private void InitialiseExtraAppearance(
        bool showGhost, bool showTitle, string ghostColour, string titleColour,
        string privateOffColour, string statusOffColour, string settingsButtonColour,
        string settingsIconColour, string exitButtonColour, string exitIconColour)
    {
        ShowGhost = showGhost;
        ShowTitle = showTitle;
        GhostColour = ghostColour;
        TitleColour = titleColour;
        PrivateOffButtonColour = privateOffColour;
        StatusOffButtonColour = statusOffColour;
        SettingsButtonColour = settingsButtonColour;
        SettingsIconColour = settingsIconColour;
        ExitButtonColour = exitButtonColour;
        ExitIconColour = exitIconColour;

        ShowGhostCheckBox.IsChecked = showGhost;
        ShowTitleCheckBox.IsChecked = showTitle;
        GhostColourTextBox.Text = ghostColour;
        TitleColourTextBox.Text = titleColour;
        PrivateOffColourTextBox.Text = privateOffColour;
        StatusOffColourTextBox.Text = statusOffColour;
        SettingsButtonColourTextBox.Text = settingsButtonColour;
        SettingsIconColourTextBox.Text = settingsIconColour;
        ExitButtonColourTextBox.Text = exitButtonColour;
        ExitIconColourTextBox.Text = exitIconColour;
        RefreshExtraColourFields();
    }

    private void SubscribeExtraAppearanceChanges()
    {
        ShowGhostCheckBox.Checked += (_, _) => ApplyInputChanges();
        ShowGhostCheckBox.Unchecked += (_, _) => ApplyInputChanges();
        ShowTitleCheckBox.Checked += (_, _) => ApplyInputChanges();
        ShowTitleCheckBox.Unchecked += (_, _) => ApplyInputChanges();

        GhostColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        TitleColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        PrivateOffColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        StatusOffColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        SettingsButtonColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        SettingsIconColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        ExitButtonColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
        ExitIconColourTextBox.TextChanged += (_, _) => ApplyInputChanges();
    }

    private void ReadExtraAppearanceSettings()
    {
        ShowGhost = ShowGhostCheckBox.IsChecked == true;
        ShowTitle = ShowTitleCheckBox.IsChecked == true;
        RefreshExtraColourFields();
    }

    private void RefreshExtraColourFields()
    {
        GhostColour = UpdateColourField(
            GhostColourTextBox, GhostColourPreview, GhostColour);
        TitleColour = UpdateColourField(
            TitleColourTextBox, TitleColourPreview, TitleColour);
        PrivateOffButtonColour = UpdateColourField(
            PrivateOffColourTextBox, PrivateOffColourPreview, PrivateOffButtonColour);
        StatusOffButtonColour = UpdateColourField(
            StatusOffColourTextBox, StatusOffColourPreview, StatusOffButtonColour);
        SettingsButtonColour = UpdateColourField(
            SettingsButtonColourTextBox, SettingsButtonColourPreview, SettingsButtonColour);
        SettingsIconColour = UpdateColourField(
            SettingsIconColourTextBox, SettingsIconColourPreview, SettingsIconColour);
        ExitButtonColour = UpdateColourField(
            ExitButtonColourTextBox, ExitButtonColourPreview, ExitButtonColour);
        ExitIconColour = UpdateColourField(
            ExitIconColourTextBox, ExitIconColourPreview, ExitIconColour);
    }

    private void ResetExtraAppearanceControls()
    {
        // Called under _synchronising by ResetAppearanceButton_Click.
        ShowGhostCheckBox.IsChecked = WidgetAppearanceOptions.ShowGhost;
        ShowTitleCheckBox.IsChecked = WidgetAppearanceOptions.ShowTitle;
        GhostColourTextBox.Text = WidgetAppearanceOptions.GhostColour;
        TitleColourTextBox.Text = WidgetAppearanceOptions.TitleColour;
        PrivateOffColourTextBox.Text = WidgetAppearanceOptions.PrivateOffColour;
        StatusOffColourTextBox.Text = WidgetAppearanceOptions.StatusOffColour;
        SettingsButtonColourTextBox.Text = WidgetAppearanceOptions.SettingsButtonColour;
        SettingsIconColourTextBox.Text = WidgetAppearanceOptions.SettingsIconColour;
        ExitButtonColourTextBox.Text = WidgetAppearanceOptions.ExitButtonColour;
        ExitIconColourTextBox.Text = WidgetAppearanceOptions.ExitIconColour;
    }
}
