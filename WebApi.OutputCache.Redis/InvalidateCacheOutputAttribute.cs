﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApi.OutputCache.Core;

namespace WebApi.OutputCache.Redis
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class InvalidateCacheOutputAttribute : ActionFilterAttribute
    {
        private string controllerName;
        private readonly string methodName;
        private readonly string[] actionParameters;

        public InvalidateCacheOutputAttribute(string methodName) : this(methodName, null)
        {
        }

        public InvalidateCacheOutputAttribute(
            string methodName,
            string controllerName,
            params string[] actionParameters
        )
        {
            this.methodName = methodName;
            this.controllerName = controllerName;
            this.actionParameters = actionParameters;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await base.OnActionExecutionAsync(context, next);

            if (
                context.HttpContext.Response != null &&
                !(
                    context.HttpContext.Response.StatusCode >= (int)HttpStatusCode.OK &&
                    context.HttpContext.Response.StatusCode < (int)HttpStatusCode.Ambiguous
                )
            )
            {
                return;
            }

            this.controllerName = this.controllerName ??
                (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerTypeInfo.FullName;

            IServiceProvider serviceProvider = context.HttpContext.RequestServices;
            IApiOutputCache cache = serviceProvider.GetService(typeof(IApiOutputCache)) as IApiOutputCache;
            ICacheKeyGenerator cacheKeyGenerator = serviceProvider.GetService(typeof(ICacheKeyGenerator)) as ICacheKeyGenerator;

            if (cache != null && cacheKeyGenerator != null)
            {
                string baseCachekey = cacheKeyGenerator.MakeBaseCachekey(this.controllerName, this.methodName);

                string key = IncludeActionParameters(context, baseCachekey, actionParameters);

                await cache.RemoveStartsWithAsync(key);
            }
        }

        private string IncludeActionParameters(
            ActionExecutingContext actionContext,
            string baseCachekey,
            string[] additionalActionParameters
        )
        {
            if (!additionalActionParameters.Any())
            {
                return $"{baseCachekey}";
            }

            IEnumerable<string> actionContextParameters = actionContext
                .ActionArguments
                .Where(x => x.Value != null && additionalActionParameters.Contains(x.Key))
                .Select(x => x.Key + "=" + GetValue(x.Value));

            return $"{baseCachekey}-{string.Join("-", actionContextParameters)}*";
        }

        private string GetValue(object val)
        {
            if (val is IEnumerable && !(val is string))
            {
                string concatValue = string.Empty;
                IEnumerable paramArray = (IEnumerable)val;

                return paramArray.Cast<object>().Aggregate(concatValue, (current, paramValue) => current + (paramValue + ";"));
            }

            return val.ToString();
        }
    }
}
