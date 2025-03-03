using MenuChanger;
using MenuChanger.Attributes;
using MenuChanger.Extensions;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using RandomizerMod.Menu;
using System.Collections.Generic;

namespace HK8YPlando.Rando;

internal class ConnectionMenu
{
    public static ConnectionMenu? Instance { get; private set; }

    public static void Setup()
    {
        RandomizerMenuAPI.AddMenuPage(OnRandomizerMenuConstruction, TryGetMenuButton);
        MenuChangerMod.OnExitMainMenu += () => Instance = null;
    }

    private static void OnRandomizerMenuConstruction(MenuPage page) => Instance = new(page);

    private static bool TryGetMenuButton(MenuPage page, out SmallButton button)
    {
        button = Instance!.entryButton;
        return true;
    }

    private SmallButton entryButton;
    private MenuElementFactory<RandomizerSettings> factory;

    private RandomizerSettings Settings => ScatteredAndLostMod.Settings.RandomizerSettings;

    private List<ILockable> requireEnabled = [];
    private List<ILockable> requireHeartDoors = [];

    private ConnectionMenu(MenuPage connectionsPage)
    {
        MenuPage scatteredAndLostPage = new("Scattered and Lost Main Page", connectionsPage);
        entryButton = new(connectionsPage, "Scattered and Lost");
        entryButton.AddHideAndShowEvent(scatteredAndLostPage);

        factory = new(scatteredAndLostPage, Settings);

        MenuItem<bool> enabled = (MenuItem<bool>)factory.ElementLookup[nameof(RandomizerSettings.Enabled)];
        enabled.ValueChanged += _ => UpdateLocks();
        MenuItem<bool> heartDoorsEnabled = (MenuItem<bool>)factory.ElementLookup[nameof(RandomizerSettings.EnableHeartDoors)];
        heartDoorsEnabled.ValueChanged += _ => UpdateLocks();

        HashSet<string> heartFields = [nameof(RandomizerSettings.MinHearts), nameof(RandomizerSettings.MaxHearts), nameof(RandomizerSettings.HeartTolerance)];
        foreach (var e in factory.ElementLookup)
        {
            if (e.Key == nameof(RandomizerSettings.Enabled)) continue;

            if (e.Value is ILockable l)
            {
                requireEnabled.Add(l);
                if (heartFields.Contains(e.Key)) requireHeartDoors.Add(l);
            }
        }

        new VerticalItemPanel(scatteredAndLostPage, SpaceParameters.TOP_CENTER_UNDER_TITLE, SpaceParameters.VSPACE_MEDIUM, true, factory.Elements);
        SetEnabledColor();
    }

    private void SetEnabledColor() => entryButton.Text.color = Settings.Enabled ? Colors.TRUE_COLOR : Colors.FALSE_COLOR;

    private (bool, bool) prevLockState = (true, true);

    private void UpdateLocks()
    {
        var lockState = (Settings.Enabled, Settings.EnableHeartDoors);
        if (lockState == prevLockState) return;

        prevLockState = lockState;

        if (!Settings.Enabled) requireEnabled.ForEach(l => l.Lock());
        else if (!Settings.EnableHeartDoors)
        {
            requireEnabled.ForEach(l => l.Unlock());
            requireHeartDoors.ForEach(l => l.Lock());
        }
        else requireEnabled.ForEach(l => l.Unlock());
    }

    internal void ApplySettings(RandomizerSettings settings)
    {
        factory.SetMenuValues(settings);
        SetEnabledColor();
        UpdateLocks();
    }
}
