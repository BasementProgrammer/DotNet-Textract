using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Intro_To_Textract.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Intro_To_Textract.Controllers
{
    public class Textract : Controller
    {
        private const string _bucketName = "{YourBucketNameHere}";

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

        public IActionResult UploadForm()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadForm(IFormFile file)
        {
            string ObjectName = SaveFile(file);
            return RedirectToAction("ViewForm", new { objectKey = ObjectName });

        }


        public async Task<IActionResult> ViewForm(string objectKey)
        {
            ExtractedFormData formData = new ExtractedFormData();

            formData.FormValues = await ScanForForm(objectKey);

            return View(formData);
        }

        public async Task<List<KeyValuePair<string, string>>> ScanForForm(string fileKey)
        {
            var FormValues = new List<KeyValuePair<string, string>>();

            Amazon.Textract.AmazonTextractClient textract = new Amazon.Textract.AmazonTextractClient();

            var textractResults = await textract.AnalyzeDocumentAsync(new Amazon.Textract.Model.AnalyzeDocumentRequest()
            {
                Document = new Amazon.Textract.Model.Document()
                {
                    S3Object = new Amazon.Textract.Model.S3Object()
                    {
                        Bucket = _bucketName,
                        Name = fileKey
                    }
                },
                FeatureTypes = new List<string>() { "FORMS" }

            });

            var KeyValueElements = (from x in textractResults.Blocks
                                    where x.BlockType == Amazon.Textract.BlockType.KEY_VALUE_SET
                                    select x).ToArray();


            foreach (var keyValue in KeyValueElements)
            {
                StringBuilder keyName = new StringBuilder();
                StringBuilder valueResult = new StringBuilder();

                var keyIdBlock = (from k in keyValue.Relationships
                                  where k.Type == Amazon.Textract.RelationshipType.CHILD
                                  select k).FirstOrDefault();

                var ValueIdBlock = (from k in keyValue.Relationships
                                    where k.Type == Amazon.Textract.RelationshipType.VALUE
                                    select k).FirstOrDefault();

                if (keyIdBlock != null)
                {

                    foreach (string keyId in keyIdBlock.Ids)
                    {
                        var keyElement = (from k in textractResults.Blocks
                                          where k.Id == keyId
                                          select k).FirstOrDefault();

                        keyName.Append(keyElement.Text + " ");
                    }
                }

                if (ValueIdBlock != null)
                {
                    var valueElement = (from x in textractResults.Blocks
                                        where x.Id == ValueIdBlock.Ids[0]
                                        select x).FirstOrDefault();

                    if (valueElement.Relationships.Count > 0)
                    {
                        foreach (var valuePart in valueElement.Relationships[0].Ids)
                        {
                            var valuePartBlock = (from x in textractResults.Blocks
                                                  where x.Id == valuePart
                                                  select x).FirstOrDefault();
                            valueResult.Append(valuePartBlock.Text + " ");
                        }
                    }
                }

                string formKey = keyName.ToString();
                string forValue = valueResult.ToString();
                if (!string.IsNullOrEmpty(formKey) && !string.IsNullOrEmpty(forValue))
                {
                    FormValues.Add(new KeyValuePair<string, string>(formKey, forValue));
                }
            }

            return FormValues;
        }
    }
}
