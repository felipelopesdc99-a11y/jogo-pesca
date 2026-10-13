using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Shop;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// OD-070, option c (13/10/2026, M24-T48): rods 1 to 5 ask for 5 / 8 / 12 / 15 / 20 fragments (were 15 / 25 / 35 / 45 /
/// 60). A save that already holds more fragments than the new number assembles the rod when the game opens, without losing
/// a fragment: what is left stays as extras, and a rod already owned keeps its extras untouched.
/// </summary>
public sealed class FewerRodFragmentsTests
{
    [Fact]
    public void A_save_with_more_fragments_than_the_new_number_assembles_on_opening_and_keeps_the_rest()
    {
        var config = TestSupport.CertainCatchConfig();
        var (game, clock, dir) = TestSupport.NewGame(config);
        TestSupport.GiveRod(game, "rod_01");
        var save = game.Session.Save;

        // Held under the old numbers: rod 1 owned with 7 extras; 12 of rod 2 (old 25, new 8); 34 of rod 3 (old 35,
        // new 12: enough for two, but only one rod is assembled); 14 of rod 4 (new 15: not yet).
        save.RodFragments["rod_01"] = 7;
        save.RodFragments["rod_02"] = 12;
        save.RodFragments["rod_03"] = 34;
        save.RodFragments["rod_04"] = 14;
        var inventoryBefore = save.Inventory.Count;
        game.Session.Persist();

        clock.AdvanceSeconds(60);
        var (again, _, _) = TestSupport.NewGame(config, saveDir: dir, clock: clock, skipTutorial: false);
        var after = again.Session.Save;
        Assert.Equal(SaveLoadStatus.Loaded, again.Session.LoadStatus);

        Assert.True(RodFragmentRules.Owns(after, "rod_02"));
        Assert.True(RodFragmentRules.Owns(after, "rod_03"));
        Assert.False(RodFragmentRules.Owns(after, "rod_04"));
        Assert.Equal(inventoryBefore + 2, after.Inventory.Count);
        Assert.Single(after.Inventory, i => i.RodId == "rod_03");

        // Nothing lost: the assembling spent exactly the new number, the rest stays as extras.
        Assert.Equal(7, RodFragmentRules.Have(after, "rod_01"));
        Assert.Equal(12 - 8, RodFragmentRules.Have(after, "rod_02"));
        Assert.Equal(34 - 12, RodFragmentRules.Have(after, "rod_03"));
        Assert.Equal(14, RodFragmentRules.Have(after, "rod_04"));

        // The highest rod assembled goes in use, at Nv.1.
        var equipped = after.EquippedRodItem();
        Assert.Equal("rod_03", equipped.RodId);
        Assert.Equal(1, equipped.Level);

        // One more fragment of rod 4 assembles it in play, as always.
        config.TryGetRod("rod_04", out var rod4);
        var gain = RodFragmentRules.Add(config, after, rod4, 1, clock.UtcNowMs);
        Assert.True(gain.Assembled);
        Assert.Equal(0, gain.Have);
        Assert.Equal("rod_04", after.EquippedRodItem().RodId);
    }
}
