using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;

namespace AzureBlobUploadApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _config;

        public HomeController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var expectedUser = _config["AppCredentials:Username"];
            var expectedPass = _config["AppCredentials:Password"];

            if (username == expectedUser && password == expectedPass)
            {
                HttpContext.Session.SetString("IsAuthenticated", "true");
                return RedirectToAction("Upload");
            }

            ViewBag.Error = "Invalid credentials";
            return View("Index");
        }

        [HttpGet]
        public IActionResult Upload()
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
                return RedirectToAction("Index");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
                return RedirectToAction("Index");

            if (file != null && file.Length > 0)
            {
                string connString = _config["AzureStorage:ConnectionString"];
                string containerName = _config["AzureStorage:ContainerName"];

                var blobServiceClient = new BlobServiceClient(connString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                
                // Ensure the container exists
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient(file.FileName);

                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                ViewBag.Message = $"File '{file.FileName}' uploaded successfully!";
            }
            else
            {
                ViewBag.Error = "Please select a file to upload.";
            }

            return View("Upload");
        }
    }
}