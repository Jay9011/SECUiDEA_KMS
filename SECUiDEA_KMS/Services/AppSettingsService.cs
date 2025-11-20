
using System.Text.Json;
using System.Text.Json.Serialization;
using SECUiDEA_KMS.Utils;

namespace SECUiDEA_KMS.Services;

/// <summary>
/// appsettings.json 파일의 읽기/쓰기를 담당하는 헬퍼 클래스
/// </summary>
public class AppSettingsService
{
    #region 의존 주입
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public AppSettingsService(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }
    #endregion

    public string FilePath => GetConfigurationFileName();

    #region public methods

    /// <summary>
    /// 섹션이 존재하는지 확인
    /// </summary>
    /// <param name="key">섹션 키</param>
    /// <returns>섹션이 존재하면 true, 존재하지 않으면 false</returns>
    public bool SectionExists(string key)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(key);

        var section = _configuration.GetSection(key);
        return section.Exists();
    }

    /// <summary>
    /// 섹션의 값을 읽어옴
    /// </summary>
    /// <param name="key">섹션 키</param>
    /// <returns>섹션의 값</returns>
    public string? ReadRawValue(string key)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(key);

        var section = _configuration.GetSection(key);
        return section.Value;
    }

    /// <summary>
    /// 섹션의 값을 읽어옴
    /// </summary>
    /// <param name="key">섹션 키</param>
    /// <returns>섹션의 값</returns>
    /// <typeparam name="T">섹션의 값 타입</typeparam>
    public T? ReadValue<T>(string key) where T : class, new()
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(key);

        var section = _configuration.GetSection(key);
        if (!section.Exists())
        {
            throw new InvalidOperationException($"Section '{key}' not found in configuration.");
        }

        var model = new T();
        section.Bind(model);
        return model;
    }

    /// <summary>
    /// 섹션의 값을 씀
    /// </summary>
    /// <param name="key">섹션 키</param>
    /// <param name="value">섹션의 값</param>
    public void WriteRawValue(string key, string value)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        WriteInternalAsync(key, value);
    }

    /// <summary>
    /// 섹션의 값을 씀
    /// </summary>
    /// <param name="key">섹션 키</param>
    /// <param name="value">섹션의 값</param>
    /// <typeparam name="T">섹션의 값 타입</typeparam>
    public void WriteValue<T>(string key, T value) where T : class, new()
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var serializedValue = DictionaryExtensions.ToDictionary(value);
        WriteInternalAsync(key, serializedValue);
    }

    #endregion

    #region private methods

    /// <summary>
    /// 설정 파일 이름을 가져옴
    /// </summary>
    /// <returns>설정 파일 이름</returns>
    private string GetConfigurationFileName()
    {
        // 1. 환경별 성정 파일을 먼저 확인 (appsettings.Development.json, appsettings.json)
        var environmentFileName = Path.Combine(_environment.ContentRootPath, $"{Consts.AppSettingsFileName}.{_environment.EnvironmentName}.{Consts.AppSettingsExtension}");
        if (File.Exists(environmentFileName))
        {
            return environmentFileName;
        }

        // 2. 기본 성정 파일을 확인 (appsettings.json)
        var baseFileName = Path.Combine(_environment.ContentRootPath, $"{Consts.AppSettingsFileName}.{Consts.AppSettingsExtension}");
        if (File.Exists(baseFileName))
        {
            return baseFileName;
        }

        // 3. 둘 다 없으면 기본 파일명 전달
        return baseFileName;
    }

    /// <summary>
    /// 중첩된 섹션 구조를 올바르게 설정 (중첩된 만큼 Dictionary를 중첩하여 설정)
    /// </summary>
    /// <param name="document">JSON 문서 (Dictionary<string, object>)</param>
    /// <param name="sectionKey">섹션 키 (중첩된 키를 ':'로 구분)</param>
    /// <param name="value">설정 값</param>
    private void SetNestedValue(Dictionary<string, object> document, string sectionKey, object value)
    {
        var keys = sectionKey.Split(Consts.SectionSeparator, StringSplitOptions.RemoveEmptyEntries);

        if (keys.Length == 1) // 단일 섹션
        {
            document[keys[0]] = value;
            return;
        }

        var current = document;
        for (int i = 0; i < keys.Length - 1; i++)
        {
            var key = keys[i];
            if (!current.ContainsKey(key))
            {
                current[key] = new Dictionary<string, object>();
            }
            else if (current[key] is not Dictionary<string, object>)
            {
                if (current[key] is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        dict[property.Name] = property.Value.ConvertToObject();
                    }
                    current[key] = dict;
                }
                else
                {
                    current[key] = new Dictionary<string, object>();
                }
            }

            if (current[key] is Dictionary<string, object> nextDict)
            {
                current = nextDict;
            }
            else
            {
                throw new InvalidOperationException($"Cannot navigate to key '{key}' because it is not a dictionary.");
            }
        }

        current[keys[^1]] = value;
    }

    /// <summary>
    /// 설정 저장 (':'로 중첩 섹션 구분)
    /// </summary>
    private void WriteInternalAsync(string sectionKey, object value)
    {
        string json;
        try
        {
            json = File.ReadAllText(GetConfigurationFileName());
        }
        catch (FileNotFoundException)
        {
            // 파일이 없으면 새로 생성
            json = "{}";
        }
        catch (IOException ex)
        {
            throw new IOException($"Failed to read configuration file: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Failed to read configuration file: {ex.Message}", ex);
        }

        var document = JsonSerializer.Deserialize<Dictionary<string, object>>(json, GetJsonSerializerOptions()) ?? new();

        SetNestedValue(document, sectionKey, value);

        var updatedJson = JsonSerializer.Serialize(document, GetJsonSerializerOptions());

        try
        {
            File.WriteAllText(GetConfigurationFileName(), updatedJson);
        }
        catch (IOException ex)
        {
            throw new IOException($"Failed to write configuration file: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Failed to write configuration file: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to write configuration file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// JSON 직렬화 옵션을 가져옴
    /// </summary>
    /// <returns>JSON 직렬화 옵션</returns>
    private JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    #endregion
}
