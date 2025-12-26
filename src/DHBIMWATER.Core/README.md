# DHBIMWATER.Core (도메인 계층)

## 🎯 한 줄 요약
**재료 + 규칙** (순수한 비즈니스 로직만, Revit/DB/UI 절대 금지!)

> 💡 **비유**: 레고 블록과 조립 설명서
> - **재료(Entity)**: 배수지, 벽, 슬래브 등의 기본 블록
> - **규칙(Business Logic)**: 어떻게 조립해야 하는지의 규칙

---

## ✅ 여기서 만드는 것

### 1. **엔티티 (Entities)** - 비즈니스 핵심 객체
```csharp
// 예시: DistributionReservoir.cs
public class DistributionReservoir
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }

    // Factory Method로 생성
    public static DistributionReservoir Create(string name)
    {
        return new DistributionReservoir { Id = Guid.NewGuid(), Name = name };
    }

    // 비즈니스 로직
    public double CalculateCapacity() { ... }
}
```

### 2. **값 객체 (Value Objects)** - 불변 값
```csharp
// 예시: Length.cs
public sealed class Length
{
    private readonly double _millimeters;

    public double Meters => _millimeters / 1000.0;

    public static Length FromMeters(double m) => new Length(m * 1000.0);
}
```

### 3. **열거형 (Enums)** - 도메인 상수
```csharp
// 예시: ReservoirType.cs
public enum ReservoirType { RC, PC, Composite }
```

### 4. **인터페이스** - 계약만 정의 (구현은 Infrastructure에서)
```csharp
// 예시: IReservoirModelingService.cs
public interface IReservoirModelingService
{
    Task CreateReservoirAsync(DistributionReservoir reservoir);
}
```

---

## ❌ 여기서 하면 안 되는 것

- ❌ Revit API 사용 (`Document`, `FamilyInstance` 등)
- ❌ 데이터베이스 접근
- ❌ WPF/UI 코드
- ❌ 파일 읽기/쓰기
- ❌ 외부 라이브러리 참조

---

## 📁 폴더 구조 (간단 버전)

```
DHBIMWATER.Core/
├── Domain/
│   ├── Entities/              # 비즈니스 객체
│   ├── ValueObjects/          # 불변 값 (Length, Point3D 등)
│   └── Enums/                 # 열거형
│
└── Interfaces/
    └── Services/              # 인터페이스만 (구현 X)
```

---

## 🔥 핵심 규칙 3가지

1. **외부 의존성 없음**: Shared 프로젝트만 참조 가능
2. **순수한 C# 코드**: 비즈니스 로직만
3. **인터페이스만**: 구현은 Infrastructure에서

---

## 예시: 엔티티 만들기

```csharp
public class ReservoirWall
{
    // 1. Private set으로 보호
    public Guid Id { get; private set; }
    public WallType Type { get; private set; }
    public Thickness Thickness { get; private set; }

    // 2. Private constructor
    private ReservoirWall() { }

    // 3. Factory Method로 생성
    public static ReservoirWall Create(WallType type, Thickness thickness)
    {
        return new ReservoirWall
        {
            Id = Guid.NewGuid(),
            Type = type,
            Thickness = thickness
        };
    }

    // 4. 비즈니스 로직 메서드
    public double GetLength()
    {
        // 계산 로직...
        return 0;
    }
}
```

**기억할 것:**
- `private set` 사용
- `Create()` 메서드로 생성
- 비즈니스 로직은 엔티티 안에

---

## ✅ 체크리스트

- [ ] Revit API 안 썼나?
- [ ] UI 코드 없나?
- [ ] 외부 라이브러리 참조 없나?
- [ ] Factory Method로 생성하나?
- [ ] Value Object는 불변인가?
