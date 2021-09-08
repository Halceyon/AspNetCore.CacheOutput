﻿using System;

namespace AspNetCore.CacheOutput
{
    public class CacheKeyGeneratorFactory
    {
        private readonly IServiceProvider serviceProvider;

        public CacheKeyGeneratorFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public ICacheKeyGenerator GetCacheKeyGenerator(Type cacheKeyGeneratorType)
        {
            if (cacheKeyGeneratorType != null && !typeof(ICacheKeyGenerator).IsAssignableFrom(cacheKeyGeneratorType))
            {
                throw new ArgumentException(nameof(cacheKeyGeneratorType));
            }

            Type generatorType = cacheKeyGeneratorType ?? typeof(ICacheKeyGenerator);

            ICacheKeyGenerator generator = serviceProvider.GetService(generatorType) as ICacheKeyGenerator;

            return generator ?? new DefaultCacheKeyGenerator();
        }
    }
}
