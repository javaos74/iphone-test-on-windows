# Windows 환경에서 iPhone 테스트 자동화 전략

## 1. 개요

### 1.1 목적
Mac 없이 Windows 환경에서 iPhone 테스트 자동화를 수행하기 위한 아키텍처 설계 및 구현 전략

### 1.2 핵심 아이디어
- WebDriverAgent(WDA)를 사전에 iPhone에 설치 (Mac에서 1회 수행)
- Windows에서 ATX 오픈소스(tidevice/pymobiledevice3)를 활용하여 WDA 실행 및 통신

### 1.3 전체 아키텍처

```
┌─────────────────────────────────────────────────────────────────┐
│                    Windows Test Machine                          │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐    ┌─────────────────┐    ┌─────────────────┐  │
│  │ Test Script │───▶│ WDA HTTP Client │───▶│ Port Relay      │  │
│  │ (Python/JS) │    │ (localhost:8100)│    │ (tidevice relay)│  │
│  └─────────────┘    └─────────────────┘    └────────┬────────┘  │
│                                                      │           │
│  ┌─────────────────────────────────────────────────┐ │           │
│  │           tidevice / pymobiledevice3            │ │           │
│  │  - xcuitest runner                              │◀┘           │
│  │  - port forwarding (relay)                      │             │
│  │  - device management                            │             │
│  └───────────────────────┬─────────────────────────┘             │
│                          │ usbmuxd (via iTunes)                  │
└──────────────────────────┼───────────────────────────────────────┘
                           │ USB
┌──────────────────────────┼───────────────────────────────────────┐
│                      iPhone                                       │
├──────────────────────────┼───────────────────────────────────────┤
│                          ▼                                        │
│  ┌─────────────────────────────────────────────────┐             │
│  │          WebDriverAgent (Pre-installed)         │             │
│  │  - HTTP Server (port 8100)                      │             │
│  │  - XCUITest Framework 연동                      │             │
│  └───────────────────────┬─────────────────────────┘             │
│                          │                                        │
│                          ▼                                        │
│  ┌─────────────────────────────────────────────────┐             │
│  │              Target App (AUT)                   │             │
│  └─────────────────────────────────────────────────┘             │
└──────────────────────────────────────────────────────────────────┘
```

---

## 2. 통신 프로토콜 분석

### 2.1 WebDriverAgent 통신 구조

| Layer | Protocol | Description |
|-------|----------|-------------|
| Test Script → WDA | HTTP/JSON (W3C WebDriver) | RESTful API로 명령 전송 |
| WDA ↔ XCUITest | Native API | iOS 시스템 UI 조작 |
| Windows ↔ iPhone | USB (usbmuxd) | USB를 통한 TCP 포트 포워딩 |

### 2.2 주요 WDA Endpoints

```
POST /session                    # 세션 생성
DELETE /session/{id}             # 세션 종료
POST /session/{id}/element       # 요소 찾기
POST /session/{id}/element/{id}/click  # 클릭
POST /session/{id}/element/{id}/value  # 텍스트 입력
GET /session/{id}/screenshot     # 스크린샷
```

---

## 3. 도구 선택 가이드

### 3.1 iOS 버전별 권장 도구

| iOS Version | 권장 도구 | 비고 |
|-------------|-----------|------|
| iOS 14~16.x | tidevice | 안정적, 간단한 사용법 |
| iOS 17.0~17.3 | pymobiledevice3 | RemoteXPC 필요, 추가 드라이버 필요 |
| iOS 17.4+ | pymobiledevice3 | lockdown tunnel 지원으로 더 안정적 |

> [!IMPORTANT]
> tidevice 프로젝트는 현재 유지보수가 중단된 상태입니다. iOS 17 이상에서는 pymobiledevice3를 사용해야 합니다.

### 3.2 tidevice (Alibaba ATX)

**장점:**
- 간단한 설치 및 사용
- wdaproxy 명령으로 WDA 실행 + 포트 포워딩 자동화
- Python 기반으로 확장 용이

**단점:**
- iOS 17 미지원 (프로젝트 유지보수 중단)
- 엔터프라이즈 인증서로 서명된 WDA 미지원

**설치:**
```bash
pip install -U "tidevice[openssl]"
```

