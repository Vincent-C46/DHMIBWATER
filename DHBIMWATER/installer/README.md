# DHBIMWATER Installer

이 폴더에는 DHBIMWATER Revit 2025 애드인의 설치 파일이 포함되어 있습니다.

## 파일 설명

- **DHBIMWATER-Setup.nsi**: NSIS 설치 스크립트
- **License.txt**: 소프트웨어 라이선스 계약서
- **README.md**: 이 문서

## 설치 파일 생성 방법

### 전제 조건
1. NSIS (Nullsoft Scriptable Install System) 설치 필요
   - 다운로드: https://nsis.sourceforge.io/Download
   - 권장 버전: NSIS 3.x 이상

### 빌드 과정

1. **프로젝트 빌드**
   ```bash
   dotnet build src/DHBIMWATER.Revit/DHBIMWATER.Revit.csproj -c Release
   ```

   빌드 후 이벤트가 자동으로 파일들을 `C:\BuildOutput\DHBIMWATER\`로 복사합니다.

2. **설치 파일 생성**
   - 방법 1: NSIS GUI 사용
     - NSIS를 설치하고 `DHBIMWATER-Setup.nsi` 파일을 우클릭
     - "Compile NSIS Script" 선택

   - 방법 2: 명령줄 사용
     ```bash
     "C:\Program Files (x86)\NSIS\makensis.exe" installer\DHBIMWATER-Setup.nsi
     ```

3. **설치 파일 위치**
   - 생성된 `DHBIMWATER-Setup.exe` 파일은 `installer` 폴더에 생성됩니다.

## 설치 내용

설치 프로그램은 다음 작업을 수행합니다:

1. **DLL 파일 복사**
   - 대상: `%APPDATA%\Autodesk\Revit\Addins\2025\DHBIMWATER\`
   - 파일: 모든 필요한 DLL 파일들

2. **.addin 파일 복사**
   - 대상: `C:\ProgramData\Autodesk\Revit\Addins\2025\`
   - 파일: `DHBIMWATER.addin`

3. **레지스트리 등록**
   - 제어판의 "프로그램 추가/제거"에 등록
   - 언인스톨러 정보 저장

## 제거 방법

- 제어판 > 프로그램 추가/제거에서 "DHBIMWATER for Revit 2025" 선택 후 제거
- 또는 설치 폴더의 `Uninstall.exe` 실행

## 버전 관리

설치 파일의 버전을 변경하려면 `DHBIMWATER-Setup.nsi` 파일에서 다음 부분을 수정하세요:

```nsis
VIProductVersion "1.0.0.0"
```

## 문제 해결

### 설치 파일이 생성되지 않는 경우
- NSIS가 올바르게 설치되었는지 확인
- `C:\BuildOutput\DHBIMWATER\` 폴더에 필요한 파일들이 있는지 확인

### Revit에서 애드인이 로드되지 않는 경우
- Revit을 완전히 종료하고 다시 시작
- Revit 저널 파일 확인: `%LOCALAPPDATA%\Autodesk\Revit\Autodesk Revit 2025\Journals\`

## 지원

문의사항이 있으시면 support@dhcorp.com으로 연락주세요.
