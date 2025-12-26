# DHBIMWATER.Application (애플리케이션 계층)

## 🎯 한 줄 요약
**UI와 Core 사이의 다리 역할 (DTO로 데이터 전달)**

---

## ✅ 여기서 만드는 것

### 1. **DTO** - 데이터 전송 객체 (단순 데이터)
```csharp
// 예시: GenericModelFamilyDto.cs
public class GenericModelFamilyDto
{
    public string FamilyName { get; set; } = string.Empty;
    public string FamilyTypeName { get; set; } = string.Empty;
    public Point3DDto Location { get; set; } = new();
    public double Rotation { get; set; }
}

public class Point3DDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}
```

### 2. **서비스 인터페이스** - 무엇을 할지만 정의
```csharp
// 예시: IRevitModelService.cs
public interface IRevitModelService
{
    // 모든 일반모델 가져오기
    Task<Result<List<GenericModelFamilyDto>>> GetAllGenericModelsAsync();

    // 일반모델 배치하기
    Task<Result<Guid>> PlaceGenericModelAsync(GenericModelFamilyDto model);
}
```

---

## ❌ 여기서 하면 안 되는 것

- ❌ Revit API 직접 사용 (`Document`, `FamilyInstance` 등)
- ❌ 서비스 구현 (구현은 Infrastructure에서!)
- ❌ WPF ViewModel (UI 프로젝트에서!)
- ❌ 데이터베이스 접근

---

## 📁 폴더 구조

```
DHBIMWATER.Application/
├── DTOs/                      # 데이터 전송 객체
│   ├── GenericModelFamilyDto.cs
│   └── Point3DDto.cs
│
└── Services/                  # 서비스 인터페이스만
    └── IRevitModelService.cs
```

---

## 🔥 핵심 규칙 3가지

1. **인터페이스만**: 구현은 Infrastructure에서
2. **DTO 사용**: Domain Entity 직접 노출 금지
3. **Result 패턴**: 성공/실패를 명확히 반환

---

## 예시: 서비스 인터페이스 만들기

```csharp
namespace DHBIMWATER.Application.Services;

public interface IRevitModelService
{
    /// <summary>
    /// 현재 문서의 모든 일반모델 가져오기
    /// </summary>
    Task<Result<List<GenericModelFamilyDto>>> GetAllGenericModelsAsync();

    /// <summary>
    /// 일반모델 배치
    /// </summary>
    Task<Result<Guid>> PlaceGenericModelAsync(GenericModelFamilyDto model);

    /// <summary>
    /// 일반모델 삭제
    /// </summary>
    Task<Result<bool>> DeleteGenericModelAsync(Guid elementId);
}
```

---

## 📊 데이터 흐름

```
ViewModel (UI)
    ↓ IRevitModelService.GetAllGenericModelsAsync() 호출
Application (인터페이스만)
    ↓ DI가 자동으로 연결
Infrastructure (실제 구현)
    ↓ Revit API 호출
Revit Document
```

---

## ✅ 체크리스트

- [ ] DTO는 단순 데이터만?
- [ ] 인터페이스만 정의하고 구현은 안 했나?
- [ ] Revit API 안 썼나?
- [ ] Result<T> 패턴 사용했나?
- [ ] Infrastructure 참조 안 했나?

---

## 💡 요약

| 항목 | 설명 |
|------|------|
| **DTO** | 계층 간 데이터 전달용 단순 객체 |
| **Interface** | "무엇을" 할지만 정의 |
| **구현** | Infrastructure에서! |