**사용 예시:**
```bash
# 장치 목록 확인
tidevice list

# WDA 실행 + 포트 포워딩
tidevice wdaproxy -B com.facebook.wda.WebDriverAgent.Runner --port 8200

# 포트 릴레이만 수행
tidevice relay 8100 8100
```

### 3.3 pymobiledevice3

**장점:**
- iOS 17+ 완벽 지원
- 활발한 유지보수
- 순수 Python 구현 (크로스 플랫폼)
- 광범위한 기능 (개발자 도구, 시스템 로그 등)

**단점:**
- iOS 17에서 추가 설정 필요 (tunnel)
- tidevice보다 복잡한 명령어

**설치:**
```bash
pip install pymobiledevice3
```

**사용 예시:**
```bash
# 장치 목록 확인
pymobiledevice3 usbmux list

# iOS 17+ 터널 시작 (별도 터미널에서 실행)
python -m pymobiledevice3 remote start-tunnel

# 개발자 디스크 이미지 마운트
pymobiledevice3 mounter auto-mount

# WDA 실행
pymobiledevice3 developer dvt launch com.facebook.wda.WebDriverAgent.Runner

# 포트 포워딩
pymobiledevice3 usbmux forward 8100 8100
```

---

## 4. 구현 전략

### 4.1 사전 준비 (1회성, Mac 필요)

> [!IMPORTANT]
> **좋은 소식**: WDA를 Mac에서 **한 번** 빌드하여 IPA 또는 .app 번들로 만들면, 이후에는 **go-ios를 통해 Windows에서 설치**가 가능합니다!

#### Step 1: WebDriverAgent 빌드 (Mac에서)

```bash
# Mac에서 수행
git clone https://github.com/appium/WebDriverAgent.git
cd WebDriverAgent

# Xcode에서 프로젝트 열기
open WebDriverAgent.xcodeproj
```

**Xcode 설정:**
1. **Signing & Capabilities**에서 개발자 계정 설정
2. **WebDriverAgentRunner** 타겟 선택
3. **Product > Destination > Any iOS Device (arm64)** 선택 (Generic Device)
4. **Product > Build** 실행 (⌘+B)

#### Step 2: WDA .app 번들 추출

빌드 후 .app 파일 위치:
```bash
# 일반적인 경로
~/Library/Developer/Xcode/DerivedData/WebDriverAgent-<random>/Build/Products/Debug-iphoneos/WebDriverAgentRunner-Runner.app

# 경로 찾기
find ~/Library/Developer/Xcode/DerivedData -name "WebDriverAgentRunner-Runner.app" -type d
```

#### Step 3: iOS 17+ 호환성 처리 (중요!)

> [!WARNING]
> iOS 17 이상에서는 XCTest 프레임워크 파일을 제거해야 합니다.

```bash
# WDA 앱 경로로 이동
cd ~/Library/Developer/Xcode/DerivedData/WebDriverAgent-xxx/Build/Products/Debug-iphoneos/

# iOS 17+ 호환성을 위해 XC** 파일 제거
rm -rf WebDriverAgentRunner-Runner.app/Frameworks/XC*

# (선택) 크기 최적화: 불필요한 프레임워크 제거 (3MB 이하로 축소)
rm -rf WebDriverAgentRunner-Runner.app/Frameworks/Testing.framework
rm -rf WebDriverAgentRunner-Runner.app/Frameworks/libXCTestSwiftSupport.dylib
```

#### Step 4: IPA 파일 생성 (선택사항)

.app 번들을 IPA로 패키징:
```bash
# Payload 디렉토리 생성
mkdir -p Payload

# .app 파일 복사
cp -r WebDriverAgentRunner-Runner.app Payload/

# IPA 생성 (ZIP 압축)
zip -r WebDriverAgentRunner.ipa Payload

# 정리
rm -rf Payload
```

#### Step 5: WDA 파일을 Windows로 전송

```bash
# 생성된 파일을 Windows 머신으로 복사
# - WebDriverAgentRunner-Runner.app (폴더) 또는
# - WebDriverAgentRunner.ipa (파일)
```

### 4.2 go-ios로 WDA 설치 (Windows에서)

> [!TIP]
> go-ios는 IPA 파일과 .app 폴더 **모두** 설치를 지원합니다!

