# DHBIMWATER 프로젝트 구조

## 솔루션 개요

DHBIMWATER는 Clean Architecture와 DDD 원칙을 따르는 배수지 설계 자동화 시스템입니다.

## 프로젝트 목록

### 1. DHBIMWATER.Core (핵심 도메인)

**목적**: Revit과 무관한 순수 도메인 로직

**주요 구성요소**:
- `Domain/Entities/`: 배수지, 벽체, 슬래브, 기둥, 보, 배관 엔티티
- `Domain/ValueObjects/`: 길이, 두께, 표고, 3D 좌표 등
- `Domain/Enums/`: 배수지 형식, 벽체 종류, 슬래브 종류 등
- `Interfaces/Services/`: 서비스 계약 정의

**의존성**: DHBIMWATER.Shared만 참조

### 2. DHBIMWATER.Application (유스케이스)

**목적**: 비즈니스 로직 조율

**주요 구성요소**:
- `UseCases/`: 배수지 생성, 수량 산출, 도면 생성 등
- `DTOs/`: Request/Response 객체
- `Mappers/`: Entity ↔ DTO 변환

**의존성**: DHBIMWATER.Core

### 3. DHBIMWATER.Infrastructure (공통 인프라)

**목적**: 횡단 관심사

**주요 구성요소**:
- `DependencyInjection/`: DI 컨테이너 설정
- `Logging/`: 로깅
- `Configuration/`: 설정 관리

**의존성**: DHBIMWATER.Core, DHBIMWATER.Application

### 4. DHBIMWATER.Shared (공통 유틸리티)

**목적**: 모든 레이어에서 사용 가능한 공통 기능

**주요 구성요소**:
- `Results/`: Result<T> 패턴
- `Helpers/`: UnitConverter 등
- `Constants/`: 애플리케이션 상수

**의존성**: 없음 (최하위 레이어)

### 5. DHBIMWATER.Revit (Revit 구현)

**목적**: Revit API를 사용한 실제 구현

**주요 구성요소**:
- `Services/Modeling/`: Wall, Slab, Column 등 Revit 요소 생성
- `Services/Quantity/`: Revit 요소에서 물량 추출
- `Services/Drawing/`: Revit 뷰/시트 생성
- `Adapters/`: Revit Document, Transaction 래퍼
- `ExternalCommands/`: Revit Add-in 진입점

**의존성**: DHBIMWATER.Application, DHBIMWATER.Infrastructure

### 6. DHBIMWATER.UI (WPF 사용자 인터페이스)

**목적**: MVVM 패턴 기반 사용자 인터페이스

**주요 구성요소**:
- `Views/`: XAML 뷰
  - `Modeling/`: 모델링 탭
  - `Quantity/`: 수량산출 탭
  - `Drawing/`: 도면화 탭
- `ViewModels/`: 뷰모델
- `Commands/`: RelayCommand (단일 ICommand 구현)
- `Base/`: ViewModelBase (INotifyPropertyChanged)

**의존성**: DHBIMWATER.Application, DHBIMWATER.Infrastructure

**UI 기술**:
- Fluent.Ribbon: 리본 메뉴
- MVVM: View-ViewModel 바인딩
- DI: Dependency Injection

### 7. DHBIMWATER.UI.Sandbox (UI 테스트)

**목적**: Revit 없이 UI를 테스트할 수 있는 독립 실행형 애플리케이션

**주요 구성요소**:
- `Services/`: Mock 서비스 구현
  - `MockReservoirModelingService`: 콘솔에 로그 출력
- `DependencyInjection/`: Mock 서비스 DI 등록

**의존성**: DHBIMWATER.UI, DHBIMWATER.Infrastructure

**실행 방법**:
```bash
dotnet run --project sandbox/DHBIMWATER.UI.Sandbox
```

## 의존성 그래프

```
DHBIMWATER.Shared (최하위)
    ↑
DHBIMWATER.Core
    ↑
DHBIMWATER.Application
    ↑                ↑
DHBIMWATER.Revit   DHBIMWATER.UI
    ↑                ↑
    └────────────────┘
            ↑
DHBIMWATER.Infrastructure
            ↑
DHBIMWATER.UI.Sandbox (최상위)
```

## 핵심 패턴

### 1. Clean Architecture
- 도메인이 인프라에 의존하지 않음
- 인터페이스를 통한 의존성 역전

### 2. MVVM (Model-View-ViewModel)
- View: XAML
- ViewModel: 프레젠테이션 로직
- Model: Domain Entities

### 3. Dependency Injection
- 생성자 주입
- 인터페이스 기반 설계

### 4. Repository Pattern
- `IReservoirRepository` (Core에 정의)
- 구현체는 Revit 레이어

## 개발 워크플로우

### 새로운 기능 추가 시

1. **Core**: 도메인 모델 정의
   ```csharp
   // DHBIMWATER.Core/Domain/Entities/NewEntity.cs
   public class NewEntity { }

   // DHBIMWATER.Core/Interfaces/Services/INewService.cs
   public interface INewService { }
   ```

2. **Sandbox**: Mock 구현 (UI 개발용)
   ```csharp
   // DHBIMWATER.UI.Sandbox/Services/MockNewService.cs
   public class MockNewService : INewService { }
   ```

3. **UI**: ViewModel과 View 추가
   ```csharp
   // DHBIMWATER.UI/ViewModels/NewViewModel.cs
   // DHBIMWATER.UI/Views/NewView.xaml
   ```

4. **Revit**: 실제 구현
   ```csharp
   // DHBIMWATER.Revit/Services/RevitNewService.cs
   public class RevitNewService : INewService { }
   ```

## 빌드 설정

### Directory.Build.props
- C# 최신 버전 사용
- Nullable 참조 타입 활성화
- 공통 메타데이터 (버전, 저작권 등)

### .gitignore
- bin/, obj/ 제외
- Visual Studio 캐시 제외
- Revit 백업 파일 제외

## 다음 단계

1. **Application 레이어 구현**: UseCases 추가
2. **Revit 서비스 구현**: Revit API 연동
3. **수량산출/도면화 기능**: ViewModel 및 View 추가
4. **단위 테스트**: xUnit 프로젝트 추가
