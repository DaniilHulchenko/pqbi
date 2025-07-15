using Newtonsoft.Json;
using Npgsql.Replication.PgOutput.Messages;
using PQBI.Web.Controllers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PQBI.IntegrationTests.Scenarios;

public abstract class ScenarioBase
{
    public ScenarioBase()
    {
        //BaseUrl = baseAuthenticationUrl; //https://localhost:44301/api
        //AuthenticateUrl = $"{BaseUrl}api/TokenAuth/Authenticate";
    }

    //protected string AuthenticateUrl { get; }

    //public string BaseUrl { get; }


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



        Console.WriteLine($"Valid scenarios: {ScenarioName} started.");
        await RunScenario();
        Console.WriteLine($"Valid scenarios: {ScenarioName} finished successfully.");


        Console.WriteLine($"Post run of {ScenarioName} started.");
        await PostRun();
        Console.WriteLine($"Post run of {ScenarioName} ended succeffully.");
    }

    protected async Task<TDto> Get<TDto>(string url, bool withResponse, string token = null)
    {
        using (HttpClient client = new HttpClient())
        {

            if (token is not null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            HttpResponseMessage response = await client.GetAsync(url);

            // Check the response status
            if (response.IsSuccessStatusCode)
            {
                var res = default(TDto);
                if (withResponse)
                {
                    res = await response.Content.ReadFromJsonAsync<TDto>();
                }

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

                var responseData = System.Text.Json.JsonSerializer.Deserialize<TResponse>(responseContent, options);
                return responseData;
            }

            throw new Exception($"Failed to perform DELETE request to {url}");
        }

    }

    protected async Task<TResponse> RunPostCommand<TRequest, TResponse>(string url, TRequest request , string token=null) where TRequest : class
    {
        return await RunPutOrPostCommand<TRequest, TResponse>(url, request, isPostRequest: true, token);

    }

    protected async Task<TResponse> RunPutCommand<TRequest, TResponse>(string url, TRequest request) where TRequest : class
    {

        return await RunPutOrPostCommand<TRequest, TResponse>(url, request, isPostRequest: false);
    }

    private async Task<TResponse> RunPutOrPostCommand<TRequest, TResponse>(string url, TRequest request, bool isPostRequest = true, string token = null)
    {
        using (HttpClient client = new HttpClient())
        {
            if (token is not null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");

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


    protected void DisplayTable(string[] headers, string[,] rows)
    {
        int[] columnWidths = new int[headers.Length];

        for (int i = 0; i < headers.Length; i++)
        {
            columnWidths[i] = headers[i].Length;
            for (int j = 0; j < rows.GetLength(0); j++)
            {
                if (rows[j, i].Length > columnWidths[i])
                {
                    columnWidths[i] = rows[j, i].Length;
                }
            }
        }

        for (int i = 0; i < columnWidths.Length; i++)
        {
            columnWidths[i] += 2;
        }

        PrintBorder(columnWidths);

        Console.Write("|");
        for (int i = 0; i < headers.Length; i++)
        {
            int paddingLeft = (columnWidths[i] - headers[i].Length) / 2;
            int paddingRight = columnWidths[i] - headers[i].Length - paddingLeft;
            Console.Write(new string(' ', paddingLeft) + headers[i] + new string(' ', paddingRight) + "|");
        }
        Console.WriteLine();

        PrintBorder(columnWidths);

        for (int i = 0; i < rows.GetLength(0); i++)
        {
            Console.Write("|");
            for (int j = 0; j < rows.GetLength(1); j++)
            {
                Console.Write(" " + rows[i, j].PadRight(columnWidths[j] - 2) + " |");
            }
            Console.WriteLine();
        }

        PrintBorder(columnWidths);
    }

    protected void PrintBorder(int[] columnWidths)
    {
        Console.Write("+");
        for (int i = 0; i < columnWidths.Length; i++)
        {
            Console.Write(new string('-', columnWidths[i]) + "+");
        }
        Console.WriteLine();
    }




}



//public class Rootobject
//{
//    public Result result { get; set; }
//    public object targetUrl { get; set; }
//    public bool success { get; set; }
//    public object error { get; set; }
//    public bool unAuthorizedRequest { get; set; }
//    public bool __abp { get; set; }

//}

//public class Result
//{
//    public Component[] components { get; set; }
//}

//public class Component
//{
//    public Network[] networks { get; set; }
//    public string name { get; set; }
//    public string guid { get; set; }
//    public string[] parameterNames { get; set; }

//public string RootTagName { get; set; }
//public string[] Labels { get; set; }

//}

//public class Network
//{
//    public int id { get; set; }
//    public string name { get; set; }
//    public object channels { get; set; }
//    public Feeder[] feeders { get; set; }
//}

//public class Feeder
//{
//    public int id { get; set; }
//    public string name { get; set; }
//    public Channel[] channels { get; set; }
//}

//public class Channel
//{
//    public int id { get; set; }
//    public string value { get; set; }
//}