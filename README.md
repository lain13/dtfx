# DTFX

**Data Transfer Framework for XML**

DTFX는 XML에 정의한 작업을 순서대로 실행하여 SQL Server, Oracle, PostgreSQL 사이의 데이터 연계와 파일 처리를 자동화하는 Windows 콘솔 ETL 엔진입니다. 기존 프로젝트명은 R7001이었으며, 공개 프로젝트 이름을 DTFX로 변경했습니다.

## 주요 기능

- SQL Server, Oracle, PostgreSQL의 Select/Insert/Update/Delete
- DB 간 Bulk Insert
- CSV 및 GZIP 읽기·쓰기
- 로컬 임시 테이블
- `If`, `ForEach`, `AppExit` 제어 흐름
- 외부 명령 실행, 로그 기록, ZIP 생성
- `${variable}` 형식의 공유 변수 치환

## 요구 사항

- Windows
- .NET Framework 4.6.2 Developer/Targeting Pack
- Visual Studio 또는 Build Tools for Visual Studio의 MSBuild
- NuGet CLI

실제 DB 작업에는 해당 데이터베이스와 접속 정보가 필요합니다. PostgreSQL과 Oracle 드라이버를 포함한 의존성은 `packages.config`로 관리합니다.

## 빌드

```bat
NuGet.exe restore IF.Batch.sln
MSBuild.exe IF.Batch.sln /p:Configuration=Release /p:Platform="Any CPU"
```

의존성이 이미 복원된 환경에서는 다음 명령도 사용할 수 있습니다.

```bat
dotnet build IF.Batch.sln --no-restore -c Release
```

빌드 결과는 `DTFX\bin\Release\IF.Batch.DTFX.exe`에 생성됩니다.

## 1분 Quick Start

Quick Start는 DB 연결 없이 표현식, 로그, 정상 종료 흐름을 확인합니다.

```bat
examples\quickstart\QUICKSTART.BAT
```

또는 직접 실행할 수 있습니다.

```bat
DTFX\bin\Release\IF.Batch.DTFX.exe -appid QUICKSTART -appdirectory examples\quickstart
```

일반적인 실행 형식은 다음과 같습니다.

```bat
IF.Batch.DTFX.exe -appid <APPID> [-appdirectory <DIRECTORY>]
```

- `appid`: 확장자를 제외한 XML 정의 파일명
- `appdirectory`: XML과 선택적 `{appid}.config`가 있는 디렉터리. 생략하면 실행 파일 디렉터리를 사용합니다.
- 종료 코드: `0` 성공, `1` 오류, `2` 경고

## XML 예제

```xml
<?xml version="1.0" encoding="utf-8"?>
<Application xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
             xsi:noNamespaceSchemaLocation="../../DTFX/XMLSchema/Application.xsd">
  <TraceLog eventType="information">Job started.</TraceLog>
  <SqlSelect dataSource="source" toFile="users.csv">
    SELECT * FROM USERS
  </SqlSelect>
  <AppExit result="0">Job completed.</AppExit>
</Application>
```

전체 요소 예시는 [`SAMPLE_APP.XML`](DTFX/01_%E3%83%90%E3%83%83%E3%83%81%E4%BD%9C%E6%88%90%E3%82%B5%E3%83%B3%E3%83%97%E3%83%AB/SAMPLE_APP.XML), 스키마는 [`Application.xsd`](DTFX/XMLSchema/Application.xsd)를 참고하세요. 전체 샘플의 DB 연결 문자열과 SQL은 실행 환경에 맞게 변경해야 합니다.

> XML 정의는 실행 권한을 가진 신뢰할 수 있는 사용자만 작성해야 합니다. `ExecuteCommand`는 Windows 명령을 실행하며 SQL 요소는 지정된 데이터베이스 권한으로 SQL을 수행합니다.

## 테스트

```bat
tests\DTFX.SmokeTests\bin\Release\DTFX.SmokeTests.exe
```

스모크 테스트는 결과 코드 우선순위, 명령행 인수 파싱, XSD 컴파일, Quick Start 및 전체 샘플의 스키마 정합성을 검사합니다.

## 프로젝트 구조

```text
Common/                  공용 로깅, CSV, 설정, 파일 유틸리티
DTFX/                    ETL 엔진과 XML 요소/Executor
docs/                    설계 문서
examples/quickstart/     DB 없는 실행 예제
tests/DTFX.SmokeTests/   외부 테스트 패키지가 필요 없는 스모크 테스트
```

설계는 [`docs/architecture.md`](docs/architecture.md), 기여 방법은 [`CONTRIBUTING.md`](CONTRIBUTING.md), 보안 정책은 [`SECURITY.md`](SECURITY.md), 변경 내용은 [`CHANGELOG.md`](CHANGELOG.md)를 참고하세요.

## 라이선스

DTFX는 [MIT License](LICENSE)로 공개됩니다.
