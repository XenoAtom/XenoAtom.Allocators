using System.Globalization;
using System.Runtime.CompilerServices;
using VerifyTests.DiffPlex;

static class ModuleInit
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDiffPlex.Initialize(OutputType.Compact);
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        UseProjectRelativeDirectory("Verified");
        DiffEngine.DiffRunner.Disabled = true;
    }
}