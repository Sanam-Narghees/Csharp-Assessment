using System;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

class TimeEntry
{
    // More flexible property mapping
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
            Console.WriteLine($"Raw JSON preview: {json.Substring(0, Math.Min(100, json.Length))}...");

            List<TimeEntry> entries = JsonConvert.DeserializeObject<List<TimeEntry>>(json);
            Console.WriteLine($"Found {entries?.Count ?? 0} entries");

            var employeeTotals = entries?
                .Where(e => !string.IsNullOrEmpty(e.EmployeeName ?? e.Name))
                .GroupBy(e => e.EmployeeName ?? e.Name)
                .Select(g => new {
                    Name = g.Key,
                    TotalHours = g.Sum(e => e.TimeWorkedSeconds) / 3600.0
                })
                .Where(e => e.TotalHours > 0)
                .OrderByDescending(e => e.TotalHours)
                .ToList();

            Console.WriteLine($"Generating pie chart for {employeeTotals?.Count ?? 0} employees...");

            if (employeeTotals == null || employeeTotals.Count == 0)
            {
                Console.WriteLine("No valid data to generate chart");
                return;
            }

            // Debug: Print the data being used
            Console.WriteLine("Chart data:");
            foreach (var emp in employeeTotals)
            {
                Console.WriteLine($"{emp.Name}: {emp.TotalHours:F2} hours");
            }

            int width = 800, height = 600;
            using (Bitmap bmp = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.WhiteSmoke); // Use light gray background for better visibility
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                Rectangle rect = new Rectangle(50, 50, 500, 500); // Smaller, centered rectangle
                float total = (float)employeeTotals.Sum(e => e.TotalHours);
                float startAngle = 0;
                Random rand = new Random();

                // Draw pie slices
                foreach (var emp in employeeTotals)
                {
                    float sweep = (float)(emp.TotalHours / total) * 360f;
                    Color color = Color.FromArgb(
                        rand.Next(100, 200), // Darker colors
                        rand.Next(100, 200),
                        rand.Next(100, 200)
                    );
                    
                    using (Brush b = new SolidBrush(color))
                    {
                        g.FillPie(b, rect, startAngle, sweep);
                    }
                    startAngle += sweep;
                }

                // Add legend
                int legendX = 580;
                int legendY = 50;
                int legendItemHeight = 20;
                
                foreach (var emp in employeeTotals)
                {
                    Color color = Color.FromArgb(
                        rand.Next(100, 200),
                        rand.Next(100, 200),
                        rand.Next(100, 200)
                    );
                    
                    using (Brush b = new SolidBrush(color))
                    {
                        g.FillRectangle(b, legendX, legendY, 15, 15);
                    }
                    
                    g.DrawString($"{emp.Name} ({emp.TotalHours:F2}h)", 
                                new Font("Arial", 8), 
                                Brushes.Black, 
                                legendX + 20, 
                                legendY);
                    
                    legendY += legendItemHeight;
                }

                // Add title
                g.DrawString("Employee Time Distribution", 
                            new Font("Arial", 14, FontStyle.Bold), 
                            Brushes.DarkBlue, 
                            width / 2 - 100, 
                            10);

                bmp.Save("output.png", ImageFormat.Png);
                Console.WriteLine("✅ Pie chart saved as output.png");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }
}