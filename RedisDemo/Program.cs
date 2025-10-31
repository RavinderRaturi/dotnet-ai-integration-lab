using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;

class Program
{
    const string EmbeddingsUrl = "https://api.openai.com/v1/embeddings";
    const string ChatUrl = "https://api.openai.com/v1/chat/completions";
    const string IndexName = "idx:documents";
    const int EmbeddingDim = 1536; // use 3072 if you're using text-embedding-3-large
    const string RedisConn = "localhost:6379"; // keep default host:port

    static async Task Main()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("ERROR: Set OPENAI_API_KEY environment variable.");
            return;
        }

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var config = new ConfigurationOptions
        {
            EndPoints = { RedisConn },
            AbortOnConnectFail = false,
            ConnectTimeout = 10000,
            SyncTimeout = 20000, // 60s
            KeepAlive = 180,
        };
        var mux = ConnectionMultiplexer.Connect(config);
        var db = mux.GetDatabase();
        Console.WriteLine("Connected to Redis config: " + mux.Configuration);
        Console.WriteLine("Using index: " + IndexName);

        await EnsureIndexExists(db);
        Console.WriteLine("✅ Redis index ready.");

        // Step 1: Define sample docs
        var docs = new Dictionary<string, string>
        {
            ["1"] = "Last night the bright moon hung low above the city, casting silver light across the rooftops.",
            ["2"] = "I spent the afternoon tuning my dirt-bike’s suspension for a rocky trail ride next weekend.",
            ["3"] = "A simple tomato and basil salad is fresh and quick to prepare after a long day.",
            ["4"] = "Scientists warn that coastal cities are seeing more frequent flooding as sea levels rise.",
            ["5"] = "The local football team practiced set plays until the sun dipped behind the stadium."
        };

        // Step 2: Embed and upsert docs
        foreach (var kv in docs)
        {
            var embedding = await GetEmbedding(http, kv.Value);
            await UpsertDoc(db, kv.Key, kv.Value, embedding);
            Console.WriteLine($"📥 Upserted doc:{kv.Key}");
        }

        // Step 3: Query
        Console.Write("\nEnter your query: ");
        var query = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(query)) query = "off-road bike suspension problems";

        var qEmbed = await GetEmbedding(http, query);
        Console.WriteLine($"DEBUG: query embedding length = {qEmbed.Length}. Expected DIM = {EmbeddingDim}");

        var topDocs = await SearchTopK(db, qEmbed, k: 2);

        Console.WriteLine("\n🔍 Top matches:");
        foreach (var d in topDocs)
            Console.WriteLine($" - {d.Id}: score {d.Score:F4}");

        var context = string.Join("\n---\n", topDocs.Select(x => x.Text));
        var answer = await GetChatAnswer(http, query, context);
        Console.WriteLine("\n🧠 Final Answer:\n" + answer);

        await mux.CloseAsync();
    }

    // ---------- Helpers ----------

    static async Task<float[]> GetEmbedding(HttpClient http, string text)
    {
        var payload = new { model = "text-embedding-3-small", input = text };
        var json = JsonSerializer.Serialize(payload);
        var resp = await http.PostAsync(EmbeddingsUrl, new StringContent(json, Encoding.UTF8, "application/json"));
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            Console.WriteLine(body);
            throw new Exception("Embedding failed");
        }
        using var doc = JsonDocument.Parse(body);
        var vector = doc.RootElement.GetProperty("data")[0].GetProperty("embedding").EnumerateArray()
            .Select(e => e.GetSingle()).ToArray();
        return vector;
    }

    static async Task UpsertDoc(IDatabase db, string id, string text, float[] vector)
    {
        var vecBytes = FloatArrayToByteArray(vector);
        var key = $"doc:{id}";
        var entries = new HashEntry[]
        {
            new("text_field", text),
            new("vector_field", vecBytes)
        };
        await db.HashSetAsync(key, entries);
    }



    static async Task<List<(string Id, string Text, float Score)>> SearchTopK(IDatabase db, float[] queryVec, int k)
    {
        var vecBytes = FloatArrayToByteArray(queryVec);
        // 1) Keep RETURN small. Do NOT return the binary vector_field by default.
        var knnExpr = $"*=>[KNN {k} @vector_field $BLOB AS score]";

        // Build args. Note explicit cast of vecBytes to RedisValue to avoid marshalling ambiguity.
        var cmdArgs = new object[]
        {
        IndexName,
        knnExpr,
        "PARAMS", "2", "BLOB", (RedisValue)vecBytes,   // pass binary param safely
        "DIALECT", "2",
       "RETURN", "2", "text_field", "score",           // return text_field and score (score guaranteed in fields-array)
        "SORTBY", "score", "ASC",
        "LIMIT", "0", k.ToString()
        };

        // Timing so you can see if Redis or your client is the bottleneck
        var sw = System.Diagnostics.Stopwatch.StartNew();
        RedisResult raw;
        try
        {
            raw = await db.ExecuteAsync("FT.SEARCH", cmdArgs).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.WriteLine($"FT.SEARCH failed after {sw.ElapsedMilliseconds} ms. Exception: {ex.GetType().Name} {ex.Message}");
            throw;
        }
        sw.Stop();
        Console.WriteLine($"FT.SEARCH elapsed={sw.ElapsedMilliseconds} ms");

        var results = ParseRedisSearchResults(raw);
        return results;
    }
    static List<(string Id, string Text, float Score)> ParseRedisSearchResults(RedisResult raw)
    {
        var results = new List<(string, string, float)>();
        if (raw.IsNull) return results;

        var arr = (RedisResult[])raw;
        if (arr.Length < 1) return results;

        int idx = 1;
        while (idx < arr.Length)
        {
            if (idx >= arr.Length) break;

            var id = arr[idx++].ToString();
            if (string.IsNullOrEmpty(id)) break;

            float score = float.NaN;
            RedisResult[] fields;

            // Handle optional score element
            if (idx < arr.Length)
            {
                var next = arr[idx];
                if (next.Type == ResultType.BulkString ||
                    next.Type == ResultType.SimpleString ||
                    next.Type == ResultType.Integer)
                {
                    var s = next.ToString();
                    if (float.TryParse(s, out var parsedScore))
                    {
                        score = parsedScore;
                        idx++;
                    }
                }
            }

            if (idx >= arr.Length) break;

            var fieldCandidate = arr[idx++];
            if (fieldCandidate.Type == ResultType.MultiBulk)
            {
                fields = (RedisResult[])fieldCandidate;
            }
            else
            {
                fields = new[] { fieldCandidate };
            }

            string text = "";
            for (int i = 0; i + 1 < fields.Length; i += 2)
            {
                var fname = fields[i].ToString();
                var fval = fields[i + 1];

                if (fname == "text_field") text = fval.ToString();
                else if (fname == "score")
                {
                    if (!float.TryParse(fval.ToString(), out score))
                    {
                        try { score = Convert.ToSingle(fval); } catch { score = float.NaN; }
                    }
                }
            }

            results.Add((id, text, score));
        }

        return results;
    }


    static async Task<string> GetChatAnswer(HttpClient http, string question, string context)
    {
        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = "You are an assistant that answers using only provided context." },
                new { role = "user", content = $"Context:\n{context}\n\nQuestion: {question}" }
            },
            max_tokens = 350,
            temperature = 0.1,
            top_p = 0.9
        };
        var json = JsonSerializer.Serialize(payload);
        var resp = await http.PostAsync(ChatUrl, new StringContent(json, Encoding.UTF8, "application/json"));
        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return text;
    }

    static byte[] FloatArrayToByteArray(float[] vector)
    {
        var bytes = new byte[vector.Length * 4];
        for (int i = 0; i < vector.Length; i++)
        {
            var b = BitConverter.GetBytes(vector[i]);
            if (!BitConverter.IsLittleEndian) Array.Reverse(b);
            Buffer.BlockCopy(b, 0, bytes, i * 4, 4);
        }
        return bytes;
    }


    static async Task EnsureIndexExists(IDatabase db)
    {
        try
        {
            await db.ExecuteAsync("FT.INFO", IndexName);
            Console.WriteLine("Index already exists.");
            return;
        }
        catch
        {
            Console.WriteLine("Creating Redis index...");
        }

        var args = new RedisValue[]
        {
        IndexName,
        "ON", "HASH",
        "PREFIX", "1", "doc:",
        "SCHEMA",
        "text_field", "TEXT",
        "vector_field", "VECTOR", "HNSW", // HNSW index
            "TYPE", "FLOAT32",
            "DIM", EmbeddingDim.ToString(),
            "DISTANCE_METRIC", "COSINE",
            "M", "16", "EF_CONSTRUCTION", "200"
        };

        var res = await db.ExecuteAsync("FT.CREATE", args);
        Console.WriteLine($"FT.CREATE result: {res}");
    }

}
