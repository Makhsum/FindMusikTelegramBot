using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Search;
using YoutubeExplode.Videos.Streams;

namespace FindMusik.Application.Services;

public class FindMusik
{
    private YoutubeClient youtube = new YoutubeClient();
    private InvalidCharsReplace _invalidCharsReplace = new InvalidCharsReplace();
    public async Task<string> DownloadAsync(string videoUrl)
    {
        var video = await youtube.Videos.GetAsync(videoUrl);
        var title = video.Title;
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
        var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        if (audioStreamInfo != null)
        {
            title = _invalidCharsReplace.ReplaceAsync(title);
            var outputFilePath = $"{title}.mp3";
            await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, outputFilePath);
            Console.WriteLine($"Аудио '{title}' скачано и сохранено в '{outputFilePath}'");
            return outputFilePath;
        }
        else
        {
            Console.WriteLine("Не удалось найти подходящий аудиопоток для скачивания.");
        }
    
        return "Не удалось найти подходящий аудиопоток для скачивания.";
    }
    private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

    // public async Task<string> DownloadAsync(string videoUrl)
    // {
    //     await _semaphoreSlim.WaitAsync();
    //     try
    //     {
    //         var video = await youtube.Videos.GetAsync(videoUrl);
    //         var title = video.Title;
    //         Console.WriteLine($"Запрос на скачивание на {title}");
    //         var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
    //         var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
    //     
    //         if (audioStreamInfo != null)
    //         {
    //             var sanitizedTitle = SanitizeFileName(title);
    //             var outputFilePath = $"{sanitizedTitle}.mp3";
    //
    //             using (var httpClient = new HttpClient())
    //             {
    //                 httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
    //                 using (var stream = await httpClient.GetStreamAsync(audioStreamInfo.Url))
    //                 {
    //                     using (var fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
    //                     {
    //                         await stream.CopyToAsync(fileStream);
    //                     }
    //                 }
    //             }
    //
    //             Console.WriteLine($"Аудио '{title}' скачано и сохранено в '{outputFilePath}'");
    //             return outputFilePath;
    //         }
    //         else
    //         {
    //             Console.WriteLine("Не удалось найти подходящий аудиопоток для скачивания.");
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine($"Ошибка при скачивании аудио: {ex.Message}");
    //     }
    //     finally
    //     {
    //         _semaphoreSlim.Release();
    //     }
    //
    //     return "Не удалось найти подходящий аудиопоток для скачивания.";
    // }

   
    public async Task<IReadOnlyList<VideoSearchResult>> SearchAsync(string query)
    {
        var result = await youtube.Search.GetVideosAsync(query);
        return result;
        
    }
    
    
    
}