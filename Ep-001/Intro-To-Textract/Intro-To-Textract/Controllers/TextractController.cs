using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Intro_To_Textract.Controllers
{
    public class Textract : Controller
    {
        private const string _bucketName = "{YourBucketNameHere}"
        private IAmazonS3 _s3 = new AmazonS3Client(RegionEndpoint.USWest2);

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult UploadFile ()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadFile(IFormFile file)
        {
            string ObjectName = SaveFile(file);
            //return View();
            return RedirectToAction("ViewFile", new { objectKey = ObjectName });

        }

        public string SaveFile(IFormFile file)
        {
            TransferUtility utility = new TransferUtility(_s3);
            TransferUtilityUploadRequest request = new TransferUtilityUploadRequest();
            string objectName = string.Format("{0}{1}", new[] { Guid.NewGuid().ToString(), Path.GetExtension(file.FileName) });

            request.BucketName = _bucketName;
            request.Key = objectName;
            try
            {
                request.InputStream = file.OpenReadStream();
                utility.Upload(request);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
            }
            finally
            {
                request.InputStream.Close();
            }
            return objectName;
        }

        public async Task<IActionResult> ViewFile(string objectKey)
        {
            string documentText = await ScanForText(objectKey);

            ViewBag.DisplayText = documentText;

            return View();
        }

        public async Task<string> ScanForText(string fileKey)
        {
            StringBuilder sb = new StringBuilder();

            Amazon.Textract.AmazonTextractClient textract = new Amazon.Textract.AmazonTextractClient();

            var textractResults = await textract.DetectDocumentTextAsync(new Amazon.Textract.Model.DetectDocumentTextRequest()
            {
                Document = new Amazon.Textract.Model.Document()
                {
                    S3Object = new Amazon.Textract.Model.S3Object()
                    {
                        Bucket = _bucketName,
                        Name = fileKey
                    }
                }
            });

            foreach (var block in textractResults.Blocks)
            {
                if (block.BlockType == Amazon.Textract.BlockType.LINE)
                {
                    sb.AppendLine(block.Text);
                }
            }

            return sb.ToString();
        }
    }
}
