using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Sendgo;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Sendgo를 ASP.NET Core 의존성 주입 컨테이너에 등록하는 확장 메서드.
/// </summary>
/// <example>
/// <code>
/// // appsettings.json의 "Sendgo" 섹션에서 바인딩
/// builder.Services.AddSendgo(builder.Configuration.GetSection("Sendgo"));
///
/// // 또는 코드로 직접 설정
/// builder.Services.AddSendgo(options =>
/// {
///     options.AccessKey      = "your_access_key";
///     options.SecretKey      = "your_secret_key";
///     options.KakaoSenderKey = "your_kakao_key";
///     options.ApiVersion     = "v2";
/// });
/// </code>
/// </example>
public static class SendgoServiceCollectionExtensions
{
    /// <summary>발송용 키 없이 계정 API 클라이언트를 등록합니다.</summary>
    public static IServiceCollection AddSendgoAccount(this IServiceCollection services, string agentToken, string baseUrl = "https://sendgo.io")
    {
        ArgumentNullException.ThrowIfNull(services);
        if (string.IsNullOrWhiteSpace(agentToken)) throw new ArgumentException("agentToken은 필수입니다.", nameof(agentToken));
        services.AddSingleton(_ => new AccountClient(agentToken, baseUrl));
        return services;
    }

    /// <summary>
    /// 람다로 <see cref="SendgoOptions"/>를 설정하고 <see cref="SendgoClient"/>를 싱글턴으로 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션.</param>
    /// <param name="configure">Sendgo 옵션 설정 델리게이트.</param>
    /// <returns>체이닝을 위한 <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddSendgo(this IServiceCollection services, Action<SendgoOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<SendgoOptions>().Configure(configure);
        return services.AddSendgoClient();
    }

    /// <summary>
    /// 이미 스코프가 지정된 설정 섹션에서 <see cref="SendgoOptions"/>를 바인딩하고
    /// <see cref="SendgoClient"/>를 싱글턴으로 등록합니다.
    /// (예: 호출 측에서 <c>Configuration.GetSection("Sendgo")</c>를 전달)
    /// </summary>
    /// <param name="services">서비스 컬렉션.</param>
    /// <param name="configuration">Sendgo 설정이 담긴 섹션.</param>
    /// <returns>체이닝을 위한 <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddSendgo(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<SendgoOptions>().Bind(configuration);
        return services.AddSendgoClient();
    }

    /// <summary>
    /// 설정된 <see cref="SendgoOptions"/>로부터 <see cref="SendgoClient"/> 싱글턴을 등록합니다.
    /// BaseUrl 등 미설정 값은 <see cref="SendgoOptions"/> 기본값(https://sendgo.io)을 따릅니다.
    /// </summary>
    private static IServiceCollection AddSendgoClient(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<SendgoOptions>>().Value;
            return new SendgoClient(opts);
        });
        return services;
    }
}
