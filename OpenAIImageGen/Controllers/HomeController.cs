using Microsoft.AspNetCore.Mvc;
using OpenAIImageGen.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace OpenAIImageGen.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient httpClient;

        public HomeController(ILogger<HomeController> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", configuration["OpenAIAPIKey"]);
            this.httpClient = httpClient;

        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<string> Index(HomeIndexViewModel model)
        {
            using var form = new MultipartFormDataContent();

            var fileContent = new StreamContent(model.File.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(model.File.ContentType);
            form.Add(fileContent, "image[]", "image.png");

            form.Add(new StringContent("gpt-image-1"), "model");

            string prompt = "restyle this image using ghibli studio style, keep all the details";

            form.Add(new StringContent(prompt), "prompt");

            var url = "https://api.openai.com/v1/images/edits";
            var response = await httpClient.PostAsync(url, form);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(jsonResponse);
            var base64 = doc.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString();
            var image = Convert.FromBase64String(base64!);

            string directory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes");
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(model.File.FileName);
            var extension = Path.GetExtension(model.File.FileName);
            string fileName = $"{nameWithoutExtension} {DateTime.Now.ToString("dd-MM-yyyy hhmmss")}{extension}";

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string completeRoute = Path.Combine(directory, fileName);

            await System.IO.File.WriteAllBytesAsync(completeRoute, image);

            return fileName;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
