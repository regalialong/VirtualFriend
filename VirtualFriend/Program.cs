using AssetsTools.NET.Extra;
using Serilog;

var manager = new AssetsManager();
var log = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Console()
    .CreateLogger();

if (args.Length < 1)
{
    log.Fatal("Missing a file parameter");
    return;
}

var bunInst = manager.LoadBundleFile(args[0]);
var afileInst = manager.LoadAssetsFileFromBundle(bunInst, 0);

log.Information("Detected CE products:");
DumpCeProducts(manager, afileInst);
return;


void DumpCeProducts(AssetsManager manager, AssetsFileInstance afileInst)
{
    var afile = afileInst.file;
    long PathID = 0;
    foreach (var monoInfo in afile.GetAssetsOfType(AssetClassID.MonoScript))
    {
        var baseField = manager.GetBaseField(afileInst, monoInfo);
        if (baseField["m_Name"].AsString != "UdonProduct") continue;
        PathID = monoInfo.PathId;
        break;
    }

    foreach (var monoInfo in afile.GetAssetsOfType(AssetClassID.MonoBehaviour))
    {
        var baseField = manager.GetBaseField(afileInst, monoInfo);
        // ReSharper disable once CompareOfFloatsByEqualityOperator - float imprecision should equal out
        if (baseField["m_Script.m_PathID"].AsFloat != PathID) continue;
        log.Information("{Name} ({ID}) '{Description}'", baseField["_Name"].AsString,
            baseField["_ID"].AsString, baseField["_Description"].AsString);
    }
}