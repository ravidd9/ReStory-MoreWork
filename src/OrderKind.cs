namespace ReStoryMoreWork;

/// <summary>The kinds of Outlink Express order the mod can ask the game's order generator for.</summary>
internal enum OrderKind
{
    /// <summary>The game's usual mix: cleaning and repairs, sometimes painting, stickers or hacking on top.</summary>
    Normal,
    CleaningOnly,
    RepairOnly,
    Painting,
    Hacking,
    Stickers
}