#### Step 1: go-ios 설치

```powershell
# GitHub Releases에서 Windows 빌드 다운로드
# https://github.com/danielpaulus/go-ios/releases
# ios.exe를 PATH에 추가하거나 프로젝트 폴더에 복사
```

#### Step 2: 장치 연결 확인

```powershell
# 연결된 장치 목록 확인
ios list --details
```

출력 예시:
```json
[{"udid":"00008030-001A35E40212345678","name":"My iPhone","productVersion":"17.2"}]
```

#### Step 3: WDA 설치

```powershell
# IPA 파일로 설치
ios install --path=WebDriverAgentRunner.ipa

# 또는 .app 폴더로 설치
ios install --path=WebDriverAgentRunner-Runner.app

# 특정 장치에 설치 (여러 장치 연결 시)
ios install --path=WebDriverAgentRunner.ipa --udid=<device-udid>
```

#### Step 4: WDA 실행 (iOS 17+)

```powershell
# iOS 17 이상: 터널 시작 (관리자 권한 필요)
ios tunnel start

# 새 터미널에서 WDA 실행
ios runwda --bundleid=com.facebook.wda.WebDriverAgent.Runner

# 포트 포워딩 (별도 터미널)
ios forward 8100 8100
```

#### Step 5: WDA 실행 (iOS 16 이하)

```powershell
# WDA 실행
ios runwda --bundleid=com.facebook.wda.WebDriverAgent.Runner

# 포트 포워딩 (별도 터미널)
ios forward 8100 8100
```

### 4.3 전체 워크플로우 요약

```
┌────────────────────────────────────────────────────────────────┐
│                    Mac (1회 작업)                              │
├────────────────────────────────────────────────────────────────┤
│  1. WebDriverAgent 소스 클론                                   │
│  2. Xcode에서 빌드 (개발자 인증서로 서명)                      │
│  3. WebDriverAgentRunner-Runner.app 추출                       │
│  4. iOS 17+ 호환성 처리 (XC** 제거)                           │
│  5. IPA 패키징 (선택)                                          │
│  6. Windows로 파일 전송                                        │
└────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────┐
│                    Windows (반복 사용)                         │
├────────────────────────────────────────────────────────────────┤
│  1. go-ios로 WDA 설치: ios install --path=WDA.ipa             │
│  2. (iOS 17+) 터널 시작: ios tunnel start                      │
│  3. WDA 실행: ios runwda --bundleid=...                       │
│  4. 포트 포워딩: ios forward 8100 8100                        │
│  5. 테스트 자동화 실행                                         │
└────────────────────────────────────────────────────────────────┘
```

### 4.4 Bundle ID 확인 및 관리

```powershell
# 설치된 앱 목록 확인
ios apps

# WDA Bundle ID 확인 (일반적인 ID)
# - com.facebook.wda.WebDriverAgent.Runner
# - 커스텀 서명 시 변경될 수 있음

# 특정 앱 검색
ios apps | findstr -i "webdriver"
```

> [!NOTE]
> 개발자 인증서로 서명 시 Bundle ID가 변경될 수 있습니다. 
> 예: `com.yourteam.WebDriverAgentRunner.xctrunner`

### 4.5 인증서 갱신 시 재설치

Apple 개발자 인증서는 유효기간이 있습니다:
- **무료 개발자 계정**: 7일
- **유료 개발자 계정**: 1년

인증서 만료 시:
1. Mac에서 WDA 재빌드 (새 인증서로 서명)
2. 새 IPA/app 파일을 Windows로 전송
3. `ios install --path=...` 재실행

### 4.2 Windows 환경 구성

#### Step 1: 필수 소프트웨어 설치

```powershell
# 1. iTunes 설치 (필수 - USB 드라이버 포함)
# Microsoft Store 또는 Apple 웹사이트에서 다운로드

# 2. Python 3.8+ 설치

# 3. 도구 설치 (iOS 버전에 따라 선택)
# iOS 16 이하
pip install -U "tidevice[openssl]"

# iOS 17 이상
pip install -U pymobiledevice3
```

#### Step 2: 장치 연결 및 신뢰

```powershell
# 장치 연결 확인
tidevice list
# 또는
pymobiledevice3 usbmux list
```

