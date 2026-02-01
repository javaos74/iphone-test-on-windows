# Implementation Plan: UiPath WDA Connection Activity

## Overview

이 구현 계획은 Windows 환경에서 iOS 기기의 WDA를 실행하고 연결을 설정하기 위한 UiPath Custom Activity 패키지를 개발합니다. go-ios CLI를 래핑하여 터널, WDA, 포트 포워딩을 관리하며, 연결 설정 후 UiPath의 기존 Mobile Device Management Activity를 사용할 수 있게 합니다.

## Tasks

- [x] 1. 프로젝트 구조 및 기본 설정
  - [x] 1.1 솔루션 및 프로젝트 생성
    - UiPath.iOS.WdaConnection.Activities 클래스 라이브러리 프로젝트 생성 (.NET Framework 4.6.1 및 .NET 6 멀티타겟)
    - UiPath.iOS.WdaConnection.Activities.Design 프로젝트 생성 (Activity 디자이너)
    - UiPath.iOS.WdaConnection.Tests 테스트 프로젝트 생성
    - _Requirements: 9.1, 9.2_
  
  - [x] 1.2 NuGet 패키지 참조 추가
    - UiPath.Workflow 패키지 참조
    - System.Text.Json 패키지 참조
    - FsCheck, FsCheck.Xunit 패키지 참조 (테스트 프로젝트)
    - _Requirements: 9.5_
  
  - [x] 1.3 go-ios 실행 파일 임베딩 설정
    - go-ios.exe를 Embedded Resource로 추가
    - 런타임 추출 로직 구현
    - _Requirements: 2.1, 2.2_

- [x] 2. 데이터 모델 및 예외 클래스 구현
  - [x] 2.1 데이터 모델 클래스 구현
    - DeviceInfo record 구현 (UDID, Name, ProductVersion, RequiresTunnel 속성)
    - WdaConnectionConfig record 구현
    - WdaStatus, WdaOsInfo, WdaBuildInfo record 구현
    - _Requirements: 1.1, 7.1_
  
  - [x] 2.2 ManagedProcess 클래스 구현
    - Process 래핑 및 stdout/stderr 캡처 로직
    - IsRunning 속성 구현
    - IDisposable 구현
    - _Requirements: 2.3, 3.5_
  
  - [x] 2.3 예외 클래스 구현
    - WdaConnectionException 기본 클래스
    - DeviceNotFoundException, WdaNotReadyException, GoiOSException, PortInUseException 구현
    - _Requirements: 8.1, 8.3, 8.4_
  
  - [ ]* 2.4 데이터 모델 Property 테스트 작성
    - **Property 1: Device Information Parsing Completeness**
    - **Validates: Requirements 1.1**

- [x] 3. Checkpoint - 데이터 모델 검증
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Core Services 구현 - ProcessManager
  - [x] 4.1 IProcessManager 인터페이스 및 구현
    - StartProcess 메서드 구현 (stdout/stderr 비동기 캡처)
    - WaitForExitAsync 메서드 구현
    - KillProcess 메서드 구현
    - IsRunning 메서드 구현
    - _Requirements: 2.3, 3.3, 4.3, 5.3_
  
  - [ ]* 4.2 ProcessManager Property 테스트 작성
    - **Property 5: Process Lifecycle Management**
    - **Property 6: Process State Consistency**
    - **Validates: Requirements 3.3, 3.5, 4.3, 5.3**

- [x] 5. Core Services 구현 - GoiOSService
  - [x] 5.1 IGoiOSService 인터페이스 정의
    - ListDevicesAsync, StartTunnelAsync, StartWdaAsync, StartForwardAsync, StopProcessAsync 메서드 시그니처
    - _Requirements: 1.1, 3.1, 4.1, 5.1_
  
  - [x] 5.2 GoiOSService 구현 - 장치 목록
    - `ios list --details` 명령 실행
    - JSON 파싱하여 DeviceInfo 리스트 반환
    - _Requirements: 1.1_
  
  - [x] 5.3 GoiOSService 구현 - 터널 관리
    - `ios tunnel start` 명령 실행 (iOS 17+)
    - 터널 프로세스 관리
    - _Requirements: 3.1, 3.2, 3.3_
  
  - [x] 5.4 GoiOSService 구현 - WDA 실행
    - `ios runwda --bundleid=<bundle_id>` 명령 실행
    - WDA 프로세스 관리
    - _Requirements: 4.1, 4.3_
  
  - [x] 5.5 GoiOSService 구현 - 포트 포워딩
    - `ios forward <local_port> <device_port>` 명령 실행
    - 포트 포워딩 프로세스 관리
    - _Requirements: 5.1, 5.3_
  
  - [ ]* 5.6 GoiOSService Property 테스트 작성
    - **Property 2: Command Output Capture**
    - **Property 3: Timeout Handling Consistency**
    - **Property 4: iOS Version-Based Tunnel Behavior**
    - **Validates: Requirements 2.3, 2.4, 3.1, 3.2, 4.4**

- [x] 6. Checkpoint - GoiOSService 검증
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Core Services 구현 - WdaStatusClient
  - [x] 7.1 IWdaStatusClient 인터페이스 및 구현
    - GetStatusAsync 메서드 구현 (HTTP GET /status)
    - WaitForReadyAsync 메서드 구현 (폴링 with 타임아웃)
    - _Requirements: 7.1, 7.2, 4.1, 5.5_
  
  - [ ]* 7.2 WdaStatusClient Property 테스트 작성
    - **Property 13: WDA Status Retrieval**
    - **Validates: Requirements 7.1, 7.2**

