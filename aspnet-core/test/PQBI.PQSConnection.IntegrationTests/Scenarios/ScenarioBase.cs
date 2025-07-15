using FluentAssertions;
using Newtonsoft.Json;
using PQBI.PQSConnection.IntegrationTests.Requests;
using System.Net.Http.Json;
using System.Text.Json;

namespace PQBI.PQSConnection.IntegrationTests.Scenarios;

public abstract class ScenarioBase
{
    public ScenarioBase(string baseUrl)
    {
        BaseUrl = baseUrl; //https://localhost:44301/api
        AuthenticateUrl = $"{BaseUrl}/TokenAuth/Authenticate";
    }

    protected string AuthenticateUrl { get; }

    public string BaseUrl { get; }


    public abstract string ScenarioName { get; }
    public abstract string Description { get; }


    protected virtual Task Setup()
    {
        return Task.FromResult(true);
    }

    protected virtual Task PostRun()
    {
        return Task.FromResult(true);
    }


    protected abstract Task RunScenario();


    public async Task StartRunScenario()
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();


        Console.WriteLine($" ------------------------{ScenarioName}----------------------------");
        Console.WriteLine($" ------------------------{ScenarioName}----------------------------");
        Console.WriteLine($" ------------------------{ScenarioName}----------------------------");
        Console.WriteLine($" ------------------------{ScenarioName}----------------------------");

        Console.WriteLine();
        Console.WriteLine();

        Console.WriteLine($"This scenario main purpose: {Description}");

        Console.WriteLine();
        Console.WriteLine();

        Console.WriteLine($"Setup of {ScenarioName} started.");
        await Setup();
        Console.WriteLine($"Setup of {ScenarioName} ended succeffully.");


        Console.WriteLine($"Valid scenarios: {ScenarioName} base url = {BaseUrl} started.");
        await RunScenario();
        Console.WriteLine($"Valid scenarios: {ScenarioName} finished successfully.");


        Console.WriteLine($"Post run of {ScenarioName} started.");
        await PostRun();
        Console.WriteLine($"Post run of {ScenarioName} ended succeffully.");
    }

    protected async Task UserAuthenticationOperation()
    {
        var payload = new AuthenticateModelRequest
        {
            UserNameOrEmailAddress = "admin",
            Password = "PQSpqs12345",
            RememberClient = false,
            SingleSignIn = false,
            ReturnUrl = (string)null,
            CaptchaResponse = (string)null
        };


        var res = await RunPutOrPostCommand<AuthenticateModelRequest, AuthenticationMockResponse>(AuthenticateUrl, payload);
        res.result.Should().NotBeNull();
    }

    protected async Task<TDto> Get<TDto>(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);

            // Check the response status
            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadFromJsonAsync<TDto>();

                return res;
            }

            throw new Exception("Server failed.");
        }
    }
    protected async Task<TResponse> DeleteCommand<TResponse>(string url) where TResponse : class
    {
        using (HttpClient client = new HttpClient())
        {
            var response = await client.DeleteAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var responseData = JsonSerializer.Deserialize<TResponse>(responseContent, options);
                return responseData;
            }

            throw new Exception($"Failed to perform DELETE request to {url}");
        }

    }

    protected async Task<TResponse> RunPostCommand<TRequest, TResponse>(string url, TRequest request) where TRequest : class
    {
        return await RunPutOrPostCommand<TRequest, TResponse>(url, request, isPostRequest: true);

    }

    protected async Task<TResponse> RunPutCommand<TRequest, TResponse>(string url, TRequest request) where TRequest : class
    {

        return await RunPutOrPostCommand<TRequest, TResponse>(url, request, isPostRequest: false);
    }

    private async Task<TResponse> RunPutOrPostCommand<TRequest, TResponse>(string url, TRequest request, bool isPostRequest = true)
    {
        using (HttpClient client = new HttpClient())
        {

            var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = null;

            if (isPostRequest)
            {
                response = await client.PostAsync(url, content);
            }
            else
            {
                response = await client.PutAsync(url, content);
            }

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var instance = JsonConvert.DeserializeObject<TResponse>(responseContent);
                return instance;
            }

            throw new Exception($"Failed Populate in {url}");
        }
    }
}