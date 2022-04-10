// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using System.Text;
using teams_status;

var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false);

IConfiguration config = builder.Build();
string iftttKey = config.GetSection("IFTTT").GetValue<string>("AccessKey");

MicrosoftTeamsStatus _lastStatus = MicrosoftTeamsStatus.Unknown;
DateTime _fileLastUpdated = DateTime.MinValue;

var fileName = Path.Combine(Environment.GetFolderPath(
    Environment.SpecialFolder.ApplicationData), @"Microsoft\Teams\logs.txt");
var fileInfo = new FileInfo(fileName);

while (true)
{
    fileInfo.Refresh();
    //Original code from https://github.com/miscalencu/OnlineStatusLight/blob/main/OnlineStatusLight.Application/MicrosoftTeamsService.cs
    if (fileInfo.Exists)
    {
        var fileLastUpdated = fileInfo.LastWriteTime;
        if (fileLastUpdated > _fileLastUpdated)
        {
            var lines = ReadLines(fileName);
            foreach (var line in lines.Reverse())
            {
                var delFrom = " (current state: ";
                var delTo = ") ";

                if (line.Contains(delFrom) && line.Contains(delTo))
                {
                    int posFrom = line.IndexOf(delFrom) + delFrom.Length;
                    int posTo = line.IndexOf(delTo, posFrom);

                    if (posFrom <= posTo)
                    {
                        var info = line.Substring(posFrom, posTo - posFrom);
                        var status = info.Split(" -> ").Last();
                        var newStatus = _lastStatus;

                        switch (status)
                        {
                            case "Available":
                                newStatus = MicrosoftTeamsStatus.Available;
                                break;
                            case "Away":
                                newStatus = MicrosoftTeamsStatus.Away;
                                break;
                            case "Busy":
                            case "OnThePhone":
                                newStatus = MicrosoftTeamsStatus.Busy;
                                break;
                            case "DoNotDisturb":
                                newStatus = MicrosoftTeamsStatus.DoNotDisturb;
                                break;
                            case "BeRightBack":
                                newStatus = MicrosoftTeamsStatus.Away;
                                break;
                            case "Offline":
                                newStatus = MicrosoftTeamsStatus.Offline;
                                break;
                            case "NewActivity":
                                // ignore this - happens where there is a new activity: Message, Like/Action, File Upload
                                // this is not a real status change, just shows the bell in the icon
                                break;
                            default:
                                newStatus = MicrosoftTeamsStatus.Unknown;
                                break;
                        }

                        if (newStatus != _lastStatus)
                        {
                            _lastStatus = newStatus;
                        }
                        break;
                    }
                }
            }
            _fileLastUpdated = fileLastUpdated;
        }
    }
    Console.WriteLine(_lastStatus);
    await KickWemo(_lastStatus);
    await Task.Delay(5000);
}

async Task KickWemo(MicrosoftTeamsStatus status)
{
    string endPointTurnOn = $"https://maker.ifttt.com/trigger/eventstarted/json/with/key/{iftttKey}";
    string endPointTurnOff = $"https://maker.ifttt.com/trigger/eventfinished/json/with/key/{iftttKey}";
    
    if (status == MicrosoftTeamsStatus.Available)
    {
        using var client = new HttpClient();
        var content = await client.GetStringAsync(endPointTurnOff);
    }
    else
    {
        using var client = new HttpClient();
        var content = await client.GetStringAsync(endPointTurnOn);
    }
}

IEnumerable<string> ReadLines(string path)
{
    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0x1000, FileOptions.SequentialScan))
    using (var sr = new StreamReader(fs, Encoding.UTF8))
    {
        string line;
        while ((line = sr.ReadLine()) != null)
        {
            yield return line;
        }
    }
}