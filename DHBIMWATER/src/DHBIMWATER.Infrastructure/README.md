# DHBIMWATER.Infrastructure (인프라스트럭처 계층)

## 🎯 한 줄 요약
**Revit API를 실제로 사용하는 곳 (Application 인터페이스 구현)**

---

## ✅ 여기서 만드는 것

### 1. **서비스 구현** - Application 인터페이스를 실제로 구현
```csharp
// 예시: RevitModelService.cs
public class RevitModelService : IRevitModelService
{
    private readonly Document _document;

    public async Task<Result<List<GenericModelFamilyDto>>> GetAllGenericModelsAsync()
    {
        // 1. Revit API로 데이터 가져오기
        var collector = new FilteredElementCollector(_document)
            .OfClass(typeof(FamilyInstance))
            .OfCategory(BuiltInCategory.OST_GenericModel);

        // 2. Revit Element -> DTO 변환
        var dtos = collector.Cast<FamilyInstance>()
            .Select(instance => MapToDto(instance))
            .ToList();

        return Result<List<GenericModelFamilyDto>>.Success(dtos);
    }

    // Revit Element -> DTO 변환
    private GenericModelFamilyDto MapToDto(FamilyInstance instance)
    {
        var location = (LocationPoint)instance.Location;
        return new GenericModelFamilyDto
        {
            FamilyName = instance.Symbol.FamilyName,
            Location = new Point3DDto
            {
                X = location.Point.X,
                Y = location.Point.Y,
                Z = location.Point.Z
            }
        };
    }
}
```

### 2. **Mock 구현** - 테스트/개발용
```csharp
// 예시: MockRevitModelService.cs
public class MockRevitModelService : IRevitModelService
{
    private List<GenericModelFamilyDto> _mockData = new();

    public async Task<Result<List<GenericModelFamilyDto>>> GetAllGenericModelsAsync()
    {
        // 가짜 데이터 반환
        return Result<List<GenericModelFamilyDto>>.Success(_mockData);
    }
}
```

---

## ❌ 여기서 하면 안 되는 것

- ❌ DTO 정의 (Application에서!)
- ❌ 비즈니스 로직 (Core에서!)
- ❌ UI 참조 (WPF, ViewModel 등)

---

## 📁 폴더 구조

```
DHBIMWATER.Infrastructure/
├── Repositories/
│   ├── Revit/                 # Revit API 실제 구현
│   │   └── RevitModelService.cs
│   │
│   └── Mock/                  # 테스트용 Mock
│       └── MockRevitModelService.cs
│
└── DependencyInjection/       # DI 설정
    └── ServiceCollectionExtensions.cs
```

---

## 🔥 핵심 규칙 3가지

1. **Revit API 사용**: Document, FamilyInstance 등 직접 다룸
2. **Element ↔ DTO 변환**: Revit 객체를 DTO로 변환
3. **Transaction 관리**: Revit 데이터 변경 시 Transaction 필수

---

## 예시: Revit API 사용

```csharp
public class RevitModelService : IRevitModelService
{
    private readonly Document _document;

    public RevitModelService(Document document)
    {
        _document = document;
    }

    public async Task<Result<Guid>> PlaceGenericModelAsync(GenericModelFamilyDto model)
    {
        // Transaction 시작
        using (Transaction trans = new Transaction(_document, "일반모델 배치"))
        {
            trans.Start();

            try
            {
                // 1. FamilySymbol 찾기
                var symbol = FindFamilySymbol(model.FamilyName, model.FamilyTypeName);

                // 2. 위치 생성
                var location = new XYZ(model.Location.X, model.Location.Y, model.Location.Z);

                // 3. 인스턴스 생성
                var instance = _document.Create.NewFamilyInstance(
                    location,
                    symbol,
                    StructuralType.NonStructural
                );

                trans.Commit();

                return Result<Guid>.Success(Guid.Parse(instance.UniqueId));
            }
            catch
            {
                trans.RollBack();
                throw;
            }
        }
    }
}
```

---

## 🔧 DI 설정

```csharp
// ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        bool useMock = false)
    {
        if (useMock)
        {
            // 테스트용 Mock
            services.AddScoped<IRevitModelService, MockRevitModelService>();
        }
        else
        {
            // 실제 Revit API
            services.AddScoped<IRevitModelService, RevitModelService>();
        }

        return services;
    }
}
```

---

## 📊 작업 흐름

```
1. Application에서 인터페이스 정의
   ↓
2. Infrastructure에서 구현
   ↓
3. Revit API 호출
   ↓
4. Element -> DTO 변환
   ↓
5. Result<T> 반환
```

---

## ✅ 체크리스트

- [ ] Application 인터페이스를 구현했나?
- [ ] Revit Transaction 관리했나?
- [ ] Element -> DTO 변환 정확한가?
- [ ] 예외 처리 했나?
- [ ] Mock 구현도 제공했나?

---

## 💡 요약

| 항목 | 설명 |
|------|------|
| **Revit API** | Document, FamilyInstance 등 직접 사용 |
| **변환** | Revit Element ↔ DTO |
| **Transaction** | 데이터 변경 시 필수 |
| **Mock** | 테스트용 가짜 구현 제공 |
