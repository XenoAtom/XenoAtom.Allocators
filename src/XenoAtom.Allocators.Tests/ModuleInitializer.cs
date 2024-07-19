using System.Globalization;
using System.Runtime.CompilerServices;

static class ModuleInit
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        UseProjectRelativeDirectory("Verified");
        DiffEngine.DiffRunner.Disabled = true;
    }
}