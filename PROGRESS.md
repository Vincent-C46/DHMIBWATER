# PROGRESS

> 작업이 시작되거나 완료될 때마다 이 파일을 업데이트한다.
> 현재 브랜치: `feature/wTank`

---

## 완료된 작업

### 인프라 / 공통
- [x] Clean Architecture 계층 구조 설계 및 프로젝트 셋업
- [x] DI Container 구성 (`ServiceContainer`, `Func<Document?>` 패턴)
- [x] Revit Transaction 추상화 (`ITransactionContext`, `RevitTransactionContext`)
- [x] `Result<T>` 공통 결과 타입
- [x] **DHBoost 매스 로더 / n점 가변 패밀리 배치 로직 바이너리 참조 연결** (2026-07-16)
  - 목적: 소스 비공개(코드 유출 방지)로 DHBoost 로직 재사용 → 컴파일된 DLL 참조 방식(A안)
  - Costura `ExcludeAssemblies` 가 제외목록 방식이라 참조 DLL은 `DHBIMWATER.Revit.dll` 에 자동 임베드(배포 추가작업 없음)
  - **ILRepack 으로 참조 4개 → 1개 병합** (2026-07-16 갱신)
    - `DHBoost.Infrastructure.csproj` 에 `ILRepack.Lib.MSBuild.Task` + `AfterTargets="Build"` 타깃 추가
      (Release 빌드 시 4개 라이브러리 → `bin/Release/net8.0-windows/ilrepack/DHBoost.Combined.dll` 자동 병합)
    - `Internalize=false` (DHBIMWATER 가 public 타입 사용) / 외부참조(Revit·M.E.DI·ExcelDataReader)는 병합 안 함
    - 함정: 출력 파일을 `LibraryPath` 와 같은 폴더에 두면 이전 산출물이 재입력돼 "Duplicate type" 오류 → **출력을 `ilrepack/` 하위 폴더로 분리**해 해결
    - `src/DHBIMWATER.Revit/libs/DHBoost/` 는 이제 `DHBoost.Combined.dll` 1개, `DHBIMWATER.Revit.csproj` 참조도 1줄
  - 빌드 검증: DHBoost Release / DHBIMWATER.Revit Release 모두 오류 0개, 출력 DLL 37.5MB(임베드 유지) 확인
  - [ ] TODO: 실제 호출부(Command) 배선 — 호출 위치 확정 후 연결
  - [ ] TODO(선택): 유출 방지 실효를 위한 병합본 난독화(obfuscator) 검토 — 평문 IL은 디컴파일 가능
  - [ ] TODO: DHBoost 로직 변경 시 Release 재빌드 → `ilrepack/DHBoost.Combined.dll` 을 libs 로 수동 갱신

### 수량산출 (Quantity)
- [x] `IQuantityExtractor` 인터페이스 정의
- [x] Extractor 구현: Beam, Column, Floor, Foundation, GenericModel, Rebar, Stairs, Wall, Railing, DirectShape
- [x] `CalculateQuantityUseCase` — Extractor 순회 → DataStorage 저장 → 반환
- [x] `ExportQuantityUseCase` — 수량 Excel 내보내기
- [x] `IElementQuantityRepo` / `ElementQuantityRepo` — Revit DataStorage 저장
- [x] `IManualQuantityRepo` / `ManualQuantityRepo` — 수동 수량 DataStorage 저장
  - DI 오류 수정: `Document` 직접 주입 → `Func<Document?>` 패턴으로 변경 (2026-06-22)
- [x] `ManualQuantityViewModel` — 수동 수량 입력 (New / Edit 모드, 산출식 미리보기)
- [x] `QuantityRequestHandler` — ExternalEvent 기반 비동기 처리

#### 수량산출 Rule Engine 전환 (2026-06-22 ~ 06-23) — 상세 로그 정리됨
- [x] Wall/Column/Beam/Floor/Foundation/Rebar 를 `IQuantityExtractor` → `IElementMeasurementExtractor` + `QuantityRuleEngine` 경로로 전환
- [x] 핵심 타입 신규: `RevitCategory`, `ElementMeasurements`, `QuantityRule`/`RuleFilter`/`RuleSet`, `DefaultRuleSet`, `QuantityRuleEngine`
- [x] `FormworkType` enum(거푸집 규격), `FaceType.OpeningSide`(EdgeLoop 기반 오프닝 측면 판별) 추가
  > 상세 구조·규칙·키 정의는 [docs/05_수량산출로직.md](docs/05_수량산출로직.md) 참조. 세부 변경 이력은 git log.

#### 동바리 수량 산출 + 슬래브 하부 거푸집 보정 (2026-06-25)
- [x] `RevitIntersectingElementFinder` — `GetTargetCategories(OST_Floors)`에서 `OST_StructuralFraming` 제거
  - 슬래브의 모든 면(Bottom/Side/OpeningSide)에서 보 접촉 공제 없음
  - 보 하부면이 슬래브 하부 거푸집 면적에 자연스럽게 포함됨
- [x] `DefaultRuleSet` — 보 하부 거푸집 규칙 제거 (`Fw(Plywood4, "A_bottom_net", Framing)`)
  - 보 하부면 거푸집은 슬래브 하부 거푸집에 통합, 보에서 별도 산출 안 함
- [x] `RevitFloorMeasurementExtractor` — `CalcShoringHeight()` / `CalcShoringRange()` (2026-06-26)
  - 하부 슬래브 탐색: `OfClass(Floor)`, BBox.Max.Z < 현재 슬래브 하면 Z + XY BBox 겹침 확인 → 가장 가까운 것 선택
  - XY 겹침 없는 슬래브는 후보에서 제외 (평면상 무관한 슬래브가 하부로 잘못 선택되는 버그 수정)
  - 높이(m) = 현재 슬래브 BBox.Min.Z − 하부 슬래브 BBox.Max.Z
  - `ShoringHeightThresholdM = 4.2` 상수 유지 (강관/시스템 분기 기준)
  - Values: `H_shoring`, `ShoringFactor(0.9)`
  - Parameters: `ShoringRange` — `"강관_2.5"` / `"강관_3.5"` / `"강관_4.2"` / `"시스템_5"` / `"시스템_10"` / `"시스템_20"` / `"시스템_30"` (높이 0 이하 → 빈 문자열)
- [x] `DefaultRuleSet` — 동바리 규칙 7개로 세분화 (2026-06-26)
  - WorkType `"동바리"` → `"강관동바리"` / `"시스템동바리"` 분리
  - 강관동바리 (m²): `H≤3.5m` / `3.5m<H≤4.2m`
  - 시스템동바리 (공m³): `H≤5m` / `5m<H≤10m` / `10m<H≤20m` / `20m<H≤30m`
  - 팩토리 헬퍼: `SteelShoring()` / `SystemShoring()` 추가

### 자동 모델링
- [x] `CreatePumpingStationUseCase` — 펌프장 자동 생성
- [x] `ParsePumpManufacturerSpecsUseCase` / `ParseValveExtensionUseCase` — Excel 파싱
- [x] `CreateReservoirUseCase` — 배수지(저수조) 자동 생성 ✅ 2026-06-24
  - DH_Revit_test/RequestHandler.cs 지오메트리 계산식 이식 → `ReservoirGeometryCalculator`
  - 레벨(6개: 구조 4 + LWL/HWL) / 슬래브(B1·L1·B2·L2·B4·L4·MS1·S1·S2·TC1-3) / 선형벽체(W1-10·B3) / 기둥(C1) / 보(G1·H1·H2)
  - LWL/HWL 레벨은 plan view 미생성 (수위 참조용 레벨만)
  - `RevitSlabCommandRepo` 버그 수정: SubPoints 빈 배열일 때 빈 CurveLoop 추가 → profile 오류 발생 ✅ 2026-06-24
  - 기둥·보 유형 선택 방식으로 변경 ✅ 2026-06-24
    - Cw/Cd/Gw/Gh 치수 입력 제거 → `ColumnTypeName`/`BeamTypeName` (Revit 패밀리 타입명)
    - `WaterTankViewModel`: `IElementTypeQueryRepo` 주입, 로드 시 타입 목록 초기화
    - UI: TextBox 4개 → ComboBox 2개 (기둥 유형 / 보 유형)
    - `BeamDefinition.TypeName` 추가 (Width/Height는 PumpingStation 호환용 유지)
  - 사용자 입력 단위 전환: LWL·HWL → m, 나머지 치수 → mm, He → 자동계산(HWL-LWL)
  - `ColumnDefinition` + `IColumnCommandRepo` + `RevitColumnCommandRepo` 신규 추가
  - TODO: 오프닝 배치, 단면뷰 작성 (Calculator 메서드 추가 후 구현)

#### 시스템 Stair(계단) 작성 코드 신규 추가 (2026-07-01)
- [x] `StairsDefinition` / `StairsRunDefinition` / `StairsLandingDefinition` (`Core/Structures`) — Run/Landing 분리 구조
  - Run: `StartPoint`/`EndPoint`/`Justification`(Left/Center/Right)
  - Landing: `BoundaryPoints`(폐곡선, Z=층계참 높이)
  - 컨테이너(`StairsDefinition`): `BaseLevelName`/`TopLevelName`/`BaseOffset`/`TopOffset`/`TypeName` + `Runs`/`Landings` + 공통 메타데이터(Category/ElementCode/Zone/Part)
- [x] `IStairCommandRepo` (`Application/Interfaces`) — `CreateStair(StairsDefinition)`
- [x] `RevitStairCommandRepo` (`Infrastructure/Repositories/Revit/Modeling`)
  - `StairsEditScope.Start()` → 내부 Transaction에서 Run별 `StairsRun.CreateStraightRun()` + Landing별 `StairsLanding.CreateSketchedLanding()` → `stairsEditScope.Commit()`
  - RevitAPI.dll 리플렉션으로 실제 시그니처 확인 후 구현 (`StairsRunJustification`은 Left/Center/Right, `LeftJustification` 아님 등)
  - StairsType은 이름으로 기존 타입 탐색만 (FindOrCreate 미구현, 없으면 실패)
  - DH_ElementCode/DH_Addin/DH_Category/DH_Part/DH_Zone 파라미터 설정
- [x] DI 등록: `ServiceCollectionExtensions.cs`에 `IStairCommandRepo → RevitStairCommandRepo` 추가 (Mock 미등록 — 요청 범위 제외)
- [x] 빌드 확인 완료 (오류 0개)
- TODO: `BaseOffset`/`TopOffset`(mm) 이 Repo에 미반영 — 계단 인스턴스 파라미터 반영 로직 확인 필요
- TODO: `StairsEditScope`는 Revit API 제약상 열려있는 Transaction 내부에서 Start/Commit 불가하나, 사용자 확인에 따라 Repo에서 별도 처리 안 함 → 호출하는 UseCase(예: `CreatePumpingStationUseCase`류)가 이미 트랜잭션을 연 상태에서 호출한다는 전제. 실제 Revit에서 예외 발생 시 트랜잭션 경계 재검토 필요
- TODO: `CreateStair`를 사용하는 UseCase/ViewModel/Ribbon 연동 미작성 (이번 요청 범위 밖)

