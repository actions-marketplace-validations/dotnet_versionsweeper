﻿using DotNet.Extensions;
using DotNet.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DotNet.Releases
{
    internal class CoreReleaseService : ICoreReleaseService
    {
        readonly HttpClient _httpClient;
        readonly IMemoryCache _cache;
        readonly ICoreReleaseIndexService _indexService;

        public CoreReleaseService(
            HttpClient httpClient,
            IMemoryCache cache,
            ICoreReleaseIndexService indexService) =>
            (_httpClient, _cache, _indexService) =
                (httpClient, cache, indexService);

        async IAsyncEnumerable<(ReleasesIndex Index, CoreReleaseDetails Details)> ICoreReleaseService.GetAllReleasesAsync()
        {
            var releases = await _indexService.GetReleaesAsync();
            foreach (var release in releases?.ReleasesIndex ?? Enumerable.Empty<ReleasesIndex>())
            {
                var coreReleaseDetails =
                    await _cache.GetOrCreateAsync(
                        release.ReleasesJson,
                        async entry =>
                        {
                            var releaseJson = await _httpClient.GetStringAsync(entry.Key.ToString());
                            return releaseJson.FromJson<CoreReleaseDetails>();
                        });

                release.ReleaseNotesUrl =
                    coreReleaseDetails!
                        .Releases
                        .OrderBy(r => r.ReleaseDate.ToDateTime())
                        .FirstOrDefault()
                        ?.ReleaseNotes ?? release.ReleasesJson;

                yield return (release, coreReleaseDetails!);
            }
        }
    }
}