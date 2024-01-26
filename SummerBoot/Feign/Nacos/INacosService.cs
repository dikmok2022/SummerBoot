﻿using SummerBoot.Feign.Attributes;
using SummerBoot.Feign.Nacos.Dto;
using System.Net.Http;
using System.Threading.Tasks;

namespace SummerBoot.Feign.Nacos
{
    [FeignClient(Url = "${nacos:serviceAddress}", InterceptorType = typeof(AuthInterceptor), Timeout = 60)]
    public interface INacosService
    {
        /// <summary>
        /// 服务器需要授权的在请求前获取Token
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [PostMapping("/nacos/v1/auth/login")]
        [IgnoreInterceptor]
        Task<GetTokenOutputDto> QueryToken([Query] QueryTokenDto dto);

        /// <summary>
        /// 注册实例
        /// </summary>
        /// <returns></returns>
        [PostMapping("/nacos/v1/ns/instance")]
        Task<string> RegisterInstance([Query]NacosRegisterInstanceDto dto);
        /// <summary>
        /// 注销实例
        /// </summary>
        /// <returns></returns>
        [DeleteMapping("/nacos/v1/ns/instance")]
        Task<string> UnRegisterInstance([Query] NacosRegisterInstanceDto dto);
        /// <summary>
        /// 发送实例心跳
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [PutMapping("/nacos/v1/ns/instance/beat")]
        Task<SendInstanceHeartBeatOutputDto> SendInstanceHeartBeat([Query] SendInstanceHeartBeatDto dto);

        /// <summary>
        /// 查询实例列表
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [GetMapping("/nacos/v1/ns/instance/list")]
        Task<QueryInstanceListOutputDto> QueryInstanceList([Query] QueryInstanceListInputDto dto);
        /// <summary>
        /// 获取配置信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [GetMapping("/nacos/v1/cs/configs")]
        Task<HttpResponseMessage> GetConfigs([Query] GetConfigsDto dto);
        /// <summary>
        /// 监听配置信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [Headers("Long-Pulling-Timeout:10000")]
        [PostMapping("/nacos/v1/cs/configs/listener")]
        Task<string> ConfigListener([Query][AliasAs("Listening-Configs")] string config);
    }
}