- [x] 8. UiPath Activity 구현 - 기본 Activity
  - [x] 8.1 GetDeviceList Activity 구현
    - GoiOSService.ListDevicesAsync 호출
    - 결과를 OutArgument로 반환
    - _Requirements: 1.1, 1.2_
  
  - [x] 8.2 CheckWdaStatus Activity 구현
    - WdaStatusClient.GetStatusAsync 호출
    - 결과를 OutArgument로 반환
    - _Requirements: 7.1, 7.2, 7.3_
  
  - [x] 8.3 기본 Activity Unit 테스트 작성
    - GetDeviceList 테스트 (빈 목록, 여러 장치)
    - CheckWdaStatus 테스트 (성공, 연결 실패)
    - _Requirements: 1.1, 1.2, 7.1, 7.2_

- [x] 9. UiPath Activity 구현 - 개별 제어 Activity
  - [x] 9.1 StartTunnel Activity 구현
    - GoiOSService.StartTunnelAsync 호출
    - ManagedProcess를 OutArgument로 반환
    - _Requirements: 3.1, 3.2_
  
  - [x] 9.2 StartWda Activity 구현
    - GoiOSService.StartWdaAsync 호출
    - ManagedProcess를 OutArgument로 반환
    - _Requirements: 4.1, 4.2, 4.5_
  
  - [x] 9.3 StartPortForward Activity 구현
    - GoiOSService.StartForwardAsync 호출
    - ManagedProcess를 OutArgument로 반환
    - _Requirements: 5.1, 5.2, 5.4_
  
  - [x] 9.4 StopManagedProcess Activity 구현
    - ManagedProcess.Dispose 호출
    - _Requirements: 3.3, 4.3, 5.3_
  
  - [x] 9.5 개별 제어 Activity Unit 테스트 작성
    - 각 Activity의 정상 동작 및 에러 케이스 테스트
    - _Requirements: 3.1-3.3, 4.1-4.3, 5.1-5.3_

- [x] 10. Checkpoint - 개별 Activity 검증
  - Ensure all tests pass, ask the user if questions arise.

- [x] 11. UiPath Activity 구현 - WdaConnectionScope
  - [x] 11.1 WdaConnectionScope NativeActivity 구현
    - Execute 메서드에서 초기화 시퀀스 구현 (장치 확인 → 터널 → WDA → 포트포워딩 → 상태확인)
    - Body Activity 실행
    - _Requirements: 6.1_
  
  - [x] 11.2 WdaConnectionScope 정리 로직 구현
    - try-finally 패턴으로 cleanup 보장
    - 역순 정리 (포트포워딩 → WDA → 터널)
    - _Requirements: 6.2, 6.3_
  
  - [x] 11.3 WdaConnectionScope 출력 및 검증 구현
    - WdaEndpointUrl 출력 설정
    - 중첩 Scope 검사 및 예외 발생
    - _Requirements: 6.4, 6.5, 6.6_
  
  - [ ]* 11.4 WdaConnectionScope Property 테스트 작성
    - **Property 9: Scope Initialization Sequence**
    - **Property 10: Scope Cleanup Guarantee**
    - **Property 11: Scope Output Availability**
    - **Property 12: Nested Scope Rejection**
    - **Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5**

- [x] 12. Checkpoint - WdaConnectionScope 검증
  - Ensure all tests pass, ask the user if questions arise.

- [x] 13. 에러 처리 및 로깅 구현
  - [x] 13.1 예외 메시지 포맷팅 구현
    - Activity 이름, 작업, 실패 이유 포함
    - go-ios 출력 포함
    - HTTP 상태 코드 및 응답 본문 포함
    - _Requirements: 8.1, 8.3, 8.4_
  
  - [x] 13.2 UiPath 로깅 통합
    - ActivityLogger 헬퍼 클래스 구현
    - Debug, Info, Warning, Error 레벨 로깅
    - _Requirements: 8.2, 8.5_
  
  - [ ]* 13.3 예외 메시지 Property 테스트 작성
    - **Property 14: Exception Message Completeness**
    - **Validates: Requirements 8.1, 8.3, 8.4**

- [x] 14. UiPath Designer 메타데이터 구현
  - [x] 14.1 Activity 카테고리 및 아이콘 설정
    - CategoryAttribute 적용
    - ToolboxBitmap 설정
    - _Requirements: 9.3_
  
  - [x] 14.2 Property 설명 및 기본값 설정
    - DisplayName, Description 속성 적용
    - DefaultValue 설정
    - _Requirements: 9.3, 9.4_
  
  - [x] 14.3 Activity Designer XAML 작성 (선택사항)
    - WdaConnectionScope용 커스텀 디자이너
    - _Requirements: 9.3_

- [x] 15. NuGet 패키지 구성
  - [x] 15.1 nuspec 파일 작성
    - 패키지 메타데이터 (ID, 버전, 설명, 작성자)
    - 의존성 정의
    - _Requirements: 9.5_
  
  - [x] 15.2 패키지 빌드 및 검증
    - NuGet pack 실행
    - UiPath Studio에서 설치 테스트
    - _Requirements: 9.5, 9.6_

- [x] 16. Final Checkpoint - 전체 통합 검증
  - Ensure all tests pass, ask the user if questions arise.
  - UiPath Studio에서 Activity 동작 확인
  - 실제 iOS 기기 연결 테스트 (가능한 경우)

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties
- Unit tests validate specific examples and edge cases
- go-ios 실행 파일은 [go-ios releases](https://github.com/danielpaulus/go-ios/releases) 에서 다운로드
- 실제 iOS 기기 테스트는 WDA가 사전 설치된 기기 필요
