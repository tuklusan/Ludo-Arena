using System.Collections.Immutable;

namespace LudoNimArena.Core;

/// <summary>Represents a player with four tokens.</summary>
public sealed class Player
{
    public PlayerColor Color { get; }
    public string DisplayName { get; }
    public string StrategyHint { get; }
    public PlayerControllerType ControllerType { get; }
    public ImmutableArray<Token> Tokens { get; }

    public Player(PlayerColor color, string displayName, string strategyHint,
        PlayerControllerType controllerType = PlayerControllerType.AiPlayer)
    {
        Color = color;
        DisplayName = displayName;
        StrategyHint = strategyHint;
        ControllerType = controllerType;
        Tokens = ImmutableArray.Create(
            new Token(color, 0),
            new Token(color, 1),
            new Token(color, 2),
            new Token(color, 3)
        );
    }

    private Player(Player other, ImmutableArray<Token> tokens)
    {
        Color = other.Color;
        DisplayName = other.DisplayName;
        StrategyHint = other.StrategyHint;
        ControllerType = other.ControllerType;
        Tokens = tokens;
    }

    public Player WithTokens(ImmutableArray<Token> tokens) => new(this, tokens);

    public Token GetToken(int index) => Tokens[index];

    public Token GetTokenById(string tokenId) =>
        Tokens.First(t => t.Id == tokenId);

    public int TokensInYard => Tokens.Count(t => t.State == TokenState.InYard);
    public int TokensOnTrack => Tokens.Count(t => t.State == TokenState.OnSharedTrack);
    public int TokensInHomeLane => Tokens.Count(t => t.State == TokenState.InHomeLane);
    public int TokensFinished => Tokens.Count(t => t.State == TokenState.Finished);
    public bool HasWon => TokensFinished == 4;

    public ImmutableArray<Token> ActiveTokens =>
        Tokens.Where(t => t.State != TokenState.InYard && t.State != TokenState.Finished)
              .ToImmutableArray();

    public override string ToString() => $"{DisplayName} ({Color})";
}
