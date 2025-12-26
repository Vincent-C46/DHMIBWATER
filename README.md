# DHBIMWATER

배수지(Distribution Reservoir) 모델링, 수량산출, 도면화 자동화 프로젝트

## 프로젝트 개요

DHBIMWATER는 Revit을 활용한 배수지 설계 자동화 솔루션입니다.
Clean Architecture와 DDD(Domain-Driven Design) 원칙을 기반으로 구축되었습니다.

### 주요 기능

- **모델링**: 배수지 구조물(벽체, 슬래브, 기둥, 보, 배관) 자동 생성
- **수량산출**: 콘크리트, 거푸집, 철근, 방수 등 물량 자동 산출
- **도면화**: 평면도, 단면도, 입면도, 시트 자동 생성

## 프로젝트 구조

```
DHBIMWATER/
├── src/
│   ├── DHBIMWATER.Core/              # 핵심 도메인 레이어 (Revit 무관)
│   │   ├── Domain/
│   │   │   ├── Entities/             # 엔티티 (배수지, 벽체, 슬래브 등)
│   │   │   ├── ValueObjects/         # 값 객체 (길이, 두께, 표고 등)
│   │   │   └── Enums/                # 열거형
│   │   └── Interfaces/
│   │       └── Services/             # 서비스 인터페이스
│   │
│   ├── DHBIMWATER.Application/       # 유스케이스 레이어
│   ├── DHBIMWATER.Infrastructure/    # 공통 인프라
│   ├── DHBIMWATER.Shared/            # 공통 유틸리티
│   ├── DHBIMWATER.Revit/             # Revit 구현 레이어
│   └── DHBIMWATER.UI/                # WPF UI (MVVM)
│
├── sandbox/
│   └── DHBIMWATER.UI.Sandbox/        # UI 테스트용 (Mock 서비스)
│
└── tests/                            # 단위 테스트
```

## 아키텍처

### Clean Architecture 계층

```
┌─────────────────────────────────────┐
│         UI Layer (WPF)              │
├─────────────────────────────────────┤
│     Application Layer (UseCase)     │
├─────────────────────────────────────┤
│      Domain Layer (Entities)        │
└─────────────────────────────────────┘

외부 어댑터:
- Revit API 어댑터
- Mock 서비스 (테스트용)
```

### 프로젝트 의존성

```
                    DHBIMWATER.Shared
                           ↑
                    DHBIMWATER.Core
                           ↑
                  DHBIMWATER.Application
                     ↑            ↑
    DHBIMWATER.Revit    DHBIMWATER.UI
              ↑                    ↑
              └────────────────────┘
                         ↑
              DHBIMWATER.Infrastructure
                         ↑
              DHBIMWATER.UI.Sandbox
```

## 시작하기

### 필수 요구사항

- .NET 8.0 SDK
- Visual Studio 2022 (또는 JetBrains Rider)
- Revit 2024 (프로덕션 환경)

### 빌드

```bash
# 전체 솔루션 빌드
dotnet build

# 특정 프로젝트 빌드
dotnet build src/DHBIMWATER.Core
```

### Sandbox에서 UI 테스트 실행

Sandbox 프로젝트는 Revit 없이 UI를 테스트할 수 있도록 Mock 서비스를 제공합니다.

```bash
# Sandbox 프로젝트 실행
dotnet run --project sandbox/DHBIMWATER.UI.Sandbox
```

또는 Visual Studio에서:
1. `DHBIMWATER.UI.Sandbox`를 시작 프로젝트로 설정
2. F5 키를 눌러 실행

## 주요 개념

### 도메인 모델

#### DistributionReservoir (배수지)
```csharp
var reservoir = DistributionReservoir.Create(
    name: "배수지-001",
    type: ReservoirType.RC,
    internalLength: Length.FromMillimeters(10000),
    internalWidth: Length.FromMillimeters(8000),
    internalHeight: Length.FromMillimeters(4000)
);

// 용량 계산
double capacity = reservoir.CalculateCapacity(); // m³
```

#### ValueObjects
```csharp
// 길이 (mm 기준)
var length = Length.FromMillimeters(5000);
var meters = length.Meters;  // 5.0
var feet = length.Feet;      // 16.4

// 두께 (유효성 검사 포함)
var thickness = Thickness.FromMillimeters(300); // 100~2000mm

// 표고
var elevation = Elevation.FromMeters(123.45);
var display = elevation.ToString(); // "EL+123.45"
```

### 서비스 인터페이스

```csharp
public interface IReservoirModelingService
{
    Task<bool> CreateReservoirAsync(DistributionReservoir reservoir);
    Task<bool> UpdateReservoirAsync(DistributionReservoir reservoir);
    Task<bool> DeleteReservoirAsync(Guid reservoirId);
}
```

## 개발 가이드

### 새로운 기능 추가

1. **Core**: 도메인 엔티티/값 객체 추가
2. **Core**: 서비스 인터페이스 정의
3. **Sandbox**: Mock 서비스 구현 (UI 테스트용)
4. **UI**: ViewModel 및 View 추가
5. **Revit**: 실제 Revit API 서비스 구현

### DI (Dependency Injection)

#### Sandbox에서 Mock 서비스 등록
```csharp
// DHBIMWATER.UI.Sandbox/DependencyInjection/SandboxServiceRegistration.cs
services.AddSingleton<IReservoirModelingService, MockReservoirModelingService>();
```

#### Revit에서 실제 서비스 등록
```csharp
// DHBIMWATER.Revit/DependencyInjection/RevitServiceRegistration.cs
services.AddSingleton<IReservoirModelingService, RevitReservoirModelingService>();
```

## 기술 스택

- **.NET 8.0**: 최신 C# 기능 활용
- **WPF**: Windows Presentation Foundation
- **Fluent.Ribbon**: 리본 메뉴 UI
- **Microsoft.Extensions.DependencyInjection**: DI 컨테이너
- **Revit API 2024**: BIM 모델링

## 라이선스

(라이선스 정보 추가)

## 기여

(기여 가이드라인 추가)

## 연락처

DH BIM Team
