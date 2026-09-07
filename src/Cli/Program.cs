using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

Console.OutputEncoding = Encoding.UTF8;

var systemInfo = new
{
    Application = "CrossApp",
    Student = "Сеньків Роксолана",
    Group = "ФЕІ-36",
    Domain = "Склад",
    OSDescription = RuntimeInformation.OSDescription,
    OSVersion = Environment.OSVersion.ToString(),
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    DotNetVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    BaseDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory
};

if (args.Contains("--json"))
{
    var options = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };
    string jsonOutput = JsonSerializer.Serialize(systemInfo, options);
    Console.WriteLine(jsonOutput);
}
else
{
    Console.WriteLine("CrossApp - практикум з крос-платформного програмування");
    Console.WriteLine($"Студент: {systemInfo.Student}, група {systemInfo.Group}");
    Console.WriteLine(new string('-', 70));
    Console.WriteLine($"| {"Параметр",-22} | {"Значення",-41} |");
    Console.WriteLine(new string('-', 70));
    Console.WriteLine($"| {"OC (OSDescription)",-22} | {systemInfo.OSDescription,-41} |");
    Console.WriteLine($"| {"OC (Environment)",-22} | {systemInfo.OSVersion,-41} |");
    Console.WriteLine($"| {"Архітектура процесу",-22} | {systemInfo.Architecture,-41} |");
    Console.WriteLine($"| {"Версія .NET (CLR)",-22} | {systemInfo.DotNetVersion,-41} |");
    Console.WriteLine($"| {"Runtime",-22} | {systemInfo.Runtime,-41} |");
    Console.WriteLine($"| {"Каталог застосунку",-22} | {systemInfo.BaseDirectory,-41} |");
    Console.WriteLine($"| {"Поточний каталог",-22} | {systemInfo.CurrentDirectory,-41} |");
    Console.WriteLine(new string('-', 70));
    Console.WriteLine($"Предметна область: {systemInfo.Domain} (товари, партії, залишки, переміщення)");
}