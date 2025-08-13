using System.Net.Http.Headers;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            string subscriptionKeyPath = Path.Combine(AppContext.BaseDirectory, "subscriptionkey.txt");

            if (!File.Exists(subscriptionKeyPath))
            {
                Console.WriteLine("subscriptionkey.txt dosyası bulunamadı!");
                return;
            }

            string subscriptionKey = File.ReadAllText(subscriptionKeyPath, Encoding.UTF8)
                                .Replace("\uFEFF", "") // BOM temizle
                                .Trim();

            if (string.IsNullOrWhiteSpace(subscriptionKey))
            {
                Console.WriteLine("API key boş veya sadece boşluklardan oluşuyor!");
                return;
            }
            string region = "westeurope";
            string tokenEndPoint = $"https://{region}.api.cognitive.microsoft.com/sts/v1.0/issuetoken";


            var token = await GetTokenAsync(subscriptionKey, tokenEndPoint);
            string userText = "Merhaba arkadaşla, bu bir deneme mesajıdır. Amacımız Microsoft Azure kullanarak metni sese dönüştürmektir. Umarım başarılı olabiliriz.";
            await SynthesizeSpeechAsync(token, region, userText);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bir hata oluştu: {ex.Message}");
        }
    }

    static async Task<string> GetTokenAsync(string key, string endPoint)
    {
        using var client = new HttpClient();

        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
        var response = await client.PostAsync(endPoint, null);
        return await response.Content.ReadAsStringAsync();

    }

    static async Task SynthesizeSpeechAsync(string token, string region, string text)
    {
        using var client = new HttpClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("User-Agent", "AzureTTSClient");
        client.DefaultRequestHeaders.Add("X-Microsoft-OutputFormat", "riff-16khz-16bit-mono-pcm");
        var requestUri = $"https://{region}.tts.speech.microsoft.com/cognitiveservices/v1";
        string ssml = $@"
                                <speak version='1.0' xml:lang='tr-TR'>
                                <voice xml:lang='en-US' name='tr-TR-AhmetNeural'>{text}</voice>
                                </speak>";
        var content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");
        var result = await client.PostAsync($"https://{region}.tts.speech.microsoft.com/cognitiveservices/v1", content);
        if (result.IsSuccessStatusCode)
        {
            var audioBytes = await result.Content.ReadAsByteArrayAsync();
            File.WriteAllBytes("output1.wav", audioBytes);
            Console.WriteLine("Speech synthesis completed successfully. Output saved to output.wav");
        }
        else
        {
            Console.WriteLine($"Error: {result.StatusCode}");
            var errorContent = await result.Content.ReadAsStringAsync();
            Console.WriteLine($"Error details: {errorContent}");

        }


    }
}