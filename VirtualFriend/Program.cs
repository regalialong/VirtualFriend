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
var sharedAssetFileInstance = manager.LoadAssetsFileFromBundle(bunInst, 0);
var worldAssetInstance = manager.LoadAssetsFileFromBundle(bunInst, 1);

log.Information("Detected CE products:");
DumpCeProducts();
log.Information("Button calls:");
DumpButtonObjects();
return;


void DumpCeProducts()
{
    var afile = sharedAssetFileInstance.file;
    long PathID = 0;
    foreach (var monoInfo in afile.GetAssetsOfType(AssetClassID.MonoScript))
    {
        var baseField = manager.GetBaseField(sharedAssetFileInstance, monoInfo);
        if (baseField["m_Name"].AsString != "UdonProduct") continue;
        PathID = monoInfo.PathId;
        break;
    }

    foreach (var monoInfo in afile.GetAssetsOfType(AssetClassID.MonoBehaviour))
    {
        var baseField = manager.GetBaseField(sharedAssetFileInstance, monoInfo);
        // ReSharper disable once CompareOfFloatsByEqualityOperator - float imprecision should equal out
        if (baseField["m_Script.m_PathID"].AsFloat != PathID) continue;
        log.Information("{Name} ({ID}) '{Description}'", baseField["_Name"].AsString,
            baseField["_ID"].AsString, baseField["_Description"].AsString);
    }
}

void DumpButtonObjects()
{
    var actionsList = new Dictionary<string, (string, long)>();
    long PathID = 0;
    foreach (var monoInfo in sharedAssetFileInstance.file.GetAssetsOfType(AssetClassID.MonoScript))
    {
        var baseField = manager.GetBaseField(sharedAssetFileInstance, monoInfo);
        if (baseField["m_Name"].AsString != "Button") continue;
        PathID = monoInfo.PathId;
        break;
    }

    if (PathID == 0)
        throw
            new InvalidOperationException(
                "DumpButtonObjects: PathID is 0, parsing failed"); // Sanity check, we need the Button PathID else this operation is bad

    foreach (var monoInfo in worldAssetInstance.file.GetAssetsOfType(AssetClassID.MonoBehaviour))
    {
        var baseField = manager.GetBaseField(worldAssetInstance, monoInfo);

        if (baseField["m_Script.m_PathID"].AsInt != PathID) continue;
        if (baseField["m_OnClick.m_PersistentCalls.m_Calls.Array"].Children.Count == 0) continue;

        // This tree is pretty ugly but in essence, actionable buttons have an OnClick that does a SendCustomEvent
        // And that has a string argument attached like "_OpenStoreListing", this fetches that.
        var buttonStringArg =
            baseField["m_OnClick.m_PersistentCalls.m_Calls.Array"][0]["m_Arguments.m_StringArgument"];
        if (buttonStringArg.IsDummy || buttonStringArg.AsString == "") continue;
        var fatherGameObject = manager.GetExtAsset(worldAssetInstance, baseField["m_GameObject"]);

        actionsList.TryAdd(buttonStringArg.AsString,
            (fatherGameObject.baseField["m_Name"].AsString, fatherGameObject.info.PathId));
    }

    foreach (var buttonObject in actionsList)
    {
        log.Information("{action}: {GameObjectName} ({GameObjectPathID})", buttonObject.Key, buttonObject.Value.Item1,
            buttonObject.Value.Item2);
    }
}