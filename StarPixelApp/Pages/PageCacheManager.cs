using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

public class PageCacheManager
{
    private readonly Dictionary<string, PageItem> _cachedPages = new();
    private readonly Dictionary<string, string> _cachedSettings = new();
    private readonly string _jsonPath;

    public PageCacheManager(string jsonPath)
    {
        _jsonPath = jsonPath;
    }

    public async Task LoadPagesAsync()
    {
        if (!File.Exists(_jsonPath)) return;

        _cachedPages.Clear();
        _cachedSettings.Clear();

        string json = await File.ReadAllTextAsync(_jsonPath);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var pageConfig = JsonSerializer.Deserialize<PageConfig>(json, options);

        if (pageConfig?.Pages != null)
        {
            foreach (var pageItem in pageConfig.Pages)
            {
                // Используем Name страницы как ключ в словаре
                if (!string.IsNullOrEmpty(pageItem.Name))
                {
                    _cachedPages[pageItem.Name] = pageItem;
                }
            }
        }

        if (pageConfig?.Settings != null)
        {
            foreach (var settingItem in pageConfig.Settings)
            {
                // Используем Name страницы как ключ в словаре
                if (!string.IsNullOrEmpty(settingItem.Key) && settingItem.Value != null)
                {
                    _cachedSettings[settingItem.Key] = settingItem.Value;
                }
            }
        }
    }


    public PageItem? GetPage(string pageName)
    {
        return _cachedPages.TryGetValue(pageName, out var page) ? page : null;
    }

    public string? GetSetting(string settingKey)
    {
        return _cachedSettings.TryGetValue(settingKey, out var value) ? value : null;
    }

    public async Task UpdateSetting(string key, string value)
    {
        // Пропускаем обновление, если ключ или значение null/пустые
        if (string.IsNullOrEmpty(key) || value == null)
        {
            return;
        }

        // Обновляем кэш
        _cachedSettings[key] = value;

        // Сохраняем весь кэш в файл
        await SaveCacheToFile();
    }

    private async Task SaveCacheToFile()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(
                System.Text.Unicode.UnicodeRanges.All), // Поддержка всех Unicode символов
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // Игнорировать null значения
        };


        // Создаем объект PageConfig на основе текущего кэша
        var pageConfig = new PageConfig
        {
            // Фильтруем null значения для страниц
            Pages = _cachedPages.Values
                .Where(p => !string.IsNullOrEmpty(p.Name))
                .ToList(),

            // Фильтруем null значения для настроек
            Settings = _cachedSettings
                .Where(kvp => !string.IsNullOrEmpty(kvp.Key) && kvp.Value != null)
                .Select(kvp => new SettingConfig { Key = kvp.Key, Value = kvp.Value })
                .ToList()
        };

        // Сериализуем и записываем в файл
        string json = JsonSerializer.Serialize(pageConfig, options);
        await File.WriteAllTextAsync(_jsonPath, json);
    }
}
