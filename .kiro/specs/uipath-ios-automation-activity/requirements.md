# Requirements Document

## Introduction

이 문서는 Windows 환경에서 미리 설치된 WebDriverAgent(WDA)를 구동하여 UiPath의 기존 Mobile Device Management 기능과 연동할 수 있도록 하는 UiPath Custom Activity 패키지의 요구사항을 정의합니다.

UiPath는 이미 Mobile Device Management 및 iOS 자동화 기능을 제공하지만, MacOS 환경이 필수입니다. 이 Activity 패키지는 WDA가 사전 설치된 iPhone에서 Windows 환경만으로 WDA를 실행하고 연결을 설정하여, 이후 UiPath의 기존 모바일 자동화 Activity들을 사용할 수 있게 합니다.

### Scope

이 패키지는 다음 기능만 제공합니다:
- iOS 장치 연결 확인
- iOS 17+ 터널 관리
- WDA 실행 및 관리
- 포트 포워딩 설정
- WDA 연결 상태 확인

UI 요소 조작, 스크린샷, 앱 관리 등의 기능은 UiPath의 기존 Mobile Device Management Activity를 사용합니다.

## Glossary

- **WDA (WebDriverAgent)**: Apple의 XCUITest 프레임워크를 기반으로 한 WebDriver 서버로, iOS 기기에서 실행되어 HTTP API를 통해 UI 자동화 명령을 수신합니다.
- **go-ios**: Go 언어로 구현된 크로스 플랫폼 iOS 도구로, CLI를 제공하며 iOS 17+를 지원합니다.
- **UiPath_Activity**: UiPath Studio에서 사용할 수 있는 재사용 가능한 자동화 컴포넌트입니다.
- **Port_Forwarding**: USB를 통해 연결된 iOS 기기의 포트를 Windows 로컬 포트로 매핑하는 기능입니다.
- **UDID**: iOS 기기의 고유 식별자(Unique Device Identifier)입니다.
- **Bundle_ID**: iOS 앱의 고유 식별자입니다.
- **Tunnel**: iOS 17 이상에서 필요한 보안 통신 채널입니다.
- **Scope_Activity**: 다른 Activity들이 실행되는 컨텍스트를 제공하는 컨테이너 Activity입니다.

## Requirements

### Requirement 1: iOS 장치 연결 확인

**User Story:** As a UiPath 개발자, I want to 연결된 iOS 장치를 확인할 수 있어서, so that WDA를 실행할 대상 장치를 식별할 수 있습니다.

#### Acceptance Criteria

1. WHEN the Get_Device_List Activity is executed, THE Activity SHALL return a list of connected iOS devices with UDID, device name, and iOS version information.
2. WHEN no iOS devices are connected, THE Get_Device_List Activity SHALL return an empty list without throwing an exception.
3. WHEN iTunes is not installed on the Windows machine, THE Activity SHALL throw a descriptive exception indicating the prerequisite is missing.

### Requirement 2: go-ios CLI 관리

**User Story:** As a UiPath 개발자, I want to go-ios CLI를 자동으로 관리할 수 있어서, so that 별도의 수동 설치 없이 Activity를 사용할 수 있습니다.

#### Acceptance Criteria

1. THE Activity_Package SHALL include the go-ios executable as an embedded resource or provide automatic download capability.
2. WHEN the go-ios executable is not found in the expected path, THE Activity SHALL attempt to extract or download it automatically.
3. WHEN a go-ios command is executed, THE Activity SHALL capture both stdout and stderr for logging and error handling.
4. WHEN a go-ios command times out, THE Activity SHALL terminate the process and throw a timeout exception with the elapsed time.
5. THE Activity SHALL support configuring a custom go-ios executable path through Activity properties.

### Requirement 3: iOS 17+ 터널 관리

**User Story:** As a UiPath 개발자, I want to iOS 17 이상 기기에서 필요한 터널을 관리할 수 있어서, so that 최신 iOS 버전에서도 WDA를 실행할 수 있습니다.

#### Acceptance Criteria

1. WHEN the Start_Tunnel Activity is executed for an iOS 17+ device, THE Activity SHALL start the tunnel process and wait until the tunnel is established.
2. WHEN the Start_Tunnel Activity is executed for an iOS 16 or lower device, THE Activity SHALL skip tunnel creation and log an informational message.
3. WHEN the Stop_Tunnel Activity is executed, THE Activity SHALL terminate the tunnel process gracefully.
4. IF the tunnel process terminates unexpectedly, THEN THE Activity SHALL detect this and provide a mechanism to restart the tunnel.
5. THE Tunnel_Manager SHALL track the tunnel process state and provide status information to other Activities.

### Requirement 4: WDA 실행 및 관리

**User Story:** As a UiPath 개발자, I want to WDA를 실행하고 관리할 수 있어서, so that iOS 기기에서 자동화 서버를 시작할 수 있습니다.

#### Acceptance Criteria

