using Flax.Build;

public class GravityToolsTarget : GameProjectTarget
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();

        // Reference the modules for game
        Modules.Add("GravityTools");
    }
}
