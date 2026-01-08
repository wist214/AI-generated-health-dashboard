using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Reflection;

namespace HealthAggregatorApi.Functions;

/// <summary>
/// Serves static files (dashboard HTML, CSS, JS) for the web interface.
/// </summary>
public class StaticFilesFunctions
{
    [Function("ServeIndex")]
    public async Task<HttpResponseData> ServeIndex(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ui")] HttpRequestData req)
    {
        return await ServeStaticFile(req, "index.html", "text/html");
    }

    [Function("ServeIndexAlt")]
    public async Task<HttpResponseData> ServeIndexAlt(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "index.html")] HttpRequestData req)
    {
        return await ServeStaticFile(req, "index.html", "text/html");
    }

    [Function("ServeStyles")]
    public async Task<HttpResponseData> ServeStyles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "css/styles.css")] HttpRequestData req)
    {
        return await ServeStaticFile(req, "css/styles.css", "text/css");
    }

    [Function("ServeStressModal")]
    public async Task<HttpResponseData> ServeStressModal(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "js/stress-modal.js")] HttpRequestData req)
    {
        return await ServeStaticFile(req, "js/stress-modal.js", "application/javascript");
    }

    [Function("ServeWorkoutsModal")]
    public async Task<HttpResponseData> ServeWorkoutsModal(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "js/workouts-modal.js")] HttpRequestData req)
    {
        return await ServeStaticFile(req, "js/workouts-modal.js", "application/javascript");
    }

    private static async Task<HttpResponseData> ServeStaticFile(HttpRequestData req, string fileName, string contentType)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyLocation = Path.GetDirectoryName(assembly.Location) ?? "";
        
        // Try multiple paths for the dashboard files
        var possiblePaths = new[]
        {
            Path.Combine(assemblyLocation, "dashboard", fileName),
            Path.Combine(assemblyLocation, "..", "dashboard", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "dashboard", fileName),
            Path.Combine(AppContext.BaseDirectory, "dashboard", fileName)
        };

        string? filePath = null;
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                filePath = path;
                break;
            }
        }

        if (filePath == null)
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            await response.WriteStringAsync($"File not found: {fileName}. Searched paths: {string.Join(", ", possiblePaths)}");
            return response;
        }

        var content = await File.ReadAllTextAsync(filePath);
        var okResponse = req.CreateResponse(HttpStatusCode.OK);
        okResponse.Headers.Add("Content-Type", contentType);
        okResponse.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
        await okResponse.WriteStringAsync(content);
        return okResponse;
    }
}
