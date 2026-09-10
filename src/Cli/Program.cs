using Core;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

Console.OutputEncoding = Encoding.UTF8;

const string Student = "Сеньків Роксолана";
const string Group = "ФЕІ-36";
const string Domain = "Склад";

EnvironmentReport report = EnvironmentInfo.Collect();

if (args.Contains("--json"))
{
    var options = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    var jsonPayload = new
    {
        Application = "CrossApp",
        Student,
        Group,
        Domain,
        report.OsDescription,
        report.FrameworkDescription,
        report.ProcessArchitecture,
        report.DetectedRid,
        report.ReportedRid,
        report.BaseDirectory,
        report.BuildNote
    };

    string jsonOutput = JsonSerializer.Serialize(jsonPayload, options);
    Console.WriteLine(jsonOutput);
}
else
{
    Console.WriteLine("CrossApp - інформація про середовище");
    Console.WriteLine($"Студент: {Student}, група {Group}");
    Console.WriteLine(new string('-', 70));
    Console.WriteLine($"| {"Параметр",-22} | {"Значення",-41} |");
    Console.WriteLine(new string('-', 70));
    Console.WriteLine($"| {"OC (OSDescription)",-22} | {report.OsDescription,-41} |");
    Console.WriteLine($"| {"Runtime",-22} | {report.FrameworkDescription,-41} |");
    Console.WriteLine($"| {"Архітектура процесу",-22} | {report.ProcessArchitecture,-41} |");
    Console.WriteLine($"| {"RID (визначено)",-22} | {report.DetectedRid,-41} |");
    Console.WriteLine($"| {"RID (від .NET)",-22} | {report.ReportedRid,-41} |");
    Console.WriteLine($"| {"Каталог застосунку",-22} | {report.BaseDirectory,-41} |");
    Console.WriteLine($"| {"Примітка збірки",-22} | {report.BuildNote,-41} |");
    Console.WriteLine(new string('-', 70));
    Console.WriteLine($"Предметна область: {Domain} (товари, партії, залишки, переміщення)");
}