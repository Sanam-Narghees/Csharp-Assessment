using System;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

class TimeEntry
{
    [JsonProperty("EmployeeName")]
    public string EmployeeName { get; set; }
    
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("StarTimeUtc")]
    public DateTime StarTimeUtc { get; set; }
    
    [JsonProperty("EndTimeUtc")]
    public DateTime EndTimeUtc { get; set; }
    
    public double TimeWorkedSeconds => (EndTimeUtc - StarTimeUtc).TotalSeconds;
}

class Program
{
    static async Task Main()
    {
        try
        {
            string apiUrl = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";
            HttpClient client = new HttpClient();

            Console.WriteLine("Fetching data from API...");
            string json = await client.GetStringAsync(apiUrl);
            Console.WriteLine($"Raw JSON data: {json.Substring(0, Math.Min(100, json.Length))}...");

            List<TimeEntry> entries = JsonConvert.DeserializeObject<List<TimeEntry>>(json);
            Console.WriteLine($"Found {entries?.Count ?? 0} time entries");

            // Create a list with a concrete type instead of anonymous type
            var employeeTotals = entries?
                .Where(e => !string.IsNullOrEmpty(e.EmployeeName ?? e.Name))
                .GroupBy(e => e.EmployeeName ?? e.Name)
                .Select(g => new EmployeeTotal
                {
                    Name = g.Key,
                    TotalHours = g.Sum(e => e.TimeWorkedSeconds) / 3600.0
                })
                .OrderByDescending(e => e.TotalHours)
                .ToList() ?? new List<EmployeeTotal>();

            Console.WriteLine($"Processed {employeeTotals.Count} employees");

            Console.WriteLine("Generating HTML table...");
            string html = "<html><head><style>" +
                          "table { border-collapse: collapse; width: 60%; font-family: Arial; }" +
                          "th, td { border: 1px solid #999; padding: 8px; text-align: left; }" +
                          ".low-hours { background-color:rgb(98, 98, 144); }" +
                          "</style></head><body>" +
                          "<h2>Employee Work Summary</h2><table>" +
                          "<tr><th>Employee Name</th><th>Total Hours</th></tr>";

            foreach (var emp in employeeTotals)
            {
                string rowClass = emp.TotalHours < 100 ? " class='low-hours'" : "";
                html += $"<tr{rowClass}><td>{emp.Name}</td><td>{emp.TotalHours:F2}</td></tr>";
            }

            html += "</table></body></html>";

            File.WriteAllText("output.html", html);
            Console.WriteLine("✅ HTML file saved as output.html");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}

// Add this class to replace the anonymous type
class EmployeeTotal
{
    public string Name { get; set; }
    public double TotalHours { get; set; }
}