1. WHEN the Start_WDA Activity is executed with a valid Bundle_ID, THE Activity SHALL launch WDA on the target device and wait until WDA is ready to accept connections.
2. WHEN the Start_WDA Activity is executed with an invalid Bundle_ID, THE Activity SHALL throw a descriptive exception indicating the app was not found.
3. WHEN the Stop_WDA Activity is executed, THE Activity SHALL terminate the WDA process on the device.
4. WHEN WDA fails to start within the configured timeout, THE Activity SHALL throw a timeout exception with diagnostic information.
5. THE Activity SHALL support specifying the WDA Bundle_ID as a configurable property with a default value of "com.facebook.wda.WebDriverAgent.Runner".

### Requirement 5: 포트 포워딩 관리

**User Story:** As a UiPath 개발자, I want to 포트 포워딩을 설정하고 관리할 수 있어서, so that Windows에서 iOS 기기의 WDA 서버에 접근할 수 있습니다.

#### Acceptance Criteria

1. WHEN the Start_Port_Forward Activity is executed, THE Activity SHALL establish port forwarding from the specified local port to the device port.
2. WHEN the specified local port is already in use, THE Activity SHALL throw a descriptive exception indicating the port conflict.
3. WHEN the Stop_Port_Forward Activity is executed, THE Activity SHALL terminate the port forwarding process.
4. THE Activity SHALL support configuring both local and device ports with default values of 8100.
5. WHEN port forwarding is established, THE Activity SHALL verify connectivity by attempting to reach the WDA status endpoint.

### Requirement 6: WDA Connection Scope

**User Story:** As a UiPath 개발자, I want to WDA 연결을 위한 Scope Activity를 사용하여, so that 터널, WDA, 포트 포워딩을 한 번에 설정하고 자동으로 정리할 수 있습니다.

#### Acceptance Criteria

1. WHEN the WDA_Connection_Scope Activity is entered, THE Activity SHALL automatically perform device validation, tunnel setup (if iOS 17+), WDA launch, and port forwarding in sequence.
2. WHEN the WDA_Connection_Scope Activity is exited normally, THE Activity SHALL automatically clean up resources in reverse order: stop port forwarding, stop WDA, stop tunnel.
3. IF an exception occurs within the WDA_Connection_Scope, THEN THE Activity SHALL still perform cleanup before propagating the exception.
4. THE WDA_Connection_Scope SHALL expose the WDA endpoint URL (e.g., http://localhost:8100) to child Activities through an output property.
5. WHEN multiple WDA_Connection_Scope Activities are nested, THE Activity SHALL throw an exception indicating nested scopes are not supported.
6. THE WDA_Connection_Scope SHALL support configuring timeout values for each initialization step.

### Requirement 7: WDA 상태 확인

**User Story:** As a UiPath 개발자, I want to WDA 연결 상태를 확인할 수 있어서, so that UiPath Mobile Activity를 사용하기 전에 연결이 정상인지 검증할 수 있습니다.

#### Acceptance Criteria

1. WHEN the Check_WDA_Status Activity is executed, THE Activity SHALL return the WDA server status including state and session information.
2. WHEN the WDA server is not reachable, THE Activity SHALL throw a connection exception with the target URL.
3. THE Activity SHALL support configuring the WDA endpoint URL with a default value of "http://localhost:8100".

### Requirement 8: 에러 처리 및 로깅

**User Story:** As a UiPath 개발자, I want to 명확한 에러 메시지와 로깅을 통해 문제를 진단할 수 있어서, so that WDA 연결 실패 시 원인을 빠르게 파악할 수 있습니다.

#### Acceptance Criteria

1. WHEN any Activity throws an exception, THE exception message SHALL include the Activity name, operation attempted, and specific failure reason.
2. THE Activity_Package SHALL integrate with UiPath's logging framework to provide debug, info, warning, and error level logs.
3. WHEN a go-ios command fails, THE Activity SHALL include the command output in the exception message.
4. WHEN a WDA HTTP request fails, THE Activity SHALL include the HTTP status code and response body in the exception message.
5. THE Activity_Package SHALL support configuring verbose logging mode for detailed diagnostic output.

### Requirement 9: UiPath 통합

**User Story:** As a UiPath 개발자, I want to Activity가 UiPath Studio와 완벽하게 통합되어, so that 다른 UiPath Activity들과 동일한 방식으로 사용할 수 있습니다.

#### Acceptance Criteria

1. THE Activity_Package SHALL be compatible with UiPath Studio 2021.10 or later versions.
2. THE Activity_Package SHALL support both .NET Framework 4.6.1+ and .NET 6+ runtimes.
3. THE Activities SHALL provide proper designer metadata including icons, categories, and property descriptions.
4. THE Activities SHALL support UiPath's Input/Output argument binding for all properties.
5. THE Activity_Package SHALL be distributable as a NuGet package compatible with UiPath's package management.
6. THE Activities SHALL support execution in both attended and unattended automation scenarios.
