using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

static string webRoot = @"D:\home\site\wwwroot";
// Point to d:\local\temp (local disk), avoid going over the network to storage
static string tempFolder = Environment.GetEnvironmentVariable("TEMP");

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    var country = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "country", true) == 0)
        .Value ?? "us";
    var countrySafe = Regex.Replace(country, @"\W", "");

    if (!Directory.Exists(tempFolder))
    {
        Directory.CreateDirectory(tempFolder);
    }

    var filePath = $@"{tempFolder}\{System.Guid.NewGuid().ToString()}.jpg";
    using (var stream = await req.Content.ReadAsStreamAsync())
    {
        var fileStream = new FileStream(filePath, FileMode.Create);
        await stream.CopyToAsync(fileStream);
        fileStream.Close();
    }
    
    var processStartInfo = new ProcessStartInfo
    {
        FileName = $@"{webRoot}\openalpr_64\alpr.exe",
        Arguments = $@"-c {countrySafe} --json {filePath}",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        WorkingDirectory = $@"{webRoot}\openalpr_64"
    };

    try {
        var process = Process.Start(processStartInfo);
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content =  new StringContent(output, Encoding.UTF8, "application/json")
        };
    } catch {
        return new HttpResponseMessage(HttpStatusCode.InternalServerError);
    } finally {
        File.Delete(filePath);
    }
}
