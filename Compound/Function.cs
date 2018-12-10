using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Compound
{
    public class Functions
    {
        private readonly APIGatewayProxyResponse _errorResponseUserAlreadyExists = new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.Conflict,
            Body = "User Already Exists",
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "text/plain" },
                { "Access-Control-Allow-Origin", "*"}
            }
        };

        private readonly APIGatewayProxyResponse _errorResponseBadRequest = new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Body = "",
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "text/plain" },
                { "Access-Control-Allow-Origin", "*"}
            }
        };

        private readonly APIGatewayProxyResponse _successResponse = new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.Created,
            Body = "User Created Successfully",
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "text/plain" },
                { "Access-Control-Allow-Origin", "*"}
            }
        };

        private readonly AmazonS3Client _s3Client;

        private readonly PutObjectRequest _putUserRequest = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = "",
            ContentBody = "",
            ContentType = "application/json"
        };

        private const string BucketName = "compound-users";

        public Functions()
        {
            _s3Client = new AmazonS3Client(Amazon.RegionEndpoint.EUCentral1);
        }

        ~Functions()
        {
            _s3Client.Dispose();
        }

        private readonly WebClient _recaptchaValidationClient = new WebClient();

        private const string Secret = "6LeRvH8UAAAAAPkSxcx-a4Xxa9QHWUocNxhI-8SL";

        public class RecaptchaValidationResponse
        {
            [JsonProperty("success")]
            public bool Success;
        }

        private bool IsRecaptchaTokenValid(string recaptchaToken)
        {
            try
            {
                var result = _recaptchaValidationClient.DownloadString(
                    $"https://www.google.com/recaptcha/api/siteverify?secret={Secret}&response={recaptchaToken}");
                var response = JsonConvert.DeserializeObject<RecaptchaValidationResponse>(result);
                return response != null && response.Success;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public async Task<APIGatewayProxyResponse> Post(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                if (request == null)
                {
                    _errorResponseBadRequest.Body = "Could Not Understand Request";
                    return _errorResponseBadRequest;
                }

                var user = JsonConvert.DeserializeObject<User>(request.Body);
                if (user == null)
                {
                    _errorResponseBadRequest.Body = "Could Not Understand Request Body";
                    return _errorResponseBadRequest;
                }

                var recaptchaToken = user.Custom;
                user.Custom = "";
                if (string.IsNullOrEmpty(recaptchaToken))
                {
                    _errorResponseBadRequest.Body = "Missing Recaptcha Token";
                    return _errorResponseBadRequest;
                }

                if (!IsRecaptchaTokenValid(recaptchaToken))
                {
                    _errorResponseBadRequest.Body = "Invalid Recaptcha Token";
                    return _errorResponseBadRequest;
                }

                var fileName = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(user.EmailAddress));
                var filePath = $"users/{fileName}.json";

                try
                {
                    await _s3Client.GetObjectMetadataAsync(BucketName, filePath);
                    return _errorResponseUserAlreadyExists; //expecting a not-found exception
                }
                catch(Exception exception)
                {
                    // ignored
                    context.Logger.Log(exception.Message + " " + exception.StackTrace);
                }

                user.IpAddress = request.Headers["X-Forwarded-For"];
                user.UserAgent = request.Headers["User-Agent"];

                _putUserRequest.ContentBody = JsonConvert.SerializeObject(user);
                _putUserRequest.Key = filePath;

                var result = await _s3Client.PutObjectAsync(_putUserRequest);
                if (result.HttpStatusCode == HttpStatusCode.OK)
                {
                    return _successResponse;
                }
                
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int) result.HttpStatusCode,
                    Body = "Could Not Create User File",
                    Headers = new Dictionary<string, string> {{"Content-Type", "text/plain"}}
                };
            }
            catch (Exception e)
            {
                _errorResponseBadRequest.Body = e.Message + " - " + e.StackTrace;
                return _errorResponseBadRequest;
            }
        }
    }
}
