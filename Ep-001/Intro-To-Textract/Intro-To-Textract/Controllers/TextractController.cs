using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Mvc;

namespace Intro_To_Textract.Controllers
{
    public class Textract : Controller
    {
        private const string _bucketName = "intro-to-textract-basementprogrammer";

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
            return View();
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
    }
}
