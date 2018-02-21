using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

static string webRoot = @"D:\home\site\wwwroot";
static string tempFolder = $@"{webRoot}\temp";

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
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
        Arguments = $@"-c us --json {filePath}",
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