iPhone 화면에서 "이 컴퓨터를 신뢰하시겠습니까?" 팝업이 나타나면 **신뢰** 선택

### 4.3 WDA 실행 및 테스트

#### Option A: tidevice 사용 (iOS 16 이하)

```powershell
# WDA 실행 + 포트 포워딩 (하나의 명령으로)
tidevice wdaproxy -B com.facebook.wda.WebDriverAgent.Runner --port 8100
```

#### Option B: pymobiledevice3 사용 (iOS 17 이상)

```powershell
# 터미널 1: 터널 시작
python -m pymobiledevice3 remote start-tunnel

# 터미널 2: 개발자 디스크 마운트
pymobiledevice3 mounter auto-mount

# 터미널 3: WDA 실행
pymobiledevice3 developer dvt launch com.facebook.wda.WebDriverAgent.Runner

# 터미널 4: 포트 포워딩
pymobiledevice3 usbmux forward 8100 8100
```

### 4.4 테스트 자동화 클라이언트 예제

#### Python (facebook-wda 라이브러리)

```python
import wda

# WDA 연결
client = wda.Client("http://localhost:8100")

# 장치 정보 확인
print(client.info)

# 세션 시작
session = client.session()

# 요소 찾기 및 클릭
session(label="로그인").click()

# 텍스트 입력
session(type="XCUIElementTypeTextField").set_text("username")

# 스크린샷
session.screenshot().save("screenshot.png")
```

#### HTTP API 직접 호출 (언어 무관)

```python
import requests

BASE_URL = "http://localhost:8100"

# 세션 생성
response = requests.post(f"{BASE_URL}/session", json={
    "capabilities": {
        "alwaysMatch": {
            "platformName": "iOS"
        }
    }
})
session_id = response.json()["sessionId"]

# 요소 찾기
response = requests.post(f"{BASE_URL}/session/{session_id}/element", json={
    "using": "accessibility id",
    "value": "Login"
})
element_id = response.json()["value"]["ELEMENT"]

# 클릭
requests.post(f"{BASE_URL}/session/{session_id}/element/{element_id}/click")
```

---

## 5. .NET 환경 구현 옵션

> [!IMPORTANT]
> .NET 환경에서 Windows 기반 iOS 자동화를 구현하기 위한 여러 옵션이 있습니다.

### 5.1 아키텍처 개요 (.NET)

```
┌─────────────────────────────────────────────────────────────────┐
│                    Windows Test Machine (.NET)                   │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐    ┌─────────────────────────────────────┐ │
│  │ Test Framework  │    │ WDA Communication Layer (택1)       │ │
│  │ (NUnit/xUnit)   │───▶│  Option A: HttpClient (직접 HTTP)  │ │
│  └─────────────────┘    │  Option B: Appium.WebDriver        │ │
│                         │  Option C: go-ios REST API         │ │
│                         └───────────────────┬─────────────────┘ │
│                                             │                   │
│  ┌─────────────────────────────────────────┐│                   │
│  │ Device Layer (택1)                      ││                   │
│  │  Option A: go-ios (실행파일)            │◀┘                  │
│  │  Option B: imobiledevice-net (NuGet)    │                    │
│  └───────────────────────┬─────────────────┘                    │
│                          │ usbmuxd (via iTunes)                 │
└──────────────────────────┼──────────────────────────────────────┘
                           │ USB
                      [iPhone with WDA]
```

### 5.2 Option A: go-ios (권장 - 가장 완성도 높음)

**go-ios**는 Go 언어로 구현된 크로스 플랫폼 iOS 도구로, **REST API**와 **CLI**를 제공합니다.

**장점:**
- iOS 17+ 완벽 지원 (`ios tunnel` 명령)
- 단일 실행 파일로 배포 (의존성 없음)
- REST API 제공으로 모든 언어에서 사용 가능
- `ios runwda` 명령으로 WDA 실행 지원
- 활발한 유지보수

**설치:**
```powershell
# GitHub Releases에서 Windows 빌드 다운로드
# https://github.com/danielpaulus/go-ios/releases
# ios.exe를 PATH에 추가
```

**WDA 실행:**
```powershell
# WDA 실행
ios runwda --bundleid=com.facebook.wda.WebDriverAgent.Runner

# 포트 포워딩 (별도 터미널)
ios forward 8100 8100

# iOS 17+: 터널 시작 필요
ios tunnel start
```

