# DHBIMWATER 코딩 프로세스 가이드

> 이 문서는 DHBIMWATER 프로젝트에서 새로운 기능을 추가하는 전체 프로세스를 단계별로 안내합니다.

## 목차
1. [프로젝트 구조 이해](#1-프로젝트-구조-이해)
2. [새로운 기능 추가 프로세스](#2-새로운-기능-추가-프로세스)
3. [Clean Architecture 원칙](#3-clean-architecture-원칙)
4. [예제: Generic Model 개수 세기 기능](#4-예제-generic-model-개수-세기-기능)

---

## 1. 프로젝트 구조 이해

### 레이어 구조 (의존성 방향: 하위 → 상위)

```
┌─────────────────────────────────────────────────────────┐
│ DHBIMWATER.Revit (진입점)                                │
│ - App.cs: Revit 애플리케이션 시작점                       │
│ - Commands/: 리본 버튼 클릭 시 실행되는 커맨드            │
│ - UI/: 리본 UI 구성                                      │
│ - DependencyInjection/: ServiceContainer                │
└─────────────────────────────────────────────────────────┘
                          ↓ 의존
┌─────────────────────────────────────────────────────────┐
│ DHBIMWATER.UI (View/ViewModel)                          │
│ - Views/: WPF View (XAML)                               │
│ - ViewModels/: ViewModel (UI 로직)                      │
│ - Commands/: RelayCommand 등                            │
│ - Base/: ViewModelBase                                  │
└─────────────────────────────────────────────────────────┘
                          ↓ 의존
┌─────────────────────────────────────────────────────────┐
│ DHBIMWATER.Infrastructure (구현)                         │
│ - Repositories/Revit/: Revit API 구현체                 │
│ - Repositories/Mock/: 테스트용 Mock 구현체               │
│ - Services/: 기타 Infrastructure 서비스                  │
└─────────────────────────────────────────────────────────┘
                          ↓ 의존
┌─────────────────────────────────────────────────────────┐
│ DHBIMWATER.Application (비즈니스 로직)                   │
│ - UseCases/: 비즈니스 로직 (UseCase 패턴)                │
│ - Interface/: 인터페이스 정의 (Repository, Service)      │
└─────────────────────────────────────────────────────────┘
                          ↓ 의존
┌─────────────────────────────────────────────────────────┐
│ DHBIMWATER.Core (도메인 모델)                            │
│ - Models/: 엔티티, Value Object                         │
│ - Enums/: 열거형                                        │
└─────────────────────────────────────────────────────────┘
```

### 중요 원칙
- **상위 레이어는 하위 레이어를 참조할 수 없습니다**
- **인터페이스는 Application에 정의하고, 구현체는 Infrastructure에 작성합니다**
- **ViewModel은 Infrastructure를 직접 참조하지 않습니다 (UseCase를 통해서만 접근)**

---

## 2. 새로운 기능 추가 프로세스

### 전체 프로세스 요약

```
1. 리소스 준비 (아이콘 이미지)
   ↓
2. Core 레이어: 도메인 모델 정의 (필요시)
   ↓
3. Application 레이어: 인터페이스 + UseCase 작성
   ↓
4. Infrastructure 레이어: Repository 구현 (Revit + Mock)
   ↓
5. UI 레이어: View + ViewModel 작성
   ↓
6. Revit 레이어: Command 작성
   ↓
7. DI 등록 (각 레이어별 ServiceCollectionExtensions)
   ↓
8. Ribbon UI 등록 (RibbonBuilder)
   ↓
9. 테스트 (Sandbox 또는 Revit)
   ↓
10. 배포 (빌드 및 설치 파일 생성)
```

---

## 3. Clean Architecture 원칙

### 의존성 규칙

#### ✅ 올바른 참조 방향
```csharp
// ViewModel → UseCase (O)
public class MyViewModel : ViewModelBase
{
    private readonly MyUseCase _useCase;  // Application 레이어

    public MyViewModel(MyUseCase useCase)
    {
        _useCase = useCase;
    }
}

// UseCase → Interface (O)
public class MyUseCase
{
    private readonly IMyRepository _repository;  // Application 레이어의 인터페이스

    public MyUseCase(IMyRepository repository)
    {
        _repository = repository;
    }
}

// Repository → Revit API (O)
internal class RevitMyRepository : IMyRepository
{
    public void DoSomething(Document doc)
    {
        // Revit API 사용
    }
}
```

#### ❌ 잘못된 참조 방향
```csharp
// ViewModel → Repository 직접 참조 (X)
using DHBIMWATER.Infrastructure.Repositories.Revit;  // ❌ 금지!

public class MyViewModel : ViewModelBase
{
    private readonly RevitMyRepository _repository;  // ❌ 금지!
}

// Application → Infrastructure 참조 (X)
namespace DHBIMWATER.Application.UseCases
{
    using DHBIMWATER.Infrastructure;  // ❌ 금지!
}
```

### DI (Dependency Injection) 규칙

모든 서비스는 각 레이어의 `DependencyInjection/ServiceCollectionExtensions.cs`에 등록해야 합니다.

---

## 4. 예제: Generic Model 개수 세기 기능

실제 구현된 기능을 단계별로 설명합니다.

### 4-1. 리소스 준비

#### 버튼 아이콘 이미지 준비
1. **파일 위치**: `src/DHBIMWATER.Revit/Resources/Icons/`
2. **파일명 규칙**: `[기능명]_32x32.png`
3. **이미지 크기**: 32x32 픽셀 (권장)
4. **형식**: PNG (투명 배경 권장)

#### 프로젝트에 리소스 추가
```xml
<!-- src/DHBIMWATER.Revit/DHBIMWATER.Revit.csproj -->
<ItemGroup>
  <EmbeddedResource Include="Resources\Icons\Modeling1_32x32.png" />
</ItemGroup>
```

---

### 4-2. Core 레이어: 도메인 모델 정의 (필요시)

이 예제에서는 단순 카운트이므로 별도 모델 불필요. 복잡한 엔티티가 필요한 경우:

```csharp
// src/DHBIMWATER.Core/Models/GenericModel.cs
namespace DHBIMWATER.Core.Models
{
    public class GenericModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FamilyName { get; set; }
        // 기타 속성...
    }
}
```

---

### 4-3. Application 레이어

#### Step 1: Repository 인터페이스 정의

**파일**: `src/DHBIMWATER.Application/Interface/IGenericModelRepository.cs`

```csharp
namespace DHBIMWATER.Application.Interface
{
    /// <summary>
    /// Generic Model 데이터 접근 인터페이스
    /// </summary>
    public interface IGenericModelRepository
    {
        /// <summary>
        /// 모든 Generic Model 요소를 가져옵니다
        /// </summary>
        /// <returns>Generic Model 요소 컬렉션</returns>
        IEnumerable<object> GetAll();
    }
}
```

**중요**:
- 인터페이스는 반드시 `Application/Interface/` 폴더에 작성
- 구현체는 `Infrastructure/Repositories/` 폴더에 작성
- Revit API 타입(Element 등)을 직접 사용하지 말고 object 또는 Core 모델 사용

#### Step 2: UseCase 작성

**파일**: `src/DHBIMWATER.Application/UseCases/CountGenericModelUseCase.cs`

```csharp
using DHBIMWATER.Application.Interface;

namespace DHBIMWATER.Application.UseCases
{
    /// <summary>
    /// Generic Model 개수를 세는 비즈니스 로직
    /// </summary>
    public class CountGenericModelUseCase
    {
        private readonly IGenericModelRepository _repository;

        public CountGenericModelUseCase(IGenericModelRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Generic Model 개수 반환
        /// </summary>
        public int Execute()
        {
            var models = _repository.GetAll();
            return models.Count();
        }
    }
}
```

**UseCase 작성 원칙**:
- 하나의 UseCase는 하나의 비즈니스 기능만 수행
- Repository 인터페이스를 통해서만 데이터 접근
- 비즈니스 로직만 포함 (UI 로직 금지)

#### Step 3: Application 레이어 DI 등록

**파일**: `src/DHBIMWATER.Application/DependencyInjection/ServiceCollectionExtensions.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using DHBIMWATER.Application.UseCases;

namespace DHBIMWATER.Application.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // UseCase 등록 (Transient: 호출마다 새 인스턴스)
            services.AddTransient<CountGenericModelUseCase>();

            return services;
        }
    }
}
```

**Lifetime 선택 가이드**:
- `AddTransient`: 매번 새 인스턴스 (UseCase, Command 등)
- `AddScoped`: 요청마다 하나의 인스턴스 (웹에서 주로 사용)
- `AddSingleton`: 애플리케이션 전체에서 하나의 인스턴스 (설정, 캐시 등)

---

### 4-4. Infrastructure 레이어

#### Step 1: Revit 구현체 작성

**파일**: `src/DHBIMWATER.Infrastructure/Repositories/Revit/RevitGenericModelRepository.cs`

```csharp
using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interface;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    /// <summary>
    /// Revit API를 사용한 Generic Model Repository 구현
    /// </summary>
    internal class RevitGenericModelRepository : IGenericModelRepository
    {
        private readonly Func<Document?> _getDocument;

        /// <summary>
        /// 생성자 - Document를 제공하는 함수 주입
        /// </summary>
        /// <param name="getDocument">현재 활성 Document를 반환하는 람다 함수</param>
        public RevitGenericModelRepository(Func<Document?> getDocument)
        {
            _getDocument = getDocument;
        }

        public IEnumerable<object> GetAll()
        {
            // 현재 Document 가져오기
            Document? doc = _getDocument();

            if (doc == null)
                return Enumerable.Empty<object>();

            // Revit API를 사용하여 Generic Model 수집
            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)   // Generic Model 카테고리 필터링
                .WhereElementIsNotElementType()                 // ElementType 제외
                .ToElements();                                  // 요소 컬렉션 반환
        }
    }
}
```

**중요 패턴**:
- `Func<Document?>` 주입: Document는 Command 실행 시점에만 존재하므로, 함수로 주입받아 필요할 때 호출
- `internal` 접근 제한자: Repository 구현체는 Infrastructure 외부에서 직접 접근 불가
- Null 체크: Document가 없을 수 있으므로 반드시 체크

#### Step 2: Mock 구현체 작성 (테스트용)

**파일**: `src/DHBIMWATER.Infrastructure/Repositories/Mock/MockGenericModelRepository.cs`

```csharp
using DHBIMWATER.Application.Interface;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    /// <summary>
    /// 테스트용 Mock Generic Model Repository
    /// Revit 없이 Sandbox에서 테스트 가능
    /// </summary>
    internal class MockGenericModelRepository : IGenericModelRepository
    {
        public IEnumerable<object> GetAll()
        {
            // 테스트 데이터 반환
            return new List<object>
            {
                "Mock Generic Model 1",
                "Mock Generic Model 2",
                "Mock Generic Model 3"
            };
        }
    }
}
```

**Mock 구현 목적**:
- Revit 없이 개발 및 테스트 가능
- Sandbox 프로젝트에서 UI 테스트 가능
- 빠른 프로토타이핑

#### Step 3: Infrastructure 레이어 DI 등록

**파일**: `src/DHBIMWATER.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using DHBIMWATER.Application.Interface;
using DHBIMWATER.Infrastructure.Repositories.Revit;
using DHBIMWATER.Infrastructure.Repositories.Mock;

namespace DHBIMWATER.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Infrastructure 서비스 등록 (환경별 분기)
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
#if DEBUG
            // Debug 모드: Mock 구현체 사용 (Sandbox 테스트용)
            // services.AddScoped<IGenericModelRepository, MockGenericModelRepository>();
            // services.AddScoped<IDialogService, MockDialogService>();
#endif
            // Release 모드 또는 기본: Revit 구현체 사용
            services.AddScoped<IGenericModelRepository, RevitGenericModelRepository>();
            services.AddScoped<IDialogService, RevitDialogService>();

            return services;
        }
    }
}
```

**환경별 서비스 분기**:
- Sandbox에서는 Mock 구현체 사용
- Revit에서는 실제 Revit API 구현체 사용
- 주석 처리로 환경 전환 가능

---

### 4-5. UI 레이어

#### Step 1: View (XAML) 작성

**파일**: `src/DHBIMWATER.UI/Views/Modeling/Modeling1View.xaml`

```xml
<Window x:Class="DHBIMWATER.UI.Views.Modeling.Modeling1View"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Generic Model Counter"
        Height="300"
        Width="400"
        WindowStartupLocation="CenterScreen">

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 제목 -->
        <TextBlock Grid.Row="0"
                   Text="Generic Model Counter"
                   FontSize="20"
                   FontWeight="Bold"
                   Margin="0,0,0,20"/>

        <!-- 카운트 표시 -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,20">
            <TextBlock Text="Total Count: " FontSize="16" VerticalAlignment="Center"/>
            <TextBlock Text="{Binding ModelCount}"
                       FontSize="16"
                       FontWeight="Bold"
                       Foreground="Blue"
                       VerticalAlignment="Center"/>
        </StackPanel>

        <!-- 버튼 -->
        <Button Grid.Row="3"
                Content="Count Models"
                Command="{Binding CountCommand}"
                Height="40"
                FontSize="14"/>
    </Grid>
</Window>
```

**XAML 작성 원칙**:
- `DataContext`는 Code-Behind에서 설정 (DI를 통해 주입)
- `Command` 바인딩 사용 (Click 이벤트 대신)
- `INotifyPropertyChanged` 구현된 속성에 바인딩

#### Step 2: View Code-Behind

**파일**: `src/DHBIMWATER.UI/Views/Modeling/Modeling1View.xaml.cs`

```csharp
using System.Windows;
using DHBIMWATER.UI.ViewModels.Modeling;

namespace DHBIMWATER.UI.Views.Modeling
{
    public partial class Modeling1View : Window
    {
        public Modeling1View(Modeling1ViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;  // DI를 통해 주입받은 ViewModel 설정
        }
    }
}
```

**Code-Behind 원칙**:
- 비즈니스 로직 금지 (ViewModel에 작성)
- DI를 통해 ViewModel 주입
- `DataContext` 설정만 수행

#### Step 3: ViewModel 작성

**파일**: `src/DHBIMWATER.UI/ViewModels/Modeling/Modeling1ViewModel.cs`

```csharp
using DHBIMWATER.Application.Interface;
using DHBIMWATER.Application.UseCases;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Modeling
{
    public class Modeling1ViewModel : ViewModelBase
    {
        private readonly CountGenericModelUseCase _useCase;
        private readonly IDialogService _dialogService;

        // 바인딩할 속성
        private int _modelCount;
        public int ModelCount
        {
            get => _modelCount;
            set
            {
                _modelCount = value;
                OnPropertyChanged();  // UI 업데이트 알림
            }
        }

        // 커맨드
        public ICommand CountCommand { get; }

        /// <summary>
        /// 생성자 - DI를 통해 UseCase와 DialogService 주입
        /// </summary>
        public Modeling1ViewModel(CountGenericModelUseCase useCase, IDialogService dialogService)
        {
            _useCase = useCase;
            _dialogService = dialogService;

            // 커맨드 초기화
            CountCommand = new RelayCommand(CountModels);
        }

        /// <summary>
        /// 모델 개수 세기 실행
        /// </summary>
        private void CountModels(object? parameter)
        {
            // UseCase 실행
            ModelCount = _useCase.Execute();

            // 다이얼로그 표시
            _dialogService.Info("Model Count", $"Total Generic Models: {ModelCount}");
        }
    }
}
```

**ViewModel 작성 원칙**:
- `ViewModelBase` 상속 (INotifyPropertyChanged 구현)
- UseCase와 Service만 주입 (Repository 직접 주입 금지)
- UI 로직만 포함 (비즈니스 로직은 UseCase에)
- Infrastructure 레이어 직접 참조 금지

#### Step 4: UI 레이어 DI 등록

**파일**: `src/DHBIMWATER.UI/DependencyInjection/ServiceCollectionExtensions.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using DHBIMWATER.UI.Views.Modeling;
using DHBIMWATER.UI.ViewModels.Modeling;

namespace DHBIMWATER.UI.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUIServices(this IServiceCollection services)
        {
            // View 등록 (Transient: 매번 새 창)
            services.AddTransient<Modeling1View>();

            // ViewModel 등록 (Transient)
            services.AddTransient<Modeling1ViewModel>();

            return services;
        }
    }
}
```

---

### 4-6. Revit 레이어

#### Step 1: CommandBase 작성 (공통 기반 클래스)

**파일**: `src/DHBIMWATER.Revit/Commands/CommandBase.cs`

```csharp
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Revit.DependencyInjection;

namespace DHBIMWATER.Revit.Commands
{
    /// <summary>
    /// 모든 Command의 기반 클래스
    /// ServiceContainer 초기화/정리를 자동 처리
    /// </summary>
    public abstract class CommandBase : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // ServiceContainer 초기화 (UIApplication 전달)
                ServiceContainer.Build(commandData.Application);

                // 실제 Command 로직 실행 (파생 클래스에서 구현)
                return ExecuteInternal(commandData, ref message, elements);
            }
            finally
            {
                // ServiceContainer 정리 (메모리 누수 방지)
                ServiceContainer.Dispose();
            }
        }

        /// <summary>
        /// 실제 Command 로직을 구현하는 메서드
        /// </summary>
        protected abstract Result ExecuteInternal(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements);
    }
}
```

**CommandBase 역할**:
- ServiceContainer 자동 초기화/정리
- 중복 코드 제거
- 일관된 에러 핸들링

#### Step 2: Command 작성

**파일**: `src/DHBIMWATER.Revit/Commands/ModelingCommand1.cs`

```csharp
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.UI.Views.Modeling;

namespace DHBIMWATER.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ModelingCommand1 : CommandBase
    {
        protected override Result ExecuteInternal(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            // DI 컨테이너에서 View 가져오기
            var view = ServiceContainer.GetService<Modeling1View>();

            // 모달 창으로 표시
            view.ShowDialog();

            return Result.Succeeded;
        }
    }
}
```

**Command 작성 원칙**:
- `CommandBase` 상속
- `[Transaction]` 특성 지정 (Manual, ReadOnly, Automatic 중 선택)
- View만 생성하고 표시 (로직은 ViewModel에)

#### Step 3: ServiceContainer에서 Func<Document?> 등록

**파일**: `src/DHBIMWATER.Revit/DependencyInjection/ServiceContainer.cs`

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.DependencyInjection;
using DHBIMWATER.Infrastructure.DependencyInjection;
using DHBIMWATER.UI.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DHBIMWATER.Revit.DependencyInjection
{
    public class ServiceContainer
    {
        private static IServiceProvider? _serviceProvider;

        public static IServiceProvider ServiceProvider =>
            _serviceProvider ?? throw new InvalidOperationException("ServiceContainer is not built.");

        /// <summary>
        /// DI 컨테이너 빌드
        /// </summary>
        /// <param name="uiApp">Revit UIApplication</param>
        internal static void Build(UIApplication uiApp)
        {
            Dispose();  // 기존 인스턴스 정리

            ServiceCollection services = new ServiceCollection();

            // 현재 활성 Document를 반환하는 람다 함수 등록
            // Repository에서 Func<Document?>로 주입받아 호출 시점에 Document 획득
            services.AddSingleton<Func<Document?>>(() => uiApp.ActiveUIDocument?.Document);

            // 각 레이어의 서비스 등록
            services.AddUIServices();              // UI View/ViewModel
            services.AddRevitServices();           // Revit 특화 서비스 (현재 비어있음)
            services.AddApplicationServices();     // Application UseCases
            services.AddInfrastructureServices();  // Infrastructure Repositories

            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// DI 컨테이너 정리
        /// </summary>
        public static void Dispose()
        {
            if (_serviceProvider is IDisposable disposable)
                disposable.Dispose();

            _serviceProvider = null;
        }

        /// <summary>
        /// 서비스 가져오기
        /// </summary>
        public static T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }
    }
}
```

**Func<Document?> 패턴 설명**:
```csharp
// 1. DI 등록 시: 람다 함수를 등록
services.AddSingleton<Func<Document?>>(() => uiApp.ActiveUIDocument?.Document);

// 2. Repository 생성자: 람다 함수를 주입받음
public RevitGenericModelRepository(Func<Document?> getDocument)
{
    _getDocument = getDocument;  // 함수 자체를 저장
}

// 3. Repository 메서드: 필요할 때 함수 호출
public IEnumerable<object> GetAll()
{
    Document? doc = _getDocument();  // 현재 시점의 Document 획득
    // ...
}
```

**왜 이 패턴을 사용하는가?**:
- Document는 Command 실행 시점에만 존재
- DI 컨테이너 빌드 시점에는 Document가 null일 수 있음
- 함수로 주입하면 필요할 때 현재 Document를 동적으로 가져올 수 있음

---

### 4-7. Ribbon UI 등록

#### Step 1: RibbonBuilder에 버튼 추가

**파일**: `src/DHBIMWATER.Revit/UI/RibbonBuilder.cs`

```csharp
using Autodesk.Revit.UI;
using System.Reflection;

namespace DHBIMWATER.Revit.UI
{
    public class RibbonBuilder
    {
        public static void CreateRibbonPanel(UIControlledApplication app)
        {
            // 리본 탭 생성
            string tabName = "DHBIMWATER";
            app.CreateRibbonTab(tabName);

            // 리본 패널 생성
            RibbonPanel panel = app.CreateRibbonPanel(tabName, "Modeling Tools");

            // 어셈블리 경로
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            // 버튼 1: Modeling Command 1
            PushButtonData buttonData1 = new PushButtonData(
                "Modeling1Button",                              // 내부 이름
                "Model\nCounter",                               // 표시 이름 (\n으로 줄바꿈)
                assemblyPath,                                   // DLL 경로
                "DHBIMWATER.Revit.Commands.ModelingCommand1"    // 전체 클래스명
            );

            // 아이콘 설정 (32x32)
            buttonData1.LargeImage = GetEmbeddedImage("DHBIMWATER.Revit.Resources.Icons.Modeling1_32x32.png");

            // 툴팁 설정
            buttonData1.ToolTip = "Count Generic Models in the current document";

            // 패널에 버튼 추가
            PushButton button1 = panel.AddItem(buttonData1) as PushButton;
        }

        /// <summary>
        /// Embedded Resource에서 이미지 가져오기
        /// </summary>
        private static System.Windows.Media.ImageSource GetEmbeddedImage(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
                return null;

            var decoder = new System.Windows.Media.Imaging.PngBitmapDecoder(
                stream,
                System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
                System.Windows.Media.Imaging.BitmapCacheOption.Default);

            return decoder.Frames[0];
        }
    }
}
```

**리본 버튼 추가 체크리스트**:
1. ✅ PushButtonData 생성 (이름, 표시명, DLL 경로, 클래스명)
2. ✅ LargeImage 설정 (32x32 아이콘)
3. ✅ ToolTip 설정 (마우스 오버 시 설명)
4. ✅ Panel에 버튼 추가

---

### 4-8. 테스트

#### Sandbox 프로젝트에서 테스트 (Mock 사용)

**파일**: `sandbox/DHBIMWATER.UI.Sandbox/DependencyInjection/SandboxServiceRegistration.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using DHBIMWATER.Application.DependencyInjection;
using DHBIMWATER.Infrastructure.DependencyInjection;
using DHBIMWATER.UI.DependencyInjection;
using DHBIMWATER.Application.Interface;
using DHBIMWATER.Infrastructure.Repositories.Mock;

namespace DHBIMWATER.UI.Sandbox.DependencyInjection
{
    public static class SandboxServiceRegistration
    {
        public static ServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            // Application 서비스 등록
            services.AddApplicationServices();

            // UI 서비스 등록
            services.AddUIServices();

            // Mock 구현체 등록 (Revit 없이 테스트)
            services.AddScoped<IGenericModelRepository, MockGenericModelRepository>();
            services.AddScoped<IDialogService, MockDialogService>();

            return services.BuildServiceProvider();
        }
    }
}
```

**Sandbox 테스트 순서**:
1. Sandbox 프로젝트 실행
2. Mock 데이터로 UI 동작 확인
3. ViewModel 로직 검증
4. 문제 없으면 Revit에서 실제 테스트

#### Revit에서 테스트

1. **빌드**: `dotnet build -c Release`
2. **PostBuild 이벤트로 자동 복사**: `C:\BuildOutput\DHBIMWATER\`
3. **Revit 실행**: 애드인 자동 로드
4. **리본 버튼 클릭**: 기능 테스트

---

### 4-9. 배포

#### Step 1: Release 빌드

```bash
cd c:\Users\user\Projects\DHBIMWATER
dotnet build -c Release
```

#### Step 2: 빌드 출력 확인

```
C:\BuildOutput\DHBIMWATER\
├── DHBIMWATER.Revit.dll  (약 23MB, 모든 의존성 포함)
└── DHBIMWATER.addin
```

#### Step 3: NSIS 설치 파일 생성

```bash
"C:\Program Files (x86)\NSIS\makensis.exe" "c:\Users\user\Projects\DHBIMWATER\installer\DHBIMWATER_2025.nsi"
```

**출력**: `C:\BuildOutput\DHBIMWATER\DHBIMWATER_2025.exe`

#### Step 4: 배포

1. `DHBIMWATER_2025.exe` 팀원들에게 전달
2. 실행하면 자동으로 설치:
   - DLL: `C:\ProgramData\Autodesk\Revit\Addins\2025\DHBIMWATER\`
   - .addin: `C:\ProgramData\Autodesk\Revit\Addins\2025\`
3. Revit 재시작 후 사용 가능

---

## 5. 체크리스트

### 새 기능 추가 시 필수 확인 사항

#### Core 레이어
- [ ] 도메인 모델 필요 시 `Core/Models/`에 작성
- [ ] 열거형 필요 시 `Core/Enums/`에 작성

#### Application 레이어
- [ ] `Application/Interface/`에 Repository 인터페이스 작성
- [ ] `Application/UseCases/`에 UseCase 작성
- [ ] `Application/DependencyInjection/ServiceCollectionExtensions.cs`에 UseCase 등록

#### Infrastructure 레이어
- [ ] `Infrastructure/Repositories/Revit/`에 Revit 구현체 작성
- [ ] `Infrastructure/Repositories/Mock/`에 Mock 구현체 작성
- [ ] Revit 구현체에서 `Func<Document?>` 주입받아 사용
- [ ] `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`에 Repository 등록

#### UI 레이어
- [ ] `UI/Views/`에 XAML View 작성
- [ ] `UI/Views/`에 Code-Behind 작성 (ViewModel 주입)
- [ ] `UI/ViewModels/`에 ViewModel 작성 (ViewModelBase 상속)
- [ ] ViewModel에서 Infrastructure 직접 참조 금지 확인
- [ ] `UI/DependencyInjection/ServiceCollectionExtensions.cs`에 View/ViewModel 등록

#### Revit 레이어
- [ ] `Revit/Resources/Icons/`에 32x32 아이콘 추가
- [ ] `.csproj`에 EmbeddedResource로 아이콘 등록
- [ ] `Revit/Commands/`에 Command 작성 (CommandBase 상속)
- [ ] `[Transaction]` 특성 지정
- [ ] `Revit/UI/RibbonBuilder.cs`에 버튼 추가

#### 테스트
- [ ] Sandbox에서 Mock으로 UI 테스트
- [ ] Revit에서 실제 기능 테스트
- [ ] 에러 핸들링 확인

#### 빌드 및 배포
- [ ] Release 빌드 성공 확인
- [ ] `C:\BuildOutput\DHBIMWATER\` 출력 확인
- [ ] NSIS 설치 파일 생성 확인
- [ ] 설치 파일 테스트 (설치/제거)

---

## 6. 트러블슈팅

### 문제: DI 등록했는데 서비스를 찾을 수 없다

**증상**:
```
InvalidOperationException: Unable to resolve service for type 'XXX'
```

**해결**:
1. `ServiceCollectionExtensions.cs`에 등록했는지 확인
2. `ServiceContainer.Build()`에서 해당 레이어의 `AddXXXServices()` 호출하는지 확인
3. 인터페이스와 구현체가 일치하는지 확인

### 문제: Document가 null이다

**증상**:
```
NullReferenceException in Repository
```

**해결**:
1. `Func<Document?>` 패턴 사용 확인
2. Repository에서 `_getDocument()` 호출 후 null 체크
3. Command 실행 전에 Revit에서 Document를 열었는지 확인

### 문제: ViewModel에서 Repository가 주입되지 않는다

**증상**:
```
InvalidOperationException: Unable to resolve service for type 'IMyRepository'
```

**해결**:
1. Infrastructure의 `ServiceCollectionExtensions`에서 Repository 등록했는지 확인
2. ViewModel 생성자에서 Repository 대신 UseCase를 주입받도록 수정
3. Clean Architecture 원칙: ViewModel → UseCase → Repository

### 문제: 리본 버튼이 표시되지 않는다

**해결**:
1. `.addin` 파일이 올바른 위치에 있는지 확인
2. `RibbonBuilder.CreateRibbonPanel()` 호출되는지 확인
3. Command 클래스의 전체 이름(네임스페이스 포함) 확인
4. Revit 재시작

---

## 7. 추가 리소스

### 프로젝트 파일 구조
```
DHBIMWATER/
├── src/
│   ├── DHBIMWATER.Core/              (도메인 모델)
│   ├── DHBIMWATER.Shared/            (공유 유틸리티)
│   ├── DHBIMWATER.Application/       (비즈니스 로직)
│   │   ├── Interface/                (인터페이스 정의)
│   │   ├── UseCases/                 (UseCase)
│   │   └── DependencyInjection/
│   ├── DHBIMWATER.Infrastructure/    (구현)
│   │   ├── Repositories/Revit/       (Revit 구현)
│   │   ├── Repositories/Mock/        (Mock 구현)
│   │   └── DependencyInjection/
│   ├── DHBIMWATER.UI/                (View/ViewModel)
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   ├── Commands/                 (RelayCommand 등)
│   │   ├── Base/                     (ViewModelBase)
│   │   └── DependencyInjection/
│   └── DHBIMWATER.Revit/             (Revit 진입점)
│       ├── App.cs
│       ├── Commands/
│       ├── UI/                       (RibbonBuilder)
│       ├── Resources/Icons/
│       └── DependencyInjection/
├── sandbox/
│   └── DHBIMWATER.UI.Sandbox/        (테스트 프로젝트)
└── installer/
    └── DHBIMWATER_2025.nsi           (설치 스크립트)
```

### 명명 규칙

- **Interface**: `I` 접두사 (예: `IGenericModelRepository`)
- **UseCase**: `[동작][대상]UseCase` (예: `CountGenericModelUseCase`)
- **Repository**: `[환경][대상]Repository` (예: `RevitGenericModelRepository`, `MockGenericModelRepository`)
- **ViewModel**: `[기능명]ViewModel` (예: `Modeling1ViewModel`)
- **View**: `[기능명]View` (예: `Modeling1View`)
- **Command**: `[기능명]Command` (예: `ModelingCommand1`)

---

## 8. 마무리

이 가이드를 따라 새로운 기능을 추가하면:

1. ✅ Clean Architecture 원칙을 준수
2. ✅ DI를 통한 느슨한 결합
3. ✅ 테스트 가능한 구조 (Mock 구현)
4. ✅ 유지보수 용이
5. ✅ 팀 협업에 적합

**질문이나 문제가 있으면 이 문서를 참고하세요!**
