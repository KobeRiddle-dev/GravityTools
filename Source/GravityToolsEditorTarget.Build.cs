using Flax.Build;

public class GravityToolsEditorTarget : GameProjectEditorTarget
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();

        // Reference the modules for editor
        Modules.Add("GravityTools");
        Modules.Add("GravityToolsEditor");
    }
}
