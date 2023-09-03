using Amazon.S3;
using Amazon.S3.Model;
using Aws_S3.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aws_S3.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class BucketsController : ControllerBase
    {
        private readonly IAmazonS3 _amazonS3;

        public BucketsController(IAmazonS3 amazonS3)
        {
            _amazonS3 = amazonS3;
        }
        [HttpPost]
        public async Task<IActionResult> CreateBucketAsync(string bucketName)
        {
            bool bucketExist = await _amazonS3.DoesS3BucketExistAsync(bucketName);
            if (bucketExist)
                return BadRequest($"Bucked {bucketName} already exist");

            await _amazonS3.PutBucketAsync(bucketName);
            return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> GetAllBucketAsync()
        {
            return Ok(await _amazonS3.ListBucketsAsync());
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteBucketAsync(string bucketName)
        {
            await _amazonS3.DeleteBucketAsync(bucketName);
            return NoContent();
        }
        [HttpPost]
        public async Task<IActionResult> UploadFileAsync(IFormFile file, string bucketName, string? prefix)
        {
            bool bucketExist = await _amazonS3.DoesS3BucketExistAsync(bucketName);
            if (!bucketExist)
                return BadRequest($"Bucked {bucketName} already exist");

            PutObjectRequest request = new PutObjectRequest()
            {
                BucketName = bucketName,
                Key = string.IsNullOrWhiteSpace(prefix) ? file.FileName : $"{prefix?.TrimEnd('/')}/{file.FileName}",
                InputStream = file.OpenReadStream()
            };

            request.Metadata.Add("Content-type", file.ContentType);
            await _amazonS3.PutObjectAsync(request);
            return Ok($"{prefix?.TrimEnd('/')}/{file.FileName} uploaded to s3 succesfully");
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFilesAsync(string bucketName, string? prefix)
        {
            bool bucketExist = await _amazonS3.DoesS3BucketExistAsync(bucketName);
            if (!bucketExist)
                return BadRequest($"Bucked {bucketName} already exist");

            ListObjectsV2Request request = new ListObjectsV2Request()
            {
                BucketName = bucketName,
                Prefix = prefix
            };

            ListObjectsV2Response response = await _amazonS3.ListObjectsV2Async(request);
            List<S3ObjectDTO> s3Objects = response.S3Objects.Select(item =>
            {
                GetPreSignedUrlRequest urlReuqest = new GetPreSignedUrlRequest()
                {
                    BucketName = bucketName,
                    Key = item.Key,
                    Expires = DateTime.UtcNow.AddMinutes(1)
                };
                return new S3ObjectDTO()
                {
                    Name = item.Key,
                    Url = _amazonS3.GetPreSignedURL(urlReuqest)
                };
            }).ToList();
            return Ok(s3Objects);

        }
        [HttpDelete]
        public async Task<IActionResult> DeleteFileAsync(string bucketName,string fileName)
        {
            bool bucketExist = await _amazonS3.DoesS3BucketExistAsync(bucketName);
            if (!bucketExist)
                return BadRequest($"Bucked {bucketName} already exist");

            await _amazonS3.DeleteObjectAsync(bucketName, fileName);
            return NoContent();
        }
        [HttpGet]
        public async Task<IActionResult> GetByFileNameAsync(string bucketName,string fileName)
        {
            bool bucketExist = await _amazonS3.DoesS3BucketExistAsync(bucketName);
            if (!bucketExist)
                return BadRequest($"Bucked {bucketName} already exist");

            GetObjectResponse response=await _amazonS3.GetObjectAsync(bucketName, fileName);
            return File(response.ResponseStream, response.Headers.ContentType);
        }
    }
}
