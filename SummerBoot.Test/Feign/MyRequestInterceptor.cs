﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SummerBoot.Feign;

namespace SummerBoot.Test.Feign
{
    public class MyRequestInterceptor : IRequestInterceptor
    {
        public MyRequestInterceptor(ITestFeign testFeign)
        {
            this.testFeign = testFeign;
        }
        private readonly ITestFeign testFeign;

        public async Task ApplyAsync(RequestTemplate requestTemplate)
        {
            requestTemplate.Headers.Add("testHeader", new List<string>() { "123" });
           var b=  await testFeign.Test(new test() { Name = "hzp2", Age = 10 });
            await Task.CompletedTask;
        }
    }
}
