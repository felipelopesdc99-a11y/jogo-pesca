using FishingIdle.GameService.Core;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>M18-T03: the player picks a name and an avatar.</summary>
public sealed class IdentityTests
{
    [Fact]
    public void A_valid_name_is_trimmed_and_saved()
    {
        var (game, _, _) = TestSupport.NewGame();
        Assert.Equal("Felipe Pesca", game.Profile.Rename("  Felipe   Pesca ").Value);
        Assert.Equal("Felipe Pesca", game.Player.GetPlayer().PlayerName);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("um nome comprido demais aqui")]
    [InlineData("<script>")]
    [InlineData("")]
    public void Invalid_names_are_refused(string name)
    {
        var (game, _, _) = TestSupport.NewGame();
        var before = game.Player.GetPlayer().PlayerName;
        Assert.Equal(ServiceError.InvalidName, game.Profile.Rename(name).Error);
        Assert.Equal(before, game.Player.GetPlayer().PlayerName);
    }

    [Fact]
    public void Only_configured_avatars_can_be_picked()
    {
        var (game, _, _) = TestSupport.NewGame();
        Assert.True(game.Profile.SetAvatar("avatar_03").Succeeded);
        Assert.Equal("avatar_03", game.Player.GetPlayer().AvatarId);
        Assert.Equal(ServiceError.AvatarNotFound, game.Profile.SetAvatar("avatar_99").Error);
        Assert.Equal(6, game.Profile.GetProfile().Avatars.Count);
    }
}
