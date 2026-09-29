using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIforMAsupport.Api.Services.Ai;

public class GeminiAiClient : IAiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiAiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        _model = configuration["Gemini:Model"] ?? "gemini-1.5-flash-latest";
    }

    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        // 1. ตั้งค่า Path ให้ตรงกับที่ Claude เคยใช้ (ถอยหลัง 2 โฟลเดอร์)
        string systemInstructionText = "คุณคือ AI Assistant สำหรับงาน MA Support";
        string instructionPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "data", "Project_Instruction_MA_Bridgestone_v2.md");

        if (File.Exists(instructionPath))
        {
            systemInstructionText = await File.ReadAllTextAsync(instructionPath, cancellationToken);
        }

        var endpoint = $"https://generativelanguage.googleapis.com/v1/models/{_model}:generateContent?key={_apiKey}";

        // 2. จัดรูปแบบ Request พร้อมเพิ่ม Generation Config ควบคุมความแม่นยำ
        var requestBody = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = systemInstructionText } }
            },
            contents = new[]
            {
            new
            {
                parts = new[] { new { text = prompt } }
            }
        },
            // ปรับแต่งให้ Gemini ตอบแบบอิงข้อมูลจริง ไม่แต่งเรื่อง (เทียบเท่า Restricted Mode)
            generationConfig = new
            {
                temperature = 0.1, // ค่าต่ำ = ลดความสร้างสรรค์ เน้นความถูกต้องเป๊ะๆ
                topP = 0.8,
                topK = 40
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // 3. จำลองการจำกัดเวลา 150 วินาทีแบบที่ Claude ตั้งไว้
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(150));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var response = await _httpClient.PostAsync(endpoint, content, linkedCts.Token);
        var responseString = await response.Content.ReadAsStringAsync(linkedCts.Token);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Gemini API error ({response.StatusCode}): {responseString}");
        }

        using var doc = JsonDocument.Parse(responseString);
        var root = doc.RootElement;

        if (root.TryGetProperty("candidates", out var candidates) &&
            candidates.GetArrayLength() > 0 &&
            candidates[0].TryGetProperty("content", out var contentElem) &&
            contentElem.TryGetProperty("parts", out var parts) &&
            parts.GetArrayLength() > 0 &&
            parts[0].TryGetProperty("text", out var textElem))
        {
            return textElem.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    public async Task<string> AskStreamAsync(string prompt, Func<string, Task> onTokenReceived, CancellationToken cancellationToken = default)
    {
        // 1. ดึงคำตอบทั้งหมดมาก่อนผ่าน AskAsync ปกติ
        var fullResponse = await AskAsync(prompt, cancellationToken);

        // 2. จำลองการค่อยๆ พิมพ์ทีละนิด (Simulate Streaming) เพื่อให้ออกหน้าจอ
        int chunkSize = 4; // จำนวนตัวอักษรต่อการพิมพ์ 1 ครั้ง
        for (int i = 0; i < fullResponse.Length; i += chunkSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunk = fullResponse.Substring(i, Math.Min(chunkSize, fullResponse.Length - i));
            await onTokenReceived(chunk);

            // หน่วงเวลาให้ดูเหมือน AI กำลังค่อยๆ คิด
            await Task.Delay(15, cancellationToken);
        }

        // 3. สิ่งที่ขาดไป: คืนค่าข้อความผลลัพธ์ทั้งหมดกลับไปตามที่ Interface ต้องการ
        return fullResponse;
    }
}