**REST API 사용 (실험적):**
```powershell
# REST API 서버 시작
ios restapi --port 8080
```

**.NET에서 go-ios 사용 (Process 실행):**

```csharp
using System.Diagnostics;
using System.Text.Json;

public class GoiOSManager
{
    private readonly string _goiOSPath;

    public GoiOSManager(string goiOSPath = "ios.exe")
    {
        _goiOSPath = goiOSPath;
    }

    // 연결된 장치 목록 가져오기
    public async Task<List<DeviceInfo>> ListDevicesAsync()
    {
        var output = await RunCommandAsync("list --details");
        return JsonSerializer.Deserialize<List<DeviceInfo>>(output);
    }

    // WDA 실행 (백그라운드)
    public Process StartWda(string bundleId, string udid = null)
    {
        var args = $"runwda --bundleid={bundleId}";
        if (!string.IsNullOrEmpty(udid))
            args += $" --udid={udid}";

        return StartBackgroundProcess(args);
    }

    // 포트 포워딩
    public Process StartForward(int hostPort, int devicePort, string udid = null)
    {
        var args = $"forward {hostPort} {devicePort}";
        if (!string.IsNullOrEmpty(udid))
            args += $" --udid={udid}";

        return StartBackgroundProcess(args);
    }

    private async Task<string> RunCommandAsync(string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _goiOSPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        return await process.StandardOutput.ReadToEndAsync();
    }

    private Process StartBackgroundProcess(string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _goiOSPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        return process;
    }
}

public record DeviceInfo(string Udid, string Name, string ProductVersion);
```

### 5.3 Option B: imobiledevice-net (NuGet 패키지)

**imobiledevice-net**은 libimobiledevice의 .NET 바인딩입니다.

**장점:**
- 순수 .NET 라이브러리 (NuGet)
- 장치 관리, 앱 설치/제거, 파일 시스템 접근 등
- 타입 안전성, IntelliSense 지원

**단점:**
- WDA 실행 기능 없음 (XCTest 실행 불가)
- iOS 17 지원 미확인
- 포트 포워딩 기능 제한적

**설치:**
```powershell
Install-Package imobiledevice-net
```

**사용 예시:**
```csharp
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;

// 초기화
NativeLibraries.Load();

var idevice = LibiMobileDevice.Instance.iDevice;
var lockdown = LibiMobileDevice.Instance.Lockdown;

// 장치 목록 가져오기
idevice.idevice_get_device_list(out var udids, ref var count);

foreach (var udid in udids)
{
    // 장치 연결
    idevice.idevice_new(out var deviceHandle, udid).ThrowOnError();
    
    // Lockdown 클라이언트 생성
    lockdown.lockdownd_client_new_with_handshake(
        deviceHandle, out var lockdownHandle, "MyApp").ThrowOnError();
    
    // 장치 이름 가져오기
    lockdown.lockdownd_get_device_name(lockdownHandle, out var deviceName);
    Console.WriteLine($"Device: {deviceName} ({udid})");
    
    lockdownHandle.Dispose();
    deviceHandle.Dispose();
}
```

> [!WARNING]
> imobiledevice-net은 WDA/XCTest 실행을 지원하지 않습니다. 장치 관리용으로만 사용하고, WDA 실행은 go-ios를 함께 사용하세요.

### 5.4 Option C: Appium.WebDriver (NuGet 패키지)

Appium 서버가 별도로 실행 중이라면 **Appium.WebDriver**를 사용할 수 있습니다.

**설치:**
```powershell
Install-Package Appium.WebDriver
```

**사용 예시:**
```csharp
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.iOS;

var options = new AppiumOptions();
options.PlatformName = "iOS";
options.AutomationName = "XCUITest";
options.AddAdditionalAppiumOption("webDriverAgentUrl", "http://localhost:8100");
options.AddAdditionalAppiumOption("usePrebuiltWDA", true);

// Appium 서버 연결 (로컬에서 실행 중이어야 함)
using var driver = new IOSDriver(new Uri("http://localhost:4723"), options);

// 요소 찾기 및 클릭
var element = driver.FindElement(MobileBy.AccessibilityId("Login"));
element.Click();
```

