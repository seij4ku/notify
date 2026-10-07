using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Notify.Desktop;

static class Ocr
{
    static readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(120) };

    public static async Task<string> RunAsync(byte[] image)
    {
        var key = ApiKeyStore.Read();
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Add an OpenAI API key in Settings before running screen OCR.");
        var prompt = "Transcribe the visible text from this screenshot. Apply basic cleanup: fix obvious OCR character and spacing errors, preserve meaning, reading order, headings, and recognizable equations, remove obvious duplicated interface noise, and do not invent missing content. Mark uncertain text [unclear]. Return only the cleaned transcription.";
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Content = JsonContent.Create(new
        {
            model = "gpt-6-astra",
            store = false,
            input = new[] { new { role = "user", content = new object[] {
                new { type = "input_text", text = prompt },
                new { type = "input_image", image_url = "data:image/png;base64," + Convert.ToBase64String(image) }
            } } }
        });
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"OpenAI OCR failed ({(int)response.StatusCode}): {body}");
        using var json = JsonDocument.Parse(body);
        var text = json.RootElement.GetProperty("output").EnumerateArray()
            .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "message")
            .SelectMany(item => item.GetProperty("content").EnumerateArray())
            .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "output_text")
            .Select(item => item.GetProperty("text").GetString())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        return text ?? throw new InvalidOperationException("OpenAI returned no OCR text.");
    }
}
