using System.Reflection;
using EpicorRestAPI;

var rest = new EpicorRest
{
    AppPoolHost = "epicor-srv2019.gpe.com",
    AppPoolInstance = "gpedev",
    Company = "GPEHZ",
    UserName = "linyaochen",
    Password = "15879361481LYC",
    APIKey = "2zj4lBIO46IrIvaCLHyxAsmuKcr1hiyMi0Se9XBnYSdd7",
    APIVersion = EpicorRestVersion.V2,
    License = EpicorLicenseType.Default
};

Console.WriteLine("== string properties after init:");
foreach (var p in typeof(EpicorRest).GetProperties(BindingFlags.Public | BindingFlags.Instance))
{
    try
    {
        var v = p.GetValue(rest);
        if (v is string s) Console.WriteLine($"  {p.Name} = {s}");
    }
    catch (Exception ex) { Console.WriteLine($"  {p.Name} = <err: {ex.GetBaseException().Message}>"); }
}

var mi = typeof(EpicorRest).GetMethod("BuildMethodEndpointURL", BindingFlags.NonPublic | BindingFlags.Instance);
Console.WriteLine("== BuildMethodEndpointURL:");
if (mi != null)
    Console.WriteLine("  " + mi.Invoke(rest, new object[] { "Ice.Lib.MetaFXSvc", "GetApp" }));
else
    Console.WriteLine("  <method not found>");

var baq = typeof(EpicorRest).GetMethod("BuildBAQEndpointURL", BindingFlags.NonPublic | BindingFlags.Instance);
if (baq != null) Console.WriteLine("== BuildBAQEndpointURL: " + baq.Invoke(rest, new object[] { "Test" }));

foreach (var name in new[] { "BoPost", "GetEnvironment" })
{
    var m = typeof(EpicorRest).GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
    Console.WriteLine($"== {name} exists: {m != null}");
}