> [!NOTE]
> 이 방식은 Appium 서버가 필요합니다. Appium 없이 직접 WDA와 통신하려면 HttpClient를 사용하세요.

### 5.5 Option D: 직접 HTTP 통신 (HttpClient)

WDA는 HTTP JSON API를 제공하므로, .NET HttpClient로 직접 통신할 수 있습니다.

**WDA HTTP 클라이언트 구현:**
```csharp
using System.Net.Http.Json;
using System.Text.Json;

public class WdaClient : IDisposable
{
    private readonly HttpClient _client;
    private string? _sessionId;

    public WdaClient(string baseUrl = "http://localhost:8100")
    {
        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    // 상태 확인
    public async Task<JsonElement> GetStatusAsync()
    {
        var response = await _client.GetFromJsonAsync<JsonElement>("/status");
        return response;
    }

    // 세션 생성
    public async Task<string> CreateSessionAsync()
    {
        var payload = new
        {
            capabilities = new
            {
                alwaysMatch = new { platformName = "iOS" }
            }
        };

        var response = await _client.PostAsJsonAsync("/session", payload);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        _sessionId = result.GetProperty("sessionId").GetString();
        return _sessionId!;
    }

    // 요소 찾기
    public async Task<string> FindElementAsync(string strategy, string value)
    {
        var payload = new { @using = strategy, value };
        var response = await _client.PostAsJsonAsync($"/session/{_sessionId}/element", payload);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return result.GetProperty("value").GetProperty("ELEMENT").GetString()!;
    }

    // 요소 클릭
    public async Task ClickElementAsync(string elementId)
    {
        await _client.PostAsJsonAsync($"/session/{_sessionId}/element/{elementId}/click", new { });
    }

    // 텍스트 입력
    public async Task SendKeysAsync(string elementId, string text)
    {
        var payload = new { value = text.ToCharArray() };
        await _client.PostAsJsonAsync($"/session/{_sessionId}/element/{elementId}/value", payload);
    }

    // 스크린샷
    public async Task<byte[]> TakeScreenshotAsync()
    {
        var response = await _client.GetFromJsonAsync<JsonElement>($"/session/{_sessionId}/screenshot");
        var base64 = response.GetProperty("value").GetString();
        return Convert.FromBase64String(base64!);
    }

    // 세션 종료
    public async Task DeleteSessionAsync()
    {
        if (_sessionId != null)
        {
            await _client.DeleteAsync($"/session/{_sessionId}");
            _sessionId = null;
        }
    }

    public void Dispose() => _client.Dispose();
}
```

**사용 예시:**
```csharp
// go-ios로 WDA 실행 + 포트 포워딩 후

using var wda = new WdaClient("http://localhost:8100");

// 상태 확인
var status = await wda.GetStatusAsync();
Console.WriteLine($"WDA Status: {status}");

// 세션 생성
var sessionId = await wda.CreateSessionAsync();
Console.WriteLine($"Session ID: {sessionId}");

// 요소 찾기 및 클릭
var loginButton = await wda.FindElementAsync("accessibility id", "Login");
await wda.ClickElementAsync(loginButton);

// 스크린샷 저장
var screenshot = await wda.TakeScreenshotAsync();
await File.WriteAllBytesAsync("screenshot.png", screenshot);

// 세션 종료
await wda.DeleteSessionAsync();
```

### 5.6 권장 조합

| iOS 버전 | Device Layer | WDA 실행 | WDA 통신 |
|----------|--------------|---------|----------|
| iOS 14~16 | go-ios | `ios runwda` | HttpClient 직접 통신 |
| iOS 17+ | go-ios + tunnel | `ios tunnel start` + `ios runwda` | HttpClient 직접 통신 |

**권장 아키텍처:**
1. **go-ios** (CLI): WDA 실행, 포트 포워딩, 장치 관리
2. **HttpClient**: WDA와 직접 HTTP 통신
3. **(선택) imobiledevice-net**: 추가 장치 관리 기능이 필요한 경우

---

## 6. 프로젝트 구조 제안

