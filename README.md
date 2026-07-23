# Sendgo.AspNetCore

> **ASP.NET Core에서 카카오 알림톡, 친구톡, SMS를 가장 쉽게 발송하는 공식 DI 확장 패키지**

[![NuGet](https://img.shields.io/nuget/v/Sendgo.AspNetCore)](https://www.nuget.org/packages/Sendgo.AspNetCore)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

`Sendgo.AspNetCore`는 [`Sendgo.SDK`](https://www.nuget.org/packages/Sendgo.SDK) 코어를 확장한 **ASP.NET Core 전용 패키지**입니다.
`IServiceCollection` 확장 메서드로 `SendgoClient`를 싱글턴으로 등록하고, `appsettings.json` 또는 코드로 손쉽게 설정 바인딩을 제공합니다.

---

## 목차

- [설치](#설치)
- [빠른 시작](#빠른-시작)
- [설정 방법](#설정-방법)
  - [appsettings.json 바인딩](#appsettingsjson-바인딩)
  - [람다(코드)로 설정](#람다코드로-설정)
- [상세 사용법](#상세-사용법)
  - [알림톡](#알림톡)
  - [친구톡](#친구톡)
  - [SMS / LMS / MMS](#sms--lms--mms)
- [서비스 클래스 패턴](#서비스-클래스-패턴)
- [예외 처리](#예외-처리)
- [설정 옵션](#설정-옵션)
- [자주 묻는 질문](#자주-묻는-질문-faq)

---

## 설치

```bash
dotnet add package Sendgo.AspNetCore
```

`Sendgo.SDK` 코어가 자동으로 함께 설치됩니다.

---

## 빠른 시작

### 1단계 — 설정 등록 (`Program.cs`)

```csharp
var builder = WebApplication.CreateBuilder(args);

// appsettings.json의 "Sendgo" 섹션에서 바인딩
builder.Services.AddSendgo(builder.Configuration.GetSection("Sendgo"));

builder.Services.AddControllers();
var app = builder.Build();
```

### 2단계 — `appsettings.json`

```json
{
  "Sendgo": {
    "AccessKey": "your_access_key",
    "SecretKey": "your_secret_key",
    "KakaoSenderKey": "your_kakao_key",
    "SmsSenderKey": "your_sms_key",
    "ApiVersion": "v2"
  }
}
```

> `BaseUrl`을 지정하지 않으면 기본값 `https://sendgo.io`가 사용됩니다.

### 3단계 — 컨트롤러에서 주입받아 발송

```csharp
using Microsoft.AspNetCore.Mvc;
using Sendgo;
using Sendgo.Models;

[ApiController]
[Route("orders")]
public class OrderController : ControllerBase
{
    private readonly SendgoClient _sendgo;

    public OrderController(SendgoClient sendgo) => _sendgo = sendgo;

    [HttpPost("{orderNo}/confirm")]
    public async Task<IActionResult> Confirm(string orderNo)
    {
        await _sendgo.SendAlimtalkAsync(new AlimtalkRequest
        {
            TemplateCode = "ORDER_CONFIRM_001",
            Contacts     = [new Contact { PhoneNumber = "01012345678", Var1 = orderNo }],
        });

        return Ok(new { success = true });
    }
}
```

---

## 설정 방법

### appsettings.json 바인딩

호출 측에서 이미 스코프가 지정된 섹션을 전달합니다.

```csharp
builder.Services.AddSendgo(builder.Configuration.GetSection("Sendgo"));
```

환경별 오버라이드는 `appsettings.Development.json`, 환경변수, User Secrets 등
ASP.NET Core의 표준 구성 소스를 그대로 활용할 수 있습니다.

```bash
# User Secrets로 민감 정보 관리 (개발 환경 권장)
dotnet user-secrets set "Sendgo:AccessKey" "your_access_key"
dotnet user-secrets set "Sendgo:SecretKey" "your_secret_key"
```

### 람다(코드)로 설정

```csharp
builder.Services.AddSendgo(options =>
{
    options.AccessKey      = builder.Configuration["SENDGO_ACCESS_KEY"]!;
    options.SecretKey      = builder.Configuration["SENDGO_SECRET_KEY"]!;
    options.KakaoSenderKey = builder.Configuration["SENDGO_KAKAO_KEY"];
    options.SmsSenderKey   = builder.Configuration["SENDGO_SMS_KEY"];
    options.ApiVersion     = "v2";
});
```

두 방식 모두 `SendgoClient`를 **싱글턴**으로 등록하므로,
생성자에서 `SendgoClient`를 타입힌트로 주입받아 사용할 수 있습니다.

---

## 상세 사용법

### 알림톡

```csharp
// 다건 발송
await _sendgo.SendAlimtalkAsync(new AlimtalkRequest
{
    TemplateCode = "ORDER_CONFIRM_001",
    Contacts =
    [
        new Contact { PhoneNumber = "01011111111", Name = "홍길동", Var1 = "ORD-001", Var2 = "29,000원" },
        new Contact { PhoneNumber = "01022222222", Name = "김철수", Var1 = "ORD-002", Var2 = "15,000원" },
    ],
});
```

### 친구톡

```csharp
await _sendgo.SendFriendtalkAsync(new
{
    content  = "안녕하세요! 7월 한정 특가 이벤트를 확인해보세요.",
    contacts = new[] { new { contact = "01012345678" } },
});
```

### SMS / LMS / MMS

```csharp
// SMS (90자 이하)
await _sendgo.SendSmsAsync(new SmsRequest
{
    Content  = "[Sendgo] 인증번호: 123456 (5분 이내 입력)",
    Contacts = [new Contact { PhoneNumber = "01012345678" }],
});

// LMS (장문, 2,000자 이하)
await _sendgo.SendLmsAsync(new SmsRequest
{
    Subject  = "[중요] 서비스 점검 안내",
    Content  = "안녕하세요. 서비스 점검이 예정되어 있습니다.",
    Contacts = [new Contact { PhoneNumber = "01012345678" }],
});

// MMS (이미지 포함)
await _sendgo.SendMmsAsync(new SmsRequest
{
    Subject  = "[이벤트] 7월 특가",
    Content  = "이번 달 특가 상품을 확인하세요!",
    Contacts = [new Contact { PhoneNumber = "01011111111" }],
});
```

---

## 서비스 클래스 패턴

`SendgoClient`가 싱글턴으로 등록되어 있으므로, 도메인 서비스에 그대로 주입할 수 있습니다.

```csharp
// Services/NotificationService.cs
using Sendgo;
using Sendgo.Models;

public class NotificationService
{
    private readonly SendgoClient _sendgo;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(SendgoClient sendgo, ILogger<NotificationService> logger)
    {
        _sendgo = sendgo;
        _logger = logger;
    }

    public Task SendOrderConfirmAsync(string phone, string orderNo, int amount) =>
        _sendgo.SendAlimtalkAsync(new AlimtalkRequest
        {
            TemplateCode = "ORDER_CONFIRM_001",
            Contacts     = [new Contact { PhoneNumber = phone, Var1 = orderNo, Var2 = $"{amount:N0}원" }],
        });
}
```

```csharp
// Program.cs
builder.Services.AddScoped<NotificationService>();
```

---

## 예외 처리

```csharp
using Sendgo.Exceptions;

try
{
    await _sendgo.SendAlimtalkAsync(request);
}
catch (SendgoException e)
{
    _logger.LogError("Sendgo 발송 실패 (status={Status}, code={Code}, endpoint={Endpoint})",
        e.StatusCode, e.ErrorCode, e.Endpoint);
    throw;
}
```

---

## 설정 옵션

`SendgoOptions` (섹션 키: `Sendgo`):

| 키 | 기본값 | 설명 |
|----|--------|------|
| `AccessKey` | — | Sendgo 액세스 키 (필수) |
| `SecretKey` | — | Sendgo 시크릿 키 (필수) |
| `KakaoSenderKey` | `null` | 카카오 발신프로필 키 |
| `SmsSenderKey` | `null` | SMS 발신자 키 |
| `ApiVersion` | `v1` | API 버전 (v1 \| v2) |
| `BaseUrl` | `https://sendgo.io` | API 기본 URL |

---

## 자주 묻는 질문 (FAQ)

**Q. `Sendgo.SDK`와의 차이는 무엇인가요?**
A. `Sendgo.SDK`는 프레임워크 독립적인 순수 .NET 코어 패키지입니다. `Sendgo.AspNetCore`는 이를 확장해 `IServiceCollection` 확장 메서드, `appsettings.json` 설정 바인딩 등 ASP.NET Core 통합을 추가합니다.

**Q. `SendgoClient`는 어떤 수명(lifetime)으로 등록되나요?**
A. 싱글턴으로 등록됩니다. `SendgoClient`는 내부적으로 `HttpClient`와 토큰을 재사용하도록 설계되어 있어 싱글턴이 적합합니다.

**Q. DI를 쓰지 않고 직접 생성할 수도 있나요?**
A. 네, `new SendgoClient(new SendgoOptions { ... })`로 직접 생성할 수 있습니다. 이 패키지는 그 등록을 자동화할 뿐입니다.

**Q. 테스트 시 Sendgo를 Mock 처리하려면?**
A. `SendgoClient`는 `sealed`이므로, 도메인 서비스가 의존하는 인터페이스를 별도로 두거나 통합 테스트에서 실제 클라이언트를 사용하는 것을 권장합니다.

---

## 관련 패키지

| 언어/프레임워크 | 패키지 |
|----------------|--------|
| .NET (순수) | `Sendgo.SDK` |
| Laravel | `sendgo/laravel` |
| Spring Boot | `io.sendgo:sendgo-spring` |
| Node.js | `@sendgo/node` |
| 전체 목록 | [send-go GitHub 조직](https://github.com/send-go) |

---

## 라이선스

MIT License © 2026 [Sendgo](https://sendgo.io)

---

*키워드: 카카오 알림톡 ASP.NET Core, 카카오 친구톡 .NET, SMS 발송 ASP.NET Core, 알림톡 .NET 패키지, ASP.NET Core 카카오 API 연동, 의존성 주입 Sendgo, Sendgo ASP.NET Core SDK*
