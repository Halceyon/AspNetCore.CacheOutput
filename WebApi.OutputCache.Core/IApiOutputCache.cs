﻿using System;
using System.Threading.Tasks;

namespace WebApi.OutputCache.Core
{
    public interface IApiOutputCache
    {
        Task RemoveStartsWithAsync(string key);

        Task<T> GetAsync<T>(string key) where T : class;

        Task RemoveAsync(string key);

        Task<bool> ContainsAsync(string key);

        Task AddAsync(string key, object o, DateTimeOffset expiration, string dependsOnKey = null);
    }
}