```
iphone-tester/
├── README.md                    # 프로젝트 문서
├── planning.md                  # 이 문서
├── requirements.txt             # Python 의존성
├── config/
│   └── device_config.yaml       # 장치 설정 (UDID, Bundle ID 등)
├── src/
│   ├── __init__.py
│   ├── device_manager.py        # 장치 연결/관리
│   ├── wda_launcher.py          # WDA 실행/관리
│   ├── wda_client.py            # WDA HTTP 클라이언트
│   └── automation/
│       ├── __init__.py
│       ├── base_test.py         # 테스트 기본 클래스
│       └── page_objects/        # 페이지 오브젝트 패턴
├── tests/
│   ├── __init__.py
│   └── test_basic_flow.py       # 기본 테스트 케이스
└── scripts/
    ├── setup_windows.ps1        # Windows 환경 설정 스크립트
    └── start_wda.ps1            # WDA 시작 스크립트
```

---

## 7. 제약사항 및 고려사항

### 6.1 제약사항

| 제약 | 설명 | 해결방안 |
|------|------|----------|
| WDA 초기 설치 | Mac + Xcode 필요 (1회) | CI/CD Mac Runner 활용 또는 외부 서비스 |
| 인증서 갱신 | 개발자 인증서 유효기간 관리 | Apple Developer Program 가입 (1년) |
| iOS 업데이트 | 새 iOS 버전에서 호환성 이슈 가능 | pymobiledevice3 최신 버전 유지 |
| 케이블 품질 | 저품질 케이블에서 연결 불안정 | MFi 인증 케이블 사용 권장 |

### 6.2 엔터프라이즈 환경 고려사항

- **다중 장치 관리**: UDID 기반으로 여러 장치 동시 관리 가능
- **CI/CD 통합**: Jenkins/GitHub Actions에서 Windows Runner 활용
- **인증서 관리**: Apple Developer Enterprise Program 활용 검토

---

## 8. 검증 계획

### 7.1 환경 검증

```bash
# 1. iTunes 설치 확인
# Windows 서비스에서 "Apple Mobile Device Service" 실행 확인

# 2. 장치 연결 확인
tidevice list  # 또는 pymobiledevice3 usbmux list

# 3. 개발자 모드 확인 (iOS 16+)
# 설정 > 개인 정보 보호 및 보안 > 개발자 모드 켜짐 확인
```

### 7.2 WDA 실행 검증

```bash
# WDA 시작 후 상태 확인
curl http://localhost:8100/status
# 정상 응답: {"value": {"state": "success", ...}}
```

### 7.3 자동화 검증

```python
# 기본 테스트
import wda

client = wda.Client("http://localhost:8100")
assert client.status()["state"] == "success"
print("✓ WDA 연결 성공")

session = client.session()
print(f"✓ 세션 생성 성공: {session.id}")
```

---

## 9. 다음 단계

1. [ ] WDA를 Mac에서 빌드하여 iPhone에 설치
2. [ ] Windows 환경 구성 (iTunes, Python, tidevice/pymobiledevice3)
3. [ ] 장치 연결 및 WDA 실행 테스트
4. [ ] 기본 자동화 클라이언트 구현
5. [ ] 테스트 프레임워크 구축

---

## 10. 참고 자료

### Python 도구
- [WebDriverAgent GitHub](https://github.com/appium/WebDriverAgent)
- [tidevice GitHub](https://github.com/alibaba/tidevice) (iOS 16 이하)
- [tidevice3 GitHub](https://github.com/codeskyblue/tidevice3) (iOS 17+, tidevice 후속)
- [pymobiledevice3 GitHub](https://github.com/doronz88/pymobiledevice3)
- [facebook-wda Python Client](https://github.com/openatx/facebook-wda)

### .NET / 크로스 플랫폼 도구
- [go-ios GitHub](https://github.com/danielpaulus/go-ios) - Go 기반, REST API, WDA 실행 지원 (권장)
- [imobiledevice-net NuGet](https://www.nuget.org/packages/imobiledevice-net) - .NET libimobiledevice 바인딩
- [Appium .NET Client](https://github.com/appium/dotnet-client) - Appium WebDriver 클라이언트
- [libimobiledevice-win32](https://github.com/libimobiledevice-win32) - Windows용 libimobiledevice 바이너리

### 기타
- [Appium XCUITest Driver](https://appium.github.io/appium-xcuitest-driver/)
- [W3C WebDriver Protocol](https://www.w3.org/TR/webdriver/)