#### 펌프장 샘플 계단 연동 (2026-07-06)
- [x] `PumpingStationGeometryCalculator.CalculateStairs(dto)` 신설 — 샘플로 **밸브실 → 상부슬래브** 직선 Run 1개 반환
  - 레벨 표고: `상부슬래브 = HWL*1000 + H3`, `밸브실 = 상부슬래브 - (H7+D+H6)`, 상승고 = `H7+D+H6`
  - 수평 진행 길이 ≈ 2×상승고, 밸브실 사이벽 안쪽(`totalLength - T4 - B7`)에서 +X 방향, 폭 중앙 배치
  - `TypeName` 빈 값 → 문서 기본 StairsType 사용, 메타데이터 `ST1`/`밸브실`/`밸브실 계단`
  - 기존 주석 처리된 `CalculateStairs`(실제로는 펌프받침 복붙 코드)는 손대지 않고 그대로 둠
- [x] `CreatePumpingStationUseCase`에 `IStairCommandRepo` 주입 + `Execute()`의 **메인 트랜잭션(`using(_tx)`) 종료 이후** 별도 단계(#11)로 `CreateStair` 호출 → StairsEditScope ↔ 트랜잭션 충돌 회피
- [x] Application 프로젝트 빌드 확인 (오류 0). Revit 실행 중이라 Infrastructure DLL은 pdb 락으로 미빌드 — Revit 종료 후 전체 빌드 필요
- TODO: 실제 Revit에서 계단 생성 동작 검증 미수행 (다음 세션에서 실행 확인)
- TODO: NS1/HS1(단수·단높이) 기반 정식 리저 규칙, 조건부(Type1) 생성, 수평 위치 정밀화는 미반영 — 아래 의사결정 항목 참조

### 도면 (Sheets)
- [x] `SheetUseCase` — 시트 생성/관리
- [x] `WaterReservoirUseCase` — 저수조 도면 배치
- [x] `PumpingStationUseCase` — 펌프장 도면 배치

### 파라미터
- [x] `ExportParamsUseCase` / `ImportParamsUseCase` — 공유 파라미터 내보내기/가져오기

### UI
- [x] 리본 메뉴 구성 (`RibbonBuilder`, `ModelingRibbonModule`, `QuantityRibbonModule` 등)
- [x] 외부 패밀리 라이브러리 (`WebFamilyLibraryCommand`)
- [x] 부재별 탭 아이템 정렬 — `공종 → 규격1 → 규격2` 순으로 변경 ✅ 2026-06-29
  - `QuantityView.xaml` `GroupByMembers` SortDescriptions: `ElementCode/ElementId` → `WorkType/Specification/SubSpecification`

### 문서
- [x] Quantity Extractor 폴더 구조 문서화 → `docs/05_수량산출로직.md` 3장에 통합 ✅ 2026-06-30
  - 16개 파일, 2개 인터페이스 계열, DI 등록 상태, 레거시 미사용 6개 파일, 신규 추가 절차 정리
  - 기존 3-1 표의 `RevitRebarExtractor`(미등록) 활성 오기 → DI 등록 기준으로 정정
  - 처음엔 별도 `06_Quantity_Extractor_구조.md`로 작성했다가 05와 중복되어 **05에 병합 후 06 삭제**
  - `CLAUDE.md` Quantity Extractor 섹션 + 신규 `AGENTS.md`에서 05로 링크 연결
  - `AGENTS.md` 주요 문서 표에 [3장 — 두 가지 산출 경로](docs/05_수량산출로직.md#3-두-가지-산출-경로) 앵커 링크 추가

---

## ⚠️ 의사결정 대기

### 밸브실 계단 작성 (2026-07-03) — 샘플 구현됨(2026-07-06), 정식 규칙은 미결
> 2026-07-06 진행: 아래 5개 항목 중 **2(트랜잭션 경계)** 해결(트랜잭션 밖 별도 단계로 분리), **3·4** 는 샘플값으로 임시 확정(밸브실 사이벽 안쪽 +X, 기본 유형). **1(리저 균등/나머지 반영)·5(NS1/HS1 DTO 전달)** 는 여전히 미결 — 현재 샘플은 `CreateStraightRun` 균등 리저.
요청 요약: 밸브실에 상부슬래브→밸브실로 내려오는 **Run 단독** 계단 작성.
- **조건**: `SelectedPumpingStationType == "Type1"` 일 때만 생성
- **단수**: `NS1` (ViewModel `UpdateNS1()`에 기존 구현: `total = H7 + D + H6`, 200 배수면 -1)
- **수직**: 밸브실 바닥에서 시작 → 상부슬래브보다 **한 단(200mm) 아래**에서 끝
- **단높이**: 기본 200mm(`HS1`), 높이차 나머지는 **최하단 리저에 반영**
  - 해석(확인 필요): 위 NS1-1개 = 200mm, 최하단 = `total - NS1*200` (예 total=2100 → NS1=10, 최하단 100mm + 200mm×9)

조사 완료 사실:
- 레벨 확인: `밸브실` = `상부슬래브 - (H7+D+H6)` → 높이차가 정확히 `total`과 일치
- 계단 인프라(`IStairCommandRepo`/`RevitStairCommandRepo`/`StairsDefinition·Run·Landing`) 이미 존재, DI 등록됨
- B7 계산에 going=300mm, `B7 ≥ NS1*300+1000` → 밸브실에 계단 공간 이미 확보

🚧 결정/정보 필요 (다음 세션 시작점):
1. **리저 모델링** — `CreateStraightRun`은 리저 균등분할만 가능 → "나머지 최하단 반영" 불가.
   - (A) `StairsRun.CreateSketchedRun`으로 Repo 확장(정확, 작업량↑) vs (B) 균등 리저 근사(간단, 요구 불충족)
2. **StairsEditScope ↔ 트랜잭션 충돌 (구조적)** — `CreatePumpingStationUseCase.Execute()`는 `_tx.Begin()`로 큰 트랜잭션을 여는데, `StairsEditScope`는 열린 트랜잭션 내부에서 Start/Commit 불가 → 그대로 끼우면 런타임 예외. 계단만 트랜잭션 밖 별도 단계로 분리 등 경계 재설계 필요.
3. **수평 배치** — Run 시작/끝점, 하강 방향(X/Y), 위치(어느 펌프열/위치) 정보 없음
4. **폭 / 유형** — Run 폭, `StairsType` 이름 (비우면 문서 기본유형 사용 가능)
5. **NS1/HS1 전달** — 현재 ViewModel에만 있음 → DTO(`PumpProfileSpecDto` 등)에 실어 Calculator로 전달 필요, `IStairCommandRepo`도 UseCase에 미주입

제안 순서(승인 시): 1·2 결정 → DTO에 NS1/HS1 추가 + 호출부 갱신 → `PumpingStationGeometryCalculator.CalculateStairs(dto)` 신설(Type1 아니면 빈 리스트) → UseCase에 `IStairCommandRepo` 주입 + 트랜잭션 경계 맞춰 호출 → 빌드/기록.
> 이번 세션은 코드 미변경(조사만). 다음 세션에서 1·3·4(리저 방식·수평배치·폭/유형)와 2(트랜잭션 경계) 확정 후 구현.

### Stair 샘플 코드 구현 방식 (2026-07-01)
- 요청: "0,0,0에서 시작하는 계단 작성하는 샘플 코드" 필요 (`IStairCommandRepo`/`RevitStairCommandRepo` 호출 예시)
- 선택지 2가지 중 미결정:
  1. **UseCase에서 직접 생성 (권장 검토)** — `StairsDefinition`을 UseCase 안에서 인라인으로 조립 후 바로 Repo 호출. 단순 샘플/테스트 목적이라 Calculator 계층 생략. 추후 파라메트릭 로직 필요해지면 그때 Calculator로 분리.
  2. **Calculator + UseCase 둘 다 생성** — `PumpingStationGeometryCalculator`처럼 `StairsCalculator`를 별도로 만들어 `StairsDefinition` 계산을 담당시키고, UseCase는 트랜잭션/Repo 호출만 담당. 지금은 로직이 단순해도 기존 패턴과 통일성 유지 목적.
- 다음 작업 시 사용자에게 위 선택지 확인 후 진행할 것.

---

## 진행 중인 작업

#### 밸브실 GeometryCalculator 샘플 단순화 (2026-07-16)
- [x] `ValveRoomGeometryCalculator`의 펌프장 복붙 대량 분기/반복 로직을 제거하고, 각 `Calculate*` 메서드가 대표 샘플 1개만 직접 생성하도록 재작성.
  - 대상: 레벨, 슬래브, 선형벽체, 프로파일벽체, 보, 솔리드, 슬래브/벽 오프닝, 일반모델, 단면뷰, 계단.
  - 파일 규모: 약 2090줄 → 360줄.
- 검증: `dotnet build src\DHBIMWATER.Application\DHBIMWATER.Application.csproj --no-restore` 오류 0개. 기존 nullable/미사용 필드 경고는 남음.

#### 밸브실 모델링 입력 UI 목업 (2026-07-15)
- 배경: 밸브실 자동 모델링 로직 착수. 사용자 입력 항목 정의를 위한 입력창 목업 선작성.
- [x] `docs/08_밸브실모델링입력목업.html` — WPF 룩앤필 입력창 목업(기존 `07_수량설정창목업.html` 스타일 준용)
  - 입력 그룹: **층 구성**(층 개수 → 층별 층고 동적 테이블), **구조 두께**(버림/기초/외벽/벽체/상부슬래브/중간슬래브), **보 배치**(X·Y축 보 개수), **부재 유형**(기둥/보 패밀리 타입 드롭다운)
  - JS: 층 개수 변경 시 층고 입력 행 자동 증감(기존값 캐시), 하단 요약 배너(총 층수·전체 층고 합계) 갱신
  - 가정: 외벽=외곽 콘크리트/벽체=내부 칸막이 구분, 두께 단위 mm, 중간슬래브는 2층↑에서만 사용
- [x] `docs/08_밸브실모델링입력목업_v2.html` — 종류 선택 + 탭 구조 도입 (범용 스타일)
  - 최상단 **밸브실 종류** 드롭다운(이토/제수/공기) → 선택 시 평면·높이·중간벽 프리셋 자동 세팅
  - **탭 2개**: `부재 유형`(기둥/보 유형 + 버림/기초/외벽/벽체/상부슬래브 두께) · `형상·배치`(내부 폭·길이·높이 + 중간벽 개수 + 보 X/Y 개수)
  - **층 구성 제거**(단층 전제) → 대신 내부 높이(H) 필드 추가, 중간슬래브 두께 제외
  - **중간벽 두께 = 벽체 두께 공용**(별도 필드 제거), 중간벽 방향은 종류별 고정(입력 제거)
  - 결정: **중간벽은 이토밸브실에 있음**(제수·공기 없음). ※ 앞선 v1 가정(제수=중간벽)에서 정정됨
- [x] `docs/08_밸브실모델링입력목업_v3.html` — **PumpingStationView 룩앤필 적용** (진행 채택본)
  - 실제 스타일 소스에서 값 이식: `Styles/Colors.xaml`(Primary `#2196F3`, bg `#F4F7F9`, 섹션 `#EEF2F7`, 헤더 `#4A6FA5`), `Controls/TitleBar.xaml`(`#2C3E50` + 물방울 아이콘), `Styles/TabControls.xaml`(밑줄형 탭)
  - 입력 행 관용구 = 펌프뷰와 동일 `코드 | 설명 | [입력] | 단위` (예: `To 외벽 두께 [ ] mm`)
  - 코드(Tb/Tf/To/Tw/Ts/W/L/H/NW/NBx/NBy)는 목업용 임시 명칭 → DTO 정의 시 확정
- 가정값(도면 확정 후 교체): 이토 2500×4000×2500·중간벽1 / 제수 2000×3000×2500·중간벽0 / 공기 1500×1800×2000·중간벽0
- 남은 작업: **v3 기준으로 진행 + 한 차례 더 수정 예정** → 도면 수령 후 프리셋 실치수·중간벽 고정 규칙 확정 → 입력 DTO 정의 → `ValveRoomGeometryCalculator`(WIP, 현재 펌프장 복붙 상태) 재작성 → 모델 생성 로직

#### 수량산출 설정 → 산출 파이프라인 연동 Phase 1+2 (2026-07-13)
- 배경: 설정창 저장/로드는 되지만 저장값이 산출에 반영 안 됨(`CalculateQuantityUseCase`가 `DefaultRuleSet.Create()`를 인자 없이 호출). 테스트(`QuantitySettingsConsumerTests`)가 미구현 API를 참조하는 TDD 스펙 상태였음.
- **Phase 1 — 순수 소비 로직 (Core/Application)**
  - [x] `QuantityItem.CategoryId`(int) 추가 + `QuantityRuleEngine.Apply`에서 `measurements.CategoryId` 전파 (철근비 카테고리 조회용)
  - [x] `DefaultRuleSet.Create(FormworkSettings)` 오버로드 — 거푸집 규칙을 설정값으로 생성. 기존 `Create()`는 `Create(new FormworkSettings())`에 위임(하위호환, 기본값=기존 하드코딩과 동일)
    - 벽 오프닝측면→마구리(End), 슬래브 오프닝측면(RC)→SideRc, (무근)→SidePlain 매핑
  - [x] `RebarApproximationCalculator.Create(items, RebarRatioSettings)` 신규 — RC 콘크리트 → `철근(개략)`[ton] (V×ρ÷1000)
  - [x] `LossRateCalculator.Calculate(net, workType, LossRateSettings)` 신규 — 정미량×(1+할증률/100)
- **Phase 2 — 산출 파이프라인 연동**
  - [x] `CalculateQuantityUseCase`에 `IQuantitySettingsRepository` 주입 → `settings.Formwork`로 규칙 생성 + `settings.RebarRatio`로 철근(개략) 병행 생성
- 변경 파일: `QuantityItem.cs`, `QuantityRuleEngine.cs`, `DefaultRuleSet.cs`, `RebarApproximationCalculator.cs`(신규), `LossRateCalculator.cs`(신규), `CalculateQuantityUseCase.cs`
- 검증: 솔루션 빌드 오류 0, 테스트 16개 전부 통과 (`QuantitySettingsConsumerTests` 5개 포함)
- 남은 작업(범위 밖): Phase 3 할증률 export 연동, Phase 4 면적공제 매트릭스+오프닝 임계값(`RevitIntersectingElementFinder`, geometry)

#### 수량산출 설정창 저장 시 "ServiceContainer is not built" 예외 수정 (2026-07-13)
- 원인: `QuantityCommand`가 모델리스 창(`_view.Show()`)을 여는데 `CommandBase.Execute`의 `finally`에서 `ServiceContainer.Dispose()`가 즉시 실행됨. 저장 시점에 `SaveQuantitySettingsUseCase`를 팩토리로 지연 resolve(`() => ServiceContainer.GetService<...>()`)하다 파기된 컨테이너를 건드려 예외 발생. (열기/내보내기/가져오기/산출은 서비스를 미리 캡처해 정상)
- [x] `QuantityCommand.WireSettings()` — `SaveQuantitySettingsUseCase`를 미리 resolve해 캡처(`() => saveUseCase`)로 변경. `RevitTransactionContext`는 Execute 후 내부 Transaction이 null로 초기화되어 재저장에도 재사용 안전함을 확인.
- 변경 파일: `src/DHBIMWATER.Revit/Commands/Quantity/QuantityCommand.cs`
- 빌드: 오류 0 (pdb 잠금 회피 위해 `-p:DebugType=none`로 검증 — Revit 실행 중이면 pdb 잠김)
- 참고(별개 미완 항목): 저장된 `ProjectSettings`가 아직 산출 파이프라인에 소비되지 않음 — `CalculateQuantityUseCase.cs:61`이 `DefaultRuleSet.Create()`를 인자 없이 호출. 아래 "수량산출 설정창" 체크리스트의 소비 연동 항목 참조.

#### 레벨 3D 범위 최대화 API + `ILevelCommandRepo` long 마이그레이션 마무리 (2026-07-03)
- [x] `ILevelCommandRepo.Maximize3dExtents(long levelId)` 추가 — 우클릭 "3D 범위 최대화"의 API 버전(`DatumPlane.Maximize3DExtents()`)
- [x] `RevitLevelCommandRepo.Maximize3dExtents` 구현 (Transaction은 UseCase에서 관리 전제, API 호출만)
- [x] `ILevelCommandRepo` int→long 마이그레이션 잔여 정리 (앞선 미완 상태였음)
  - `MockLevelCommandRepo` — `CreateLevel`/`UpdateLevel` 반환형 + `CreatePlan` 파라미터 int→long, `Maximize3dExtents` 스텁 추가
  - `CreateReservoirUseCase`의 `int levelId → long` (사용자가 처리)
  - `CreatePumpingStationUseCase`는 이미 `long levelId`로 갱신돼 있었음
- [x] 전체 솔루션 빌드 오류 0개 확인
- TODO: `Maximize3dExtents`를 호출하는 UseCase/호출부 미작성 (사용처 확정 후 연동)

#### Revit Element DTO `ElementId` long 마이그레이션 (2026-07-03)
- [x] `RevitElementDto` / `RevitWallDto` / `RevitColumnDto` / `RevitSlabDto` 의 `ElementId`를 `int` -> `long`으로 변경
- [x] 회귀 방지 테스트 추가
  - `tests/DHBIMWATER.UI.Tests/DTOs/Revit/Elements/RevitElementIdTypeTests.cs`
  - 네 DTO의 `ElementId` 프로퍼티가 `long`인지 reflection으로 검증
- [x] 테스트로 RED -> GREEN 확인
  - 변경 전 `dotnet test tests/DHBIMWATER.UI.Tests/DHBIMWATER.UI.Tests.csproj --filter RevitElementIdTypeTests` 실패 (`Expected: long, Actual: int`)
  - 변경 후 동일 테스트 통과
- [ ] 전체 UI 테스트 프로젝트 단독 빌드 검증은 현재 환경 이슈로 보류
  - `dotnet build tests/DHBIMWATER.UI.Tests/DHBIMWATER.UI.Tests.csproj` 실행 시 `src/DHBIMWATER.UI/obj/Debug/net8.0-windows/...*.g.cs` 누락으로 실패
  - 이번 DTO 변경과 직접 관련된 타입 오류는 재현되지 않음

#### 수동 수량 입력 — 변수 길이/면적 측정 연동 구현 완료 (2026-07-03)
- [x] 목업 `docs/manual_quantity_view_v2.html` 기반 UI를 실제 WPF 변수 카드에 반영
- [x] `IMeasurePickService` / `MeasureKind` / `MeasureResult` 추가 (`Application/Interfaces/Quantity`)
- [x] `RevitMeasurePickService` 추가 (`Revit/Commands/Quantity`)
  - `ExternalEvent` + `TaskCompletionSource` 기반으로 길이/면적 측정 요청 처리
  - 길이 측정은 활성 뷰 종류로 분기: `View3D`는 면 선택으로 임시 작업기준면 설정 후 `PickPoint()`, 평면/단면/입면 등은 현재 뷰에서 바로 `PickPoint()`
  - 3D 측정 종료 후 이전 작업평면 복원, 점 부족/비평면 면/면적 null은 `null` 반환으로 기존 값 유지
  - 완료 안내 문구를 실제 동작 기준인 `ESC`로 정리
  - 면적 측정은 `Selection.PickObject(ObjectType.Face)` + `face.Area` → `m²` 변환
- [x] `ManualQuantityViewModel` 확장
  - 선택적 측정 서비스 주입, `MeasureLengthCommand` / `MeasureAreaCommand`, `VariableInput.IsMeasuring` 추가
  - 측정 성공 시 변수 값 `F3` 포맷 반영 + 변수 단위(`m`/`m²`) 동적 세팅, 취소 시 값 유지
- [x] `ManualQuantityView` / `QuantityView` 모드리스 전환
  - `DialogResult` 제거, `CloseRequested` 콜백으로 부모 창에서 `AddItem` / `ReplaceItem` 처리
  - 변수 카드 5열 구조(`변수명 | 값 | 단위 | 길이 | 면적`) + 측정 중 강조 UI 추가
- [x] `QuantityCommand` / `QuantityViewModel` 배선 완료
  - `QuantityCommand`에서 `RevitMeasurePickService` 생성 후 `QuantityViewModel.SetMeasureService()`로 전달
- [x] 자동 테스트 추가
  - `tests/DHBIMWATER.UI.Tests` 신규 생성
  - `ManualQuantityViewModelTests`: 측정 성공, 취소 유지, 서비스 미주입 시 비활성 검증
- [x] 빌드/테스트 확인
  - `dotnet test tests/DHBIMWATER.UI.Tests/DHBIMWATER.UI.Tests.csproj -c Release` 통과
  - `dotnet build src/DHBIMWATER.Revit/DHBIMWATER.Revit.csproj -c Release` 오류 0
- [x] 길이/면적 버튼 아이콘 교체 (2026-07-03)
  - `ManualQuantityView` 변수 카드의 임시 텍스트(`↔`, `▱`) → `Resources/Quantity/length.png` / `area.png` 이미지로 대체
  - 기존 pack URI 패턴(`/DHBIMWATER.UI;component/Resources/Quantity/*.png`) 사용, PNG는 이미 csproj `<Resource>` 등록됨
- TODO: `Enter`/`Space` 완료 지원이 필요하면 Revit 공개 Selection API 바깥의 별도 UX/입력 처리 방식 검토

#### 밸브받침 제원 Excel 파싱 추가 (2026-07-03)
- [x] `PumpValveExtensionDto` — `WithoutCheckValve`/`WithCheckValve`(각 `PumpValveDimensionDto`) 필드 추가
  - `PumpValveDimensionDto(ValveBaseWidth, ValveBaseLength, ValveBaseHeight, ValveBasePlacement)` 신규 record
  - 역지밸브 없음/있음 두 세트를 모두 DTO에 저장 → 소비 시점(`HasCheckValve`)에 선택하는 방식 (토글 시 재파싱 불필요)
- [x] `ParseValveExtensionUseCase` — "밸브 연장" 시트에서 밸브받침 제원 파싱 추가
  - 역지밸브 없음: G,H,I,J열(index 6~9), 역지밸브 있음: N,O,P,Q열(index 13~16)
  - `ParseValveDimension(row, startCol)` 헬퍼로 연속 4개 컬럼 파싱
- [x] 밸브받침 제원 소비 로직 연결
  - `PumpCreationRequestDto`에 `ValveBase`(`PumpValveDimensionDto`) 필드 추가
  - `PumpingStationViewModel`: `ApplyValveExtension()`에서 `HasCheckValve`로 세트 선택 → `_selectedValveBase` 저장, `CreatePumpingStation()`에서 DTO로 전달
  - `PumpingStationGeometryCalculator.CalculateGenericModels()` 밸브받침("DH_받침"):
    - 매개변수 `{ "B"=Width, "L"=Length, "H"=Height }` 설정 (`RevitGenericModelCommandRepo:72` 경로로 인스턴스 매개변수 Set)
    - Origin.X = `totalLength - T4(Type2는 T3) - B7 + ValveBasePlacement`(J/Q열) 로 변경
- [x] 밸브받침 배치값 누락 시 B7/2 기본값 처리
  - `valveBasePlacement = ValveBasePlacement != 0 ? ValveBasePlacement : B7 / 2`
  - 파싱값이 `0`이 되는 세 케이스(① Excel 미로드, ② 관경 D 미매칭, ③ J/Q 셀 비어있음)를 한 곳에서 커버
  - 가정: `0`을 "누락"으로 판정 (배치 오프셋이 실제 0일 가능성 낮음). 실제 0이 유효하면 DTO 필드를 `double?`로 전환 필요

#### PumpingStationViewModel 초기값 B7/NS1 미반영 버그 수정 (2026-07-01)
- [x] `InitializeDerivedValues()` — `_h7` 계산 직후 `UpdateNS1()` 호출 추가
  - 기존엔 생성자에서 필드를 직접 대입(`_d = 800.0` 등)해서 `D`/`H6`/`HS1` 프로퍼티 setter의 `UpdateH7Calculation → UpdateNS1 → ApplyB7Final` 체인이 한 번도 실행되지 않음
  - 그 결과 Type1 기본값 로드 시 `NS1=0`, `B7`이 필드 기본값 `3000`에 멈춰있었음 (정상 계산 시 `NS1=11`, `B7=4300`이어야 함)
  - `UpdateNS1()`이 내부에서 `ApplyB7Final()`까지 연쇄 호출하므로 별도 호출 불필요
  - `NS1`/`B7` setter가 값 변경 시 자체적으로 `OnPropertyChanged` 호출하므로 수동 알림 추가 안 함
  - TODO: NS1 산출식(`ROUNDDOWN`/`MOD`)이 원본 엑셀과 정확히 일치하는지는 엑셀 원본 재확인 필요 — 나머지 0일 때 계단 1단 빼는 로직으로 구현되어 있음

#### PumpingStationViewModel B7 100mm 올림 (2026-06-30)
- [x] `ApplyB7Final` — B7 최종값에 100mm 올림 적용
  - 기존 `effective` 계산을 `baseValue`로 분리 후 `Math.Ceiling(baseValue / 100.0) * 100`
  - Type1(밸브/계단 `Math.Max` 결과)·Type2·3(`_b7Base`) 모두 최종 B7이 100mm 단위로 올림됨

#### 펌프장 θ(기초 경사부 기울기) 힌트 연동 (2026-06-30)
- [x] `PumpingStationView.xaml` — θ ComboBox에 `Tag="θ"` 추가
- [x] `PumpingStationView.xaml.cs` `OnParameterFocused` — 시각 트리를 따라 올라가며 string `Tag` 탐색
  - TextBox만 처리하던 것을 ComboBox 등 모든 컨트롤 호환으로 확장 (기존 TextBox 동작 유지)
- [x] `PumpingStationViewModel._paramHints` — `["θ"]` 힌트 항목 추가 (설계기준 30°/45° 권고 문구)

#### 철근 HostElementId 파이프라인 추가 (2026-06-29)
- [x] `ElementMeasurements.HostElementId` (`long?`) 필드 추가
- [x] `QuantityRuleEngine` — `QuantityItem` 생성 시 `HostElementId` 전달
- [x] `RevitRebarMeasurementExtractor` — `rebar.GetHostId()` 로 `HostElementId` 세팅
  - 구버전 `RevitRebarExtractor` 대비 누락된 유일한 필드였음

#### RevitStairsExtractor 재료 판별 수정 (2026-06-29)
- [x] `Stairs` 객체 자체에는 재료 없음 → `GetStairsMaterialClass()` 헬퍼 추가
  - `stair.GetMaterialIds(false)` 직접 호출로 재료 취득 (Run/Landing 순환 불필요)
  - `STRUCTURAL_MATERIAL_PARAM` 기반 `FamilyInstanceHelper` 미사용
  - Concrete → "철근콘크리트", Metal → "강재", Generic → "기타", null → "미분류"

#### GenericModel / DirectShape 콘크리트 수량 산출 (2026-06-29)
- [x] `FamilyInstanceHelper.IsConcreteMaterial()` 추가
  - `StructuralAssetClass.Concrete` 인 경우만 true, 이름 폴백 없음
  - StructuralAsset 미설정 → false
- [x] `RevitGenericModelExtractor` — 재료 판별 + 귀속 철근 체크
  - 콘크리트 재료가 아니면 수량 없음
  - `GetRebarHostIds()` 캐시 HashSet → `rebar.GetHostId()` 1회 수집 후 O(1) 조회
  - 철근 있으면 철근콘크리트, 없으면 무근콘크리트 (기본값 무근)
- [x] `RevitDirectShapeExtractor` — 동일 로직
  - `ds.GetMaterialIds(false)` 로 재료 취득 (FamilyInstance와 다른 경로)
  - 클래스 기반 수집(`OfClass(typeof(DirectShape))`) 유지 → Floor 카테고리 DirectShape와 Floor 시스템 패밀리 간 충돌 없음
  - 동일한 `GetRebarHostIds()` 캐시 패턴 적용

### WaterTank 기능 (`feature/wTank`)
- [x] 수조(Water Tank) 자동 모델링 로직 ✅ 2026-06-24
- [ ] `WaterTankCommand` 구현 (ExternalEvent 연결)
- [ ] Ribbon 연동 (`ModelingRibbonModule` 수정)

### Rule Engine 검증 (벽체 / 기둥 / 보 / 슬래브 / 기초)
- [ ] Revit에서 수량산출 실행 후 QuantityItem 생성 확인
- [ ] 벽체: 콘크리트(A x Thk), 거푸집(외/내벽 Euroform, 마구리 합판3회), 스페이서 항목 검증
- [ ] 기둥: 직사각형(B x D x L), 원형(PI x R^2 x L), 이형(A_cs x L) 분기 검증
- [ ] 보: 직사각형(B x D x L), 이형(A_cs x L) 분기 검증
- [ ] 슬래브: 콘크리트(A x Thk), 거푸집(하부 합판4회 — 보 공제 없음, 측면 합판3회/합판6회), 스페이서 검증
- [ ] 슬래브: 동바리 — 강관동바리(3구간 m²) / 시스템동바리(4구간 공m³) 높이별 구간 분류 검증
- [ ] 독립기초: 콘크리트(Vol), 거푸집(측면 합판4회/합판6회) 검증

---

## 🔧 미비점 / 개선 필요 (2026-06-30 코드 점검)

- [ ] **레거시 미사용 Extractor 6개 정리 검토** — `RevitWallExtractor` / `RevitColumnExtractor` /
  `RevitBeamExtractor` / `RevitFloorExtractor` / `RevitFoundationExtractor` / `RevitRebarExtractor`
  는 `IQuantityExtractor` 구현이지만 **DI 미등록 + 코드 미참조** (dead code, `*MeasurementExtractor`로 대체됨).
  - 잘못된 using 다수 포함: `DocumentFormat.OpenXml.*`, `System.Windows.Controls`,
    `System.ComponentModel.DataAnnotations`, `System.Data.Common`, `System.Reflection.Metadata.Ecma335`
  - 삭제 여부 결정 필요 (CLAUDE.md: 기존 dead code 임의 삭제 금지 → **컨펌 후 진행**)
- [ ] **`CanExtract(long)` 인터페이스 메서드 전면 미사용** — `IQuantityExtractor` /
  `IElementMeasurementExtractor` 양쪽에 선언되고 모든 Extractor가 구현하지만 **호출부 없음**
  (산출 경로는 `CollectElementIds()` + `Extract()`만 사용). → 단일 요소 재산출 용도로 살리거나 인터페이스에서 제거 검토.
- [ ] **`CalculateQuantityUseCase` `CollectElementIds()` 이중 열거** — `ids.Any()` 체크 후
  `foreach (ids)` 로 lazy `IEnumerable`(`FilteredElementCollector`)를 2회 실행. 양 경로 모두 `.ToList()` 1회 캐싱 권장.
- [ ] **활성 `RevitRailingExtractor` 의 `using System.Diagnostics;`** — 사용 여부 확인 후 미사용이면 정리 (빌드 경고).
- [ ] **거푸집 Specification 하드코딩 잔존** — `DefaultRuleSet` 거푸집 규격이 `FormworkType` enum이지만
  여전히 코드 하드코딩 상태 (CLAUDE.md: Specification 하드코딩 금지). → 아래 "거푸집 종류 설정창 연동" 작업으로 해소 예정.

### QuantityView 상세목록 버튼(삭제/복사/수정) 활성화 지연 (2026-07-01 원인 파악)
- [ ] **`RelayCommand`가 `CommandManager.RequerySuggested`에만 의존** (`RelayCommand.cs:29-33`)
  — `CommandManager.InvalidateRequerySuggested()`(`QuantityViewModel.cs:48` 등)는 Background 우선순위로
  디스패처 큐에 들어가 UI 스레드가 바쁘면 버튼 `IsEnabled` 갱신이 지연됨.
  → `RelayCommand`가 자체 `event` 필드로 `CanExecuteChanged`를 보관하고 동기적으로 `Invoke`하도록 수정 검토.
- [ ] **DataGrid `ColumnWidth="Auto"`가 행 가상화 무력화** (`DataGrids.xaml:61` `QuantityDataGridStyle`,
  `QuantityView.xaml` 상세 목록 컬럼들 `Width="Auto"`) — Auto 폭 계산을 위해 전체 행을 실측해야 해서
  `VirtualizationMode="Recycling"` 지정에도 사실상 가상화 무력화. 항목 수가 많을수록 위 지연 체감 증폭.
  → 고정 픽셀 폭 또는 `*`로 변경 검토.

---

## 예정된 작업

### 비계 수량 산출
- [ ] 건물 외피(envelope)를 하나의 연속 면으로 취급하여 비계 면적 산출
  - 외벽 면 + 슬래브 측면(외벽 연장선상) 포함
  - 예: [1층 벽체 외면] + [슬래브 측면] + [2층 벽체 외면] = 연속 외피

**채택 방향: 외벽 외부 Face 엣지 수집 → 폴리라인 조립**
1. `DH_IsExterior=1` 벽체의 외부 면에서 하단 수평 엣지 수집
2. 슬래브 측면 중 외벽 연장선상에 있는 면의 엣지도 동일하게 수집
3. 끝점이 맞닿는 엣지끼리 연결 → 외곽 폴리라인 조립
4. 폴리라인 세그먼트별 높이 × 길이 = 비계 면적 합산

**검토한 다른 방법**
- 외벽 LocationCurve 끝점 그래프 순회: 중심선 기준이라 실제 외면과 오프셋 차이 있음
- Solid Union 후 외곽 추출: Boolean 연산 비용 크고 실패 가능성 있음



### OpeningBottom / OpeningTop FaceType 추가 (다음 작업)
- [ ] `FaceType.OpeningBottom`, `FaceType.OpeningTop` 추가
  - Wall 오프닝의 헤드(soffit) / 실(sill) 면 분리
  - EdgeLoop 기반: Left/Right 내부 루프 엣지를 수평면도 공유하면 Opening{Bottom/Top}
- [ ] `RevitWallMeasurementExtractor` — `A_opening_bottom_gross/net`, `A_opening_top_gross/net` 키 추가
- [ ] `DefaultRuleSet` — Wall 헤드/실 거푸집 규칙 추가 (실 여부 결정 후 적용)

### Rule Engine 확장
- [x] `IQuantityRuleRepository` DataStorage 구현체 (`RevitQuantityRuleRepo`) ✅ 2026-06-24
  - `QuantityRuleStorageSchema` 신규 (`Infrastructure/Storage/Schemas/`)
  - `RevitQuantityRuleRepo` 신규 (`Infrastructure/Repositories/Revit/Storage/`)
  - DI 등록 완료
  - `CalculateQuantityUseCase`: DefaultRuleSet(공통 공종) + ProjectRuleSet(프로젝트 특화 공종) 병합 적용
    - DefaultRuleSet: 콘크리트·거푸집·철근 등 모든 구조물 공통
    - ProjectRuleSet: 지수판 등 프로젝트별 추가 공종 (없으면 빈 리스트로 처리)

### 수량산출 설정창 (거푸집 + 면적 공제 + 철근비 + 할증률) — 기획 확정 2026-07-09
- 기획 문서: `docs/07_수량설정창기획.md` / 목업: `docs/07_수량설정창목업.html`
- [ ] Core: `DeductionSettings`, `RebarRatioSettings`, `LossRateSettings` 추가, `ProjectSettings` 확장
- [ ] Application: `IQuantitySettingsRepository` + Load/Save UseCase
- [ ] Infrastructure: `RevitQuantitySettingsRepo` (DataStorage, `QuantitySettingsSchema` 활용) + DI 등록
- [x] `DefaultRuleSet.Create(FormworkSettings)` 파라미터화 — 거푸집 하드코딩 제거 (2026-07-13)
- [ ] `RevitIntersectingElementFinder` 공제 매트릭스 연동 + 오프닝 최소 체적(1m³) 임계값
- [x] 철근(개략) 병행 산출 — RC 콘크리트 체적 × 카테고리별 kg/m³ (2026-07-13)
- [ ] 할증률: 집계·Excel에 할증 반영량 열 추가 (정미량은 유지) — 계산기(`LossRateCalculator`)만 구현됨, export 연동 미완
- [ ] UI: `QuantitySettingsView` (탭 4개) + QuantityView ⚙버튼 + ExternalEvent + .dhcfg 내보내기/가져오기

### 수량산출 추가 기능 로드맵 — 기획 확정 2026-07-09
- 기획 문서: `docs/08_수량산출_추가기능_기획.md` (우선순위 순)
- [ ] 1. 산출근거서 출력 — Excel에 RenderedFormula·공제내역 시트 추가
- [ ] 2. 산출 누락 진단 리포트 — 스킵 요소·사유 수집 및 표시
- [ ] 3. 방수·방식 면적 산출 — 수조 내부 wet face 판별 (HWL 설정)
- [ ] 4. 수량 증감 비교 — 스냅샷 DataStorage 저장 + diff 뷰/증감표
- [ ] 5. 층별/구역별 집계 + 요소 역추적 (하이라이트/줌)
- ❌ 토공량(터파기/되메우기/버림콘크리트) — Civil3D에서 산출, 애드인 범위 영구 제외

### 프로젝트 RuleSet
- [ ] 지수판 길이 등 프로젝트 특화 공종 정의
- [ ] Rule CRUD 설정창 UI

### 부재별 탭 ElementCode 순 그룹 정렬 (계획 문서 작성됨 2026-06-30)
- 계획 문서: `docs/부재별탭_ElementCode_정렬_계획.md`
- [x] `QuantityView.xaml` `GroupByMembers` SortDescriptions를 `Category → ElementCode → ElementId → WorkType → Specification → SubSpecification` 순으로 변경
  - 그룹 표시 순서를 ElementCode 순으로 (단순 문자열 정렬, `B1, B10, B2` 순 허용)
- [x] `QuantityItem.ElementGroupLabel` — `WorkType == "철근"`이면 ElementId 없이 ElementCode로만 그룹화
- 주의: 기존 line 161 "공종 → 규격1 → 규격2" 정렬을 ElementCode 우선으로 되돌리는 변경

### 기타
- [x] 배수지 일반 패밀리 배치(단차버림 `L3` / PIT `P1`) 복원 + 설정 CSV 내보내기/가져오기 — 지시서 `docs/09_배수지_일반패밀리배치_지시서.md` 기준 구현 완료 (2026-07-10)
  - 완료: `SLv` DTO 연결, `Dimensionless` 값 타입, `GenericModelPlacementDefinition.Class`, `RevitGenericModelCommandRepo` 무차원/`DH_Class` 세팅, `ReservoirGeometryCalculator.CalculateGenericModels()`, `CreateReservoirUseCase` 배치 단계, `WaterTankViewModel` CSV Export/Import + XAML 버튼
  - 검증: `dotnet test tests\DHBIMWATER.UI.Tests\DHBIMWATER.UI.Tests.csproj` 통과, `dotnet build DHBIMWATER.sln /p:DebugType=None /p:DebugSymbols=false` 컴파일 오류 0
  - 미결: Revit 실행 중 애드인 DLL/PDB 잠금으로 일반 `dotnet build DHBIMWATER.sln`의 복사/PDB 단계는 실패/경고 발생. Revit 종료 후 일반 빌드 및 L3/P1 실제 배치 위치/패밀리 매개변수 육안 확인 필요
- [ ] 배수지 오프닝 배치 — `ReservoirGeometryCalculator.CalculateOpenings()` 추가 후 UseCase 연결
- [ ] 배수지 단면뷰 작성 — `ReservoirGeometryCalculator.CalculateSectionViews()` 추가 후 UseCase 연결
- [ ] 수량산출 결과 검증 로직 보강





---

## 2026-07-06

### 계단 원하는 챌판 수 내장 파라미터 설정
- [x] RevitStairCommandRepo에서 기존 StairsDefinition.RisersNumber 값을 BuiltInParameter.STAIRS_DESIRED_NUMBER_OF_RISERS에 직접 설정하도록 보강.
- [x] UpdateNS1 계산식은 요청에 따라 변경하지 않음.
- 검증: dotnet build DHBIMWATER.sln -c Release 종료 코드 0. dotnet build DHBIMWATER.sln Debug 빌드는 DHBIMWATER.Core.pdb 파일 잠금으로 실패.

---

## 2026-07-06

### 계단 유형 최대 챌판 높이 / 최소 디딤판 깊이 설정
- [x] StairsDefinition.MaxRiserHeight 추가.
- [x] 펌프장 밸브실 계단 생성 정의에서 HS1을 최대 챌판 높이로 전달.
- [x] RevitStairCommandRepo에서 기존 StairsType을 DHBIMWATER 전용 이름으로 복사/재사용하고, STAIRS_ATTR_MAX_RISER_HEIGHT, STAIRS_ATTR_MINIMUM_TREAD_DEPTH를 설정하도록 보강.
- [x] 생성 중인 계단에 복사 타입을 Run 생성 전에 적용하고, 생성 후에도 동일 타입을 재적용.
- 검증: dotnet build src\DHBIMWATER.Infrastructure\DHBIMWATER.Infrastructure.csproj -c Release 종료 코드 0, dotnet build DHBIMWATER.sln -c Release 종료 코드 0. 기존 nullable/MSB3277 경고 및 Revit/Visual Studio 파일 잠금으로 인한 Addins 복사 경고는 남음.

---

## 2026-07-07

### 계단 첫 생성 시 단수 오류(첫 계단만 챌판 13개) 수정
- 증상: 빌드 후 첫 계단만 실제 챌판 수 13개, 2번째 이후는 정상 12개.
- 원인: `RevitStairCommandRepo.CreateStair()`에서 `TOP_OFFSET`(-100mm 등 → 유효높이 2500→2400)과 `STAIRS_DESIRED_NUMBER_OF_RISERS` 확정을 **단일 `doc.Regenerate()`** 로 처리. 첫 계단만 높이 축소가 챌판 수 확정보다 늦게 반영되어, 전체높이 2500 기준 208.33mm > 최대 챌판높이(200)가 되고 Revit이 단수를 12→13으로 늘림.
- [x] `TOP_OFFSET` 설정 직후 `doc.Regenerate()`로 **유효높이(2400)를 먼저 확정**한 뒤 `DESIRED_NUMBER_OF_RISERS` 설정 + 최종 `Regenerate()`로 분리(2단계 재생성). 파일: `src/DHBIMWATER.Infrastructure/Repositories/Revit/Modeling/RevitStairCommandRepo.cs`.
- [x] `GetOrCreateConfiguredStairsType` 내 `typeTx.Commit()` 직전 `doc.Regenerate()` 추가(새 타입 파라미터 즉시 반영). 사용자가 트랜잭션 밖에 넣어 "no open transaction" 났던 것 → 트랜잭션 안으로 이동.
- [x] **Run 생성 전** 빈 계단에 `ChangeTypeId`+`ActualTreadDepth` 먼저 적용(→ Run이 올바른 타입으로 생성, 기본 타입 오염 방지) → `Regenerate` → Run 생성 → `TOP_OFFSET`/`DESIRED_NUMBER_OF_RISERS`는 Run 후 적용. **결과: 첫 계단 챌판 수 13→12로 수정 확인됨.**
- [~] 첫 계단만 실제 챌판높이가 어긋나는(2500/13=192.3mm) 잔여 문제 대응 시도: 계단 커밋 후 postTx(안정된 컨텍스트)에서 `TOP_OFFSET`(유효높이 2400) + `DESIRED_NUMBER_OF_RISERS` 재확정. **→ 실테스트 결과 실패(첫 계단 여전히 192.3mm). 미해결.**
- **⚠️ 미해결 인계 문서: [`docs/BUGFIX_계단_첫생성_챌판높이_현황.md`](docs/BUGFIX_계단_첫생성_챌판높이_현황.md)** — 증상/근본원인/시도 이력/다음 단계(진단 로그) 정리. 다른 세션에서 이 문서부터 확인.
- 검증: dotnet build src\DHBIMWATER.Infrastructure\DHBIMWATER.Infrastructure.csproj --no-dependencies 종료 코드 0(오류 0). Revit이 애드인 DLL/pdb 로드 중이면 CS2012/MSB3030 파일 잠금 발생 → 배포 빌드는 Revit 종료 후.
- 참고(별건, 미수정): `PumpingStationGeometryCalculator`의 `rise = treadNum*riserHeight`(11칸)와 Repo의 `actualStairHeight = MaxRiserHeight*RisersNumber`(12칸) 높이 개념 불일치 존재.

---

## 2026-07-07

### 계단 첫 생성 챌판높이 잔여 문제 진단 로그 추가
- [x] `RevitStairCommandRepo.CreateStair()`에 `STAIR_DIAG` 진단 로그 추가.
  - 기록 위치: `%LOCALAPPDATA%\DHBIMWATER\Logs\DHBIMWATER.log`
  - 단계: 입력값, 전용 `StairsType` 준비 직후, Run 생성 전 타입/디딤판 적용 후, Run 생성 후, 편집 스코프 내부 `TOP_OFFSET`/단수 설정 후, `StairsEditScope.Commit()` 후, postTx 재확정 후.
  - 기록값: 계단 생성 순번(seq), 레벨 높이(mm), 목표 높이/TopOffset(mm), 타입 ID/이름/최대 챌판높이/최소 디딤판깊이/최소 폭, 계단 ID/typeId, `STAIRS_TOP_OFFSET`, `STAIRS_DESIRED_NUMBER_OF_RISERS`, `ActualRisersNumber`, `ActualRiserHeight`, `ActualTreadDepth`.
- 목적: 첫 번째 계단과 두 번째 계단의 실제 Revit 파라미터 차이를 데이터로 확인해, 새 `StairsType` 첫 사용 문제인지 `TOP_OFFSET` 반영 문제인지 분리.
- 검증: `dotnet build src\DHBIMWATER.Infrastructure\DHBIMWATER.Infrastructure.csproj --no-dependencies` 오류 0개. 기존 nullable/MSB3277 경고는 남음.

---

## 2026-07-07

### 계단 첫 생성 챌판높이 로그 분석 및 Height 직접 세팅 실험
- 로그 분석 결과:
  - 첫 계단(`seq=1`)도 전용 타입, `topOffsetMm=-100`, `desiredRisers=12`, `actualRisers=12`는 정상 반영됨.
  - 문제는 첫 계단만 `actualRiserHeightMm=192.308`이 `TOP_OFFSET`/단수 재설정 후에도 유지되는 것.
  - 두 번째 계단(`seq=2`)은 동일 단계에서 `actualRiserHeightMm=200`으로 정상 재계산됨.
  - 결론: `TOP_OFFSET` Set 실패가 아니라 첫 계단 Run 생성 시 계산된 실제 챌판높이가 이후 재계산되지 않는 문제로 판단.
- [x] `RevitStairCommandRepo`에서 Run 생성 후 `STAIRS_TOP_OFFSET` 적용 다음에 `Stairs.Height = actualStairHeight`를 직접 설정하도록 실험 코드 추가.
  - edit scope 내부와 postTx 양쪽에 적용.
  - `STAIR_DIAG` 로그에 `heightMm` 추가.
- 검증:
  - `dotnet build src\DHBIMWATER.Infrastructure\DHBIMWATER.Infrastructure.csproj --no-dependencies`는 Debug PDB 파일 잠금(CS2012)으로 실패.
  - `dotnet build src\DHBIMWATER.Infrastructure\DHBIMWATER.Infrastructure.csproj --no-dependencies -c Release` 오류 0개. 기존 nullable/MSB3277 경고는 남음.
- 다음 확인: Revit에서 다시 첫 생성 실행 후 `STAIR_DIAG seq=1`의 `04b-after-edit-height`, `05-after-edit-final-regenerate`, `10-after-post-commit`에서 `actualRiserHeightMm=200`인지 확인.

---

## 2026-07-07

### 계단 첫 생성 챌판높이 warm-up 우회 적용
- 사용자 Revit 오류 확인: `The stairs top level is not "None", so the height cannot be set independently`.
  - 원인: 상부 레벨이 지정된 계단은 `Stairs.Height`를 독립 설정할 수 없음.
  - 조치: `Stairs.Height = actualStairHeight` 실험 코드는 제거.
- [x] 새 전용 `StairsType`을 처음 생성한 경우에만 warm-up 계단을 1개 생성 후 삭제하도록 `RevitStairCommandRepo` 수정.
  - `GetOrCreateConfiguredStairsType()`가 `(StairsType, Created)`를 반환하도록 변경.
  - `Created == true`일 때 `WarmUpNewStairsType()` 실행.
  - warm-up 계단은 동일 타입/디딤판/Run/TopOffset/DesiredRisers 설정 후 `StairsEditScope.Commit()`하고, 별도 Transaction으로 즉시 삭제.
  - 목적: Revit 내부 계단 타입/solver 첫 사용 상태를 더미 계단에서 먼저 소모하고 실제 첫 계단이 두 번째 계단처럼 계산되도록 우회.
- 검증: `dotnet build src\DHBIMWATER.Infrastructure\DHBIMWATER.Infrastructure.csproj --no-dependencies -c Release` 오류 0개. 기존 nullable/MSB3277 경고는 남음.
- 다음 확인: Revit에서 타입이 없는 새 모델/문서 상태로 다시 생성 후 `STAIR_DIAG seq=1`의 실제 계단이 `actualRiserHeightMm=200`인지 확인.

---

## 2026-07-09

### 수량산출 설정창 기획 (거푸집 / 면적 공제 / 철근비)
- [x] 기획 문서 작성: `docs/07_수량설정창기획.md`
  - 저장: DataStorage 기본 + `.dhcfg` 내보내기/가져오기 (기존 `DhcfgRepo`·`QuantitySettingsSchema` 재활용)
  - 철근비: 카테고리별 kg/m³, 실물 철근과 무관하게 "철근(개략)" 항목 항상 병행 산출
  - 공제: 호스트×인접 카테고리 매트릭스 + 오프닝 최소 체적 임계값(기본 1m³, 기존 예정 규칙 통합)
- [x] UI 목업 작성: `docs/07_수량설정창목업.html` (브라우저에서 열어 확인)
- 미결: 철근비 기본값 숫자 확정(기초 80/슬래브 100/벽 110/보 130/기둥 150은 가안), 계단 거푸집은 1차 범위 제외

### 수량산출 추가 기능 로드맵 확정 (같은 날 후속)
- [x] 설정창에 **할증률 탭** 추가 결정 → 문서 07에 `LossRateSettings`(§3-4) 반영, 목업에 탭 4번째 추가
  - 원칙: 정미량 유지, 집계·Excel에서만 할증 반영량 병기. 기본값(콘크리트 2/철근 3/거푸집 5/강재 3%)은 가안
- [x] 후속 기능 로드맵 문서 작성: `docs/08_수량산출_추가기능_기획.md`
  - 순서: 산출근거서 → 누락 진단 → 방수·방식 → 증감 비교 → 층별 집계·역추적
- [x] **토공량은 영구 제외** — Civil3D에서 산출하기로 결정
- [x] **철근 할증률 확정: 직경 구분 없이 일괄 3%** (2026-07-09) — 표준품셈은 이형철근 3%/원형철근 5%,
  교량 등 복잡구조물 주철근 6~7% 조정 가능하지만, 본 애드인은 직경 구간별 세분화 없이 단일 3% 값으로 단순화.
  `LossRateSettings.RatePercent["철근"] = 3.0` 단일 항목으로 충분 — 문서 07 §3-4·목업 반영 완료.
- **다음 세션 시작점**: 문서 07 §7 구현 순서 1번부터 착수.
  Core에 `DeductionSettings`, `RebarRatioSettings`, `LossRateSettings` 추가 → `ProjectSettings` 확장.
  (`RebarSettings`는 겹이음/정착길이용으로 이미 존재하니 혼동 주의 — 할증률은 신규 `LossRateSettings`)


---

## 2026-07-13

### WaterTankViewModel 대형 치수 입력 단위 m 전환
- [x] `WaterTankViewModel`의 대형 수조/밸브실 치수 기본값을 m 단위로 변경.
  - 대상: `W`, `L`, `M1`~`M4`, `Wh`, `Lh`, `Hh`, `Ltt`, `H1F`, `Lv`, `Wv`, `Lvt`, `We`, `Wp`, `Hp`.
  - `Hf`, `Hm`, `TrOff`, `WpThk`, `SpThk`, 단면 두께류는 mm 유지.
- [x] 내부 `ReservoirGeometryCalculator` 기준은 mm로 유지하고, DTO 생성 직전 m 입력값을 mm로 환산하도록 `BuildCreationRequestDto()` 분리.
- [x] `H2F`와 `CRT` 계산식을 m 입력 기준으로 보정.
- [x] WaterTank XAML/CSV 내보내기 단위 표시를 m/mm 기준에 맞게 정리.
- [x] `WaterTankViewModelTests` 추가: 기본 m 입력값 및 DTO mm 환산 검증.

### 수량산출 설정창 View/ViewModel 작성 (UI 셸 + 콜백)
- 기획/목업 기준: `docs/07_수량설정창기획.md` §7 구현순서 1번 + `docs/07_수량설정창목업.html`
- [x] Core 데이터 모델 3개 추가 + `ProjectSettings` 확장
  - `DeductionSettings` (CategoryMatrix `Dictionary<RevitCategory, List<RevitCategory>>` + OpeningMinVolumeM3 + UseOpeningMinVolume) — 기본 매트릭스는 현재 하드코딩과 동일
  - `RebarRatioSettings` (Enabled + 카테고리별 kg/m³ 기본 가안: 기초80/슬래브100/벽110/보130/기둥150)
  - `LossRateSettings` (Enabled + 공종별 % 기본: 철근콘크리트2/무근콘크리트2/철근3/거푸집5/강재3)
  - `ProjectSettings`에 `Deduction`/`RebarRatio`/`LossRate` 프로퍼티 추가
- [x] `QuantitySettingsViewModel` (`ViewModels/Quantity/`) — 탭 4개 바인딩
  - 거푸집: `FormworkRowVm` 12행, 선택값은 주입된 `FormworkSettings`에 write-through / `FormworkOption`(enum+한글)
  - 면적 공제: `DeductionRowVm` 5행(벽/슬래브/기둥/보/계단) × 5열 체크박스 + 오프닝 임계값
  - 철근비/할증률: `RebarRatioRowVm`/`LossRateRowVm` + Enabled 토글
  - `SaveCommand`/`CancelCommand`/`ImportCommand`/`ExportCommand`, 이벤트 `CloseRequested`/`SaveRequested`/`ImportRequested`/`ExportRequested`
  - `ApplyToSettings()`로 각 탭 상태를 주입 `ProjectSettings`에 반영
- [x] `QuantitySettingsView.xaml` + code-behind (`Views/Quantity/`)
  - 기존 `TitleBar` + `PrimaryTabControlStyle` + `Generic.xaml` 재사용, DataGrid 4개로 목업 레이아웃 반영
  - `ManualQuantityView`와 동일한 모드리스 패턴(`CloseRequested += Close`)
- 검증: `dotnet build src\DHBIMWATER.UI\DHBIMWATER.UI.csproj -c Release` 오류 0개(기존 경고만), `dotnet test tests\DHBIMWATER.UI.Tests` 11/11 통과

### 수량산출 설정창 배선 (Repo + ExternalEvent + ⚙버튼) — 문서 07 §7 2·3·8
- [x] Application: `IQuantitySettingsRepository`(DataStorage용 Load/Save) + `SaveQuantitySettingsUseCase`(ITransactionContext) + DI 등록
  - 파일 기반 `IProjectSettingsRepository`(.dhcfg)와 이름 분리 — 충돌 없음
- [x] Infrastructure: `RevitQuantitySettingsRepo` — `ManualQuantityRepo` 패턴, 미사용이던 `QuantitySettingsSchema` 활용, ProjectSettings 단일 JSON 저장(`JsonStringEnumConverter`로 `RevitCategory` enum 키 직렬화) + DI 등록
- [x] UI: `QuantityViewModel`에 `OpenSettingsCommand` + `SetSettingsAction` 추가, `QuantityView.xaml` 툴바에 ⚙ 설정 버튼 추가
- [x] Revit: `QuantitySettingsRequest`/`QuantitySettingsRequestHandler`(ExternalEvent) 신규 — Open=DataStorage 로드 후 UI스레드에서 창 오픈, Save=UseCase 트랜잭션 저장
  - `QuantityCommand.WireSettings()`/`WireSettingsVm()`: ⚙→로드 이벤트, 저장→DataStorage, 가져오기/내보내기→`DhcfgRepo`(.dhcfg) 파일 다이얼로그, 가져오기 시 VM 재생성+재바인딩
- 검증: `dotnet build src\DHBIMWATER.Revit\DHBIMWATER.Revit.csproj -c Release` 오류 0개, `dotnet test` 11/11 통과
- 미결/다음: Revit 실물 동작 검증(로드/저장/`.dhcfg` 입출력 육안 확인), 소비지점 연동 — 아직 저장만 되고 산출엔 미반영. 철근비 기본값 숫자 확정(가안)
- ▶ **소비지점 연동은 codex용 지시서로 분리 발주**: [`docs/10_수량설정_소비지점연동_지시서.md`](docs/10_수량설정_소비지점연동_지시서.md) (거푸집 `DefaultRuleSet` 파라미터화 / 공제 매트릭스+오프닝 임계값 / 철근개략 / 할증률 열, 문서 07 §7 4~7)

### B4/L4 바닥 Y축 50mm 돌출 수정
- [x] 원인 확인: `ReservoirGeometryCalculator.CalculateSlabs()`의 B4/L4 Y 방향 외곽 계산에서 밸브실 외벽두께(`WveThk=300`) 대신 수조부 외벽두께(`WteThk=350`)가 섞여 기본값 기준 50mm 과대 돌출됨.
- [x] 수정: B4/L4 Y 길이 산정식을 `2 * WveThk + Wv + Lvt` 기준으로 변경.
- [x] 회귀 테스트 추가: `CalculateSlabs_UsesValveExteriorWallThicknessForB4AndL4YExtent`에서 B4/L4 Y 최소/최대 좌표 검증.
- 검증:
  - `dotnet test tests\DHBIMWATER.UI.Tests\DHBIMWATER.UI.Tests.csproj` 통과: 11/11.
  - `dotnet build DHBIMWATER.sln` 오류 0개. 기존 Revit Addins 복사 잠금 경고 및 기존 참조/nullable 경고는 남음.

---

## 2026-07-14

### 수량산출 설정창을 독립 리본 커맨드로 분리 (`QuantitySettingCommand`)
- 계획/설계 문서: `docs/superpowers/specs/2026-07-14-quantity-settings-command-design.md`, `docs/superpowers/plans/2026-07-14-quantity-settings-command.md`
- 배경: 기존엔 `QuantityView` 툴바 ⚙ 버튼으로 설정창을 열었는데, 계획대로 산출창과 설정창을 완전히 분리하기로 확정. 진행 중 `QuantityViewModel.SetSettingsAction`이 이미 제거된 상태에서 `QuantityCommand.WireSettings()`가 여전히 이를 호출해 빌드 오류(CS1061류) 발생 → 이를 계기로 나머지 Task 마무리.
- [x] `QuantitySettingCommand.cs` 신규 (`Revit/Commands/Quantity/`) — `QuantityCommand`의 `WireSettings`/`WireSettingsVm`을 그대로 이동
  - 설정창 owner를 기존 `_view`(QuantityView) 대신 `commandData.Application.MainWindowHandle`(`WindowInteropHelper`)로 변경 — 독립 커맨드라 QuantityView 인스턴스가 없어도 동작
  - `IQuantitySettingsRepository`/`SaveQuantitySettingsUseCase`/`IFileDialogService`/`IProjectSettingsRepository`를 `ExecuteInternal` 진입 시 미리 resolve해 캡처(모델리스 창 특성상 지연 resolve 시 파기된 ServiceContainer 접근 문제 재발 방지, 기존 패턴 유지)
- [x] `QuantityCommand.cs` — `WireSettings`/`WireSettingsVm`/`WireSettings();` 호출 및 관련 using 제거. 산출/선택/수동수량만 담당하도록 축소
- [x] `QuantityRibbonModule.cs` — `quantitySettingsBtn`이 `RevitCommandType<QuantitySettingCommand>`를 가리키도록 라우팅 (버튼 ID/라벨은 유지)
- [x] `QuantityViewModel`/`QuantityView.xaml`의 설정 관련 멤버(`OpenSettingsCommand`, `SetSettingsAction`)는 이번 세션 이전에 이미 제거된 상태였음(확인만)
- [x] 회귀 테스트: `tests/DHBIMWATER.UI.Tests/ViewModels/Quantity/QuantityViewModelTests.cs` — `QuantityViewModel`이 `ExtractCommand`/`SelectInRevitCommand`만 노출하고 `OpenSettingsCommand` 프로퍼티는 없음을 리플렉션으로 검증
- 검증: `dotnet build src\DHBIMWATER.Revit\DHBIMWATER.Revit.csproj -c Release` 오류 0개(기존 MSB3277/MSB3270 경고만), `dotnet test tests\DHBIMWATER.UI.Tests\DHBIMWATER.UI.Tests.csproj` 17/17 통과
- 미결: Revit 실물에서 리본 "수량산출 설정" 버튼 단독 클릭 → 설정창 로드/저장/`.dhcfg` 가져오기·내보내기 동작 육안 확인 필요 (이번 세션은 빌드/유닛테스트만 수행)
- 커밋: 계획 문서의 Task별 `git commit` 단계는 미수행 — 사용자 명시 요청 시 진행

### 수량산출 설정창 철근비·할증률 입력창 하단 잘림 수정 (2026-07-14)
- [x] `QuantitySettingsView.xaml`의 철근비·할증률 DataGrid에만 `RowHeight="38"` 지정 — 32px 텍스트 박스와 셀 경계 사이 여유 확보.
- [x] 회귀 테스트 추가: `QuantitySettingsLayoutTests`가 두 탭의 입력 Grid 행 높이를 검증.
- 검증: `dotnet test tests\DHBIMWATER.UI.Tests\DHBIMWATER.UI.Tests.csproj -c Release --no-build --filter FullyQualifiedName~QuantitySettingsLayoutTests` 통과(1/1), `dotnet build src\DHBIMWATER.UI\DHBIMWATER.UI.csproj -c Release --no-restore` 오류 0개.
- 미결: Revit에서 두 탭을 열어 실제 표시를 육안 확인해야 함.

### 면적 공제 탭 체크박스 재산출 미반영 버그 수정 (2026-07-14)
- 배경: 사용자 보고 — `QuantitySettingsView` "면적공제" 탭 체크박스를 바꿔도 `QuantityView` 재산출 결과에 반영 안 됨.
  원인 조사 결과 [`docs/10_수량설정_소비지점연동_지시서.md`](docs/10_수량설정_소비지점연동_지시서.md) Task 2(면적 공제 매트릭스 + 오프닝 임계값)가 미착수 상태였음 확인.
  Task 1(거푸집 파라미터화)·Task 3(철근개략)은 이미 `CalculateQuantityUseCase`에 반영되어 있었음(기 완료 확인). Task 4(할증률 열)는 `LossRateCalculator`만 존재하고 UI/Excel 미연동 — 이번 범위 밖.
- [x] **매트릭스 연동**: `RevitIntersectingElementFinder` 생성자에 `IQuantitySettingsRepository` 주입, `DeductionSettings.CategoryMatrix`를 캐시해 `GetTargetCategories`가 하드코딩 switch 대신 매트릭스를 조회하도록 변경. 매트릭스에 없는 카테고리는 기존 `FallbackCategories` 유지, 특정 호스트의 체크박스를 전부 끄면 해당 호스트는 공제 없음으로 정확히 반영.
- [x] **오프닝 최소 체적 임계값**: 조사 결과 `CategoryMatrix`/`FindContactAreas`와 무관하고, `RevitFaceClassifier.ClassifyWall/ClassifyFloor`의 EdgeLoop 기반 `FaceType.OpeningSide` 판별부가 실제 지점임을 확인(사용자 확인 후 진행). 벽/슬래브 솔리드의 내부 루프(홀)마다 두께만큼 돌출시켜 근사 체적(m³)을 구하고, `OpeningMinVolumeM3` 미만이면 `FaceType.None`으로 분류해 `GetFaceAreas`에서 제외(거푸집 OpeningSide 면적에서만 제외, 콘크리트 체적/Left·Right·Top·Bottom 겉면적은 기존과 동일하게 Revit 지오메트리 그대로 사용 — 사용자가 선택한 범위).
  - 접촉면 공제 태깅용 `RevitFaceClassifier.Classify(elem, face)`(static, `RevitIntersectingElementFinder`에서 호출)는 임계값 미적용 오버로드로 분리해 기존 동작 100% 유지.
- 변경 파일: `Infrastructure/Repositories/Revit/Geometry/RevitIntersectingElementFinder.cs`, `Infrastructure/Repositories/Revit/Geometry/RevitFaceClassifier.cs`
- 검증: `dotnet build DHBIMWATER.sln -p:DebugType=none` 오류 0개(Revit 실행 중이라 Addins 폴더 복사 경고만 발생, 컴파일은 정상), `dotnet test tests\DHBIMWATER.UI.Tests` 18/18 통과.
- 미결: 두 클래스 모두 Revit API 지오메트리(Solid/Face)에 의존해 순수 유닛테스트 불가 — Revit 실물에서 (1) 면적공제 탭 체크박스 변경 → 재산출 반영, (2) 작은 오프닝 임계값 미만 시 거푸집 OpeningSide 면적 감소 확인 필요. Task 4(할증률 열)는 미착수로 남음.

### 밸브실 모델링 입력 목업 보완 (2026-07-16)
- [x] `docs/08_밸브실모델링입력목업_v3.html`에 4변 공통 기초 Toe(`Lt`) 입력 및 기초 외곽 치수 요약 추가
- [x] 이토밸브실: 중간벽체·중간슬래브 선택형 구성, 중간슬래브 적용 시 1F/2F 내부 높이 입력 추가
- [x] 제수밸브실: X/Y 방향 보 개수·외벽 내측~첫 보 중심 거리·보 중심 간격 입력 및 보 교차부 기둥 자동배치 안내 추가
- [x] 공기밸브실: 기초·4개 외벽·상부슬래브 단층 구성으로 표시; 하부 관통관 void는 추후 결정
- 검증: 추출 JavaScript `node --check` 통과
### 밸브실 WPF 입력 UI (2026-07-16)
- [x] `ValveRoomView` / `ValveRoomViewModel` 추가: 이토·제수·공기밸브실 유형별 입력 노출, 4변 공통 기초 Toe 및 외곽 치수 요약
- [x] 이토밸브실 선택형 중간벽체·중간슬래브 및 중간슬래브 적용 시 1F/2F 높이 입력 구현
- [x] 제수밸브실 X/Y 보 배치 입력, 보 교차부 기둥 자동배치 안내 및 Revit 프레임 타입 목록 연동 구현
- [x] DI 등록, `밸브실 모델링` 리본 버튼 및 `ValveRoomCommand` 추가
- [x] 목업의 밸브실 종류 옆 설명 제거
- [ ] TODO: `ValveRoomRequestDto` / UseCase / Revit 생성 Repo 연결 및 공기밸브실 하부 관통관 void 결정
- 검증: `dotnet build src\\DHBIMWATER.UI\\DHBIMWATER.UI.csproj --no-dependencies -p:DebugSymbols=false -p:DebugType=none` 성공 (오류 0, 기존 경고 80개).
- 검증: Costura 포함 Revit 애드인 재빌드·2026 배포 완료 — dotnet build src\\DHBIMWATER.Revit\\DHBIMWATER.Revit.csproj --no-dependencies -p:DebugSymbols=false -p:DebugType=none (오류 0, 기존 경고 5개).
### DHBoost 난독화 병합 DLL 갱신 (2026-07-16)
- [x] `F:\02_Work\04_Addin\DHBoost\src\DHBoost.Infrastructure\bin\Release\net8.0-windows\ilrepack\DHBoost.Combined.dll`을 `src\DHBIMWATER.Revit\libs\DHBoost\DHBoost.Combined.dll`로 복사
- [x] 원본·참조 DLL SHA-256 일치 확인, 기존 단일 `DHBoost.Combined` 참조 유지
- [x] `dotnet build src\DHBIMWATER.Revit\DHBIMWATER.Revit.csproj -c Release -p:DebugSymbols=false -p:DebugType=none` 성공 (오류 0, 기존 경고 116개)
### 밸브실 내부 배관 배치 — Phase 1 (2026-07-16)
- [x] Core `Piping` 그래프 추가: 노드 차수 기반 Cap·Inline·Elbow·Tee·Cross 분류, 모델 표고(mm) 예약 필드.
- [x] `PipeTopologyBuilder`/`Segment2D` 구현: 끝점 스냅, 교차·T접점·공선 겹침 분할, 근접 노드 병합 및 엣지 정규화.
- [x] `PipeLayoutView`/`PipeLayoutViewModel` 추가: 2회 클릭 직선 드로잉, 분기 색상 표시, 선택 엣지 부속품 순서형 +add/삭제/초기화.
- [x] `CanvasModelTransform`, `PipeLayoutCommand`, UI DI 및 Modeling 리본 `밸브실 배관` 버튼 추가.
- [x] `IPipeCommandRepo`와 순수 생성 정의만 추가 — Revit 모델 생성 구현은 제외.
- 변경 파일: `Core/Piping/*`, `Application/Interfaces/IPipeCommandRepo.cs`, `UI/Utilities/CanvasModelTransform.cs`, `UI/ViewModels/Modeling/PipeLayoutViewModel.cs`, `UI/Views/Modeling/PipeLayoutView.*`, `Revit/Commands/PipeLayoutCommand.cs`, UI DI·Modeling 리본.
- [ ] TODO (Phase 2): 사용자 표고 입력 UI, `IPipeCommandRepo` MEP Pipe/GenericModel 구현체, UseCase 트랜잭션 기반 Revit 생성·Connector 연결, 드래그 UX.
### 밸브실 내부 배관 배치 — Phase 2 (2026-07-16)
- [x] `PipeNetworkDefinition` 추가: 노드·엣지·인라인 부속, 작업 표고(mm), 관경(mm), 출력 모드를 불변 생성 요청으로 전달.
- [x] `CreateValvePipingUseCase` 추가: `ITransactionContext`에서만 Transaction을 열고, 선택한 `IPipeCommandRepo` 구현으로 모델 생성을 위임.
- [x] `RevitPipeMepCommandRepo` 구현: 첫 PipingSystemType/PipeType/Level을 기본 선택해 `Pipe.Create`로 생성하고, Inline·Elbow·Tee·Cross 노드를 Connector 기반 표준 부속으로 연결.
- [x] `RevitPipeGenericModelCommandRepo` 구현: MEP 타입이 없는 프로젝트에서 선택 가능한 GenericModel DirectShape 대체 출력 제공.
- [x] 배관 배치 UI에 출력 방식(MEP 기본), 표고, 관경(기본 Ø100 mm), `모델 생성` 버튼 추가. 명령이 UseCase 콜백을 연결.
- [x] Infrastructure DI에 두 `IPipeCommandRepo` 구현체, Application DI에 UseCase 등록.
- [ ] 제한/TODO: Cap은 Revit 2026에서 범용 `NewCapFitting` API가 없어 열린 Connector로 유지한다. 사용자별 PipeType·PipingSystemType·Level 선택 UI, 특정 인라인 부속 패밀리(밸브/플랜지 등) 배치, 수직 라이저 및 실제 Revit 모델 육안 검증은 후속 작업.
- 검증: `dotnet build src\\DHBIMWATER.Revit\\DHBIMWATER.Revit.csproj -c Release --no-restore -p:DebugSymbols=false -p:DebugType=none` 오류 0개. 실행 중 Revit/Visual Studio의 배포 DLL 잠금 및 기존 경고는 남음.
### 배관 Canvas 각도·선 스냅 및 가상선 (2026-07-16)
- [x] `직교 + 45° 각도 스냅` 토글 추가(기본 켜짐). 자유 모드로 해제 가능.
- [x] 첫 클릭 뒤 마우스 이동에 따라 주황 점선 가상선을 표시하고, 두 번째 클릭에서 세그먼트 확정.
- [x] `PipeTopologyBuilder.SnapPoint()` 추가: 노드와 기존 엣지 내부 투영점 모두 100mm 허용오차로 스냅. 선 중간 클릭은 T 접점 분할로 연결.
- [x] 선·노드 스냅은 각도 제약보다 우선하여, 기존 배관 접속점이 각도 보정으로 밀려나지 않도록 처리.
- [x] 엣지 분할의 파라미터 경계 판정을 0~1 정규화 값에 맞게 수정.
- 검증: `dotnet build src\\DHBIMWATER.Revit\\DHBIMWATER.Revit.csproj -c Release --no-restore -p:DebugSymbols=false -p:DebugType=none` 오류 0개(기존 경고만).
### 밸브실 배관 배치 — 현재 작업 정리 (2026-07-16)
- 구현 완료
  - Phase 1: 2D `PipeNetwork` 토폴로지, 교차·T 접점 분할, 노드 차수 분류(Cap/Inline/Elbow/Tee/Cross), 인라인 부속 순서형 목록, Canvas 렌더링.
  - Phase 2: 표고·관경·출력방식(MEP 기본/GenericModel 대체) 입력, `CreateValvePipingUseCase`의 Transaction 관리, MEP `Pipe.Create` 및 Elbow/Tee/Cross/Union Connector 연결, GenericModel DirectShape 대체 출력.
  - UX 보완: 기본 활성화된 직교+45° 각도 스냅 토글, 노드·기존 선 내부 100mm 스냅, 첫 클릭 이후 주황 점선 가상선, 선 중간 클릭의 T 접점 연결.
- 최신 빌드 상태
  - `dotnet build src\\DHBIMWATER.UI\\DHBIMWATER.UI.csproj -c Release --no-restore -p:DebugSymbols=false -p:DebugType=none` 오류 0개.
  - Revit 프로젝트는 이전 확인에서 오류 0개였으나, 실행 중 Revit/Visual Studio가 배포 DLL을 잠그면 복사 경고가 발생할 수 있음.
- 확인 필요 / 후속 TODO
  - Revit 실물에서 MEP 파이프 시스템·타입·레벨 자동 선택, 엘보·티·크로스 생성, DirectShape 대체 출력을 육안 검증.
  - PipeType·PipingSystemType·Level을 사용자가 선택하는 UI 추가.
  - Cap 패밀리 선택/배치, 밸브·플랜지 등 인라인 부속의 실제 MEP/패밀리 배치 및 Connector 연결.
  - 수직 라이저, 드래그 기반 부속 위치 편집, Canvas 팬/줌 및 그리드 표시.

### 밸브실 배관 Canvas 기준점·빈 화면 스냅 보완 (2026-07-16)
- [x] 빈 배관 네트워크에서 `SnapPoint()`가 null 좌표를 반환해 첫 점 지정 뒤 `DistanceTo(other)` 예외가 발생하던 문제 수정 — 스냅 대상이 없으면 원래 좌표를 반환.
- [x] Canvas 중앙에 연두색 점선 십자 기준점 표시 및 기준점 X/Y(mm) 입력 추가.
- [x] 빈 화면에서는 기준점(0,0)만 100mm 허용오차로 스냅; 기준점 입력 X/Y는 배관의 상대 좌표에 더해 Revit 실제 배치 좌표로 전달.
- [x] `PipeNetworkDefinition.ReferencePoint`를 추가하고 MEP Pipe/GenericModel 출력 모두에 기준점 오프셋 적용.
- 검증: `dotnet build src\\DHBIMWATER.Revit\\DHBIMWATER.Revit.csproj -c Release --no-restore -p:DebugSymbols=false -p:DebugType=none` 오류 0개 (기존 경고 89개).
