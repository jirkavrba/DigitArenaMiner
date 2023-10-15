using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace DigitArenaBot.Services
{
    public class VideoDownloadService
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;
        private readonly IConfigurationRoot _config;

        private readonly string _rootPath = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string _downloadPath;

        private readonly string _youtubeDdpPath;
        private readonly string _FFmpegPath;

        private readonly float _maxVideoLengthSeconds;

        public VideoDownloadService(DiscordSocketClient client, InteractionService commands, IServiceProvider services, IConfigurationRoot config)
        {
            _client = client;
            _commands = commands;
            _services = services;
            _config = config;
            _downloadPath = Path.Combine(_rootPath, "Downloads");
            Directory.CreateDirectory(_downloadPath);

            _youtubeDdpPath = Path.Combine(_downloadPath, "YTDLP");
            _FFmpegPath = Path.Combine(_downloadPath, "FFMPEG");
            
            Directory.CreateDirectory(_youtubeDdpPath);
            Directory.CreateDirectory(_FFmpegPath);
            
            _maxVideoLengthSeconds = _config.GetSection("MaxVideoDuration").Get<float>();

        }

        public async Task Init()
        {
            await YoutubeDLSharp.Utils.DownloadYtDlp(_youtubeDdpPath);
            await YoutubeDLSharp.Utils.DownloadFFmpeg(_FFmpegPath);
            Console.WriteLine("DEBUG");
            Console.WriteLine(string.Join(",", Directory.GetFiles(_youtubeDdpPath)));
        }

        public async Task<string> DownloadVideo(string url, ExampleCommands.VideoFormat format)
        {
            var loweredUrl = url.ToLower();

            var formatString = format == ExampleCommands.VideoFormat.Best ? "bestvideo+bestaudio/best" : "worstvideo+worstaudio/worst";
            
            return await DownloadVideoWithYtDl(url, formatString);
        }

        public Task<FileStream> GetVideoStream(string path)
        {
            return Task.FromResult(File.OpenRead(path));
        }

        public Task DeleteVideo(string path)
        {
            var dir = Path.GetDirectoryName(path);
            Directory.Delete(dir, true);
            return Task.CompletedTask;
        }

        protected async Task<string> DownloadVideoWithYtDl(string videoUrl, string format = "bestvideo+bestaudio/best")
        {
            var ytdl = CreateYoutubeDl();
            var data = await ytdl.RunVideoDataFetch(videoUrl);

            if (data.Data == null || data.Data.Duration == null) throw new Exception("Data o videu jsou null.");

            var mexVideoDuration = format == "bestvideo+bestaudio/best"
                ? _maxVideoLengthSeconds
                : _maxVideoLengthSeconds * 6;
            
            
            Console.WriteLine($"Délka {data.Data.Duration} - {mexVideoDuration}");
            if (data.Data.Duration.Value > mexVideoDuration)
            {
                throw new Exception($"Video (délky {data.Data.Duration}s) je delší než povolená délka (Best-{_maxVideoLengthSeconds}s; Worst-{mexVideoDuration}s)");
            }

            ytdl.OutputFolder = Path.Combine(_downloadPath, data.Data.ID);

            var res = await ytdl.RunVideoDownload(videoUrl, format, mergeFormat: DownloadMergeFormat.Mp4, overrideOptions: new OptionSet()
            {
                PostprocessorArgs = "ffmpeg:-vcodec h264_nvenc"
            });

            return res.Data;
        }
        
        protected YoutubeDL CreateYoutubeDl()
        {
            var ytdl = new YoutubeDL();
            ytdl.YoutubeDLPath = Path.Combine(_youtubeDdpPath, "yt-dlp.exe");
            ytdl.FFmpegPath = Path.Combine(_FFmpegPath, "ffmpeg.exe");
            return ytdl;
        }
    }
}